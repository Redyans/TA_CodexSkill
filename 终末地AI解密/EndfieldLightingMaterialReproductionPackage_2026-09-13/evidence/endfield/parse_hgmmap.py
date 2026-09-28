from pathlib import Path
import struct, sys, subprocess, tempfile, json

sys.stdout.reconfigure(encoding="utf-8", errors="backslashreplace")
MANIFEST = Path(r"C:\Users\xverse\AppData\Local\Temp\manifest.hgmmap")
HEAD1 = 0xFF11FF11
HEAD2 = 0xF1F2F3F4

def u32(b,o): return struct.unpack_from('<I',b,o)[0],o+4
def i32(b,o): return struct.unpack_from('<i',b,o)[0],o+4
def i64(b,o): return struct.unpack_from('<q',b,o)[0],o+8

def utf16(b):
    return b.decode('utf-16le', errors='replace').rstrip('\x00')

def len_u16_string(b,o):
    n,o=i32(b,o); raw=b[o:o+n*2]; return utf16(raw),o+n*2

def brotli_decompress(raw):
    # The manifest's path table uses a Brotli payload per string.
    exe = r'C:\Users\xverse\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\mingw64\bin\brotli.exe'
    with tempfile.NamedTemporaryFile(delete=False) as fi, tempfile.NamedTemporaryFile(delete=False) as fo:
        fi.write(raw); fi.flush()
        p=subprocess.run([exe,'-d','-f','-o',fo.name,fi.name],capture_output=True)
        if p.returncode != 0:
            raise RuntimeError(p.stderr.decode('utf-8','replace'))
        return Path(fo.name).read_bytes()

def cstring_at(b, data_base, rel):
    p=data_base+rel
    n=struct.unpack_from('<i',b,p)[0]
    return utf16(b[p+4:p+4+n])

def cpath_at(b, data_base, rel):
    p=data_base+rel
    n=struct.unpack_from('<i',b,p)[0]
    raw=b[p+4:p+4+n]
    try: return utf16(brotli_decompress(raw))
    except Exception:
        # Some tool builds store uncompressed UTF-16 strings; retain fallback.
        return utf16(raw)

def int_array_at(b, data_base, rel):
    p=data_base+rel
    n=struct.unpack_from('<i',b,p)[0]
    return list(struct.unpack_from('<'+('i'*n),b,p+4)) if n > 0 and p+4+4*n <= len(b) else []

def parse(path=MANIFEST):
    b=Path(path).read_bytes()
    o=0
    h,o=u32(b,o)
    if h != HEAD1: raise ValueError(f'HEAD1 {h:#x}')
    version,o=len_u16_string(b,o)
    h2,o=u32(b,o)
    if h2 != HEAD2: raise ValueError(f'HEAD2 {h2:#x}')
    hashv,o=len_u16_string(b,o)
    cl,o=len_u16_string(b,o)
    asset_size,o=i32(b,o); asset_addr=o; o += asset_size
    bundle_size,o=i32(b,o); bundle_addr=o; o += bundle_size
    bundle_array_size,o=i32(b,o); bundle_array_addr=o
    data_addr=bundle_array_addr+bundle_array_size+4
    # Bundle array
    p=bundle_array_addr
    bundle_count,p=i32(b,p)
    bundles=[]
    for _ in range(bundle_count):
        vals=[]
        for _j in range(5):
            v,p=i32(b,p); vals.append(v)
        flags,p=i32(b,p)
        hn,p=i64(b,p); hv,p=i64(b,p)
        cat,p=i32(b,p); _,p=i32(b,p)
        bundle_index,name_off,deps_off,rev_off,dir_off=vals
        bundles.append({
            'bundle_index': bundle_index,
            'index': bundle_index, 'name': cstring_at(b,data_addr,name_off),
            'dependencies': int_array_at(b,data_addr,deps_off),
            'reverse_dependencies': int_array_at(b,data_addr,rev_off),
            'direct_dependencies': int_array_at(b,data_addr,dir_off),
            'flags': flags, 'hash_name': hn, 'hash_version': hv, 'category': cat,
        })
    # Asset records
    p=asset_addr
    capacity,p=i32(b,p); p += capacity*8
    asset_end=asset_addr+asset_size
    assets=[]
    # CB3 hgmmap stores one 48-byte record per asset. The final two i32s
    # are the bundle index and asset size (the 24-byte parser used by older
    # builds silently desynchronizes on this format).
    for _ in range(capacity):
        if p+48 > asset_end: break
        ph=struct.unpack_from('<q',b,p)[0]
        path_off=struct.unpack_from('<i',b,p+8)[0]
        bi=struct.unpack_from('<i',b,p+40)[0]
        sz=struct.unpack_from('<i',b,p+44)[0]
        p += 48
        assets.append({'path_hash_head':ph,'path':cpath_at(b,data_addr,path_off),'bundle_index':bi,'asset_size':sz})
    return {'version':version,'hash':hashv,'perforce_cl':cl,'asset_addr':asset_addr,'asset_size':asset_size,'bundle_addr':bundle_addr,'bundle_array_addr':bundle_array_addr,'data_addr':data_addr,'bundles':bundles,'assets':assets}

if __name__=='__main__':
    s=parse()
    print({k:s[k] for k in ('version','hash','perforce_cl','asset_addr','asset_size','bundle_addr','bundle_array_addr','data_addr')})
    byidx={x['index']:x for x in s['bundles']}
    pats=('additionallights','pointlight','spotlight','rimlight','skill_ult','handlight','weapon_light','light_asset')
    hits=[]
    for a in s['assets']:
        low=a['path'].lower()
        if any(x in low for x in pats):
            x=dict(a); x['bundle_name']=byidx.get(a['bundle_index'],{}).get('name',''); hits.append(x)
            print(json.dumps(x,ensure_ascii=False))
    print('HITS',len(hits),'BUNDLES',len(s['bundles']),'ASSETS',len(s['assets']))
