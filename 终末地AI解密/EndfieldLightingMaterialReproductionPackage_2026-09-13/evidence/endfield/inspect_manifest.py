from pathlib import Path
import struct
import re
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="backslashreplace")

p = Path(r"C:\Users\xverse\AppData\Local\Temp\manifest.hgmmap")
b = p.read_bytes()
print("size", len(b), "head", b[:16].hex())
for needle in [
    "assets/beyond/dynamicassets/gameplay/prefabs/charinfo/additionallights/light_chr_0002.prefab",
    "p_laevat_skill_ult_wep_01_sword_light_asset.prefab",
    "p_laevat_skill_ult_cloth_02_start_light_asset.asset",
    "p_fxbat_reaper_skill_single_left_handlight_asset.prefab",
    "p_rodin_skill04_01_pointlight_01.prefab",
]:
    for enc_name, enc in (("utf8", needle.encode()), ("utf16", needle.encode("utf-16le"))):
        pos = b.lower().find(enc.lower())
        if pos < 0:
            continue
        print("\nTARGET", needle, enc_name, "offset", pos)
        s = max(0, pos - 256)
        e = min(len(b), pos + len(enc) + 256)
        print("hex", b[s:e].hex())
        # Show printable ASCII and UTF-16 strings in the same neighborhood.
        print("ascii", repr(b[s:e].decode("ascii", "replace")))
        print("u16  ", repr(b[s:e].decode("utf-16le", "replace")))

# Report all UTF-16 strings that look like bundle hashes or target paths.
u = b.decode("utf-16le", "ignore")
for pat in ("pointlight", "spotlight", "rimlight", "skill_ult", "additionallights", "handlight"):
    print("\nPAT", pat)
    for m in list(re.finditer(pat, u, re.I))[:20]:
        print(m.start() * 2, repr(u[max(0, m.start()-80):m.end()+120]))
