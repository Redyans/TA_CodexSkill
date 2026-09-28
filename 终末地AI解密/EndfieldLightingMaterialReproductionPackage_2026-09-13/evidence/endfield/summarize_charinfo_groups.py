"""Verify native Light membership for the main character-info lighting groups."""

from __future__ import annotations

import csv
import re
from collections import Counter
from pathlib import Path


INPUT = Path(r"C:\Users\xverse\AppData\Local\Temp\endfield-per-bundle2")
OUTPUT = Path(r"C:\Users\xverse\AppData\Local\Temp\ef-charinfo-native-groups.csv")
DETAIL = Path(r"C:\Users\xverse\AppData\Local\Temp\ef-charinfo-native-lights.csv")
SUMMARY = Path(r"C:\Users\xverse\AppData\Local\Temp\ef-charinfo-hgadditional-summary.txt")

PREFABS = {
    "0122c987a129c36fdf764011": "light_chr_0004_pelica",
    "01f2e454c7db280ad4b6a286": "light_chr_0018_dapan",
    "09bad14ba586db713fa4dda4": "light_chr_0015_lifeng",
    "0cd20da3833906c0a2c4f1b1": "light_chr_0005_chen",
    "0d22902aa367989924d08ead": "light_chr_0002_endminm",
    "08144b9dc01c135821a05908": "light_chr_0029_pograni",
    "11577482fb94ac337b2499f9": "light_chr_0022_bounda",
    "168b956854138b7f4ab460c7": "light_chr_0023_antal",
    "19065ea4c20f16530a2ba1c5": "light_chr_0007_ikut",
    "29e986bdf112078f9bcdc3f8": "light_chr_0016_laevat",
    "31060f953bad9699e100f010": "light_chr_0006_wolfgd",
    "52a5094e2a3708184457a4ba": "light_chr_0017_yvonne",
    "530ea45dd8722b31c98df75a": "light_chr_0021_whiten",
    "5896efdadf23289d07d7033b": "light_chr_0025_ardelia",
    "750b0a2438ab67750f31e1ac": "light_chr_0024_qianneng",
    "abf35f11b14495b9c461c7f1": "light_chr_0020_meurs",
    "bcef75f7c82cd16282e796a4": "light_chr_0026_lastrite",
    "be5859c8f2839c0149af07ce": "light_chr_0011_seraph",
    "c8bd02d5b361b31dfede5c4a": "light_chr_0014_aurora",
    "ce3611e65d7b5f73d9b61bfc": "light_chr_0013_aglina",
    "d733c9494547a8a94f0a1b60": "light_chr_0019_karin",
    "de803f488c3824f3a7349177": "light_chr_0024_deepfin",
    "df99503874c40d33a5adbd27": "light_chr_0009_azrila",
    "f671f4078a551360421c1f90": "light_chr_0012_avywen",
}

# This prefab is a dedicated five-probe rig, not one of the standard
# overview/skill/equip native-Light controllers.
SPECIAL_NO_GROUPS = {"light_chr_0024_qianneng"}

LIGHT_RE = re.compile(
    r"^Light\tCLR=Object\tGO=(.*?)\tID=(-?\d+)\t.*?"
    r"^ \| m_Type=([^|]+?)\s*\| m_Shape=.*?"
    r"m_Color=\{([^}]*)\}\s*\| m_Intensity=([^|]+?)\s*\| "
    r"m_SpecularIntensity=([^|]+?)\s*\| m_Range=([^|]+?)\s*\|",
    re.MULTILINE | re.DOTALL,
)

GROUP_RE = re.compile(
    r"^FULL_MONO\tGO=light_(overview|skill|equip)\tID=.*?\n"
    r"(.*?)(?=^(?:[A-Za-z]+\tCLR=|BUNDLE|EXTERNAL)|\Z)",
    re.MULTILINE | re.DOTALL,
)

NATIVE_LIGHT_BLOCK_RE = re.compile(
    r"^FULL_NATIVE\tTYPE=Light\tGO=.*?\tID=(-?\d+)\n"
    r"(.*?)(?=^(?:[A-Za-z]+\tCLR=|BUNDLE|EXTERNAL)|\Z)",
    re.MULTILINE | re.DOTALL,
)

MONO_BLOCK_RE = re.compile(
    r"^FULL_MONO\tGO=.*?\tID=(-?\d+)\n"
    r"(.*?)(?=^(?:[A-Za-z]+\tCLR=|BUNDLE|EXTERNAL)|\Z)",
    re.MULTILINE | re.DOTALL,
)

