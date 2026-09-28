from pathlib import Path
import re, struct, zlib, sys

VFS = Path(r"F:\END\EndField Game\EndfieldTBeta2_Data\StreamingAssets\VFS")
OUT = Path(r"C:\Users\xverse\Documents\工作\HowellWork\EndfieldExtract\bundles")
KEY = bytes.fromhex("E95B317AC4F828569D23A86BF271DCB53E846FA75C924D671DBA8E38F4CA52E1")

TARGETS = {
    "main/0d8e7908746bf64d567e3a6b.ab",  # common fire rimlight
    "main/0ea31a3b61c54fdc25d79b43.ab",  # reaper handlight VFX asset
    "main/29e986bdf112078f9bcdc3f8.ab",  # Laevat charinfo light
    "main/31060f953bad9699e100f010.ab",  # Wolf charinfo light
    "main/31bf3f0b9dddd3f87ded701f.ab",  # natural rimlight
    "main/60a7852a741dcdf651835ffb.ab",  # lightning rimlight
    "main/66fd3cb72783d0f959f1a2c1.ab",  # formation light Wolf
    "main/7824d85894d02a39e8fefad1.ab",  # Laevat ult cloth light VFX
    "main/7ca9e733672b58a84005817c.ab",  # Rodin pointlight 03
    "main/91c1c5ad2023afff4f514eee.ab",  # Reaper handlight prefab
    "main/983b32c1b7b7feb5433bf8f8.ab",  # formation light Avywen
    "main/9fb22fc8112a94e7ff6b2c15.ab",  # Rodin pointlight 02
    "main/a91d36e4eed5623ab1720d43.ab",  # Laevat lighting ult
    "main/a9d54070cb32ea350df99557.ab",  # Laevat ult weapon VFX asset
    "main/ac9b4e081b67eb385a46242d.ab",  # Rodin pointlight 01
    "main/adb812647ca520a11490ef71.ab",  # Lastrite lighting ult
    "main/aebc7e491d4eff3b633ffbec.ab",  # Meurs lighting ult
    "main/b42602cc88c4af72e6f62363.ab",  # Seraph formation light
    "main/be5859c8f2839c0149af07ce.ab",  # Seraph charinfo light
    "main/d33fd7a244e54277b091509c.ab",  # Laevat sword light prefab
    "main/f671f4078a551360421c1f90.ab",  # Avywen charinfo light

    # Additional character-page light prefabs, ult lighting prefabs, and
    # scene/dialog spotlights selected from the manifest for the follow-up
    # lighting audit. These remain a bounded list; do not turn this into a
    # full VFS scan.
    "main/6b5ff688d756a4728e028bd0.ab",  # p_lighting_lifeng_skill_ult
    "main/079896bea4be8b8d412a66d4.ab",  # p_fxbat_lighting_amtal_skill_ult
    "main/654eb107558734dcb2fa0531.ab",  # p_fxbat_lighting_ardelia_skill_ult
    "main/196303097450f93eb9262ae0.ab",  # p_fxbat_lighting_bounda_skill_ult
    "main/8df1adeea05c68cd6bba23c2.ab",  # p_fxbat_lighting_deepfin_skill_ult
    "main/bca600a5096f3a9c17326c0e.ab",  # p_fxbat_lighting_karin_skill_ult
    "main/53f984bbf4c5cf9885c2398e.ab",  # p_fxbat_lighting_pograni_skill_ult
    "main/7be3ef2080721794be175f75.ab",  # p_fxbat_lighting_whiten_skill_ult
    "main/6065bcce2f33ebaae1f60c60.ab",  # p_lighting_aglina_skill_ult
    "main/3af82a823719fff421375637.ab",  # p_lighting_aurora_skill_ult
    "main/be811420d86e7125af0dc466.ab",  # p_lighting_avywen_skill_ult
    "main/ffe13396cc172d501bb7d950.ab",  # p_lighting_azrila_skill_ult
    "main/3ba147fdc8f8eefbf1790762.ab",  # p_lighting_chen_skill_ult
    "main/1d876a82e7b1595d035977d8.ab",  # p_lighting_dapan_skill_ult
    "main/dee0bf9bff325f2c78486651.ab",  # p_lighting_endminf_skill_ult
    "main/6170091f0ee4d96408322e08.ab",  # p_lighting_endminm_skill_ult
    "main/1e411dbfb11b396f124953d1.ab",  # p_lighting_ikut_skill_ult
    "main/ff89ec02a235f3d2fe0f3ad7.ab",  # p_lighting_perlica_skill_ult_1
    "main/346da1034bc89fc9fd572d23.ab",  # p_lighting_seraph_skill_ult
    "main/3494583123fabedd2931fca7.ab",  # p_lighting_wolfgd_skill_ult
    "main/297fca3ae5ff2d79bc4542a9.ab",  # p_lighting_yvonne_skill_ult
    "main/d25f85198fa5a6e09dcba3f7.ab",  # dlg spotlight 6
    "main/56dbdd727ab26e022673e47d.ab",  # dlg spotlight 1
    "main/f260421e73b3a17a24e12378.ab",  # dlg spotlight 2
    "main/d8c6236803d680cca210ac42.ab",  # dlg spotlight 4
    "main/216f65cecc22d4e25979234e.ab",  # dlg spotlight 5
    "main/a20d5cb9646b720b374cfe0c.ab",  # dlg spotlight 3
    "main/0122c987a129c36fdf764011.ab",  # light_chr_0004_pelica
    "main/01f2e454c7db280ad4b6a286.ab",  # light_chr_0018_dapan
    "main/0cd20da3833906c0a2c4f1b1.ab",  # light_chr_0005_chen
    "main/0d22902aa367989924d08ead.ab",  # light_chr_0002_endminm
    "main/11577482fb94ac337b2499f9.ab",  # light_chr_0022_bounda
    "main/168b956854138b7f4ab460c7.ab",  # light_chr_0023_antal
    "main/19065ea4c20f16530a2ba1c5.ab",  # light_chr_0007_ikut
    "main/52a5094e2a3708184457a4ba.ab",  # light_chr_0017_yvonne
    "main/530ea45dd8722b31c98df75a.ab",  # light_chr_0021_whiten
    "main/5896efdadf23289d07d7033b.ab",  # light_chr_0025_ardelia
    "main/750b0a2438ab67750f31e1ac.ab",  # light_chr_0024_qianneng
    "main/d733c9494547a8a94f0a1b60.ab",  # light_chr_0019_karin
    "main/df99503874c40d33a5adbd27.ab",  # light_chr_0009_azrila
    "main/ce3611e65d7b5f73d9b61bfc.ab",  # light_chr_0013_aglina
    "main/c8bd02d5b361b31dfede5c4a.ab",  # light_chr_0014_aurora
    "main/09bad14ba586db713fa4dda4.ab",  # light_chr_0015_lifeng
    "main/abf35f11b14495b9c461c7f1.ab",  # light_chr_0020_meurs
    "main/de803f488c3824f3a7349177.ab",  # light_chr_0024_deepfin
    "main/bcef75f7c82cd16282e796a4.ab",  # light_chr_0026_lastrite
    "main/08144b9dc01c135821a05908.ab",  # light_chr_0029_pograni
    "main/e412a21a27a0bd6249354f94.ab",  # lv006_spotlight_postmodel / scene lighting
}

