# First retirement balance review

**Superseded experiment: do not use the EUR 400,000 recommendation or its completion-time estimates as tuning targets.** The player rejected the very low late upgrade prices. The old average-income/greedy-buy model also omits meaningful input and purchase behavior. See [ECONOMY_TESTING.md](ECONOMY_TESTING.md) for the replacement testing approach. No gameplay assets or saves were changed by this review.

Target: retirement becomes available when the ordinary tree is nearly complete,
with the option to continue. Exclude 999-rank mastery nodes and skills gated
behind Gold shop purchases from first-run completeness. Use total money earned
this run, not an additional wallet payment.

Current exported assets contain 540 ordinary, first-run upgrade levels across
55 nodes. Buying all of them costs EUR 5,906,099,774. The current retirement
threshold is EUR 57,500,000. At full modeled activity it is reached after about
49.8 hours, with 419/540 levels bought and Pin 12 still locked. These are
approximate simulator results, not human playtests.

## Recommended starting proposal

Retirement available at EUR 400,000 total earned, together with the following
price changes. Do not apply the new threshold without the price rebalance.
Effects, rank counts, payouts and prerequisites stay unchanged in this experiment.

| Upgrade | Proposed base price | Proposed growth per rank |
|---|---:|---:|
| Auto Spin | 18 | 1.025 |
| Spin Limit | 20 | 1.025 |
| Spin Pay | 60 | 1.20 |
| Double Spin Pay | 2,000 | One purchase |
| Auto Fire | 200 | One purchase |
| Case Pay | 60 | 1.40 |
| Case Wait | 75 | 1.40 |
| Auto Stamp | 100 | 1.40 |
| Less Evidence | 300 | 2.00 |
| Cheaper Boxes | 150 | 1.60 |
| Better Loot | 250 | 1.60 |
| Longer Boost | 70 | 1.35 |
| Drink Wait | 85 | 1.35 |
| Stronger Boost | 200 | 1.60 |

Pin 1 stays EUR 10. Pins 2-12: ceil(20 * 1.7^(pinNumber - 1)).
Strength base: 15 * (1 + 0.2 * (pinNumber - 1)), growth 1.4.
Pin Pay base: 12.5 * (1 + 0.2 * (pinNumber - 1)), growth 1.35.
This separates the cost of adding a pin from the cost of improving it.

Reload opening stays 10, 15, 23, 34, 51, 76. Subsequent prices are
ceil(76 * 1.12^n), n = 1 through 34. The final rank costs EUR 3,583.
All 40 timing benefits are unchanged. This replaces the unbounded 50% price
growth that made the last rank EUR 73,715,549.

The proposed ordinary tree costs EUR 155,355 in total. Remaining earned money
can sit in the wallet, be spent on loot, or go toward optional purchases.

## Simulation results at EUR 400,000 earned

| Modeled activity | Retirement available | Ordinary levels bought | Pins unlocked | Reload levels |
|---|---:|---:|---:|---:|
| 100% | 38.6 minutes | 521/540 | 12 | 34/40 |
| 75% | 41.6 minutes | 526/540 | 12 | 34/40 |
| 50% | 52.0 minutes | 540/540 | 12 | 40/40 |

Activity is the simulator's sustained manual-speed assumption, not a measured
percentage of player time. The greedy buying strategy, average pin lifetimes,
manual firing, whiskey use and stamping are approximations. Higher activity
can reach the earnings threshold before buying as many levels. Later retirements,
Gold shop prices, loot economics and the five-hour overall goal need separate
validation after a first-run playtest. These runs do not establish full-game balance.

Executed: cost audit of actual exported assets and in-memory candidate runs
through Tools/economy_model.py. Candidate inputs and results are saved at
SpinTheGlobe/Logs/prestige-balance-review.json. No Unity tests were run because
this review changes no runtime code or gameplay data.