GAME_OBJECT_ID_RE = re.compile(
    r"PPtr<[^>]*GameObject> m_GameObject\s+"
    r"int m_FileID = -?\d+\s+SInt64 m_PathID = (-?\d+)",
)

MONO_SCRIPT_ID_RE = re.compile(
    r"PPtr<MonoScript> m_Script\s+"
    r"int m_FileID = -?\d+\s+SInt64 m_PathID = (-?\d+)",
)

HG_ADDITIONAL_LIGHT_DATA_SCRIPT_ID = 4098216658219718577

HG_FIELDS: dict[str, type[int] | type[float]] = {
    "m_LightCharacterOnly": int,
    "m_lightNPRSpecMetalOnly": int,
    "m_lightNPRSpecMaxRoughness": float,
    "m_lightNPRSpecRoughnessBias": float,
    "m_lightNPRRimWidth": float,
    "m_volumetricScatteringIntensity": float,
    "enableLightMeshForReflectionProbe": int,
}


def parse_field(block: str, name: str, converter: type[int] | type[float]) -> int | float | None:
    match = re.search(rf"^\s*(?:UInt8|float) {re.escape(name)} = ([^\r\n]+)$", block, re.MULTILINE)
    return converter(match.group(1)) if match else None


def value_distribution(rows: list[dict[str, object]], field: str) -> str:
    values = [row[field] for row in rows if row[f"{field}_present"]]
    counts = Counter(values)
    return ",".join(f"{value}:{counts[value]}" for value in sorted(counts, key=float))


def percentage(part: int, total: int) -> str:
    return f"{part / total:.1%}" if total else "n/a"


def parse(path: Path, prefab: str) -> tuple[list[dict[str, object]], list[dict[str, object]]]:
    text = path.read_text(encoding="utf-8-sig", errors="replace")
    lights: dict[int, dict[str, object]] = {}
    for match in LIGHT_RE.finditer(text):
        light_id = int(match.group(2))
        lights[light_id] = {
            "prefab": prefab,
            "group": "",
            "light_id": light_id,
            "game_object": match.group(1).strip(),
            "type": match.group(3).strip(),
            "color": match.group(4).strip(),
            "intensity": float(match.group(5).strip()),
            "specular_intensity": float(match.group(6).strip()),
            "range": float(match.group(7).strip()),
        }

    for match in NATIVE_LIGHT_BLOCK_RE.finditer(text):
        light_id = int(match.group(1))
        game_object_match = GAME_OBJECT_ID_RE.search(match.group(2))
        if light_id not in lights:
            raise RuntimeError(f"native Light detail without summary in {path.name}:{light_id}")
        if not game_object_match:
            raise RuntimeError(f"native Light has no GameObject PPtr in {path.name}:{light_id}")
        lights[light_id]["game_object_id"] = int(game_object_match.group(1))

    hg_by_game_object: dict[int, list[dict[str, object]]] = {}
    for match in MONO_BLOCK_RE.finditer(text):
        component_id = int(match.group(1))
        block = match.group(2)
        # Identify component presence by its MonoScript PPtr, independently of
        # whether any requested serialized field is present or which value it
        # stores. This avoids conflating "component exists" with field coverage.
        script_match = MONO_SCRIPT_ID_RE.search(block)
        if not script_match or int(script_match.group(1)) != HG_ADDITIONAL_LIGHT_DATA_SCRIPT_ID:
            continue
        game_object_match = GAME_OBJECT_ID_RE.search(block)
        if not game_object_match:
            raise RuntimeError(f"HGAdditionalLightData has no GameObject PPtr in {path.name}:{component_id}")
        component: dict[str, object] = {"component_id": component_id}
        for field, converter in HG_FIELDS.items():
            value = parse_field(block, field, converter)
            component[f"{field}_present"] = value is not None
            component[field] = "" if value is None else value
        hg_by_game_object.setdefault(int(game_object_match.group(1)), []).append(component)

    for light_id, light in lights.items():
        if "game_object_id" not in light:
            raise RuntimeError(f"native Light has no parsed detail in {path.name}:{light_id}")
        components = hg_by_game_object.get(int(light["game_object_id"]), [])
        light["hg_component_count"] = len(components)
        light["has_hg_additional_light_data"] = bool(components)
        light["hg_component_ids"] = ";".join(str(component["component_id"]) for component in components)
        # Multiple components are reported as ambiguous rather than silently
        # selecting one. This preserves the one-to-one evidence boundary.
        component = components[0] if len(components) == 1 else None
        for field in HG_FIELDS:
            light[f"{field}_present"] = bool(component and component[f"{field}_present"])
            light[field] = component[field] if component and component[f"{field}_present"] else ""

    groups: list[dict[str, object]] = []
    details: list[dict[str, object]] = []
    seen_groups: set[str] = set()
    for match in GROUP_RE.finditer(text):
        group = match.group(1)
        if group in seen_groups:
            raise RuntimeError(f"duplicate group {group} in {path.name}")
        seen_groups.add(group)
        block = match.group(2).split("\n\tLightInfo ", 1)[0]
        size_match = re.search(r"vector m_LightList.*?\n\s*int size = (\d+)", block, re.DOTALL)
        if not size_match:
            raise RuntimeError(f"missing m_LightList in {path.name}:{group}")
        listed = int(size_match.group(1))
        ids = [int(value) for value in re.findall(r"SInt64 m_PathID = (-?\d+)", block)]
        # The first two PPtrs belong to the controller's GameObject and script.
        ids = ids[-listed:] if listed else []
        resolved = [lights[light_id] for light_id in ids if light_id in lights]
        missing = [light_id for light_id in ids if light_id not in lights]
        for light in resolved:
            row = dict(light)
            row["group"] = group
            details.append(row)
        groups.append(
            {
                "prefab": prefab,
                "bundle": path.name.removesuffix(".ab.txt"),
                "group": group,
                "listed_lights": listed,
                "native_lights": len(resolved),
                "missing_ids": ";".join(map(str, missing)),
                "spot_lights": sum(light["type"].startswith("Spot") for light in resolved),
                "point_lights": sum(light["type"].startswith("Point") for light in resolved),
                "intensity_min": min((light["intensity"] for light in resolved), default=""),
                "intensity_max": max((light["intensity"] for light in resolved), default=""),
                "range_min": min((light["range"] for light in resolved), default=""),
                "range_max": max((light["range"] for light in resolved), default=""),
                "hg_component_lights": sum(bool(light["has_hg_additional_light_data"]) for light in resolved),
                "hg_component_missing": sum(not bool(light["has_hg_additional_light_data"]) for light in resolved),
                "hg_component_ambiguous": sum(int(light["hg_component_count"]) > 1 for light in resolved),
            }
        )
    missing_groups = {"overview", "skill", "equip"} - seen_groups
    if missing_groups and prefab not in SPECIAL_NO_GROUPS:
        raise RuntimeError(f"missing groups {sorted(missing_groups)} in {path.name}")
    return groups, details