def rotl(v, n): return ((v << n) | (v >> (32-n))) & 0xffffffff
def qr(x,a,b,c,d):
    x[a]=(x[a]+x[b])&0xffffffff; x[d]^=x[a]; x[d]=rotl(x[d],16)
    x[c]=(x[c]+x[d])&0xffffffff; x[b]^=x[c]; x[b]=rotl(x[b],12)
    x[a]=(x[a]+x[b])&0xffffffff; x[d]^=x[a]; x[d]=rotl(x[d],8)
    x[c]=(x[c]+x[d])&0xffffffff; x[b]^=x[c]; x[b]=rotl(x[b],7)
def chacha(data, key, nonce):
    state=list(struct.unpack('<4I8II3I',b'expand 32-byte k'+key+b'\0\0\0\0'+nonce))
    out=bytearray()
    for counter,pos in enumerate(range(0,len(data),64)):
        state[12]=counter; w=state[:]
        for _ in range(10):
            qr(w,0,4,8,12);qr(w,1,5,9,13);qr(w,2,6,10,14);qr(w,3,7,11,15)
            qr(w,0,5,10,15);qr(w,1,6,11,12);qr(w,2,7,8,13);qr(w,3,4,9,14)
        stream=struct.pack('<16I',*[(a+b)&0xffffffff for a,b in zip(w,state)])
        out.extend(a^b for a,b in zip(data[pos:pos+64],stream))
    return bytes(out)
