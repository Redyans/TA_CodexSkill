import re,sys
sys.stdout.reconfigure(encoding='utf-8')
b=open(r'C:\Users\xverse\AppData\Local\Temp\manifest.hgmmap','rb').read()
s=b.decode('utf-16le',errors='ignore')
for pat in ['pointlight','spotlight','rimlight','light.prefab','light_asset','light_01','light_02','light_03']:
    vals=sorted(set(re.findall(r'assets/[^\x00]{0,220}'+re.escape(pat)+r'[^\x00]{0,100}',s,re.I)))
    print('\nPAT',pat,'COUNT',len(vals))
    for v in vals[:120]: print(v)
