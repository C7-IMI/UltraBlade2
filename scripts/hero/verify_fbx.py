#!/usr/bin/env python3
"""Independently verify an exported hero LOD FBX by re-importing it into a fresh scene.

Usage:
    blender --background --factory-startup --python scripts/hero/verify_fbx.py -- ^
        --input <fbx> [--expect-bones 23] [--expect-actions idle,run,reach]

Marker (only when every expectation holds):

    LOD_FBX_VERIFY_OK file=<absolute path> bones=<n> actions=<names> meshes=<n>
"""

import argparse
import json
import os
import re
import sys

import bpy

MARKER_OK = "LOD_FBX_VERIFY_OK"
MARKER_FAIL = "LOD_FBX_VERIFY_FAIL"
PREFIX = "[verify_fbx]"


def log(msg):
    print("%s %s" % (PREFIX, msg), flush=True)


def fail(msg):
    sys.stderr.write("%s %s\n" % (MARKER_FAIL, msg))
    sys.stderr.flush()
    sys.exit(1)


def parse_args(argv):
    if "--" in argv:
        argv = argv[argv.index("--") + 1:]
    else:
        argv = []
    parser = argparse.ArgumentParser(prog="verify_fbx.py")
    parser.add_argument("--input", required=True)
    parser.add_argument("--expect-bones", type=int, default=23)
    parser.add_argument("--expect-actions", default="idle,run,reach")
    return parser.parse_args(argv)


def abspath(p):
    return os.path.normpath(os.path.abspath(os.path.expanduser(p)))


def clear_scene():
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


def call_op_with_fallback(op, kwargs, label):
    kw = dict(kwargs)
    for _ in range(60):
        try:
            return op(**kw)
        except TypeError as exc:
            match = (re.search(r'unexpected keyword argument "([^"]+)"', str(exc))
                     or re.search(r'"([A-Za-z0-9_]+)" is an invalid keyword', str(exc)))
            if not match:
                raise
            bad = match.group(1)
            if bad not in kw or bad == "filepath":
                raise
            log("%s: dropping unsupported keyword %r" % (label, bad))
            kw.pop(bad)
    raise RuntimeError("%s: could not satisfy operator signature" % label)


def import_fbx(path):
    return call_op_with_fallback(
        bpy.ops.import_scene.fbx,
        {"filepath": path, "use_anim": True, "ignore_leaf_bones": False, "use_custom_normals": True},
        "import",
    )


def short_name(name):
    """FBX take names come back as '<armature>|<object>|<action>'; the action is the last part."""
    return name.rsplit("|", 1)[-1].strip()


def curve_count(action):
    """Count F-Curves on either legacy or Blender 5 slotted (layered) actions."""
    try:
        return len(action.fcurves)
    except Exception:
        pass
    total = 0
    for layer in getattr(action, "layers", []):
        for strip in getattr(layer, "strips", []):
            for channelbag in getattr(strip, "channelbags", []):
                total += len(channelbag.fcurves)
    return total


def main():
    args = parse_args(list(sys.argv))
    src = abspath(args.input)
    expect_actions = [a.strip() for a in args.expect_actions.split(",") if a.strip()]

    log("blender %s" % bpy.app.version_string)
    if not os.path.isfile(src):
        fail("input not found: %s" % src)

    clear_scene()
    import_fbx(src)

    armatures = [o for o in bpy.data.objects if o.type == "ARMATURE"]
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]

    per_armature = []
    bones = 0
    for arm in armatures:
        count = len(arm.data.bones)
        bones += count
        per_armature.append("%s:%d" % (arm.name, count))
        log("armature %r bones=%d :: %s" % (
            arm.name, count, ", ".join(sorted(b.name for b in arm.data.bones))))

    actions = sorted(a.name for a in bpy.data.actions)
    short_actions = sorted(short_name(a.name) for a in bpy.data.actions)
    for action in bpy.data.actions:
        try:
            start, end = action.frame_range
        except Exception:
            start, end = 0.0, 0.0
        log("action %r (short=%r) frames=%.1f..%.1f fcurves=%d" % (
            action.name, short_name(action.name), start, end, curve_count(action)))

    skinned = []
    mesh_facts = []
    for mesh in meshes:
        groups = len(mesh.vertex_groups)
        arm_mods = [md for md in mesh.modifiers if md.type == "ARMATURE"]
        arm_parent = mesh.parent.type == "ARMATURE" if mesh.parent else False
        is_skinned = bool(arm_mods) or (groups > 0 and arm_parent)
        if is_skinned:
            skinned.append(mesh.name)
        mesh_facts.append({
            "name": mesh.name,
            "vertices": len(mesh.data.vertices),
            "triangles": len(mesh.data.loop_triangles),
            "vertex_groups": groups,
            "armature_modifiers": len(arm_mods),
            "skinned": is_skinned,
        })
        mesh.data.calc_loop_triangles()
        log("mesh %r verts=%d polys=%d tris=%d vgroups=%d armature_mods=%d skinned=%s parent=%r dims=(%.3f,%.3f,%.3f) materials=%s" % (
            mesh.name, len(mesh.data.vertices), len(mesh.data.polygons),
            len(mesh.data.loop_triangles), groups, len(arm_mods), is_skinned,
            mesh.parent.name if mesh.parent else None,
            mesh.dimensions[0], mesh.dimensions[1], mesh.dimensions[2],
            ",".join(s.material.name if s.material else "<none>" for s in mesh.material_slots)))

    triangles = sum(len(m.data.loop_triangles) for m in meshes)
    names = ",".join(short_actions) if short_actions else "<none>"
    log("raw action names: %s" % (", ".join(actions) if actions else "<none>"))
    log("summary: bones=%d actions=%d skinned_meshes=%d tris=%d" % (
        bones, len(actions), len(skinned), triangles))

    problems = []
    if bones != args.expect_bones:
        problems.append("bones=%d expected %d" % (bones, args.expect_bones))
    lowered = {a.lower() for a in short_actions}
    missing = [a for a in expect_actions if a.lower() not in lowered]
    if missing:
        problems.append("missing actions %s (found %s)" % (",".join(missing), names or "<none>"))
    if not skinned:
        problems.append("no skinned mesh found")
    facts = {
        "file": src,
        "bones": bones,
        "armatures": per_armature,
        "actions_raw": actions,
        "actions": short_actions,
        "meshes": mesh_facts,
        "skinned_meshes": sorted(skinned),
        "triangles_total": triangles,
        "problems": problems,
        "passed": not problems,
    }
    print("LOD_FBX_VERIFY_DETAIL " + json.dumps(facts, sort_keys=True), flush=True)

    if problems:
        fail("file=%s bones=%d actions=%s meshes=%d :: %s" % (
            src, bones, names, len(skinned), "; ".join(problems)))

    print("%s file=%s bones=%d actions=%s meshes=%d" % (
        MARKER_OK, src, bones, names, len(skinned)), flush=True)
    sys.exit(0)


if __name__ == "__main__":
    main()
