"""Behavioral economy experiments; never writes Unity assets or player saves.

Simulates releases, friction, individual busy pins, payouts, case eligibility,
stamping and drink cycles. Purchase policies save toward a chosen upgrade rather
than spending only on whichever upgrade happens to be affordable. These are
synthetic players, not measured human completion times.
"""
import argparse
from collections import defaultdict, deque
import copy
import csv
import json
import math
from pathlib import Path
import random

from economy_model import Economy, cost


PROFILES = {
    'mixed': dict(weights={1: 1, 2: 1, 3: 1, 4: .6}),
    'pins': dict(weights={1: .6, 2: 3, 3: 1.7, 4: .5}),
    'spin': dict(weights={1: 3, 2: .7, 3: .7, 4: .6}),
    'automation': dict(weights={1: 1.5, 2: 1, 3: .7, 4: .5}),
}

INPUTS = {
    'steady': dict(swipe_interval=1.1, swipe_pixels=180, swipe_seconds=.3, manual_fire=.8),
    'active': dict(swipe_interval=.8, swipe_pixels=200, swipe_seconds=.22, manual_fire=.6),
    'relaxed': dict(swipe_interval=2, swipe_pixels=150, swipe_seconds=.4, manual_fire=1.5),
}


def case_strength(balance, level):
    remaining, required = level, 0
    while required < balance['caseMaxDurabilityRequirement']:
        length = max(1, balance['caseBaseTierLength']) * (required + 1)
        if remaining < length:
            break
        remaining -= length
        required += 1
    return required


def integrate(velocity, seconds, friction, floor, ceiling):
    """Same baseline-plus-exponential-excess integration as GameplayMath."""
    floor = max(0, min(floor, ceiling))
    velocity = max(floor, min(ceiling, velocity))
    drag = -60 * math.log(max(.000001, min(1, friction)))
    if drag < 1e-10:
        return velocity, velocity * seconds
    excess = velocity - floor
    decay = math.exp(-drag * seconds)
    result = floor + excess * decay
    return (0 if floor == 0 and result < .005 else result,
            floor * seconds + excess * (1 - decay) / drag)