def main() -> None:
    group_rows: list[dict[str, object]] = []
    detail_rows: list[dict[str, object]] = []
    for bundle, prefab in PREFABS.items():
        source = INPUT / f"{bundle}.ab.txt"
        if not source.exists():
            raise FileNotFoundError(source)
        groups, details = parse(source, prefab)
        group_rows.extend(groups)
        detail_rows.extend(details)

    with OUTPUT.open("w", newline="", encoding="utf-8-sig") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(group_rows[0]))
        writer.writeheader()
        writer.writerows(group_rows)
    with DETAIL.open("w", newline="", encoding="utf-8-sig") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(detail_rows[0]))
        writer.writeheader()
        writer.writerows(detail_rows)

    summary_lines: list[str] = []
    for group in ("overview", "skill", "equip"):
        rows = [row for row in group_rows if row["group"] == group]
        lights = [row for row in detail_rows if row["group"] == group]
        component_count = sum(bool(row["has_hg_additional_light_data"]) for row in lights)
        headline = " ".join(
            map(
                str,
                (
            group,
            f"prefabs={len(rows)}",
            f"native={sum(int(row['native_lights']) for row in rows)}",
            f"per_prefab={sorted({int(row['native_lights']) for row in rows})}",
            f"unresolved={sum(bool(row['missing_ids']) for row in rows)}",
                    f"hg_component={component_count}/{len(lights)}({percentage(component_count, len(lights))})",
                    f"hg_missing={sum(not bool(row['has_hg_additional_light_data']) for row in lights)}",
                    f"hg_ambiguous={sum(int(row['hg_component_count']) > 1 for row in lights)}",
                ),
            )
        )
        print(headline)
        summary_lines.append(headline)
        for field in HG_FIELDS:
            present = sum(bool(row[f"{field}_present"]) for row in lights)
            field_line = (
                f"  {field}: present={present}/{len(lights)}({percentage(present, len(lights))}) "
                f"values={value_distribution(lights, field)}"
            )
            print(field_line)
            summary_lines.append(field_line)
    SUMMARY.write_text("\n".join(summary_lines) + "\n", encoding="utf-8-sig")
    print(f"group_csv={OUTPUT}")
    print(f"detail_csv={DETAIL}")
    print(f"summary={SUMMARY}")


if __name__ == "__main__":
    main()
