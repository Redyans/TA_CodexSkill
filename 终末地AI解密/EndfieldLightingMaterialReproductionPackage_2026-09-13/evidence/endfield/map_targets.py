import os
import re
import struct
import sys
import zlib
from pathlib import Path

VFS = Path(r"F:\END\EndField Game\EndfieldTBeta2_Data\StreamingAssets\VFS")
KEY = bytes.fromhex("E95B317AC4F828569D23A86BF271DCB53E846FA75C924D671DBA8E38F4CA52E1")

def _rotl32(v, n):
    return ((v << n) | (v >> (32 - n))) & 0xFFFFFFFF


def _qr(x, a, b, c, d):
    x[a] = (x[a] + x[b]) & 0xFFFFFFFF
    x[d] ^= x[a]
    x[d] = _rotl32(x[d], 16)
    x[c] = (x[c] + x[d]) & 0xFFFFFFFF
    x[b] ^= x[c]
    x[b] = _rotl32(x[b], 12)
    x[a] = (x[a] + x[b]) & 0xFFFFFFFF
    x[d] ^= x[a]
    x[d] = _rotl32(x[d], 8)
    x[c] = (x[c] + x[d]) & 0xFFFFFFFF
    x[b] ^= x[c]
    x[b] = _rotl32(x[b], 7)


def chacha20_xor(data, key, nonce):
    """Minimal IETF ChaCha20 (counter starts at zero; caller discards 64 bytes)."""
    constants = b"expand 32-byte k"
    state = list(struct.unpack("<4I8II3I", constants + key + b"\0\0\0\0" + nonce))
    out = bytearray()
    counter = 0
    for pos in range(0, len(data), 64):
        state[12] = counter & 0xFFFFFFFF
        work = state[:]
        for _ in range(10):
            _qr(work, 0, 4, 8, 12); _qr(work, 1, 5, 9, 13)
            _qr(work, 2, 6, 10, 14); _qr(work, 3, 7, 11, 15)
            _qr(work, 0, 5, 10, 15); _qr(work, 1, 6, 11, 12)
            _qr(work, 2, 7, 8, 13); _qr(work, 3, 4, 9, 14)
        stream = struct.pack("<16I", *[(a + b) & 0xFFFFFFFF for a, b in zip(work, state)])
        block = data[pos:pos + 64]
        out.extend(a ^ b for a, b in zip(block, stream))
        counter += 1
    return bytes(out)

BLOCK_NAMES = {
    1: "Bundle", 2: "Audio", 3: "Video", 4: "Streaming", 5: "DynamicStreaming",
    6: "Lua", 7: "Table", 8: "JsonData", 9: "ExtendData", 10: "IV",
    11: "InitialAudio", 12: "InitialBundle", 13: "InitialExtendData",
    14: "BundleManifest", 15: "IFixPatch", 16: "AuditStreaming",
    17: "AuditDynamicStreaming", 18: "AuditIV", 19: "AuditAudio", 20: "AuditVideo",
}

TARGETS = (
    "additionallights/",
    "pointlight",
    "spotlight",
    "rimlight",
    "skill_ult",
    "handlight",
    "weapon_light",
    "light_asset",
)


def decrypt_blc(path: Path) -> bytes:
    data = path.read_bytes()
    nonce = data[:12]
    # PyCryptodome's ChaCha20 stream is advanced by one block before payload.
    plain = chacha20_xor(b"\0" * 64 + data[12:], KEY, nonce)[64:]
    if len(plain) >= 4:
        expected = struct.unpack("<i", plain[-4:])[0] & 0xFFFFFFFF
        actual = zlib.crc32(plain[:-4]) & 0xFFFFFFFF
        if expected != actual:
            print(f"WARN crc {path}: {expected:#x} != {actual:#x}", file=sys.stderr)
        plain = plain[:-4]
    return plain


