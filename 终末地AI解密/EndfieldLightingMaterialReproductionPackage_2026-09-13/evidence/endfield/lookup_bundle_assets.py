"""List manifest assets for one bundle without decompressing the full path table."""

from pathlib import Path
import argparse
import struct

from parse_hgmmap import (
    MANIFEST,
    HEAD1,
    HEAD2,
    cpath_at,
    cstring_at,
    i32,
    i64,
    int_array_at,
    len_u16_string,
    u32,
)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("bundle", help="bundle index or bundle hash/name")
    parser.add_argument("--manifest", type=Path, default=MANIFEST)
    args = parser.parse_args()

    data = args.manifest.read_bytes()
    offset = 0
    header, offset = u32(data, offset)
    if header != HEAD1:
        raise ValueError(f"unexpected header {header:#x}")
    _, offset = len_u16_string(data, offset)
    header2, offset = u32(data, offset)
    if header2 != HEAD2:
        raise ValueError(f"unexpected second header {header2:#x}")
    _, offset = len_u16_string(data, offset)
    _, offset = len_u16_string(data, offset)
    asset_size, offset = i32(data, offset)
    asset_address = offset
    offset += asset_size
    bundle_size, offset = i32(data, offset)
    offset += bundle_size
    bundle_array_size, offset = i32(data, offset)
    bundle_array_address = offset
    data_address = bundle_array_address + bundle_array_size + 4

    target_text = args.bundle.lower()
    target_indexes: set[int] = set()
    p = bundle_array_address
    bundle_count, p = i32(data, p)
    for _ in range(bundle_count):
        values = []
        for _ in range(5):
            value, p = i32(data, p)
            values.append(value)
        _, p = i32(data, p)  # flags
        _, p = i64(data, p)
        _, p = i64(data, p)
        _, p = i32(data, p)
        _, p = i32(data, p)
        bundle_index, name_offset, deps_offset, _, _ = values
        name = cstring_at(data, data_address, name_offset)
        if target_text == str(bundle_index) or target_text in name.lower():
            target_indexes.add(bundle_index)
            deps = int_array_at(data, data_address, deps_offset)
            print(f"BUNDLE index={bundle_index} name={name} dependencies={deps}")

    if not target_indexes:
        raise SystemExit(f"bundle not found: {args.bundle}")

    p = asset_address
    capacity, p = i32(data, p)
    p += capacity * 8
    for _ in range(capacity):
        if p + 48 > asset_address + asset_size:
            break
        path_offset = struct.unpack_from("<i", data, p + 8)[0]
        bundle_index = struct.unpack_from("<i", data, p + 40)[0]
        asset_bytes = struct.unpack_from("<i", data, p + 44)[0]
        p += 48
        if bundle_index not in target_indexes:
            continue
        path = cpath_at(data, data_address, path_offset)
        print(f"ASSET bundle_index={bundle_index} size={asset_bytes} path={path}")


if __name__ == "__main__":
    main()
