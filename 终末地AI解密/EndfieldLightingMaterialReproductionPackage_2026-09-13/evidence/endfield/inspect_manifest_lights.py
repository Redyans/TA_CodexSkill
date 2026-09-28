import sys
sys.stdout.reconfigure(encoding="utf-8")
b=open(r"C:\Users\xverse\AppData\Local\Temp\manifest.hgmmap","rb").read()
for pat in [x.encode("utf-16le") for x in ["additionallights","light_","superskill","bigskill"]]:
    p=0
    print("PAT",pat)
    while True:
        p=b.lower().find(pat,p)
        if p<0: break
        s=max(0,p-400); e=min(len(b),p+500)
        print("OFFSET",p)
        print(repr(b[s:e].decode("utf-16le",errors="replace")))
        p+=1