def read_u16(buf, off):
    return struct.unpack_from("<H", buf, off)[0], off + 2


def read_i32(buf, off):
    return struct.unpack_from("<i", buf, off)[0], off + 4


def read_i64(buf, off):
    return struct.unpack_from("<q", buf, off)[0], off + 8


def read_u8(buf, off):
    return buf[off], off + 1


def read_bytes(buf, off, n):
    return buf[off:off + n], off + n


def normalize(raw: str) -> str:
    raw = re.sub(r"[^\x20-\x7e/\\]", "", raw)
    m = re.search(r"(?:Data/|Assets/)[A-Za-z0-9_./\\-]+", raw)
    return m.group(0) if m else raw


def process(d: Path):
    blc = d / f"{d.name}.blc"
    if not blc.exists():
        return
    chk_sizes = {p.stem.upper(): p.stat().st_size for p in d.glob("*.chk")}
    if not chk_sizes:
        return
    try:
        buf = decrypt_blc(blc)
        off = 0
        raw_version, off = read_i32(buf, off)
        code_version = 3 if raw_version >= 11 else raw_version
        if raw_version < 11:
            _, off = read_i32(buf, off)
        name_len, off = read_u16(buf, off)
        name, off = read_bytes(buf, off, name_len)
        name = name.decode("ascii", "replace")
        _, off = read_i64(buf, off)  # dir hash
        file_cnt, off = read_i32(buf, off)
        _, off = read_i64(buf, off)  # chunks len
        block_type, off = read_u8(buf, off)
        chunk_count, off = read_i32(buf, off)
    except Exception as exc:
        print(f"WARN parse {blc}: {exc}", file=sys.stderr)
        return
    print(f"DBG {d.name}: raw={raw_version} code={code_version} name={name!r} block={block_type} files={file_cnt} chunks={chunk_count}", flush=True)
    global_seen = 0
    for _ in range(chunk_count):
        try:
            chunk_md5, off = read_bytes(buf, off, 16)
            _, off = read_bytes(buf, off, 16)  # content md5
            _, off = read_i64(buf, off)
            _, off = read_u8(buf, off)
            if code_version > 3:
                _, off = read_i32(buf, off)
            nfiles, off = read_i32(buf, off)
            chunk_hex = chunk_md5.hex().upper()
            chk_size = chk_sizes.get(chunk_hex, -1)
            for _ in range(nfiles):
                fn_len, off = read_u16(buf, off)
                raw_fn, off = read_bytes(buf, off, fn_len)
                raw_fn = raw_fn.decode("ascii", "replace")
                fn_hash, off = read_i64(buf, off)
                file_chunk_md5, off = read_bytes(buf, off, 16)
                file_data_md5, off = read_bytes(buf, off, 16)
                file_off, off = read_i64(buf, off)
                file_len, off = read_i64(buf, off)
                file_bt, off = read_u8(buf, off)
                enc, off = read_u8(buf, off)
                iv_seed = 0
                if enc:
                    iv_seed, off = read_i64(buf, off)
                if code_version > 3:
                    _, off = read_i32(buf, off)
                norm = normalize(raw_fn)
                low = norm.lower().replace("\\", "/")
                if global_seen < 5:
                    print(f"DBGFILE {d.name}: raw_fn={raw_fn!r} norm={norm!r} chunk={chunk_hex} off={file_off} len={file_len}", flush=True)
                global_seen += 1
                if any(t in low for t in TARGETS):
                    print("\t".join([
                        d.name, BLOCK_NAMES.get(block_type, str(block_type)), norm,
                        chunk_hex, str(chk_size), str(file_off), str(file_len),
                        str(enc), str(iv_seed), str(file_bt),
                    ]), flush=True)
        except Exception as exc:
            print(f"WARN entry {d.name}: {exc}", file=sys.stderr)
            return


for directory in sorted(VFS.iterdir()):
    if directory.is_dir():
        process(directory)