def blc_decrypt(path):
    raw=path.read_bytes(); return chacha(b'\0'*64+raw[12:],KEY,raw[:12])[64:]
def i32(b,o): return struct.unpack_from('<i',b,o)[0],o+4
def i64(b,o): return struct.unpack_from('<q',b,o)[0],o+8
def u16(b,o): return struct.unpack_from('<H',b,o)[0],o+2
def u8(b,o): return b[o],o+1
def blob(b,o,n): return b[o:o+n],o+n

def per_file(data, iv):
    nonce=struct.pack('<i',3)+struct.pack('<q',iv)
    return chacha(b'\0'*64+data,KEY,nonce)[64:]

def norm(raw):
    raw=re.sub(r'[^\x20-\x7e/\\]','',raw)
    m=re.search(r'(?:Data/|Assets/)([A-Za-z0-9_./\\-]+)',raw)
    return m.group(1).replace('\\','/') if m else raw

def extract():
    d=VFS/'7064D8E2'; blc=d/'7064D8E2.blc'; b=blc_decrypt(blc); o=0
    raw,o=i32(b,o); code=3 if raw>=11 else raw
    if raw<11: _,o=i32(b,o)
    n,o=u16(b,o); name=b[o:o+n].decode('ascii','replace'); o+=n
    _,o=i64(b,o); file_count,o=i32(b,o); _,o=i64(b,o); block,o=u8(b,o); chunks,o=i32(b,o)
    found={}
    for ci in range(chunks):
        cm,o=blob(b,o,16); _,o=blob(b,o,16); _,o=i64(b,o); _,o=u8(b,o)
        if code>3: _,o=i32(b,o)
        nfiles,o=i32(b,o); cmhex=cm.hex().upper(); chk=d/(cmhex+'.chk')
        cdata=None
        for fi in range(nfiles):
            fnlen,o=u16(b,o); rawfn,o=blob(b,o,fnlen); rawfn=rawfn.decode('ascii','replace')
            _,o=i64(b,o); _,o=blob(b,o,16); _,o=blob(b,o,16); off,o=i64(b,o); length,o=i64(b,o); _,o=u8(b,o); enc,o=u8(b,o); iv=0
            if enc: iv,o=i64(b,o)
            if code>3: _,o=i32(b,o)
            rel=norm(rawfn).lower()
            if rel.startswith('bundles/windows/'):
                rel=rel[len('bundles/windows/'):]
            if rel not in TARGETS: continue
            if not chk.exists(): print('MISSING CHK',rel,cmhex); continue
            with chk.open('rb') as f: f.seek(off); data=f.read(length)
            if enc: data=per_file(data,iv)
            out=OUT/rel; out.parent.mkdir(parents=True,exist_ok=True); out.write_bytes(data)
            found[rel]=(len(data),cmhex,off,length,enc,iv)
            print(f'EXTRACT {rel} bytes={len(data)} chunk={cmhex} off={off} declared={length} enc={enc} iv={iv}',flush=True)
    print(f'FOUND {len(found)}/{len(TARGETS)}')
    for missing in sorted(TARGETS-set(found)): print('MISSING',missing)

if __name__=='__main__': extract()
