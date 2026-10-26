#!/usr/bin/env python3
"""Convert a glTF-binary (.glb) hero LOD into FBX, headless, with Blender 5.2.

Usage:
    blender --background --factory-startup --python scripts/hero/glb_to_fbx.py -- ^
        --input <glb> --output <fbx>

The script clears the scene, imports the GLB, keeps the armature/skinning intact and
exports every animation action (idle / run / reach) as its own FBX take.  Diagnostics
are printed with a '[glb_to_fbx]' prefix; on success the last stdout line is exactly:

    LOD_FBX_OK <absolute output path>

On failure it prints LOD_FBX_FAIL <reason> to stderr and exits 1.
"""

import argparse
import os
import re
import sys

import bpy

MARKER_OK = "LOD_FBX_OK"
MARKER_FAIL = "LOD_FBX_FAIL"
PREFIX = "[glb_to_fbx]"


def log(msg):
    print("%s %s" % (PREFIX, msg), flush=True)


def die(msg):
    sys.stderr.write("%s %s\n" % (MARKER_FAIL, msg))
    sys.stderr.flush()
    sys.exit(1)


def parse_args(argv):
    """Take only what follows the '--' separator (Blender owns everything before it)."""
    if "--" in argv:
        argv = argv[argv.index("--") + 1:]
    else:
        argv = []
    parser = argparse.ArgumentParser(prog="glb_to_fbx.py", add_help=True)
    parser.add_argument("--input", required=True, help="source .glb path")
    parser.add_argument("--output", required=True, help="destination .fbx path")
    parser.add_argument(
        "--no-embed-textures",
        action="store_true",
        help="do not embed images in the FBX (default: embed)",
    )
    return parser.parse_args(argv)


def abspath(p):
    return os.path.normpath(os.path.abspath(os.path.expanduser(p)))


def clear_scene():
    """Remove every object and stale datablock so nothing from the startup file leaks in."""
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for collection in list(bpy.data.collections):
        try:
            bpy.data.collections.remove(collection)
        except Exception:
            pass
    for datablocks in (
        bpy.data.meshes,
        bpy.data.armatures,
        bpy.data.actions,
        bpy.data.materials,
        bpy.data.images,
        bpy.data.curves,
        bpy.data.cameras,
        bpy.data.lights,
        bpy.data.node_groups,
    ):
        for block in list(datablocks):
            try:
                datablocks.remove(block)
            except Exception:
                pass
    try:
        bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
    except Exception:
        pass
    log("scene cleared: objects=%d actions=%d armatures=%d" % (
        len(bpy.data.objects), len(bpy.data.actions), len(bpy.data.armatures)))


def call_op_with_fallback(op, kwargs, label):
    """Call an operator, dropping keyword arguments this Blender build does not know."""
    kw = dict(kwargs)
    for _ in range(60):
        try:
            return op(**kw), kw
        except TypeError as exc:
            match = (re.search(r'unexpected keyword argument "([^"]+)"', str(exc))
                     or re.search(r'"([A-Za-z0-9_]+)" is an invalid keyword', str(exc)))
            if not match:
                raise
            bad = match.group(1)
            if bad not in kw or bad == "filepath":
                raise
            log("%s: %s does not support keyword %r, retrying without it" % (label, op.idname(), bad))
            kw.pop(bad)
    raise RuntimeError("%s: could not satisfy operator signature" % label)


def import_glb(path):
    kwargs = {
        "filepath": path,
        "import_pack_images": True,
        "merge_vertices": False,
        "import_shading": "NORMALS",
        "guess_original_bind_pose": True,
        "bone_heuristic": "BLENDER",
    }
    before = set(bpy.data.objects.keys())
    _result, used = call_op_with_fallback(bpy.ops.import_scene.gltf, kwargs, "import")
    imported = [o for o in bpy.data.objects if o.name not in before]
    log("imported %d objects from %s (kwargs accepted: %s)" % (
        len(imported), os.path.basename(path), ",".join(sorted(used))))
    return imported


HELPER_COLLECTION = "glTF_not_exported"


def strip_import_helpers():
    """Drop the importer's bone-display helpers (Icosphere in 'glTF_not_exported').

    Blender's glTF importer builds an icosphere and assigns it as the custom shape of
    every pose bone.  It is viewport furniture, not asset content, and the FBX exporter
    would happily ship it to Unity as a stray unskinned mesh.
    """
    helpers = {
        obj.name
        for obj in bpy.data.objects
        if any(coll.name == HELPER_COLLECTION for coll in obj.users_collection)
    }
    if not helpers:
        return sorted(helpers)
    for armature in bpy.data.objects:
        if armature.type != "ARMATURE" or armature.pose is None:
            continue
        for pose_bone in armature.pose.bones:
            shape = pose_bone.custom_shape
            if shape is not None and shape.name in helpers:
                pose_bone.custom_shape = None
    for name in helpers:
        obj = bpy.data.objects.get(name)
        if obj is not None:
            bpy.data.objects.remove(obj, do_unlink=True)
    for coll in list(bpy.data.collections):
        if coll.name == HELPER_COLLECTION and not coll.objects:
            bpy.data.collections.remove(coll)
    try:
        bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
    except Exception:
        pass
    return sorted(helpers)