class Playtest:
    def __init__(self, data, profile='mixed', seed=7, step=.05, idle=False,
                 input_style='steady', idle_every=0, idle_seconds=0, quantity=10, shop=None):
        if step <= 0:
            raise ValueError('step must be positive')
        self.model = Economy(data, loot=False)
        self.b = data['balance']
        self.profile = dict(PROFILES[profile], **INPUTS[input_style])
        self.input_style, self.seed = input_style, seed
        self.idle_every, self.idle_seconds = idle_every, idle_seconds
        self.quantity = quantity
        self.shop = dict(shop or {})
        self.name, self.rng, self.step, self.idle = profile, random.Random(seed), step, idle
        self.ranks = [0] * len(self.model.nodes)
        self.money = self.b['startingMoney']
        self.earned = self.phase = self.velocity = self.time = 0.
        self.s = {}
        self.passes, self.pay = (), ()
        self.stats()
        self.pins = {}  # index -> [landing time, next crossing phase, remaining passes]
        self.next_pin = 0
        self.reload = self.next_swipe = self.next_manual = 0.
        self.boost_end = self.drink_ready = self.menu_until = 0.
        self.level = self.evidence = 0
        self.case_ready = self.stamp_at = self.swap_until = 0.
        self.stamp_automatic = False
        self.sources = defaultdict(float)
        self.events = defaultdict(int)
        self.window_recurring = self.income_rate = self.next_sample = 0.
        self.history, self.samples = [], []
        self.receipts = deque()
        self.target = None
        self.target_since = 0.
        self.next_decision = 0.
        self.retirement = None

    def stats(self):
        self.s, self.passes, self.pay = self.model.sheet(tuple(self.ranks), tuple(sorted(self.shop.items())), 0)

    def rank(self, node_id):
        return self.ranks[self.model.index[node_id]]

    def recurring(self, amount, source):
        self.credit(amount, source)
        self.window_recurring += amount

    def credit(self, amount, source):
        self.money += amount
        self.earned += amount
        self.sources[source] += amount
        self.events[source] += 1
        self.receipts.append((self.time, amount))

    def measured_income(self):
        while self.receipts and self.receipts[0][0] < self.time - 60:
            self.receipts.popleft()
        return sum(amount for _, amount in self.receipts) / max(self.step, min(60, self.time))

    def rotation_limits(self, boosted):
        maximum = self.s[100] * (self.s[402] if boosted and self.shop.get('unlock_whiskey') else 1)
        floor = min(maximum, max(self.s[101], maximum * min(1, self.s.get(106, 0))))
        return maximum, floor

    def expected_income(self, ranks):
        """Policy heuristic using this input preset's repeating swipes and both boost phases."""
        s, passes, pay = self.model.sheet(tuple(ranks), tuple(sorted(self.shop.items())), 0)
        b, p = self.b, self.profile
        values = []
        for buff in (1, s[402]):
            maximum = s[100]*buff
            floor = min(maximum, max(s[101], maximum*min(1, s.get(106, 0))))
            interval = p['swipe_interval']
            pixels = p['swipe_pixels']
            impulse = (min(pixels, b['dragFullPixels'])*b['dragAcceleration']
                       * min(b['dragMaxSpeedFactor'], pixels/p['swipe_seconds']/b['dragReferencePixelsPerSecond'])
                       * s[102]*max(1, maximum/b['impulseReferenceSpeed'])) if pixels >= b['dragMinPixels'] else 0
            impulse = min(impulse, maximum*b['dragMaxBoostFraction'])
            friction = b['frictionAt60Hz']**s.get(107, 1)
            decay = friction**(60*interval)
            peak = min(maximum, floor+impulse/max(1e-9, 1-decay))
            _, angle = integrate(peak, interval, friction, floor, maximum)
            turns = angle/interval/360
            count = min(12, round(s[200]))
            life = sum(b['pinFlightSeconds']+(passes[i]-.5)/max(1e-9, turns) for i in range(count))/max(1, count)
            shots = 1/s[201] if s[220] >= 1 else min(1/s[201], 1/p['manual_fire'])
            launches = min(shots, count/max(1e-9, life))
            recurring = b['revolutionReward']*turns*s[104]*s[105]*buff
            recurring += launches*sum(passes[i]*pay[i] for i in range(count))/max(1, count)*b['pinBasePayout']*s[105]*buff
            cases = 0
            if s.get(306, 0) >= 1:
                required_strength = case_strength(b, self.level)
                eligible = sum(ranks[self.model.index[f'pin_{i+1:02}_durability']] >= required_strength for i in range(count))/max(1, count)
                evidence = launches*eligible
                if evidence > 0:
                    need = max(1, b['caseBaseEvidence']+self.level//max(1, b['caseRepeatCount'])*b['caseEvidencePerGroup']-round(s[303]))
                    reward = max(b['caseBaseReward']*(1+self.level*b['caseRewardGrowthPerLevel'])*s[105], b['caseIncomeSeconds']*recurring)*s[300]
                    cases = reward/(need/evidence+max(2.8, s[301])+1.5*(1-min(1, s[302])))
            values.append(recurring+cases)
        uptime = s[400]/(s[400]+s[401]+10) if self.shop.get('unlock_whiskey') else 0
        return values[0]*(1-uptime)+values[1]*uptime

    def choose_target(self):
        # Expected-rate scoring is a purchasing heuristic, not the simulation's
        # income source. Convenience has value even when an active player could
        # perform the same action manually.
        base_income = self.expected_income(self.ranks)
        best, best_score = None, -1
        for i, price in self.model.candidates(self.ranks, self.shop, self.s):
            node = self.model.nodes[i]
            if node['maxRank'] >= 999:
                continue  # Optional mastery is not a first-run completion target.
            if node['branch'] not in (0, 1, 2, 3, 4):
                continue  # Loot/statue value needs gold/shop behavior, out of scope.
            self.ranks[i] += 1
            income = self.expected_income(self.ranks)
            self.ranks[i] -= 1
            benefit = max(0, income - base_income)
            if node['id'] == 'root':
                benefit += 1
            elif node['id'] == 'case_unlock':
                benefit += .15
            elif node['id'] == 'globe_auto':
                benefit += (max(.2, base_income) * .5 if self.name == 'automation' and self.s[101] < self.s[100]
                            else base_income * .025)
            elif node['id'] == 'pin_autoloader':
                benefit += base_income * (2 if self.name == 'automation' else .2)
            elif node['id'].startswith('pin_') and node['id'].count('_') == 1:
                # A new, unupgraded pin can initially dilute a strong pin's
                # round-robin payouts. It also unlocks its own Pay/Strength.
                benefit += max(.1, base_income) * .04
            elif node['id'] == 'case_autostamp':
                benefit += max(.001, base_income * .0025)
            if benefit <= 0:
                continue
            score = benefit * self.profile['weights'].get(node['branch'], 1) / max(1, price)
            if score > best_score:
                best, best_score = i, score
        self.target, self.target_since = best, self.time

    def buy(self):
        if self.target is None:
            return
        i = self.target
        node = self.model.nodes[i]
        price = cost(node['cost'], self.ranks[i])
        if self.money + 1e-9 < price:
            return
        count = 1
        while count < min(self.quantity, node['maxRank']-self.ranks[i]):
            next_cost = price + cost(node['cost'], self.ranks[i]+count)
            if next_cost > self.money + 1e-9:
                break
            if node['capStat']:
                self.ranks[i] += count + 1
                preview = self.model.sheet(tuple(self.ranks), (), 0)[0]
                self.ranks[i] -= count + 1
                if preview[node['capStat']] > self.s[node['capByStat']] + 1e-9:
                    break
            price, count = next_cost, count + 1
        self.money -= price
        self.ranks[i] += count
        measured = self.measured_income()
        self.history.append(dict(seconds=round(self.time, 2), id=node['id'], rank=self.ranks[i], levels=count,
                                 price=price, saving_seconds=round(self.time-self.target_since, 2),
                                 earned=round(self.earned, 2), recurring_per_second_before=self.income_rate,
                                 total_per_second_before=measured,
                                 seconds_from_zero=price/measured if measured > 0 else None))
        self.stats()
        self.menu_until = self.time + 1.2
        self.target = None
        self.next_decision = self.time + 2

    def tick(self, purchasing=True):
        dt, t = self.step, self.time
        s, b = self.s, self.b
        taking_break = self.idle_every > 0 and t % self.idle_every < self.idle_seconds and t >= self.idle_every
        active = not self.idle and not taking_break and t >= self.menu_until
        if self.shop.get('unlock_whiskey') and active and t >= self.drink_ready and t >= self.boost_end:
            # Fill/drink attention cost is an explicit behavioral assumption.
            self.boost_end = t + s[400]
            self.drink_ready = self.boost_end + s[401] + 10
        boosted = bool(self.shop.get('unlock_whiskey')) and t < self.boost_end
        buff = s[402] if boosted else 1
        maximum, floor = self.rotation_limits(boosted)
        if active and t >= self.next_swipe:
            pixels = self.profile['swipe_pixels']
            if pixels >= b['dragMinPixels']:
                distance = min(pixels, max(b['dragMinPixels'], b['dragFullPixels']))
                factor = min(b['dragMaxSpeedFactor'], pixels/self.profile['swipe_seconds']/b['dragReferencePixelsPerSecond'])
                impulse = distance*b['dragAcceleration']*factor*s[102]*max(1, maximum/b['impulseReferenceSpeed'])
                self.velocity = min(maximum, max(floor, self.velocity)+min(impulse, maximum*b['dragMaxBoostFraction']))
            self.next_swipe = t + self.profile['swipe_interval']
        self.velocity, angle = integrate(self.velocity, dt, b['frictionAt60Hz']**s.get(107, 1), floor, maximum)
        previous = self.phase
        self.phase += angle
        turns = math.floor(self.phase/360)-math.floor(previous/360)
        for _ in range(turns):
            self.recurring(b['revolutionReward']*s[104]*s[105]*buff, 'spin')
        for index in list(self.pins):
            pin = self.pins[index]
            if pin[0] > t:
                continue
            if pin[1] is None:
                pin[1] = self.phase + self.rng.random()*360
                pin[2] = self.passes[index]
            while self.phase >= pin[1] and pin[2] > 0:
                self.recurring(b['pinBasePayout']*self.pay[index]*s[105]*buff, 'pins')
                pin[2] -= 1
                pin[1] += 360
            if pin[2] == 0:
                del self.pins[index]
                if s.get(306, 0) >= 1 and t >= self.case_ready and self.evidence < self.required():
                    if self.rank(f'pin_{index+1:02}_durability') >= case_strength(b, self.level):
                        self.evidence += 1
                        if self.evidence >= self.required():
                            automatic = self.rng.random() < s[302]
                            self.stamp_automatic = automatic
                            self.stamp_at = max(t, self.swap_until) + (.73 if automatic else 1.5)
        if self.evidence >= self.required() and self.stamp_at > 0 and t >= self.stamp_at and s.get(306, 0) >= 1 and (active or self.stamp_automatic):
            reward = max(b['caseBaseReward']*(1+self.level*b['caseRewardGrowthPerLevel'])*s[105], b['caseIncomeSeconds']*self.income_rate)*s[300]
            self.credit(reward, 'cases')
            self.level += 1
            self.evidence = 0
            self.stamp_at = 0
            self.case_ready = t + s[301]
            self.swap_until = t + 2.8
        self.reload -= dt
        auto = s[220] >= 1
        attempt = auto or active and t >= self.next_manual
        if attempt:
            if not auto:
                self.next_manual = t + self.profile['manual_fire']
            count = min(12, round(s[200]))
            for _ in range(12 if auto else 1):
                if self.reload > 1e-9 or not count:
                    break
                ready = next(((self.next_pin+j) % count for j in range(count)
                              if (self.next_pin+j) % count not in self.pins), None)
                if ready is None:
                    break
                self.next_pin = (ready+1) % count
                self.pins[ready] = [t+b['pinFlightSeconds'], None, 0]
                self.reload += s[201]
                self.events['shots'] += 1
            self.reload = max(0, self.reload)
        else:
            self.reload = max(0, self.reload)
        if t >= self.next_sample:
            sample = self.window_recurring  # Samples occur once per simulated second.
            self.income_rate = sample if self.income_rate <= 0 else self.income_rate+(sample-self.income_rate)/30
            self.window_recurring = 0
            self.next_sample += 1
        if purchasing and active and t >= self.next_decision:
            if self.target is None:
                self.choose_target()
            self.buy()
        self.time = round(t + dt, 8)

    def required(self):
        return max(1, self.b['caseBaseEvidence'] + self.level//max(1, self.b['caseRepeatCount'])*self.b['caseEvidencePerGroup']-round(self.s[303]))

    def run(self, minutes):
        next_report = 60
        while self.time < minutes*60:
            self.tick()
            if self.retirement is None and self.earned >= self.b['prestigeThreshold']:
                ordinary = [n for n in self.model.nodes if n['maxRank'] < 999 and not n.get('requiresUnlock')]
                self.retirement = dict(seconds=round(self.time, 2), earned=round(self.earned, 2),
                                       levels=sum(self.rank(n['id']) for n in ordinary),
                                       total_levels=sum(n['maxRank'] for n in ordinary),
                                       pins=round(self.s[200]))
            if self.time >= next_report:
                self.samples.append(dict(seconds=round(self.time, 2), earned=round(self.earned, 2),
                                         wallet=round(self.money, 2), recurring_per_second=round(self.income_rate, 3),
                                         total_per_second=round(self.measured_income(), 3),
                                         rpm=round(self.velocity/6, 2), completed_cases=self.level,
                                         ranks={n['id']: r for n, r in zip(self.model.nodes, self.ranks) if r}))
                next_report += 60
        return dict(profile=self.name, input_style=self.input_style, seed=self.seed,
                    minutes=minutes, earned=round(self.earned, 2),
                    retirement=self.retirement,
                    sources=dict(self.sources), events=dict(self.events),
                    purchases=self.history, samples=self.samples)


def build_states():
    """Illustrative builds, independent of a forced buying order."""
    states = [
        ('First pin + folder', dict(root=1, pin_01=1, case_unlock=1)),
        ('Early mixed build', dict(root=1, pin_01=1, case_unlock=1, shooter_cooldown=3, globe_max_speed=5, globe_auto=5, globe_multiplier=3)),
        ('Mid build', dict(root=1, case_unlock=1, globe_auto=25, globe_max_speed=30, globe_multiplier=12, shooter_cooldown=15, pin_autoloader=1, case_payout=5, case_cooldown=5)),
        ('Late build', dict(root=1, case_unlock=1, globe_auto=90, globe_max_speed=95, globe_multiplier=25, globe_gilded_axis=1, shooter_cooldown=34, pin_autoloader=1, case_payout=10, case_cooldown=10, case_cold_cases=3, whiskey_duration=10, whiskey_cooldown=10, whiskey_strength=5)),
    ]
    for label, owned in states:
        pins = 1 if label.startswith(('First', 'Early')) else 4 if label.startswith('Mid') else 12
        for p in range(1, pins+1):
            owned[f'pin_{p:02}'] = 1
            owned[f'pin_{p:02}_durability'] = 0 if pins == 1 else 2 if pins == 4 else 5
            owned[f'pin_{p:02}_payout'] = 0 if pins == 1 else 3 if pins == 4 else 10
    return states


def checkpoints(model, step=.05):
    rows = []
    for label, owned in build_states():
        for boosted in (False, True):
            test = Playtest(model.data, 'mixed', step=step, shop={'unlock_whiskey': 1} if boosted else None)
            test.ranks = [owned.get(n['id'], 0) for n in model.nodes]
            test.stats()
            # First 60 seconds are warmup, then a five-minute measurement.
            test.boost_end = 1e9 if boosted else 0
            test.drink_ready = 1e9
            for _ in range(round(60/test.step)):
                test.tick(purchasing=False)
            initial = dict(test.sources)
            test.events.clear()
            for _ in range(round(300/test.step)):
                test.tick(purchasing=False)
            per_second = {key: (test.sources[key]-initial.get(key, 0))/300 for key in ('spin', 'pins', 'cases')}
            total = sum(per_second.values())
            price = cost(model.nodes[model.index['globe_auto']]['cost'], owned.get('globe_auto', 0))
            rows.append(dict(build=label, boosted=boosted, income=per_second, total_per_second=total,
                             spin_payment=model.b['revolutionReward']*test.s[104]*test.s[105]*(test.s[402] if boosted else 1),
                             pin_payment=model.b['pinBasePayout']*test.pay[0]*test.s[105]*(test.s[402] if boosted else 1),
                             next_auto_price=price, next_auto_seconds_from_zero=price/total if total else None,
                             ten_thousand_seconds_from_zero=10000/total if total else None))
    return rows


def write_html(path, payload):
    """Self-contained report; all user-facing data comes from these experiments."""
    document = '''<!doctype html><meta charset="utf-8"><title>Economy playtest</title>
<style>body{font:16px system-ui;background:#151913;color:#eee;max-width:1100px;margin:32px auto;padding:0 20px}select{padding:8px;margin:8px;background:#283025;color:white}table{border-collapse:collapse;width:100%;font-size:14px}td,th{text-align:left;padding:8px;border-bottom:1px solid #3a4335}small{color:#b8c5b0}svg{width:100%;background:#20261d}h1{color:#d5bc79}a{color:#d5bc79}</style>
<h1>Income and upgrade waiting time</h1><p id="variant"></p>
<p>Compare the same mouse behavior across spending styles. Large differences identify paths that need a real playtest.</p>
<label>Spending <select id="profile"></select></label><label>Input <select id="input"></select></label><label>Seed <select id="seed"></select></label>
<p id="summary"></p><svg id="chart" viewBox="0 0 1000 240" aria-label="Cumulative earnings over time"></svg>
<p><small>Gold/shop, loot and prestige decisions excluded. Mouse gestures, menu attention, drinking and stamping are assumptions. Synthetic results are not measured completion times.</small></p>
<h2>Purchases</h2><p>Saving time is the actual time spent pursuing a selected purchase. From-zero time is price divided by measured total income over the preceding minute, including cases. They can differ because of wallet leftovers and burst rewards.</p>
<table><thead><tr><th>Minute</th><th>Upgrade</th><th>New rank</th><th>Levels bought</th><th>Price</th><th>Income / second</th><th>From-zero seconds</th><th>Saving seconds</th></tr></thead><tbody id="purchases"></tbody></table>
<h2>Illustrative builds</h2><p>These are independently constructed builds, not the path above. Sustained boost rows test peak sensitivity; the boost cannot normally stay active permanently.</p>
<table><thead><tr><th>Build</th><th>Boost</th><th>Spin / s</th><th>Pins / s</th><th>Cases / s</th><th>Total / s</th><th>Seconds for 10k</th></tr></thead><tbody id="builds"></tbody></table>
<script>const data=PAYLOAD;
const el=id=>document.getElementById(id), fmt=n=>Number(n).toLocaleString(undefined,{maximumFractionDigits:1});
for(const [id,values] of [['profile',Object.keys(data.profiles)],['input',[...new Set(data.runs.map(r=>r.input_style))]],['seed',[...new Set(data.runs.map(r=>r.seed))]]]){el(id).innerHTML=values.map(v=>`<option>${v}</option>`).join('');el(id).onchange=render;}
function render(){const r=data.runs.find(r=>r.profile===el('profile').value&&r.input_style===el('input').value&&r.seed===Number(el('seed').value));if(!r)return;
el('summary').textContent=`${r.minutes} minutes: EUR ${fmt(r.earned)} earned, ${r.purchases.length} purchases. `+(r.retirement?`Retirement available at ${fmt(r.retirement.seconds/60)} minutes with ${r.retirement.levels}/${r.retirement.total_levels} ordinary levels and ${r.retirement.pins} pins. The timeline continues without resetting. `:'Retirement not reached. ')+`Attention breaks: ${data.idle_seconds}s every ${data.idle_every}s (zero interval disables).`;
const points=r.samples,maxY=Math.max(1,...points.map(p=>p.earned)),maxX=r.minutes*60;
el('chart').innerHTML=`<text x="45" y="22" fill="#bcc8b5">Total earned: EUR ${fmt(maxY)}</text><path d="M45 30V210H980" stroke="#75836b" fill="none"/><polyline points="45,210 ${points.map(p=>`${45+p.seconds/maxX*930},${210-p.earned/maxY*175}`).join(' ')}" fill="none" stroke="#d5bc79" stroke-width="3"/><text x="45" y="234" fill="#bcc8b5">0 minutes</text><text x="900" y="234" fill="#bcc8b5">${r.minutes} minutes</text>`;
el('purchases').innerHTML=r.purchases.map(p=>`<tr><td>${fmt(p.seconds/60)}</td><td>${p.id}</td><td>${p.rank}</td><td>${p.levels||1}</td><td>${fmt(p.price)}</td><td>${fmt(p.total_per_second_before)}</td><td>${p.seconds_from_zero==null?'—':fmt(p.seconds_from_zero)}</td><td>${fmt(p.saving_seconds)}</td></tr>`).join('');}
el('variant').textContent=(data.price_experiment?`Hypothetical price experiment: final globe speed upgrade approximately EUR ${fmt(data.price_experiment)}. `:'Current exported prices. ')+'Synthetic players; no gameplay assets changed.';
el('builds').innerHTML=data.checkpoints.map(c=>`<tr><td>${c.build}</td><td>${c.boosted?'On':'Off'}</td><td>${fmt(c.income.spin)}</td><td>${fmt(c.income.pins)}</td><td>${fmt(c.income.cases)}</td><td>${fmt(c.total_per_second)}</td><td>${fmt(c.ten_thousand_seconds_from_zero)}</td></tr>`).join('');render();</script>'''
    path.write_text(document.replace('PAYLOAD', json.dumps(payload)), encoding='utf-8')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--minutes', type=float, default=40)
    parser.add_argument('--step', type=float, default=.05)
    parser.add_argument('--snapshot', type=Path, help='Optional proposed snapshot; never edits the source')
    parser.add_argument('--inputs', nargs='+', choices=INPUTS, default=list(INPUTS))
    parser.add_argument('--seeds', nargs='+', type=int, default=[7, 19, 41])
    parser.add_argument('--idle-every', type=float, default=180, help='Seconds between attention breaks; 0 disables')
    parser.add_argument('--idle-seconds', type=float, default=30)
    parser.add_argument('--globe-endpoint', type=float, help='In-memory experiment: final Auto Spin and Spin Limit rank price')
    parser.add_argument('--quantity', type=int, choices=[1, 5, 10], default=10)
    parser.add_argument('--output', type=Path, default=Path('SpinTheGlobe/Logs/economy-playtest'))
    args = parser.parse_args()
    model = Economy(json.loads(args.snapshot.read_text(encoding='utf-8-sig')) if args.snapshot else None)
    if args.globe_endpoint:
        for node_id in ('globe_auto', 'globe_max_speed'):
            node = model.nodes[model.index[node_id]]
            node['cost']['growth'] = (args.globe_endpoint/node['cost']['baseCost'])**(1/(node['maxRank']-1))
        print(f'IN-MEMORY PRICE EXPERIMENT: last globe speed upgrade approximately EUR {args.globe_endpoint:,.0f}', flush=True)
    args.output.mkdir(parents=True, exist_ok=True)
    (args.output/'snapshot-used.json').write_text(json.dumps(model.data, indent=2), encoding='utf-8')
    results = []
    for name in PROFILES:
        for input_style in args.inputs:
            group = []
            for seed in args.seeds:
                result = Playtest(copy.deepcopy(model.data), name, input_style=input_style,
                                  seed=seed, step=args.step, idle_every=args.idle_every,
                                  idle_seconds=args.idle_seconds, quantity=args.quantity).run(args.minutes)
                results.append(result)
                group.append(result['earned'])
            print(f'{name:10} {input_style:8}: {args.minutes:g} min, EUR {min(group):,.0f}-{max(group):,.0f} earned', flush=True)
    audit = checkpoints(model, step=args.step)
    payload = dict(assumptions='Synthetic players; no gold/shop, loot spending or prestige. Case animations and menu attention approximate. Boost checkpoint is sustained sensitivity, not achievable permanent uptime. No player saves or Unity assets changed.',
                   profiles=PROFILES, inputs=INPUTS, step=args.step, idle_every=args.idle_every,
                   idle_seconds=args.idle_seconds, price_experiment=args.globe_endpoint,
                   quantity=args.quantity, runs=results, checkpoints=audit)
    (args.output/'results.json').write_text(json.dumps(payload, indent=2), encoding='utf-8')
    write_html(args.output/'report.html', payload)
    with (args.output/'purchases.csv').open('w', newline='', encoding='utf-8') as handle:
        writer = csv.DictWriter(handle, fieldnames=['profile', 'input_style', 'seed', 'seconds', 'id', 'rank', 'levels', 'price', 'saving_seconds', 'earned', 'recurring_per_second_before', 'total_per_second_before', 'seconds_from_zero'])
        writer.writeheader()
        for result in results:
            writer.writerows(dict(profile=result['profile'], input_style=result['input_style'], seed=result['seed'], **purchase)
                             for purchase in result['purchases'])
    for row in audit:
        print(f'{row["build"]:22} boost={str(row["boosted"]):5} EUR/s={row["total_per_second"]:7.2f} spin payment={row["spin_payment"]:6.2f} 10k wait={row["ten_thousand_seconds_from_zero"]/60:7.2f} min')


if __name__ == '__main__':
    main()
