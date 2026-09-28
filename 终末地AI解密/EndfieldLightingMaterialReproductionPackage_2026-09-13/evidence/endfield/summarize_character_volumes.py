"""Summarize actual overrides in dumped HGCharacterVolume components."""

from collections import Counter, defaultdict
from pathlib import Path
import argparse
import re


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("dump", type=Path)
    args = parser.parse_args()
    lines = args.dump.read_text(encoding="utf-8", errors="replace").splitlines()

    blocks: list[list[str]] = []
    current: list[str] | None = None
    for line in lines:
        if line.startswith("FULL_MONO\t"):
            if current:
                blocks.append(current)
            current = [line]
        elif current is not None:
            if re.match(r"^(?:MonoBehaviour|Light|ReflectionProbe|LightProbeGroup|RenderSettings)\t", line):
                blocks.append(current)
                current = None
            else:
                current.append(line)
    if current:
        blocks.append(current)

    volume_blocks = [
        b for b in blocks
        if any(line.strip() == 'string m_Name = "HGCharacterVolume"' for line in b)
    ]
    declared: Counter[str] = Counter()
    overridden: Counter[str] = Counter()
    values: dict[str, Counter[str]] = defaultdict(Counter)
    parameter_re = re.compile(r"^\s*\w+Parameter\s+(\w+)$")
    override_re = re.compile(r"^\s*UInt8 overrideState = (\d+)$")
    value_re = re.compile(r"^\s*(?:UInt8|int|float) m_Value = (.+)$")

    for block in volume_blocks:
        index = 0
        while index < len(block):
            match = parameter_re.match(block[index])
            if not match:
                index += 1
                continue
            name = match.group(1)
            declared[name] += 1
            state = None
            value = None
            cursor = index + 1
            while cursor < len(block) and not parameter_re.match(block[cursor]):
                state_match = override_re.match(block[cursor])
                if state_match:
                    state = int(state_match.group(1))
                value_match = value_re.match(block[cursor])
                if value_match:
                    value = value_match.group(1)
                cursor += 1
            if state == 1:
                overridden[name] += 1
                if value is not None:
                    values[name][value] += 1
            index = cursor

    print(f"HGCharacterVolume blocks={len(volume_blocks)}")
    for name in sorted(declared):
        if overridden[name] == 0:
            continue
        value_summary = ", ".join(f"{v}:{n}" for v, n in values[name].most_common())
        print(f"{name}\tdeclared={declared[name]}\toverridden={overridden[name]}\tvalues={value_summary}")

    for name in (
        "charEyeBaseLightMultiplier",
        "charEyeHighlightMultiplier",
        "charEyeScatteringMultiplier",
        "charAutoRimEnable",
        "charFaceRimEnable",
        "charOutlineQualityMode",
    ):
        print(
            f"CHECK {name}\tdeclared={declared[name]}\t"
            f"overridden={overridden[name]}\tvalues={dict(values[name])}"
        )


if __name__ == "__main__":
    main()
