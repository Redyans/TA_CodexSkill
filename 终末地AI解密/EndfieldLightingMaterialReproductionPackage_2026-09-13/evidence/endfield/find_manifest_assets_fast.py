"""Bounded manifest path lookup without spawning Brotli for each record."""

from __future__ import annotations

import argparse
import struct
from pathlib import Path

from parse_hgmmap import MANIFEST, HEAD1, HEAD2, cstring_at, i32, i64, int_array_at, len_u16_string, u32


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("patterns", nargs="+")
    parser.add_argument("--manifest", type=Path, default=MANIFEST)
    args = parser.parse_args()
    patterns = tuple(value.lower() for value in args.patterns)

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

    bundles: dict[int, tuple[str, list[int]]] = {}
    p = bundle_array_address
    bundle_count, p = i32(data, p)
    for _ in range(bundle_count):
        values = []
        for _ in range(5):
            value, p = i32(data, p)
            values.append(value)
        _, p = i32(data, p)
        _, p = i64(data, p)
        _, p = i64(data, p)
        _, p = i32(data, p)
        _, p = i32(data, p)
        bundle_index, name_offset, deps_offset, _, _ = values
        bundles[bundle_index] = (
            cstring_at(data, data_address, name_offset),
            int_array_at(data, data_address, deps_offset),
        )

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
        raw_at = data_address + path_offset
        byte_count = struct.unpack_from("<i", data, raw_at)[0]
        raw = data[raw_at + 4 : raw_at + 4 + byte_count]
        # This CB3 manifest stores these path-table entries directly as UTF-16.
        # Ignore entries that do not decode cleanly instead of invoking an
        # external decompressor for every one of the 266k records.
        try:
            path = raw.decode("utf-16le").rstrip("\0")
        except UnicodeDecodeError:
            continue
        low = path.lower()
        if not any(pattern in low for pattern in patterns):
            continue
        bundle_name, dependencies = bundles.get(bundle_index, ("", []))
        print(
            f"ASSET bundle_index={bundle_index} size={asset_bytes} "
            f"bundle={bundle_name} dependencies={dependencies} path={path}"
        )


if __name__ == "__main__":
    main()
