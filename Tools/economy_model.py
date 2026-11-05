"""Approximate active-play model driven by Unity's exported player assets.

Pin lifetimes and landing bonuses are averaged. Manual input, whiskey and stamp
animations are approximations. This predicts pacing, not actual completion time.
"""
from functools import lru_cache
import json
import math
from pathlib import Path
import random

SNAPSHOT = Path(__file__).with_name('economy_snapshot.json')


def cost(curve, rank):
    explicit = curve.get('explicitCosts') or []
    return math.ceil(explicit[rank] if rank < len(explicit)
                     else curve['baseCost'] * curve['growth'] ** rank)


class Economy:
    def __init__(self, data=None, activity=.5, loot=True):
        self.data = data or json.loads(SNAPSHOT.read_text(encoding='utf-8-sig'))
        self.b = self.data['balance']
        self.nodes = self.data['skills']
        self.index = {n['id']: i for i, n in enumerate(self.nodes)}
        self.shop = {n['id']: n for n in self.data['shop']}
        self.base = {s['stat']: s['value'] for s in self.data['bases']}
        self.activity, self.loot = activity, loot

    @lru_cache(maxsize=8192)
    def sheet(self, ranks, shop_ranks, prestiges):
        add, pct, mul = {}, {}, {}
        def apply(m, rank):
            key = (m['stat'], m['pinIndex'])
            if m['kind'] == 2:
                mul[key] = mul.get(key, 1) * m['valuePerRank'] ** rank
            else:
                dst = add if m['kind'] == 0 else pct
                dst[key] = dst.get(key, 0) + m['valuePerRank'] * rank
        for n, rank in zip(self.nodes, ranks):
            if rank:
                for m in n['modifiers'] or []:
                    apply(m, rank)
        for key, rank in shop_ranks:
            for m in self.shop[key]['modifiers'] or []:
                apply(m, rank)
        bonus = max((r['incomeBonus'] for r in self.b['ranks'] if r['retirements'] <= prestiges), default=0)
        pct[105, -1] = pct.get((105, -1), 0) + bonus
        def stat(s, pin=-1):
            keys = [(s, -1)] + ([(s, pin)] if pin >= 0 else [])
            return ((self.base.get(s, 0) + sum(add.get(k, 0) for k in keys))
                    * max(.05, 1 + sum(pct.get(k, 0) for k in keys))
                    * math.prod(mul.get(k, 1) for k in keys))
        return ({s: stat(s) for s in self.base},
                tuple(max(1, round(stat(210, p))) for p in range(12)),
                tuple(stat(211, p) for p in range(12)))

    def rates(self, ranks, shop_ranks, prestiges, level):
        s, durability, payout = self.sheet(ranks, shop_ranks, prestiges)
        b = self.b
        speed = max(min(s[100], s[101]), min(1, self.activity * s[102] ** .4) * s[100])
        uptime = s[400] / (s[400] + s[401] + 10)
        boost = 1 + (s[402] - 1) * uptime
        turns = speed / 360
        scale = s[104] * s[105] * boost
        globe = b['revolutionReward'] * turns * scale
        pins = min(12, round(s[200]))
        life = sum(b['pinFlightSeconds'] + (durability[p] - .5) / max(turns, 1e-9)
                   for p in range(pins)) / max(1, pins)
        shots = (1 / s[201] if s[220] >= 1 else min(3, 1 / s[201]) * .7)
        launches = min(shots * (1 + s[221]), pins / max(life, 1e-9))
        pin_income = launches * sum(durability[p] * payout[p] for p in range(pins)) / max(1, pins)
        pin_income *= b['pinBasePayout'] * s[105] * boost
        need = max(1, b['caseBaseEvidence'] + (level // max(1, b['caseRepeatCount'])) * b['caseEvidencePerGroup'] - round(s[303]))
        requirement, remaining = 0, level
        while requirement < b['caseMaxDurabilityRequirement']:
            length = max(1, b['caseBaseTierLength']) * (requirement + 1)
            if remaining < length:
                break
            remaining -= length
            requirement += 1
        eligible = sum(ranks[self.index[f'pin_{p+1:02}_durability']] >= requirement for p in range(pins)) / max(1, pins)
        evidence = launches * eligible
        reward = max(b['caseBaseReward'] * (1 + level * b['caseRewardGrowthPerLevel']) * s[105], b['caseIncomeSeconds'] * (globe + pin_income)) * s[300]
        wait = max(s[301], 2.8) + 1.5 * (1 - min(1, s[302]))
        return globe + pin_income, reward / (need / max(evidence, 1e-9) + wait), evidence, need, reward, turns, s

    def candidates(self, ranks, shop, s):
        for i, n in enumerate(self.nodes):
            rank = ranks[i]
            if rank >= n['maxRank'] or n['currency'] != 0:
                continue
            if n.get('requiresUnlock') and not shop.get(n['requiresUnlock']):
                continue
            if any(ranks[self.index[p]] < 1 for p in n['prerequisites'] or []):
                continue
            if n['capStat']:
                delta = sum(m['valuePerRank'] for m in n['modifiers'] if m['stat'] == n['capStat'])
                if s[n['capStat']] + delta > s[n['capByStat']] + 1e-9:
                    continue
            yield i, cost(n['cost'], rank)

    def run(self, shop=None, prestiges=0, cap_minutes=120, dt=2, retire_multiple=1,
            statues=None, seed=7):
        shop = dict(shop or {})
        shop_key = tuple(sorted(shop.items()))
        ranks = [0] * len(self.nodes)
        if shop.get('start_ledger'):
            ranks[self.index['root']] = 1
            if shop.get('start_motor'):
                ranks[self.index['globe_auto']] = self.data['startingMotorRanks']
            if shop.get('start_pin'):
                ranks[self.index['pin_01']] = 1
        money = self.b['startingMoney'] + self.data['seedMoney'][shop.get('start_money', 0)]
        earned = gold = evidence = cooldown = loot_timer = 0.
        level = opened = 0
        statues = [dict(pieces=s['pieces'], fill=s['fill']) for s in (statues or [dict(pieces=0, fill=0.) for _ in range(3)])]
        milestones = {}
        rng = random.Random(seed)
        for elapsed in range(0, int(cap_minutes * 60), dt):
            key = tuple(ranks)
            inc, case_inc, falls, need, reward, turns, s = self.rates(key, shop_key, prestiges, level)
            money += inc * dt
            earned += inc * dt
            if cooldown > 0:
                cooldown = max(0, cooldown - dt)
            else:
                evidence += falls * dt
                if evidence >= need:
                    money += reward
                    earned += reward
                    gold += min(self.b['caseGoldChanceCap'], s[305]) * round(s[304] * (1 + level / self.b['caseGoldLevelDivisor']))
                    level += 1
                    evidence = 0
                    cooldown = max(s[301], 2.8) + 1.5 * (1 - min(1, s[302]))
            requirements, incomplete = [], []
            for i, statue in enumerate(statues):
                tier = shop.get(f'statue_tier_{i+1}', 0)
                required = max(2, self.b['statueBasePieces'] + i * self.b['statuePiecesPerIndex']
                               + tier * self.b['statueTierExtraPieces'] - round(s[502]))
                requirements.append(required)
                if not shop.get(f'unlock_statue_{i+1}'):
                    continue
                if statue['pieces'] < required:
                    incomplete.append(i)
                else:
                    statue['fill'] += turns * dt * self.b['statueGoldFillPerRevolution'] * s[500]
                    if statue['fill'] >= 1:
                        gold += self.b['statueBaseGold'] * (i + 1) * self.b['statueTierGoldMultipliers'][tier] * s[501]
                        statue.update(pieces=0, fill=0.)
            loot_timer -= dt
            price = math.ceil(cost(self.b['lootboxCost'], opened) * s[600])
            if self.loot and loot_timer <= 0 and price <= money * .05:
                money -= price
                opened += 1
                loot_timer = 3
                entries = [(e['kind'], e['weight'] * (1 + s[601] if e['kind'] in (1, 2) else 1))
                           for e in self.b['lootTable'] if e['kind'] != 2 or incomplete]
                for _ in range(1 + int(rng.random() < s[602])):
                    kind = rng.choices([k for k, _ in entries], [w for _, w in entries])[0]
                    if kind == 1:
                        gold += round(rng.uniform(self.b['lootGoldMin'], self.b['lootGoldMax']))
                    elif kind == 2 and incomplete:
                        index = incomplete[0]
                        statues[index]['pieces'] += 1
                        if statues[index]['pieces'] >= requirements[index]:
                            incomplete.pop(0)
                    elif kind in (2, 4):
                        amount = math.ceil(price * (rng.uniform(2, 4) if rng.random() < self.b['moneyJackpotChance'] else rng.uniform(.3, .8)))
                        money += amount
                        earned += amount
                    elif kind == 3:
                        amount = self.b['paperBallReward'] * s[104] * s[105]
                        money += amount
                        earned += amount
            if earned >= self.b['prestigeThreshold']:
                milestones.setdefault('retire_available', elapsed + dt)
            if earned >= self.b['prestigeThreshold'] * retire_multiple:
                prestige_gold = math.floor(self.b['prestigeGoldScale'] * (earned / self.b['prestigeThreshold']) ** self.b['prestigeExponent'])
                return dict(seconds=elapsed+dt, earned=earned, gold=gold+prestige_gold,
                            milestones=milestones, statues=statues, reached=True, ranks=ranks)
            best, best_value = None, -1
            for i, price in self.candidates(ranks, shop, s):
                if price > money:
                    continue
                ranks[i] += 1
                ni, nc, *_ = self.rates(tuple(ranks), shop_key, prestiges, level)
                ranks[i] -= 1
                value = max(1e-9, (ni + nc) - (inc + case_inc)) / max(1, price)
                if self.nodes[i]['id'].startswith('pin_') and self.nodes[i]['id'].count('_') == 1:
                    value *= 1.5
                if value > best_value:
                    best, best_value = (i, price), value
            if best:
                i, price = best
                money -= price
                ranks[i] += 1
                node_id = self.nodes[i]['id']
                milestones.setdefault(node_id, elapsed + dt)
                if ranks[i] == self.nodes[i]['maxRank']:
                    milestones[node_id + '_max'] = elapsed + dt
        return dict(seconds=cap_minutes*60, earned=earned, gold=gold, milestones=milestones,
                    statues=statues, reached=False, ranks=ranks)
