# Rough prestige simulation for Spin The Globe: plays run after run with the economy model from economy_sim.py,
# retires when the gold-per-minute rate peaks, then buys Gold shop items (cheapest first). Run: python Tools/prestige_sim.py
# Statue gold is a guess (~2 smashes per hour per unlocked statue); treat the totals as a rough guide.
import math, os
src=open(os.path.join(os.path.dirname(os.path.abspath(__file__)),'economy_sim.py')).read(); src=src[:src.index('R,log,m=sim()')]
G={}; exec(src,G)
base_income=G['income']
MULT=[1.0]
def income(R,c):
    a=base_income(R,c); m=MULT[0]
    return (a[0]*m,a[1]*m,a[2]*m,a[3]*m,a[4],a[5])
G['income']=income
N=G['N']; stats=G['stats']; roi=G['roi_candidates']
RANKS=[(0,0),(1,.1),(3,.25),(5,.5),(8,1),(12,2),(20,4)]
def rankbonus(p): return max(b for r,b in RANKS if r<=p)

def run(shop, prestiges, cap_h=3, dt=2.0):
    MULT[0]=(1+0.1*shop.get('rep',0))*(1+rankbonus(prestiges))*(1.5 if shop.get('meridian') else 1)
    R={n['id']:0 for n in N}
    if shop.get('ledger'): R['root']=1
    if shop.get('wound'): R['auto']=3
    if shop.get('spare'): R['pin1']=1
    money=[0,100,1e3,1e4,1e5,1e6][shop.get('seed',0)]
    earned=0; t=0; case_cd=0; lvl=0; prog=0; best=0; bestT=0; sgold=0
    statues=sum(shop.get(f'st{i}',0) for i in range(3))
    while t<cap_h*3600:
        inc,cinc,_,_,falls,need=income(R,lvl)
        money+=inc*dt; earned+=inc*dt
        if t>=case_cd: prog+=falls*dt
        if prog>=need:
            case_cd=t+30*max(0.3,1-0.07*R['case_cd']); prog-=need
            c=max(40*(1+lvl),12*inc)*stats(R)['case_x']*MULT[0]; money+=c; earned+=c; lvl+=1
        # statue gold: ~2 smashes/h per unlocked statue
        for i in range(3):
            if shop.get(f'st{i}'): sgold+= dt/1800 * 10*(i+1)*[1,2.5,6][shop.get(f'tier{i}',0)]*(1+0.25*R['st_pay'])
        # buy best
        best_c=None;bv=0
        for n,c in roi(R,money,0):
            if n['cur']=='g' or c>money: continue
            R[n['id']]+=1; ni,nc,*_=income(R,lvl); R[n['id']]-=1
            gain=(ni+nc)-(inc+cinc); v=gain/c if gain>0 else 1e-9/c
            if n['id']=='root' or n['id'].startswith('pin'): v*=3
            if v>bv: bv=v; best_c=(n,c)
        if best_c: money-=best_c[1]; R[best_c[0]['id']]+=1
        t+=dt
        g=math.floor(25*math.sqrt(earned/2.5e7)) if earned>=2.5e7 else 0
        if g>0 and int(t)%60==0:
            rate=(g+sgold)/t
            if rate>best: best=rate; bestT=t
            elif rate<0.97*best and t>bestT+300: break
    g=math.floor(25*math.sqrt(earned/2.5e7)) if earned>=2.5e7 else 0
    return t, g+sgold

ITEMS=[('ledger',10),('st0',25),('wound',20),('spare',30),('press',40),('st1',75),('steel',60),('double',100),('twin',120),
       ('meridian',150),('st2',150),('bottomless',200),('fast',250)]
def tiercost(i,r): return [60,150,300][i]*3.3**r
shop={}; gold=0; total=0; p=0; milestones=[]
while p<25:
    t,g=run(shop,p); total+=t; gold+=g; p+=1
    # spend: cheapest useful first, reputation and seed in between
    bought=True
    while bought:
        bought=False
        opts=[(c,k) for k,c in ITEMS if not shop.get(k)]
        if not shop.get('rep') or True: opts.append((20*1.35**shop.get('rep',0),'rep'))
        if shop.get('seed',0)<5: opts.append((15*2.6**shop.get('seed',0),'seed'))
        for i in range(3):
            if shop.get(f'st{i}') and shop.get(f'tier{i}',0)<2: opts.append((tiercost(i,shop.get(f'tier{i}',0)),f'tier{i}'))
        opts.sort()
        for c,k in opts:
            if c<=gold:
                gold-=c
                if k in('rep','seed') or k.startswith('tier'): shop[k]=shop.get(k,0)+1
                else: shop[k]=1
                bought=True; break
    done=all(shop.get(k) for k,_ in ITEMS) and all(shop.get(f'tier{i}',0)==2 for i in range(3)) and shop.get('seed',0)==5
    print(f"run {p:2d}: {t/60:5.1f} min, +{g:7.0f} gold, total {total/3600:5.1f} h, rep {shop.get('rep',0)}, seed {shop.get('seed',0)}, items {sum(1 for k,_ in ITEMS if shop.get(k))}/{len(ITEMS)}, tiers {[shop.get(f'tier{i}',0) for i in range(3)]}{'  ALL SHOP ITEMS DONE' if done else ''}")
    if done and not milestones: milestones.append((p,total))
print(milestones)
