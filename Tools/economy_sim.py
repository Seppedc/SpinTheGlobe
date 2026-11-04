# Economy pacing simulation for Spin The Globe. Models an active player buying the best-value upgrade.
# Mirror price changes from DefaultSkillTreeFactory / GameBalance here, then run: python Tools/economy_sim.py
import math
# ---------------- proposed node set ----------------
# (id, branch, maxRank, baseCost, growth, effect)
N=[]
def node(i,b,r,c,g,eff,cur='€',req=None): N.append(dict(id=i,b=b,max=r,c=c,g=g,eff=eff,cur=cur,req=req))
node('root','root',1,10,1,None)
# Globe
node('max_speed','globe',20,15,1.6,('maxspd',150),req='auto')
node('auto','globe',5,6,2.0,('auto_share',0.2),req='root')
node('globe_mult','globe',25,110,1.42,('gmult_add',0.25),req='max_speed')
node('drag','globe',5,12,1.8,None,req='max_speed')
node('click','globe',5,12,1.8,None,req='max_speed')
node('gilded_axis','globe',1,5e6,1,('gmult_x',2),req='globe_mult')
node('golden_meridian','globe',1,150,1,('all_x',1.5),cur='g',req='gilded_axis')
# Pins
for i in range(12):
    node(f'pin{i+1}','pins',1,(20*3.6**i if i else 10),1,('pin',i),req=('root' if i==0 else f'pin{i}'))
    node(f'dur{i+1}','pins',5,30*3.6**i,1.6,('dur',i),req=f'pin{i+1}')
    node(f'pay{i+1}','pins',10,25*3.6**i,1.45,('pay',i),req=f'pin{i+1}')
node('reload','pins',15,15,1.45,('reload',0.06),req='pin1')
node('autoloader','pins',1,50000,1,('autofire',1),req='reload')
node('land','pins',5,50,1.9,('land',0.08),req='pin2')
node('city','pins',5,200,2.0,('city',0.05),req='land')
node('steel_tips','pins',1,60,1,('dur_all',1),cur='g',req='city')
# Case
node('case_pay','case',10,60,1.7,('case_x',0.25),req='root')
node('case_cd','case',10,75,1.7,None,req='case_pay')
node('stamp','case',10,250,1.7,None,req='case_pay')
node('cold_cases','case',3,1000,3,('case_need',-1),req='case_cd')
node('press','case',1,40,1,('case_gold',1),cur='g',req='stamp')
# Whiskey
node('wh_dur','whiskey',10,70,1.55,('wh_dur',2),req='root')
node('wh_cd','whiskey',10,85,1.55,('wh_cd',0.06),req='wh_dur')
node('wh_str','whiskey',5,400,2.2,('wh_str',0.25),req='wh_dur')
# Statues
node('st_fill','statues',10,150,1.6,('st_fill',0.2),req='root')
node('st_pay','statues',10,250,1.6,('st_pay',0.25),req='st_fill')
# Lootbox
node('bulk','loot',5,300,2.0,('loot_cost',0.06),req='root')
node('luck','loot',5,500,2.0,None,req='bulk')

ids={n['id']:n for n in N}
def cost(n,r): return math.ceil(n['c']*n['g']**r)

# ---------------- economy model ----------------
B=dict(maxspd=90, rev=2.4, pin=3, landM=2, cityM=3, reload=4, land=0.3, city=0.05, wh_dur=10, wh_cd=60, wh_str=1.5,
       case_base=40, case_growth=1.32, loot_base=150, loot_g=1.12)
def stats(R):
    s=dict(B); s['maxspd']+=150*R['max_speed']; s['auto']=s['maxspd']*min(1,0.2*R['auto'])
    s['gmult']=(1+0.25*R['globe_mult'])*(2 if R['gilded_axis'] else 1)*(1.5 if R['golden_meridian'] else 1)
    s['pins']=sum(R[f'pin{i+1}'] for i in range(12))
    s['dur']=[1+R[f'dur{i+1}']+R['steel_tips'] for i in range(12)]
    s['pay']=[1+0.5*R[f'pay{i+1}'] for i in range(12)]
    s['reload']=4*max(0.05,1-0.06*R['reload']); s['autofire']=R['autoloader']
    s['land']=0.3+0.08*R['land']; s['city']=0.05+0.05*R['city']
    s['case_x']=1+0.25*R['case_pay']; s['case_need_minus']=R['cold_cases']
    s['wh_dur']=10+2*R['wh_dur']; s['wh_cd']=60*max(0.05,1-0.06*R['wh_cd']); s['wh_str']=1.5+0.25*R['wh_str']
    return s
