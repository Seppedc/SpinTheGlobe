"""Repeated active runs using current shop requirements, ranks and loot.
Estimates first retirement availability, not optimal retirement timing or a guaranteed completion time.
"""
import argparse
from economy_model import Economy, cost


def simulate(runs=20, activity=.5, retire_multiple=1, seed=7, verbose=True, data=None):
    model = Economy(data, activity=activity)
    shop, gold, total, statues = {}, 0., 0., None
    rows = []
    for prestige in range(runs):
        result = model.run(shop, prestige, statues=statues, seed=seed+prestige, retire_multiple=retire_multiple)
        total += result['seconds']
        gold += result['gold']
        statues = result['statues']
        if not result['reached']:
            raise RuntimeError(f'Run {prestige+1} did not reach retirement within the cap.')
        while True:
            affordable = [(cost(item['cost'], shop.get(key, 0)), key)
                          for key, item in model.shop.items()
                          if shop.get(key, 0) < item['maxRank']
                          and (not item.get('requires') or shop.get(item['requires']))
                          and cost(item['cost'], shop.get(key, 0)) <= gold]
            if not affordable:
                break
            price, key = min(affordable)
            gold -= price
            shop[key] = shop.get(key, 0) + 1
        row = dict(run=prestige+1, minutes=result['seconds']/60, first_available=result['milestones']['retire_available']/60,
                   hours=total/3600, gold=result['gold'], shop_items=len(shop), reputation=shop.get('agency_reputation', 0))
        rows.append(row)
        if verbose:
            print(f'Run {row["run"]:2}: available {row["first_available"]:5.1f} min; retire {row["minutes"]:5.1f} min; total {row["hours"]:5.2f} h; gold +{row["gold"]:5.0f}; shop {len(shop)}/{len(model.shop)}; reputation {row["reputation"]}',flush=True)
    return rows


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--runs',type=int,default=20)
    parser.add_argument('--activity',type=float,default=.5)
    parser.add_argument('--retire-multiple',type=float,default=1)
    parser.add_argument('--seed',type=int,default=7)
    args=parser.parse_args()
    simulate(args.runs,args.activity,args.retire_multiple,args.seed)
