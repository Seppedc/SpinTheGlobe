import math, json
N={}  # id: (x,y,parents,branch)
def a(i,x,y,p,b): N[i]=(x,y,p,b)
a('root',0,0,[],'R')
# Globe (up)
a('globe_max_speed',0,1,['root'],'G'); a('globe_drag',-1,2,['globe_max_speed'],'G'); a('globe_auto',0,2,['globe_max_speed'],'G'); a('globe_click',1,2,['globe_max_speed'],'G')
a('globe_multiplier',0,3,['globe_auto'],'G'); a('globe_gilded_axis',0,4,['globe_multiplier'],'G'); a('globe_golden_meridian',1,4,['globe_gilded_axis'],'G'); a('globe_mastery',0,5,['globe_gilded_axis'],'G')
# Pins (right)
prev='root'
for k in range(1,13):
    x=k+1
    a(f'pin_{k:02}',x,0,[prev],'P'); a(f'pin_{k:02}_durability',x,1,[f'pin_{k:02}'],'P'); a(f'pin_{k:02}_payout',x,-1,[f'pin_{k:02}'],'P'); prev=f'pin_{k:02}'
a('pin_mastery',14,0,['pin_12'],'P')
a('shooter_cooldown',3,-2,['pin_01'],'P'); a('pin_autoloader',3,-3,['shooter_cooldown'],'P')
a('pin_land_chance',4,-2,['pin_02'],'P'); a('pin_city_chance',5,-2,['pin_land_chance'],'P'); a('pin_steel_tips',6,-2,['pin_city_chance'],'P'); a('pin_twin_barrel',7,-2,['pin_steel_tips'],'P')
# Case (left)
a('case_payout',-1,0,['root'],'C'); a('case_cooldown',-2,0.5,['case_payout'],'C'); a('case_autostamp',-2,-0.5,['case_payout'],'C')
a('case_cold_cases',-3,0.5,['case_cooldown'],'C'); a('case_press',-3,-0.5,['case_autostamp'],'C'); a('case_mastery',-4,0.5,['case_cold_cases'],'C')
# Whiskey (up-left)
a('whiskey_duration',-2,2,['root'],'W'); a('whiskey_cooldown',-3,2.5,['whiskey_duration'],'W'); a('whiskey_strength',-3,1.5,['whiskey_duration'],'W')
a('whiskey_bottomless',-4,2.5,['whiskey_cooldown'],'W'); a('whiskey_mastery',-4,1.5,['whiskey_strength'],'W')
# Statues (down)
a('statue_gold_fill',0,-1,['root'],'S'); a('statue_payout',0,-2,['statue_gold_fill'],'S'); a('statue_fast_casting',1,-2.5,['statue_payout'],'S'); a('statue_mastery',0,-3,['statue_payout'],'S')
# Lootbox (down-left)
a('loot_bulk',-2,-2,['root'],'L'); a('loot_luck',-3,-2,['loot_bulk'],'L'); a('loot_double',-4,-1.5,['loot_luck'],'L'); a('loot_mastery',-4,-2.5,['loot_luck'],'L')
ids=list(N)
bad=[]
for i in range(len(ids)):
    for j in range(i+1,len(ids)):
        p,q=N[ids[i]],N[ids[j]]
        d=math.hypot(p[0]-q[0],p[1]-q[1])
        if d<0.99: bad.append(('close',ids[i],ids[j],round(d,2)))
def segdist(px,py,ax,ay,bx,by):
    dx,dy=bx-ax,by-ay; t=max(0,min(1,((px-ax)*dx+(py-ay)*dy)/(dx*dx+dy*dy))); return math.hypot(px-ax-t*dx,py-ay-t*dy)
for c,(x,y,ps,b) in N.items():
    for p in ps:
        ax,ay=N[p][0],N[p][1]
        for o,(ox,oy,_,_) in N.items():
            if o in (c,p): continue
            d=segdist(ox,oy,ax,ay,x,y)
            if d<0.4: bad.append(('edge',p,c,'near',o,round(d,2)))
print(len(N),'nodes'); print('\n'.join(map(str,bad)) or 'OK')
json.dump({k:[v[0],v[1],v[2],v[3]] for k,v in N.items()},open('layout.json','w'))