def stash_actions_on_armature(armature):
    """Give every action a fake user and an NLA track so the exporter cannot miss it."""
    if armature.animation_data is None:
        armature.animation_data_create()
    anim = armature.animation_data
    already = set()
    for track in anim.nla_tracks:
        for strip in track.strips:
            if strip.action is not None:
                already.add(strip.action.name)

    # Blender 5.x removed Action.id_root (slotted actions); after clear_scene() every
    # remaining action came from the import, so take them all.
    actions = list(bpy.data.actions)
    for action in actions:
        action.use_fake_user = True
        if action.name in already:
            continue
        try:
            track = anim.nla_tracks.new()
            track.name = action.name
            start = int(round(action.frame_range[0]))
            strip = track.strips.new(action.name, start, action)
            strip.name = action.name
            track.mute = True
            log("stashed action %r on NLA track %r (frames %d..%d)" % (
                action.name, track.name, int(action.frame_range[0]), int(action.frame_range[1])))
        except Exception as exc:
            log("WARN could not stash action %r: %s" % (action.name, exc))

    if anim.action is None and actions:
        anim.action = actions[0]
    log("actions available: %s" % ", ".join(sorted(a.name for a in bpy.data.actions)))


def scene_summary():
    armatures = [o for o in bpy.data.objects if o.type == "ARMATURE"]
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    bones = sum(len(a.data.bones) for a in armatures)
    skinned = [m for m in meshes if any(md.type == "ARMATURE" for md in m.modifiers)]
    return {
        "armatures": [a.name for a in armatures],
        "bones": bones,
        "meshes": len(meshes),
        "skinned_meshes": len(skinned),
        "actions": sorted(a.name for a in bpy.data.actions),
        "triangles": sum(len(m.data.loop_triangles) for m in meshes),
    }


def export_fbx(path, embed_textures):
    kwargs = {
        "filepath": path,
        "use_selection": False,
        "use_visible": False,
        "use_active_collection": False,
        "object_types": {"ARMATURE", "MESH", "EMPTY"},
        "use_mesh_modifiers": True,
        "mesh_smooth_type": "FACE",
        "use_tspace": False,
        "use_custom_props": False,
        "add_leaf_bones": False,
        "primary_bone_axis": "Y",
        "secondary_bone_axis": "X",
        "use_armature_deform_only": False,
        "armature_nodetype": "NULL",
        "bake_anim": True,
        "bake_anim_use_all_bones": True,
        "bake_anim_use_nla_strips": False,
        "bake_anim_use_all_actions": True,
        "bake_anim_force_startend_keying": True,
        "bake_anim_step": 1.0,
        "bake_anim_simplify_factor": 0.0,
        "bake_space_transform": False,
        "apply_scale_options": "FBX_SCALE_NONE",
        "apply_unit_scale": True,
        "global_scale": 1.0,
        "axis_forward": "-Z",
        "axis_up": "Y",
        "path_mode": "AUTO",
        "embed_textures": bool(embed_textures),
    }
    _result, used = call_op_with_fallback(bpy.ops.export_scene.fbx, kwargs, "export")
    return used


def main():
    args = parse_args(list(sys.argv))
    src = abspath(args.input)
    dst = abspath(args.output)

    log("blender %s" % bpy.app.version_string)
    if not os.path.isfile(src):
        die("input not found: %s" % src)
    out_dir = os.path.dirname(dst)
    if out_dir and not os.path.isdir(out_dir):
        os.makedirs(out_dir, exist_ok=True)

    clear_scene()
    import_glb(src)
    helpers = strip_import_helpers()
    if helpers:
        log("stripped importer helper objects (bone display shapes): %s" % ", ".join(helpers))

    armatures = [o for o in bpy.data.objects if o.type == "ARMATURE"]
    if not armatures:
        die("no armature imported from %s" % src)
    for armature in armatures:
        stash_actions_on_armature(armature)

    for obj in bpy.context.scene.objects:
        obj.hide_set(False)
        obj.hide_viewport = False
        obj.hide_render = False

    summary_before = scene_summary()
    log("pre-export: bones=%d actions=%s meshes=%d skinned=%d triangles=%d" % (
        summary_before["bones"], ",".join(summary_before["actions"]),
        summary_before["meshes"], summary_before["skinned_meshes"],
        summary_before["triangles"]))
    for mesh in [o for o in bpy.data.objects if o.type == "MESH"]:
        mesh.data.calc_loop_triangles()
        log("pre-export mesh %r tris=%d vgroups=%d armature_mod=%s materials=%s" % (
            mesh.name, len(mesh.data.loop_triangles), len(mesh.vertex_groups),
            any(md.type == "ARMATURE" for md in mesh.modifiers),
            ",".join(s.material.name if s.material else "<none>" for s in mesh.material_slots)))

    if summary_before["bones"] <= 0:
        die("imported armature has zero bones")
    if not summary_before["actions"]:
        die("imported file has no animation actions")
    if summary_before["skinned_meshes"] < 1:
        die("imported file has no skinned mesh")

    used = export_fbx(dst, embed_textures=not args.no_embed_textures)
    if not os.path.isfile(dst):
        die("exporter reported success but %s does not exist" % dst)
    size = os.path.getsize(dst)
    if size <= 0:
        die("exported file is empty: %s" % dst)
    log("wrote %s (%d bytes); export kwargs: %s" % (dst, size, ",".join(sorted(used))))
    print("%s %s" % (MARKER_OK, dst), flush=True)
    sys.exit(0)


if __name__ == "__main__":
    main()
