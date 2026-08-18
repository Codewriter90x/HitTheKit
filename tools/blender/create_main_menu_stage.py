#!/usr/bin/env python3
"""Generate the original HitTheKit main-menu stage diorama.

Run through scripts/build-main-menu-stage-assets.sh. The scene intentionally
uses only procedural geometry and Blender-provided primitives: there are no
third-party meshes, textures, fonts, or copyrighted reference assets.
"""

from __future__ import annotations

import math
import os
import random
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def argument_after_separator(index: int) -> Path:
    if "--" not in sys.argv:
        raise RuntimeError("Expected output paths after '--'.")
    arguments = sys.argv[sys.argv.index("--") + 1 :]
    if len(arguments) <= index:
        raise RuntimeError("Both .blend and .fbx output paths are required.")
    return Path(arguments[index]).resolve()


BLEND_OUTPUT = argument_after_separator(0)
FBX_OUTPUT = argument_after_separator(1)
RANDOM = random.Random(1847)


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def material(name: str, color: tuple[float, float, float, float], metallic: float, roughness: float):
    value = bpy.data.materials.new(name)
    value.diffuse_color = color
    value.use_nodes = True
    shader = value.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = color
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    return value


def assign(obj, value) -> None:
    if obj.data is not None and hasattr(obj.data, "materials"):
        obj.data.materials.append(value)


def smooth(obj) -> None:
    if obj.type != "MESH":
        return
    for polygon in obj.data.polygons:
        polygon.use_smooth = True


