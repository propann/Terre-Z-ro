#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "godot_project"

EXT_DECL = re.compile(
    r'^\[ext_resource\s+type="(?P<type>[^"]+)"\s+path="(?P<path>[^"]+)"\s+id="(?P<id>[^"]+)"\]$'
)
SUB_DECL = re.compile(r'^\[sub_resource\s+type="[^"]+"\s+id="(?P<id>[^"]+)"\]$')
EXT_REF = re.compile(r'ExtResource\("(?P<id>[^"]+)"\)')
SUB_REF = re.compile(r'SubResource\("(?P<id>[^"]+)"\)')
MAIN_SCENE = re.compile(r'^run/main_scene="(?P<path>[^"]+)"$')


def res_to_path(value: str) -> Path:
    if not value.startswith("res://"):
        raise ValueError(f"unsupported resource path: {value}")
    return PROJECT / value.removeprefix("res://")


def validate_scene(path: Path) -> list[str]:
    errors: list[str] = []
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines()

    ext: dict[str, tuple[str, str]] = {}
    sub: set[str] = set()

    for lineno, line in enumerate(lines, 1):
        stripped = line.strip()
        m = EXT_DECL.match(stripped)
        if m:
            rid = m.group("id")
            if rid in ext:
                errors.append(f"{path}:{lineno}: duplicate ExtResource id {rid}")
            ext[rid] = (m.group("type"), m.group("path"))
            continue

        m = SUB_DECL.match(stripped)
        if m:
            rid = m.group("id")
            if rid in sub:
                errors.append(f"{path}:{lineno}: duplicate SubResource id {rid}")
            sub.add(rid)

    for rid, (rtype, rpath) in ext.items():
        try:
            target = res_to_path(rpath)
        except ValueError as exc:
            errors.append(f"{path}: ExtResource {rid}: {exc}")
            continue

        if not target.exists():
            errors.append(
                f"{path}: ExtResource {rid} ({rtype}) points to missing {rpath}"
            )

    for lineno, line in enumerate(lines, 1):
        for match in EXT_REF.finditer(line):
            rid = match.group("id")
            if rid not in ext:
                errors.append(
                    f"{path}:{lineno}: ExtResource({rid!r}) is not declared"
                )

        for match in SUB_REF.finditer(line):
            rid = match.group("id")
            if rid not in sub:
                errors.append(
                    f"{path}:{lineno}: SubResource({rid!r}) is not declared"
                )

    return errors


def validate_project() -> list[str]:
    errors: list[str] = []
    project_file = PROJECT / "project.godot"
    if not project_file.exists():
        return [f"missing {project_file}"]

    for lineno, line in enumerate(project_file.read_text(encoding="utf-8").splitlines(), 1):
        m = MAIN_SCENE.match(line.strip())
        if not m:
            continue

        target = res_to_path(m.group("path"))
        if not target.exists():
            errors.append(
                f"{project_file}:{lineno}: main scene does not exist: {m.group('path')}"
            )

    return errors


def main() -> int:
    errors = validate_project()

    scene_files = sorted(PROJECT.rglob("*.tscn"))
    if not scene_files:
        errors.append("no .tscn files found")

    for scene in scene_files:
        errors.extend(validate_scene(scene))

    if errors:
        print("Godot resource validation failed:", file=sys.stderr)
        for error in errors:
            print(f"  - {error}", file=sys.stderr)
        return 1

    print(f"Godot resource validation OK ({len(scene_files)} scenes).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
