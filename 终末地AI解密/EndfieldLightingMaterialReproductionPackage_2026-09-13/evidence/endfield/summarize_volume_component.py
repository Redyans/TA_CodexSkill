"""Summarize overridden primitive parameters in dumped Unity Volume components.

The inspector emits one text file per bundle.  This helper keeps the bundle
name attached to each VolumeComponent so results from different ult prefabs
are not accidentally treated as one runtime volume.
"""

from __future__ import annotations

import argparse
from collections import Counter, defaultdict
from pathlib import Path
import re


OBJECT_START_RE = re.compile(
    r"^(?:MonoBehaviour|Light|ReflectionProbe|LightProbeGroup|RenderSettings|Material|Shader)\t"
)
PARAMETER_RE = re.compile(r"^\s*\w+Parameter\s+(\w+)$")
OVERRIDE_RE = re.compile(r"^\s*UInt8 overrideState = (\d+)$")
VALUE_RE = re.compile(r"^\s*(?:UInt8|int|float) m_Value = (.+)$")
PATH_ID_RE = re.compile(r"^\s*SInt64 m_PathID = (-?\d+)$")
NAME_RE = re.compile(r'^\s*string m_Name = "([^"]*)"$')


def full_mono_blocks(lines: list[str]) -> list[list[str]]:
    blocks: list[list[str]] = []
    current: list[str] | None = None
    for line in lines:
        if line.startswith("FULL_MONO\t"):
            if current:
                blocks.append(current)
            current = [line]
        elif current is not None:
            if OBJECT_START_RE.match(line):
                blocks.append(current)
                current = None
            else:
                current.append(line)
    if current:
        blocks.append(current)
    return blocks


def component_name(block: list[str]) -> str | None:
    for line in block:
        match = NAME_RE.match(line)
        if match:
            return match.group(1)
    return None


def primitive_parameters(block: list[str]) -> dict[str, tuple[int | None, str | None]]:
    result: dict[str, tuple[int | None, str | None]] = {}
    index = 0
    while index < len(block):
        match = PARAMETER_RE.match(block[index])
        if not match:
            index += 1
            continue
        name = match.group(1)
        override = None
        value = None
        cursor = index + 1
        while cursor < len(block) and not PARAMETER_RE.match(block[cursor]):
            override_match = OVERRIDE_RE.match(block[cursor])
            if override_match:
                override = int(override_match.group(1))
            value_match = VALUE_RE.match(block[cursor])
            if value_match:
                value = value_match.group(1)
            elif value is None:
                path_match = PATH_ID_RE.match(block[cursor])
                if path_match:
                    value = f"path:{path_match.group(1)}"
            cursor += 1
        result[name] = (override, value)
        index = cursor
    return result


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("dump_dir", type=Path)
    parser.add_argument("component")
    args = parser.parse_args()

    rows: list[tuple[str, dict[str, tuple[int | None, str | None]]]] = []
    paths = [args.dump_dir] if args.dump_dir.is_file() else sorted(args.dump_dir.glob("*.txt"))
    for path in paths:
        lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
        # A combined inspector dump contains multiple BUNDLE sections.  Keep
        # their CAB labels so row counts remain auditable even without the
        # original input filename list.
        sections: list[tuple[str, list[str]]] = []
        label = path.stem
        section: list[str] = []
        for line in lines:
            if line.startswith("BUNDLE "):
                if section:
                    sections.append((label, section))
                label = line.split(" objects=", 1)[0].removeprefix("BUNDLE ")
                section = [line]
            else:
                section.append(line)
        if section:
            sections.append((label, section))

        for label, section in sections:
            for block in full_mono_blocks(section):
                if component_name(block) == args.component:
                    rows.append((label, primitive_parameters(block)))

    declared: Counter[str] = Counter()
    overridden: Counter[str] = Counter()
    values: dict[str, Counter[str]] = defaultdict(Counter)
    for _, params in rows:
        for name, (override, value) in params.items():
            declared[name] += 1
            if override == 1:
                overridden[name] += 1
                if value is not None:
                    values[name][value] += 1

    print(f"component={args.component} instances={len(rows)}")
    for name in sorted(declared):
        value_summary = ", ".join(f"{value}:{count}" for value, count in values[name].most_common())
        print(
            f"{name}\tdeclared={declared[name]}\toverridden={overridden[name]}"
            f"\tvalues={value_summary}"
        )

    print("ROWS")
    for bundle, params in rows:
        enabled = [
            f"{name}={value}"
            for name, (override, value) in params.items()
            if override == 1 and value is not None
        ]
        print(f"{bundle}\t" + "\t".join(enabled))


if __name__ == "__main__":
    main()