ACTIVITY=0.3   # share of max speed a player sustains by hand (measured feel: friction is strong)
def income(R,case_level):
    s=stats(R)
    speed=max(s['auto'], ACTIVITY*s['maxspd'])
    revps=speed/360
    landM=s['city']*s['landM']*(1+(s['cityM']-1)*0.5)+(1-s['city'])*(s['land']*s['landM']+(1-s['land']))
    # whiskey uptime boost (assume player keeps it going)
    up=s['wh_dur']/(s['wh_dur']+s['wh_cd']+8); boost=1+(s['wh_str']-1)*up
    g=s['gmult']*boost
    revinc=s['rev']*g*revps*(1+ (s['wh_str']-1)*up*0)  # speed boost ignored
    # pins: each pin cycle = flight + dur revolutions; limited by shots (manual: player fires when ready)
    pininc=0; falls=0
    shots_per_s=1/s['reload']*(1 if s['autofire'] else 0.7)
    capacity=0
    for i in range(s['pins']):
        life=s['dur'][i]/max(revps,1e-6)
        capacity+=1/life
    pin_rate=min(shots_per_s,capacity)  # pins landing per second (steady state)
    for i in range(s['pins']):
        share=(1/(s['dur'][i]/max(revps,1e-6)))/capacity if capacity else 0
        pininc+=pin_rate*share*s['dur'][i]*s['pin']*landM*s['pay'][i]*g
    falls=pin_rate
    need=max(1,min(40,3+2*case_level-s['case_need_minus']))
    reqdur=min(5,case_level//4)
    eligible=sum(1 for i in range(s['pins']) if R[f'dur{i+1}']+R['steel_tips']>=reqdur)/max(1,s['pins'])
    efalls=falls*eligible
    cycle=(need/efalls if efalls>0 else 1e9)+30*max(0.3,1-0.07*R['case_cd'])
    reward=max(40*(1+case_level), 12*(revinc+pininc))*s['case_x']
    case_inc=reward/cycle
    falls=efalls
    return revinc+pininc, case_inc, revinc, pininc, falls, need
def roi_candidates(R,money,gold):
    out=[]
    for n in N:
        r=R[n['id']]
        if r>=n['max']: continue
        if n['req'] and R[n['req']]<1: continue
        out.append((n,cost(n,r)))
    return out
def sim(hours=8,dt=1.0):
    R={n['id']:0 for n in N}; money=0; gold=0; t=0; case_cd_until=0; case_level=0; case_prog=0; log=[]; milestones={}
    while t<hours*3600:
        inc,cinc,revinc,pininc,falls,need=income(R,case_level)
        money+= (inc)*dt
        # cases: discrete
        if t>=case_cd_until: case_prog+=falls*dt
        if case_prog>=need:
            case_cd_until=t+30*max(0.3,1-0.07*R['case_cd'])
            case_prog-=need; money+=max(40*(1+case_level),12*inc)*stats(R)['case_x']; case_level+=1
            if R['press']: gold+=2*case_level
        # statues/gold: assume ~1 smash per 20 min early, gold 10..30
        if int(t)%1200==0 and t>600: gold+=15*(1+0.25*R['st_pay'])
        # buy: greedy by income gain per cost among affordable, else save for best
        cands=roi_candidates(R,money,gold)
        best=None;bestv=0
        for n,c in cands:
            have= gold if n['cur']=='g' else money
            if c>have: continue
            R[n['id']]+=1; ni,nc,*_=income(R,case_level); R[n['id']]-=1
            gain=(ni+nc)-(inc+cinc)
            v=gain/c if gain>0 else 1e-9/c  # tiny value for utility nodes
            if n['id']=='root' or n['id'].startswith('pin'): v*=3
            if v>bestv: bestv=v;best=(n,c)
        if best:
            n,c=best
            if n['cur']=='g': gold-=c
            else: money-=c
            R[n['id']]+=1
            for key in ('root','max_speed','pin1','pin2','pin3','pin4','pin6','pin9','pin12','autoloader','gilded_axis'):
                if n['id']==key and key not in milestones: milestones[key]=t
        if int(t)%1800==0: log.append((t/60,money,inc*60,cinc*60,case_level,stats(R)['pins'],sum(R.values())))
        t+=dt
    return R,log,milestones
R,log,m=sim()
for k,v in m.items(): print(f'{k:12} at {v/60:6.1f} min')
print(' min   money   income/min  case/min  caselvl pins  ranks')
for l in log: print(f'{l[0]:5.0f} {l[1]:9.0f} {l[2]:10.0f} {l[3]:8.0f} {l[4]:5d} {l[5]:4d} {l[6]:5d}')
tot=sum(n['max'] for n in N); print('total ranks',tot,'nodes',len(N))