def cube(name: str, location, scale, mat, bevel: float = 0.0):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0:
        modifier = obj.modifiers.new("EdgeSoftness", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
    assign(obj, mat)
    return obj


def cylinder(name: str, location, radius: float, depth: float, mat, vertices: int = 32, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    assign(obj, mat)
    smooth(obj)
    return obj


def sphere(name: str, location, scale, mat, segments: int = 20, rings: int = 12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    assign(obj, mat)
    smooth(obj)
    return obj


def torus(name: str, location, major_radius: float, minor_radius: float, mat, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major_radius,
        minor_radius=minor_radius,
        major_segments=40,
        minor_segments=8,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    assign(obj, mat)
    smooth(obj)
    return obj


def beam(name: str, start, end, radius: float, mat, vertices: int = 12):
    a = Vector(start)
    b = Vector(end)
    direction = b - a
    obj = cylinder(name, (a + b) * 0.5, radius, direction.length, mat, vertices)
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    return obj


def empty(name: str, location=(0, 0, 0), parent=None):
    obj = bpy.data.objects.new(name, None)
    obj.empty_display_type = "PLAIN_AXES"
    obj.empty_display_size = 0.3
    obj.location = location
    obj.parent = parent
    bpy.context.collection.objects.link(obj)
    return obj


def parent_all(parent, objects) -> None:
    for obj in objects:
        # Assigning a parent in Blender changes the interpretation of an
        # object's transform. Preserve the authored world matrix explicitly;
        # otherwise non-zero component roots (kick, cymbals, hi-hat) offset
        # their children twice and the assembled kit becomes mechanically
        # impossible.
        world_matrix = obj.matrix_world.copy()
        obj.parent = parent
        obj.matrix_world = world_matrix


def cone(name: str, location, radius_a: float, radius_b: float, depth: float, mat, vertices: int = 48, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices,
        radius1=radius_a,
        radius2=radius_b,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    assign(obj, mat)
    smooth(obj)
    return obj


def add_bevel(obj, width: float, segments: int = 3) -> None:
    modifier = obj.modifiers.new("PrecisionBevel", "BEVEL")
    modifier.width = width
    modifier.segments = segments


def create_cymbal_surface(name: str, location, radius: float, mat, tilt=(0, 0, 0)):
    """Create a lathed cymbal with a bell, bow, edge and physical thickness."""
    angular_segments = 96
    radial_profile = (
        (0.035, 0.120),
        (0.120, 0.168),
        (0.235, 0.095),
        (0.420, 0.048),
        (0.660, 0.018),
        (0.850, -0.010),
        (1.000, -0.025),
    )
    thickness = max(radius * 0.012, 0.010)
    vertices = []
    faces = []
    ring_count = len(radial_profile)
    for side in (1.0, -1.0):
        for radial_fraction, profile_z in radial_profile:
            ring_radius = radius * radial_fraction
            for segment in range(angular_segments):
                angle = math.tau * segment / angular_segments
                vertices.append(
                    (
                        math.cos(angle) * ring_radius,
                        math.sin(angle) * ring_radius,
                        profile_z + side * thickness * 0.5,
                    )
                )
    side_stride = ring_count * angular_segments
    for side_index in range(2):
        side_offset = side_index * side_stride
        reverse = side_index == 1
        for ring_index in range(ring_count - 1):
            first = side_offset + ring_index * angular_segments
            second = first + angular_segments
            for segment in range(angular_segments):
                following = (segment + 1) % angular_segments
                quad = (first + segment, first + following, second + following, second + segment)
                faces.append(tuple(reversed(quad)) if reverse else quad)
    for ring_index in (0, ring_count - 1):
        top = ring_index * angular_segments
        bottom = side_stride + ring_index * angular_segments
        for segment in range(angular_segments):
            following = (segment + 1) % angular_segments
            faces.append((top + segment, bottom + segment, bottom + following, top + following))
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    obj.location = location
    obj.rotation_euler = tilt
    bpy.context.collection.objects.link(obj)
    assign(obj, mat)
    smooth(obj)
    bevel = obj.modifiers.new("RolledCymbalEdge", "BEVEL")
    bevel.width = 0.006
    bevel.segments = 2
    return obj


def create_tripod_stand(name: str, base_location, top_location, chrome_mat, boom_end=None):
    """Create telescopic chrome hardware with collars, braces and rubber feet."""
    x, y, _ = base_location
    top = Vector(top_location)
    parts = [
        beam(f"Chrome_{name}_LowerTube", (x, y, 0.10), (x, y, max(0.58, top.z * 0.48)), 0.030, chrome_mat, 16),
        beam(f"Chrome_{name}_UpperTube", (x, y, max(0.52, top.z * 0.43)), (x, y, top.z), 0.021, chrome_mat, 16),
    ]
    for collar_index, z in enumerate((max(0.55, top.z * 0.45), max(0.82, top.z * 0.70))):
        parts.append(cylinder(f"Chrome_{name}_Collar_{collar_index}", (x, y, z), 0.055, 0.045, chrome_mat, 24))
    tripod_center = (x, y, 0.18)
    for leg_index, angle in enumerate((math.radians(-90), math.radians(30), math.radians(150))):
        foot = (x + math.cos(angle) * 0.56, y + math.sin(angle) * 0.56, 0.025)
        parts.append(beam(f"Chrome_{name}_Tripod_{leg_index}", tripod_center, foot, 0.020, chrome_mat, 12))
        rubber = cylinder(f"Hardware_{name}_Foot_{leg_index}", foot, 0.034, 0.10, chrome_mat, 16, (0, math.radians(90), angle))
        parts.append(rubber)
    if boom_end is not None:
        boom_start = (x, y, top.z - 0.03)
        parts.append(beam(f"Chrome_{name}_Boom", boom_start, boom_end, 0.020, chrome_mat, 16))
        joint = sphere(f"Chrome_{name}_BoomJoint", boom_start, (0.075, 0.075, 0.075), chrome_mat, 20, 12)
        parts.append(joint)
    return parts


def create_drum_shell(name: str, location, radius: float, height: float, shell_mat, head_mat, chrome_mat, lug_count: int = 10):
    """Build a production-style drum with separate shell, heads and hardware."""
    x0, y0, z0 = location
    parts = []
    shell = cylinder(f"DrumShell_{name}", location, radius, height, shell_mat, 96)
    add_bevel(shell, 0.018, 3)
    parts.append(shell)
    # Slightly recessed inner bearing surface makes the shell read as a
    # physical object instead of a capped primitive.
    inner = cylinder(f"DrumInterior_{name}", location, radius * 0.925, height + 0.012, head_mat, 96)
    parts.append(inner)
    top_z = z0 + height * 0.5
    bottom_z = z0 - height * 0.5
    for suffix, z, sign in (("Top", top_z, 1), ("Bottom", bottom_z, -1)):
        head = cylinder(f"DrumHead_{name}_{suffix}", (x0, y0, z + sign * 0.014), radius * 0.945, 0.026, head_mat, 96)
        hoop = torus(f"Chrome_{name}_{suffix}Hoop", (x0, y0, z), radius * 1.012, 0.032, chrome_mat)
        bearing = torus(f"Chrome_{name}_{suffix}Bearing", (x0, y0, z + sign * 0.025), radius * 0.945, 0.012, chrome_mat)
        parts.extend((head, hoop, bearing))
    for index in range(lug_count):
        angle = math.tau * index / lug_count
        radial = Vector((math.cos(angle), math.sin(angle), 0))
        lug_center = Vector(location) + radial * (radius * 1.015)
        lug = cube(
            f"Chrome_{name}_Lug_{index:02d}",
            lug_center,
            (0.045, 0.032, max(0.070, height * 0.17)),
            chrome_mat,
            0.018,
        )
        lug.rotation_euler.z = angle
        tension_top = (lug_center.x, lug_center.y, top_z - 0.025)
        tension_bottom = (lug_center.x, lug_center.y, bottom_z + 0.025)
        tension_rod = beam(f"Chrome_{name}_TensionRod_{index:02d}", tension_bottom, tension_top, 0.010, chrome_mat, 10)
        parts.extend((lug, tension_rod))
    badge = cube(f"Chrome_{name}_Badge", (x0, y0 - radius * 1.02, z0), (0.105, 0.018, 0.055), chrome_mat, 0.018)
    parts.append(badge)
    return parts


def create_kick(shell_mat, head_mat, chrome_mat, accent_mat):
    root = empty("Drum_Kick_ROOT", (0, -1.05, 0.9))
    rotation = (math.radians(90), 0, 0)
    shell = cylinder("DrumShell_Kick", root.location, 0.96, 1.14, shell_mat, 128, rotation)
    add_bevel(shell, 0.025, 4)
    front_y, rear_y = -1.635, -0.465
    front = cylinder("DrumHead_Kick_Front", (0, front_y, 0.9), 0.91, 0.035, head_mat, 128, rotation)
    rear = cylinder("DrumHead_Kick_Rear", (0, rear_y, 0.9), 0.91, 0.035, head_mat, 128, rotation)
    front_hoop = torus("Chrome_Kick_FrontHoop", (0, front_y - 0.025, 0.9), 0.96, 0.045, chrome_mat, rotation)
    rear_hoop = torus("Chrome_Kick_RearHoop", (0, rear_y + 0.025, 0.9), 0.96, 0.045, chrome_mat, rotation)
    front_bearing = torus("Chrome_Kick_FrontBearing", (0, front_y - 0.040, 0.9), 0.91, 0.014, chrome_mat, rotation)
    rear_bearing = torus("Chrome_Kick_RearBearing", (0, rear_y + 0.040, 0.9), 0.91, 0.014, chrome_mat, rotation)
    logo_outer = torus("Accent_KickLogoRing", (0, front_y - 0.048, 0.9), 0.43, 0.018, accent_mat, rotation)
    parts = [shell, front, rear, front_hoop, rear_hoop, front_bearing, rear_bearing, logo_outer]
    for index in range(12):
        angle = math.tau * index / 12
        x = math.cos(angle) * 0.965
        z = 0.9 + math.sin(angle) * 0.965
        front_lug = cube(f"Chrome_Kick_FrontLug_{index:02d}", (x, front_y + 0.13, z), (0.040, 0.075, 0.040), chrome_mat, 0.016)
        rear_lug = cube(f"Chrome_Kick_RearLug_{index:02d}", (x, rear_y - 0.13, z), (0.040, 0.075, 0.040), chrome_mat, 0.016)
        rod = beam(f"Chrome_Kick_TensionRod_{index:02d}", (x, front_y + 0.02, z), (x, rear_y - 0.02, z), 0.011, chrome_mat, 10)
        parts.extend((front_lug, rear_lug, rod))
    pedal_board = cube("Hardware_KickPedalBoard", (0, -2.10, 0.105), (0.155, 0.46, 0.035), chrome_mat, 0.040)
    pedal_hinge = cylinder("Chrome_KickPedalHinge", (0, -1.69, 0.13), 0.075, 0.34, chrome_mat, 24, (0, math.radians(90), 0))
    beater = beam("Chrome_KickBeaterShaft", (0, -1.73, 0.16), (0, -1.66, 0.79), 0.018, chrome_mat, 12)
    beater_head = sphere("Drum_KickBeater", (0, -1.65, 0.84), (0.085, 0.045, 0.105), head_mat, 24, 16)
    spring = torus("Chrome_KickPedalSpring", (0.18, -1.76, 0.31), 0.055, 0.010, chrome_mat, (0, math.radians(90), 0))
    spurs = [
        beam("Chrome_KickSpur_L", (-0.60, -0.80, 0.58), (-0.82, -0.36, 0.04), 0.028, chrome_mat, 14),
        beam("Chrome_KickSpur_R", (0.60, -0.80, 0.58), (0.82, -0.36, 0.04), 0.028, chrome_mat, 14),
    ]
    parts.extend((pedal_board, pedal_hinge, beater, beater_head, spring, *spurs))
    parent_all(root, parts)
    return root


def create_cymbal(name: str, location, radius: float, cymbal_mat, chrome_mat, tilt=(0, 0, 0)):
    root = empty(f"Cymbal_{name}_ROOT", location)
    z = location[2]
    boom_origin = (location[0] * 0.78, location[1] + 0.12, z - 0.34)
    stand_parts = create_tripod_stand(name, (boom_origin[0], boom_origin[1], 0), boom_origin, chrome_mat, location)
    disc = create_cymbal_surface(f"Cymbal_{name}_Disc", location, radius, cymbal_mat, tilt)
    grooves = []
    for groove_index, fraction in enumerate((0.34, 0.52, 0.69, 0.84, 0.94)):
        groove = torus(
            f"Cymbal_{name}_Groove_{groove_index}",
            location,
            radius * fraction,
            0.0045,
            cymbal_mat,
            tilt,
        )
        grooves.append(groove)
    felt = cylinder(f"Hardware_{name}_Felt", (location[0], location[1], z + 0.155), radius * 0.065, 0.035, chrome_mat, 32)
    wing_nut = sphere(f"Chrome_{name}_WingNut", (location[0], location[1], z + 0.205), (0.055, 0.022, 0.028), chrome_mat, 20, 12)
    parent_all(root, [*stand_parts, disc, *grooves, felt, wing_nut])
    return root


def create_hi_hat(cymbal_mat, chrome_mat):
    root = empty("Cymbal_HiHat_ROOT", (-1.72, -0.35, 1.77))
    x, y = -1.72, -0.35
    stand_parts = create_tripod_stand("HiHat", (x, y, 0), (x, y, 1.83), chrome_mat)
    lower = create_cymbal_surface("Cymbal_HiHat_Lower", (x, y, 1.72), 0.53, cymbal_mat)
    upper = create_cymbal_surface("Cymbal_HiHat_Upper", (x, y, 1.79), 0.53, cymbal_mat, (math.radians(180), 0, 0))
    pull_rod = beam("Chrome_HiHat_PullRod", (x, y, 0.28), (x, y, 2.02), 0.010, chrome_mat, 10)
    clutch = cylinder("Chrome_HiHat_Clutch", (x, y, 1.93), 0.052, 0.13, chrome_mat, 24)
    pedal = cube("Hardware_HiHat_Pedal", (x, y - 0.46, 0.085), (0.15, 0.38, 0.035), chrome_mat, 0.035)
    linkage = beam("Chrome_HiHat_Linkage", (x, y - 0.35, 0.10), (x, y, 0.32), 0.018, chrome_mat, 12)
    parent_all(root, [*stand_parts, lower, upper, pull_rod, clutch, pedal, linkage])
    return root


def create_drum_kit(materials):
    root = empty("HTK_DrumKit_ROOT")
    shell, head, chrome, cymbal, accent = materials
    kick = create_kick(shell, head, chrome, accent)
    snare_parts = create_drum_shell("Snare", (-1.05, -0.55, 1.16), 0.50, 0.34, shell, head, chrome, 10)
    tom1_parts = create_drum_shell("Tom1", (-0.58, -0.18, 1.72), 0.43, 0.40, shell, head, chrome, 8)
    tom2_parts = create_drum_shell("Tom2", (0.48, -0.14, 1.76), 0.47, 0.44, shell, head, chrome, 8)
    floor_parts = create_drum_shell("FloorTom", (1.31, -0.28, 1.12), 0.61, 0.64, shell, head, chrome, 10)
    stands = []
    snare_base = (-1.05, -0.55, 0.12)
    stands.extend(create_tripod_stand("Snare", snare_base, (-1.05, -0.55, 1.00), chrome))
    # Snare basket arms and floor-tom legs make the kit physically supported.
    for arm_index, angle in enumerate((math.radians(25), math.radians(145), math.radians(265))):
        stands.append(
            beam(
                f"Chrome_SnareBasket_{arm_index}",
                (-1.05, -0.55, 0.98),
                (-1.05 + math.cos(angle) * 0.39, -0.55 + math.sin(angle) * 0.39, 1.05),
                0.017,
                chrome,
                12,
            )
        )
    for leg_index, angle in enumerate((math.radians(15), math.radians(135), math.radians(255))):
        shell_point = (1.31 + math.cos(angle) * 0.52, -0.28 + math.sin(angle) * 0.52, 1.02)
        foot_point = (1.31 + math.cos(angle) * 0.73, -0.28 + math.sin(angle) * 0.73, 0.04)
        stands.append(beam(f"Chrome_FloorTomLeg_{leg_index}", shell_point, foot_point, 0.024, chrome, 14))
    rack = beam("Chrome_TomRack", (-0.78, -0.05, 1.39), (0.73, -0.05, 1.42), 0.030, chrome, 16)
    rack_left = beam("Chrome_TomMount_Left", (-0.58, -0.05, 1.40), (-0.58, -0.18, 1.52), 0.025, chrome, 14)
    rack_right = beam("Chrome_TomMount_Right", (0.48, -0.05, 1.42), (0.48, -0.14, 1.54), 0.025, chrome, 14)
    stands.extend((rack, rack_left, rack_right))
    cymbals = [
        create_hi_hat(cymbal, chrome),
        create_cymbal("CrashLeft", (-2.0, 0.18, 2.62), 0.76, cymbal, chrome, (math.radians(6), math.radians(-7), 0)),
        create_cymbal("CrashRight", (1.70, 0.42, 2.70), 0.79, cymbal, chrome, (math.radians(-5), math.radians(8), 0)),
        create_cymbal("Ride", (2.30, -0.30, 2.20), 0.87, cymbal, chrome, (math.radians(-8), math.radians(4), 0)),
    ]
    throne_base = beam("Chrome_ThroneStand", (0, -2.72, 0.02), (0, -2.72, 0.62), 0.04, chrome)
    throne = cylinder("Drum_Throne", (0, -2.72, 0.68), 0.40, 0.16, head, 64)
    add_bevel(throne, 0.035, 3)
    parent_all(root, [kick, *snare_parts, *tom1_parts, *tom2_parts, *floor_parts, *stands, *cymbals, throne_base, throne])
    # The drum kit is the main-menu hero asset. Bring it closer to the camera
    # and slightly right of center so its hardware remains readable beside the
    # left-side navigation without sacrificing the audience sightline.
    root.location = (1.05, -0.62, 0.0)
    root.scale = (1.28, 1.28, 1.28)
    return root


def create_truss(stage_mat, chrome_mat, fixture_mat, accent_mat):
    root = empty("HTK_Truss_ROOT")
    parts = []
    for x in (-7.4, 7.4):
        parts.append(beam(f"Chrome_TrussTower_{'L' if x < 0 else 'R'}", (x, 2.5, 0), (x, 2.5, 6.7), 0.10, chrome_mat, 16))
        for z in (1.2, 2.7, 4.2, 5.7):
            parts.append(beam(f"Chrome_TrussBrace_{x}_{z}_A", (x - 0.22, 2.5, z - 0.5), (x + 0.22, 2.5, z + 0.5), 0.025, chrome_mat))
            parts.append(beam(f"Chrome_TrussBrace_{x}_{z}_B", (x + 0.22, 2.5, z - 0.5), (x - 0.22, 2.5, z + 0.5), 0.025, chrome_mat))
    parts.append(beam("Chrome_TrussTop", (-7.4, 2.5, 6.7), (7.4, 2.5, 6.7), 0.11, chrome_mat, 16))
    for x in (-5.8, -3.6, -1.2, 1.2, 3.6, 5.8):
        fixture = cylinder(f"Fixture_MovingHead_{x}", (x, 2.35, 6.25), 0.27, 0.42, fixture_mat, 24, (math.radians(90), 0, 0))
        lens = cylinder(f"Accent_Lens_{x}", (x, 2.10, 6.25), 0.19, 0.04, accent_mat, 24, (math.radians(90), 0, 0))
        parts.extend((fixture, lens))
    # Two side video walls preserve a clear sightline from the drummer to the
    # audience; the central ring frames the crowd instead of covering it.
    screen_left = cube("Stage_BackdropLeft", (-4.15, 3.35, 3.55), (1.35, 0.10, 2.35), stage_mat, 0.15)
    screen_right = cube("Stage_BackdropRight", (4.15, 3.35, 3.55), (1.35, 0.10, 2.35), stage_mat, 0.15)
    logo_frame = torus("Accent_BackdropRing", (0, 3.18, 3.62), 1.15, 0.055, accent_mat, (math.radians(90), 0, 0))
    parts.extend((screen_left, screen_right, logo_frame))
    parent_all(root, parts)
    return root


def create_stage(stage_mat, chrome_mat, fixture_mat, accent_mat):
    root = empty("HTK_Stage_ROOT")
    floor = cube("Stage_Floor", (0, 0.8, -0.28), (9.0, 7.0, 0.28), stage_mat, 0.12)
    riser = cylinder("Stage_DrumRiser", (0, -0.7, 0.02), 3.4, 0.28, stage_mat, 64)
    rim = torus("Accent_DrumRiserRim", (0, -0.7, 0.17), 3.32, 0.055, accent_mat)
    front = cube("Stage_Apron", (0, -5.5, 0.24), (8.8, 1.0, 0.55), fixture_mat, 0.16)
    runway_left = cube("Stage_RunwayLeft", (-6.9, 1.2, 0.02), (0.13, 5.4, 0.10), accent_mat, 0.04)
    runway_right = cube("Stage_RunwayRight", (6.9, 1.2, 0.02), (0.13, 5.4, 0.10), accent_mat, 0.04)
    speakers = []
    for x in (-7.9, 7.9):
        side = "L" if x < 0 else "R"
        for index in range(4):
            z = 1.0 + index * 0.92
            cabinet = cube(f"Stage_Speaker_{side}_{index}", (x, 2.1, z), (0.55, 0.42, 0.38), fixture_mat, 0.08)
            cone = cylinder(f"Stage_SpeakerCone_{side}_{index}", (x, 1.65, z), 0.25, 0.035, stage_mat, 24, (math.radians(90), 0, 0))
            speakers.extend((cabinet, cone))
    parent_all(root, [floor, riser, rim, front, runway_left, runway_right, *speakers])
    return root


def join_objects(name: str, objects, mat):
    if not objects:
        return None
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    joined = bpy.context.object
    joined.name = name
    if len(joined.data.materials) == 0:
        assign(joined, mat)
    return joined


def append_group(groups, key: str, *objects) -> None:
    groups.setdefault(key, []).extend(obj for obj in objects if obj is not None)


def create_audience_character(row_index: int, index: int, x: float, y: float, height: float, materials, groups) -> None:
    """Create an original concertgoer with readable anatomy at game-camera distance.

    The audience is deliberately authored from smooth, human-proportioned
    primitives rather than imported character assets.  Each figure has a
    torso, pelvis, two-segment limbs, hands, shoes, facial volume and hair.
    Parts are later batched by row/material, retaining the silhouette detail
    without turning the menu into hundreds of Unity renderers.
    """
    skin_mats, clothes_mats, hair_mats, shoe_mat, detail_mat, accent_mat = materials
    person_id = f"{row_index:02d}_{index:02d}"
    unit = height / 1.76
    skin_index = (index * 5 + row_index * 2) % len(skin_mats)
    clothes_index = (index + row_index * 3) % len(clothes_mats)
    hair_index = (index * 3 + row_index) % len(hair_mats)
    skin = skin_mats[skin_index]
    clothes = clothes_mats[clothes_index]
    hair = hair_mats[hair_index]
    lean = RANDOM.uniform(-0.035, 0.035) * unit
    depth = RANDOM.uniform(-0.055, 0.055) * unit
    person_y = y + depth

    # Lower body: feet point toward the stage (negative Y), while offset knees
    # and hips avoid the mannequin-straight silhouette of the old crowd.
    stance = RANDOM.uniform(0.11, 0.17) * unit
    hip_z = 0.52 * height
    knee_z = 0.285 * height
    ankle_z = 0.075 * height
    hips = sphere(
        f"Audience_Clothes{clothes_index}_Hips_{person_id}",
        (x + lean * 0.35, person_y, hip_z),
        (0.205 * unit, 0.125 * unit, 0.145 * unit),
        clothes,
        16,
        10,
    )
    append_group(groups, f"Clothes{clothes_index}", hips)
    for side_index, side in enumerate((-1, 1)):
        hip = (x + side * stance + lean * 0.25, person_y, hip_z)
        knee = (
            x + side * stance * RANDOM.uniform(0.82, 1.15),
            person_y + RANDOM.uniform(-0.035, 0.035) * unit,
            knee_z,
        )
        ankle = (x + side * stance * RANDOM.uniform(0.76, 1.08), person_y - 0.018 * unit, ankle_z)
        thigh = beam(f"Audience_Clothes{clothes_index}_Thigh_{person_id}_{side_index}", hip, knee, 0.092 * unit, clothes, 12)
        shin = beam(f"Audience_Clothes{clothes_index}_Shin_{person_id}_{side_index}", knee, ankle, 0.075 * unit, clothes, 12)
        shoe = cube(
            f"Audience_Shoes_Foot_{person_id}_{side_index}",
            (ankle[0], person_y - 0.085 * unit, 0.045 * height),
            (0.105 * unit, 0.175 * unit, 0.060 * unit),
            shoe_mat,
            0.045 * unit,
        )
        append_group(groups, f"Clothes{clothes_index}", thigh, shin)
        append_group(groups, "Shoes", shoe)

    # Torso and neckline.  A slightly wider shoulder line, tapered waist and
    # visible neck read as a clothed person even in the darker rear rows.
    shoulder_z = 0.785 * height
    torso = sphere(
        f"Audience_Clothes{clothes_index}_Torso_{person_id}",
        (x + lean * 0.72, person_y, 0.675 * height),
        (RANDOM.uniform(0.225, 0.265) * unit, 0.135 * unit, 0.225 * unit),
        clothes,
        20,
        12,
    )
    neck = cylinder(
        f"Audience_Skin{skin_index}_Neck_{person_id}",
        (x + lean, person_y, 0.835 * height),
        0.065 * unit,
        0.11 * unit,
        skin,
        16,
    )
    append_group(groups, f"Clothes{clothes_index}", torso)
    append_group(groups, f"Skin{skin_index}", neck)

    # Concert poses: relaxed, one-hand cheer, both hands up, clap, fist pump,
    # phone recording and shoulder-level dance.  The two-segment arm makes
    # every gesture anatomically legible instead of a single diagonal stick.
    pose = (index + row_index * 2) % 7
    shoulder_width = 0.225 * unit
    shoulders = [
        (x - shoulder_width + lean * 0.72, person_y, shoulder_z),
        (x + shoulder_width + lean * 0.72, person_y, shoulder_z),
    ]
    if pose == 0:  # relaxed
        elbows = [(x - 0.285 * unit, person_y, 0.61 * height), (x + 0.285 * unit, person_y, 0.61 * height)]
        wrists = [(x - 0.245 * unit, person_y - 0.02, 0.45 * height), (x + 0.245 * unit, person_y - 0.02, 0.45 * height)]
    elif pose == 1:  # left hand raised
        elbows = [(x - 0.30 * unit, person_y, 0.93 * height), (x + 0.29 * unit, person_y, 0.61 * height)]
        wrists = [(x - 0.22 * unit, person_y, 1.10 * height), (x + 0.25 * unit, person_y, 0.45 * height)]
    elif pose == 2:  # both hands raised
        elbows = [(x - 0.31 * unit, person_y, 0.94 * height), (x + 0.31 * unit, person_y, 0.94 * height)]
        wrists = [(x - 0.22 * unit, person_y, 1.12 * height), (x + 0.22 * unit, person_y, 1.12 * height)]
    elif pose == 3:  # overhead clap
        elbows = [(x - 0.32 * unit, person_y, 0.92 * height), (x + 0.32 * unit, person_y, 0.92 * height)]
        wrists = [(x - 0.045 * unit, person_y - 0.02, 1.10 * height), (x + 0.045 * unit, person_y - 0.02, 1.10 * height)]
    elif pose == 4:  # right fist pump
        elbows = [(x - 0.29 * unit, person_y, 0.61 * height), (x + 0.34 * unit, person_y, 0.93 * height)]
        wrists = [(x - 0.24 * unit, person_y, 0.46 * height), (x + 0.40 * unit, person_y, 1.08 * height)]
    elif pose == 5:  # phone held above eye line
        elbows = [(x - 0.20 * unit, person_y - 0.03, 0.77 * height), (x + 0.27 * unit, person_y - 0.03, 0.90 * height)]
        wrists = [(x - 0.06 * unit, person_y - 0.09, 0.88 * height), (x + 0.09 * unit, person_y - 0.09, 1.02 * height)]
    else:  # dancing, hands wide
        elbows = [(x - 0.34 * unit, person_y, 0.78 * height), (x + 0.34 * unit, person_y, 0.78 * height)]
        wrists = [(x - 0.43 * unit, person_y, 0.84 * height), (x + 0.43 * unit, person_y, 0.84 * height)]

    for arm_index in range(2):
        shoulder = shoulders[arm_index]
        elbow = elbows[arm_index]
        wrist = wrists[arm_index]
        upper = beam(
            f"Audience_Clothes{clothes_index}_UpperArm_{person_id}_{arm_index}",
            shoulder,
            elbow,
            0.068 * unit,
            clothes,
            12,
        )
        forearm = beam(
            f"Audience_Skin{skin_index}_Forearm_{person_id}_{arm_index}",
            elbow,
            wrist,
            0.052 * unit,
            skin,
            12,
        )
        hand = sphere(
            f"Audience_Skin{skin_index}_Hand_{person_id}_{arm_index}",
            wrist,
            (0.063 * unit, 0.040 * unit, 0.080 * unit),
            skin,
            14,
            8,
        )
        append_group(groups, f"Clothes{clothes_index}", upper)
        append_group(groups, f"Skin{skin_index}", forearm, hand)

    # Face, ears, nose and hair are intentionally low-to-medium density: they
    # catch the stage fill light but stay cheap after the per-row batching.
    head_center = (x + lean, person_y, 0.925 * height)
    head = sphere(
        f"Audience_Skin{skin_index}_Head_{person_id}",
        head_center,
        (0.112 * height, 0.090 * height, 0.135 * height),
        skin,
        20,
        14,
    )
    left_ear = sphere(
        f"Audience_Skin{skin_index}_EarL_{person_id}",
        (head_center[0] - 0.112 * height, person_y, head_center[2]),
        (0.022 * height, 0.012 * height, 0.034 * height),
        skin,
        12,
        8,
    )
    right_ear = sphere(
        f"Audience_Skin{skin_index}_EarR_{person_id}",
        (head_center[0] + 0.112 * height, person_y, head_center[2]),
        (0.022 * height, 0.012 * height, 0.034 * height),
        skin,
        12,
        8,
    )
    nose = sphere(
        f"Audience_Skin{skin_index}_Nose_{person_id}",
        (head_center[0], person_y - 0.092 * height, head_center[2] - 0.005 * height),
        (0.018 * height, 0.025 * height, 0.032 * height),
        skin,
        12,
        8,
    )
    append_group(groups, f"Skin{skin_index}", head, left_ear, right_ear, nose)

    hair_cap = sphere(
        f"Audience_Hair{hair_index}_Cap_{person_id}",
        (head_center[0], person_y + 0.008 * height, head_center[2] + 0.085 * height),
        (0.116 * height, 0.094 * height, 0.075 * height),
        hair,
        18,
        10,
    )
    append_group(groups, f"Hair{hair_index}", hair_cap)
    if (index + row_index) % 5 == 0:
        ponytail = sphere(
            f"Audience_Hair{hair_index}_Ponytail_{person_id}",
            (head_center[0], person_y + 0.090 * height, head_center[2] + 0.015 * height),
            (0.052 * height, 0.045 * height, 0.105 * height),
            hair,
            14,
            8,
        )
        append_group(groups, f"Hair{hair_index}", ponytail)

    # Tiny dark facial marks survive only in the nearest rows and prevent the
    # heads from reading as blank spheres when the blue audience wash rises.
    if row_index < 3:
        for eye_side in (-1, 1):
            eye = sphere(
                f"Audience_Detail_Eye_{person_id}_{eye_side}",
                (head_center[0] + eye_side * 0.040 * height, person_y - 0.088 * height, head_center[2] + 0.025 * height),
                (0.010 * height, 0.008 * height, 0.014 * height),
                detail_mat,
                10,
                6,
            )
            append_group(groups, "Detail", eye)

    if pose == 5:
        phone_center = (
            (wrists[0][0] + wrists[1][0]) * 0.5,
            person_y - 0.125 * unit,
            (wrists[0][2] + wrists[1][2]) * 0.5,
        )
        phone = cube(
            f"Audience_Accessory_Phone_{person_id}",
            phone_center,
            (0.075 * unit, 0.018 * unit, 0.125 * unit),
            detail_mat,
            0.016 * unit,
        )
        screen = cube(
            f"Audience_Accent_PhoneScreen_{person_id}",
            (phone_center[0], phone_center[1] - 0.020 * unit, phone_center[2]),
            (0.060 * unit, 0.006 * unit, 0.105 * unit),
            accent_mat,
            0.010 * unit,
        )
        append_group(groups, "Detail", phone)
        append_group(groups, "Accent", screen)


def create_audience(audience_materials):
    root = empty("HTK_Audience_ROOT")
    row_objects = []
    row_specs = (
        (4.8, 14, 0.95),
        (6.2, 17, 1.02),
        (7.8, 20, 1.07),
        (9.6, 23, 1.13),
        (11.6, 26, 1.20),
        (13.8, 30, 1.26),
    )
    skin_mats, clothes_mats, hair_mats, shoe_mat, detail_mat, accent_mat = audience_materials
    material_lookup = {f"Skin{index}": value for index, value in enumerate(skin_mats)}
    material_lookup.update({f"Clothes{index}": value for index, value in enumerate(clothes_mats)})
    material_lookup.update({f"Hair{index}": value for index, value in enumerate(hair_mats)})
    material_lookup.update({"Shoes": shoe_mat, "Detail": detail_mat, "Accent": accent_mat})

    for row_index, (y, count, scale) in enumerate(row_specs):
        row_root = empty(f"Audience_Row_{row_index:02d}")
        row_root["htk_crowd_row"] = row_index
        row_root["htk_character_count"] = count
        groups = {}
        spacing = 15.5 / max(count - 1, 1)
        for index in range(count):
            x = -7.75 + index * spacing + RANDOM.uniform(-0.15, 0.15)
            height = RANDOM.uniform(1.55, 1.92) * scale
            create_audience_character(row_index, index, x, y, height, audience_materials, groups)
        batched = []
        for group_name in sorted(groups):
            joined = join_objects(
                f"Audience_{group_name}_Row_{row_index:02d}",
                groups[group_name],
                material_lookup[group_name],
            )
            batched.append(joined)
        parent_all(row_root, batched)
        row_objects.append(row_root)
    parent_all(root, row_objects)
    root["htk_character_count"] = sum(spec[1] for spec in row_specs)
    return root


def create_scene():
    reset_scene()
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0
    bpy.context.scene.render.engine = "BLENDER_EEVEE"

    stage_mat = material("HTK_Stage", (0.009, 0.014, 0.025, 1), 0.45, 0.32)
    fixture_mat = material("HTK_Fixture", (0.018, 0.025, 0.038, 1), 0.65, 0.24)
    chrome_mat = material("HTK_Chrome", (0.32, 0.40, 0.48, 1), 0.92, 0.15)
    shell_mat = material("HTK_DrumShell", (0.025, 0.07, 0.11, 1), 0.78, 0.20)
    head_mat = material("HTK_DrumHead", (0.012, 0.017, 0.023, 1), 0.05, 0.44)
    cymbal_mat = material("HTK_Cymbal", (0.55, 0.29, 0.055, 1), 0.80, 0.20)
    accent_mat = material("HTK_Accent", (0.01, 0.52, 0.95, 1), 0.42, 0.17)
    skin_mats = [
        material("HTK_AudienceSkin0", (0.56, 0.31, 0.19, 1), 0.0, 0.72),
        material("HTK_AudienceSkin1", (0.35, 0.17, 0.095, 1), 0.0, 0.76),
        material("HTK_AudienceSkin2", (0.14, 0.060, 0.032, 1), 0.0, 0.80),
        material("HTK_AudienceSkin3", (0.72, 0.48, 0.31, 1), 0.0, 0.68),
    ]
    clothes_mats = [
        material("HTK_AudienceClothes0", (0.012, 0.030, 0.055, 1), 0.0, 0.78),
        material("HTK_AudienceClothes1", (0.055, 0.012, 0.055, 1), 0.0, 0.76),
        material("HTK_AudienceClothes2", (0.065, 0.027, 0.010, 1), 0.0, 0.82),
        material("HTK_AudienceClothes3", (0.018, 0.055, 0.045, 1), 0.0, 0.80),
    ]
    hair_mats = [
        material("HTK_AudienceHair0", (0.008, 0.006, 0.005, 1), 0.0, 0.86),
        material("HTK_AudienceHair1", (0.055, 0.020, 0.008, 1), 0.0, 0.82),
        material("HTK_AudienceHair2", (0.14, 0.075, 0.022, 1), 0.0, 0.78),
    ]
    shoe_mat = material("HTK_AudienceShoes", (0.008, 0.010, 0.014, 1), 0.0, 0.66)
    detail_mat = material("HTK_AudienceDetail", (0.003, 0.004, 0.006, 1), 0.0, 0.82)

    master = empty("HTK_MainMenuStage_ROOT")
    stage = create_stage(stage_mat, chrome_mat, fixture_mat, accent_mat)
    truss = create_truss(stage_mat, chrome_mat, fixture_mat, accent_mat)
    kit = create_drum_kit((shell_mat, head_mat, chrome_mat, cymbal_mat, accent_mat))
    audience = create_audience((skin_mats, clothes_mats, hair_mats, shoe_mat, detail_mat, accent_mat))
    parent_all(master, [stage, truss, kit, audience])

    master["htk_asset"] = "main-menu-stage"
    master["htk_schema"] = 3
    master["htk_detail"] = "hd-hard-surface-and-procedural-humans"
    master["htk_reference"] = "docs/design/references/modern-drum-kit-hd-concept.png"
    master["htk_original_asset"] = True
    master["htk_license"] = "Project-owned original procedural geometry"

    for obj in bpy.context.scene.objects:
        obj.select_set(obj.type in {"MESH", "EMPTY"})

    BLEND_OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    FBX_OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    # Export while the procedural scene is still untitled. Blender otherwise
    # embeds the absolute .blend path as ApplicationNativeFile in the FBX.
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_OUTPUT),
        use_selection=True,
        object_types={"MESH", "EMPTY"},
        use_mesh_modifiers=True,
        use_custom_props=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
    )
    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_OUTPUT), compress=True)
    print(f"HTK_STAGE_BLEND={BLEND_OUTPUT}")
    print(f"HTK_STAGE_FBX={FBX_OUTPUT}")
    print(f"HTK_STAGE_OBJECTS={len(bpy.context.scene.objects)}")


if __name__ == "__main__":
    create_scene()
