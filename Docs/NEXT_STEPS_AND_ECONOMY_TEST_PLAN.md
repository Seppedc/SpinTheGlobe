# Next steps and economy test plan

Prepared 2026-11-07. **Proposal for review; implementation has not started.**

## Recommendation

Build a measured version of the current game loop before adding more systems.
The next useful work is a local playtest recorder, verification of the economy
model against Unity, and a report comparing several spending and activity
styles. Use that evidence for a small balance pass, then test retirement and
the Gold shop across several runs. Finish onboarding and feedback around the
resulting progression.

Keep the latest EUR 15,000 retirement threshold as the baseline. Do not make
another large price or payout change while establishing the measurements.

## What is actually in the project

Read-only inspection of the current Unity assets confirmed:

| Item | Current value |
|---|---:|
| Retirement available | EUR 15,000 earned during the run |
| Gold at that threshold | 25 |
| Skill nodes | 64 |
| Ordinary, first-run-accessible nodes | 55 |
| Levels in those nodes | 840 |
| Cost to buy all those levels | EUR 46,568,780 |
| Individual levels costing more than EUR 15,000 | 309 |

The last three counts exclude 999-level mastery nodes and nodes requiring a
Gold-shop unlock. Prices include nearest-EUR-5 rounding. They describe asset
costs, not a simulated or human completion time.

This conflicts with the earlier idea of nearly completing the tree before
the first retirement. A player reaching EUR 15,000 lifetime earnings in a run
cannot also have spent tens of millions on its upgrades. Later skill ranks
also reset on retirement, so we must check that players eventually reach and
use them during later, stronger runs or a deliberate extended run.

**Recommended interpretation:** first retirement follows a satisfying early
build; the full tree is a later objective. Keep EUR 15,000 during testing and
measure the actual first-retirement build before deciding which unlocks it
should contain. If almost-complete-first-run remains a requirement, that
needs a separate redesign of rank counts, prices or retirement requirements.

Existing useful tools:

- `Tools/economy_playtest.py`: individual pin events, rotation/friction,
  evidence, cases, boosts and purchase histories; four buying profiles,
  three activity presets and Buy 1/5/10 support.
- `Tools/economy_model.py`: stat calculations and rounded price evaluation.
- `Tools/economy_snapshot.json`: an exported input snapshot, which must be
  refreshed from canonical assets for each experiment.
- Seven Python economy tests and a Unity EditMode suite. The last executed
  Unity suite passed 145 tests; that verifies tested behavior, not pacing.

The current playtest simulator records when the first retirement becomes
available, then keeps playing without resetting. It does not yet simulate
complete prestige/Gold-shop campaigns or loot spending. Existing historical
reports predate important price and retirement changes and are not current
completion-time evidence.

## Work order and review points

| Order | Work | Reviewable result |
|---|---|---|
| 1 | Record the current build and define first-retirement milestones | Baseline sheet, target hypotheses and a versioned snapshot |
| 2 | Add local economy recording and isolated test saves | One real playthrough with a purchase timeline and income breakdown |
| 3 | Verify simulator behavior against Unity | A comparison report identifying matches and remaining approximations |
| 4 | Run a range of spending/input strategies | Ranked bottlenecks, outliers and purchase-value charts |
| 5 | Propose a small price/effect adjustment | Before/after report using the same test conditions |
| 6 | Add multi-retirement/Gold-shop simulation | Five-hour progression report and first/second-retirement human checks |
| 7 | Improve player feedback around the verified progression | Clear boost/RPM information, next goals and first-use guidance |

High-speed globe strobing remains an open visual issue. Disabling the duplicate
surface did not demonstrate a visible fix in the isolated comparison. Include
it in the next usability pass; do not lower economic speed merely to hide it.
The drink should visibly identify boosted RPM, since it has already made an
ordinary speed upgrade look unexpectedly large during testing.

## Why we should not enumerate every spending order

Hundreds of levels, optional waiting, input choices and randomness create far
too many sequences to exhaustively simulate. Most sequences are also not
plausible player behavior. Coverage should instead combine representative
players, deliberate edge cases, randomized policies and a search for unusually
powerful builds. Human sessions establish whether the promising paths feel
good. An optimizer establishes a possible fast path, not an average player.

Equal retirement times are not required for every strategy. Sensible choices
should remain viable, and comfort should have a measurable benefit. Deliberate
bad choices are useful for finding traps and recovery problems, but should not
define the main pacing target.

## Test protocol

### 1. Establish an immutable baseline

Export the active GameBalance, SkillTree and Gold catalog. Record the revision,
hashes of the actual data and runtime files, simulator version, seed, input
profile, purchase policy, bulk quantity, timestep and stopping condition.
The revision alone is insufficient while there are uncommitted changes.

Store each experiment under a separate `SpinTheGlobe/Logs/economy/<run-id>/`
directory: `manifest.json`, `snapshot-used.json`, `events.jsonl`,
`purchases.csv`, `checkpoints.csv` and a standalone `report.html`. These outputs
and recorder extensions are proposed; the current tool already emits a report,
results, purchase CSV and snapshot, but not this full set.

Use isolated test-save locations. Preserve the real player save. A fresh-run
fixture starts with zero money, zero Gold, no skills or permanent bonuses.
Flag every developer grant, forced unlock or manual reset. Exclude contaminated
runs from pacing results. A deterministic state fixture is useful for a unit
test, but must not be presented as a naturally reachable player build.

### 2. Add a local recorder in Unity

Record events at their actual gameplay source, rather than inferring income
from wallet changes. An event needs elapsed game time, wall-clock time, type,
amount/source, wallet before/after and the relevant skill or object id.

Capture:

- Spin, pin, case and loot income separately; Gold and developer grants separately.
- Purchase id, old/new rank, quantity, total charged, wallet and all prerequisites.
- Manual swipe distance/duration, clicks, manual shots and stamps. Record
  accepted inputs as well as attempts that produce no action.
- Current RPM, permanent maximum RPM, temporary maximum RPM and boost status.
- Pin launches, busy/ready counts, passes, drops and reload waiting.
- Evidence accepted/rejected, required Strength, case readiness and stamp delay.
- Boost start/end, filling/drinking time and cooldown.
- Retirement availability, actual retirement, Gold award and Gold purchases.
- Focus changes, settings/menu time, frame time and save/load events.

Take a state sample every five seconds and on milestone/purchase events.
Keep the recorder local, with no external analytics service. Avoid per-frame
disk writes; buffer events and flush safely. Confirm recording does not change
income or introduce noticeable frame-time spikes.

### 3. Prove the mechanics before trusting simulated pacing

Use small, controlled Unity fixtures and the equivalent simulation state:

| Check | What must match |
|---|---|
| One minute of unboosted auto-rotation | Turns, payout crossings and earned money |
| Accepted swipe/click sequence | Velocity, friction decay and maximum-speed behavior |
| First pin at fixed location | First crossing, number of paid passes, drop and readiness |
| Multiple pins | Round-robin selection, flight time and busy-pin exclusion |
| Reload with few/many pins | Benefit only while shooter cooldown is a real constraint |
| Strength change | Extra paid passes and the changed rate of evidence production |
| Cases | Groups of five evidence counts, Strength-tier transitions and cooldown rejection |
| Boost starts/ends | Both payout and speed-cap effects; correct clamping on expiry |
| Purchase | Rounded unit cost, affordable quantity, exact charge and capped motor behavior |
| Save/load | Money, ranks, eligibility and intended temporary-state handling |
| Focus loss | Automation continues while manual input stops; no offline award is invented |

Python and Unity do not necessarily share the same random generator. For
cross-engine verification, replay a shared tape of landings, random decisions
and accepted inputs; giving them the same numeric seed is not sufficient.

Compare exact prices/ranks/counts and wallet reconciliation to a small numeric
tolerance. For timed samples allow explicit one-frame boundary differences,
then compare after the same event rather than overlooking a missing payout.
Reconcile: starting wallet + credited money - spending = final wallet; measure
total earned separately. No duplicated or missing credits are acceptable.

Check actual Unity behavior at 30, 60 and 144 FPS. Repeat deterministic simulator
replays at 0.1, 0.05 and 0.025-second steps. Proposed initial numerical gate:
under 1% unexplained income drift in scripted Unity frame-rate checks and under
2% simulator timestep drift over ten minutes, with boundary differences
investigated. These are test tolerances to validate, not measured results.

### 4. Calibrate with real play

Record three exploratory sessions: an active player, a relaxed player and a
player who seeks automation and regularly switches windows. Start with 15-25
minutes each, extending at least one through first retirement.

Measure actual input frequency, swipe quality, decision/menu time, manual
shooting, boost usage and delays before stamping. Fit these input distributions
to the simulator; do not assume perfect swipes or immediate purchases. Treat
automation as reduced effort as well as possible income.

Replay accepted actions through the model, then compare milestone times,
wallet, source income and ranks. Investigate systematic deviations rather than
changing prices to compensate for a model error. Reserve two additional fresh
sessions as holdouts after calibration; do not tune the model to those results.
An initial goal is to explain roughly 90-95% of observed income and milestone
timing, while documenting the remaining approximation and randomness.

### 5. Explore different players systematically

Keep the four implemented spending styles: mixed, pin-focused, spin-focused
and automation-focused. Add four explicit policies:

| New policy | Purpose |
|---|---|
| Case-focused | Evidence, cooldown, stamping and case income as the main investment |
| Saving for major goals | Tests patience for Auto Fire, new pins or a large payout upgrade |
| Cheapest-first / novice | Reveals tempting purchases that leave weak or frustrating builds |
| Varied, reasonably random | Exposes combinations a hand-written priority list misses |

Use four input modes: active dragging, relaxed dragging, click-heavy play and
mostly-background play. The latter two need implementing; the existing three
presets should be recalibrated rather than relabeled as measured behavior.
Background mode still needs a plausible manual opening before automation.

Test Buy 1, Buy 5 and Buy 10. For the initial expanded sweep, eight policies ×
four input modes × three quantities × ten seeds = **960 runs per candidate**.
Run until retirement or a 90-minute cap, recording failures/timeouts explicitly.
Do not compute a median from successful runs while hiding those that timed out.
Continue a subset beyond availability to see what saving longer actually buys.

Before that extension exists, use the existing tool for a cheap smoke sweep:
four policies × three existing input presets × three seeds = 36 runs, with one
bulk quantity. Repeat other quantities only after checking the initial output.

The 960-run matrix is coverage, not a forecast of the player population. Report
each mode separately; do not mix an assumed equal number of background and
active players into a misleading overall average. Increase finalist/outlier
groups to at least 30-50 seeds when randomness materially changes the decision.

### 6. Measure the interaction bottlenecks

For representative early, middle and later builds, measure the same 60-120
second window before and after buying one upgrade. Use the same random tape
and input schedule. Include its required prerequisite bundle when relevant.

Record extra income, time spent interacting and which constraint is active:

- All pins busy versus shooter reloading versus shooter ready but unclicked.
- Motor at its permanent cap versus manual dragging under the cap.
- Pins waiting for rotations versus pins being replaced too slowly.
- Case waiting on evidence, cooldown or the player's stamp.

Strength is especially important: it can increase income per shot while making
pins fall less often, slowing evidence collection and keeping pins occupied.
Faster Reload may do almost nothing when every pin is already busy. More RPM
can improve pin turnover but leave reload as the new constraint. Compare these
combinations, not just each upgrade's advertised percentage.

Sweep selected neighboring ranks for pin count × Strength × Reload × RPM,
first with boosts off, then with realistic boost use. Checkpoint builds are
mechanical sensitivity tests unless a purchase timeline proves reachability.

### 7. Record the measures that explain player feel

| Measure | Why it matters |
|---|---|
| Time to first purchase, pin, case, automation and retirement | Whether features arrive at a satisfying pace |
| Actual time saving for the chosen goal | How long the player really waits |
| Next price / trailing 60-second total income | A comparable from-empty earning-time estimate |
| Income by source and boost-on/off windows | Whether one system overwhelms the others |
| Incremental income and payback after a purchase | Whether an income upgrade earns its cost back |
| Manual actions per minute | Whether automation improves comfort |
| Longest interval without an affordable useful choice | Where progression stalls |
| Useful choices affordable at decision points | Whether spending is a decision or a forced path |
| Levels/features owned at retirement | Whether the first run reaches its intended milestones |
| Unspent wallet and abandoned saving goals | Whether prices create hoarding or frustration |
| Gold per minute and time to the next permanent unlock | Whether retirement is worth doing |

Separate source shares during the opening and later stages. A source being 90%
of income is a review flag, not automatically wrong. A comfort purchase may be
good even with little financial payback. Diminishing benefits are acceptable
when the interface and alternative purchases make that tradeoff clear.

Use medians, 10th/90th percentiles, worst stalls and timeout rates rather than
only averages. Define a *useful* choice using measured benefit or a meaningful
unlock; a cheap level with no practical effect should not hide a dead period.

### 8. Set provisional pacing targets, then revise from observation

These are proposals for review, not claims about today's build:

| Stage | Proposed feel target |
|---|---|
| Opening | A meaningful purchase in about 30-60 seconds |
| Early spending | Frequent useful choices, roughly 15-45 seconds of saving for small purchases |
| Middle of a run | Roughly 30-90 seconds for ordinary decisions, with meaningful alternatives |
| Major unlock | Roughly 1-3 minutes of deliberate saving, with a clear benefit |
| First retirement | Start by testing the earlier 30-40-minute active-play hypothesis at EUR 15,000 |
| Subsequent runs | A noticeably quicker opening and a new goal, rather than only faster repetition |

Do not demand increasing wait time for every individual rank across every
build: income, different branches and batch buying make that inappropriate.
Target increasing importance and understandable tradeoffs. Flag waits over
two minutes with no useful alternative early on, and investigate before using
them as intentional pacing.

Proposed warning gates: a sensible active strategy consistently takes over
1.5× the mixed baseline to retire; an optimized sequence is over 2× faster;
several ordinary purchases have negligible practical effect; or first retirement
arrives before the intended core features. These trigger a review, not an
automatic rebalance. Comfort-focused strategies may reasonably earn less.

### 9. Check randomness, temptation and exploits

Add focused experiments for boosts unused, realistically used, and aggressively
used. Add loot spending at zero, modest and heavy budget shares once its exact
economics are modeled. Keep jackpots separate from recurring income and show
bad-luck results, not only expected value.

Search for fast routes with randomized policy parameters and a bounded beam
search over promising purchase sequences. Compare short and longer income
horizons so an optimizer does not reject prerequisite investments. Include
attention costs. Test the discovered fast sequences manually before calling
them achievable; report search limits rather than claiming global optimality.

Exercise case-Strength tier boundaries, reload minima, max motor/boost expiry,
repeated load/reset, bulk purchases with exact/insufficient funds and rounded
prices near EUR 5 boundaries. Recheck that switching windows grants no manual
actions and continues existing automation. Background play is not offline
income: closed-game time must not be credited.

### 10. Test the full five-hour progression separately

Extend the simulation to actually retire, clear run state, award Gold, retain
permanent upgrades, apply fresh-start perks and choose Gold purchases. Model
loot/statue income and unlock dependencies, including the time needed to use
newly unlocked systems. These mechanics cannot be inferred from the current
first-retirement-only simulation.

Compare retirement at EUR 15k, 30k, 60k and 150k, plus a policy saving for a
specific run milestone. Compare permanent-income, fresh-start/comfort and new
feature Gold spending. Track the first, second and later retirements across
five simulated hours. Search for both rapid-retirement farming and situations
where retiring never becomes an attractive choice.

At the current formula those four thresholds award 25, 35, 50 and 79 Gold
respectively, before any future rule changes. More Gold per run does not
necessarily mean more Gold per minute. Measure actual reset/rebuild time.

The five-hour goal should mean sustained meaningful progression: new features,
valuable purchases and milestones across that period. Record time to the final
intended unlock and whether anything meaningful remains to pursue. Mastery
levels or unavoidable waiting alone do not demonstrate five hours of content.
Check that high-rank skills are realistically reached in some intended run.

Human confirmation should include a full fresh first retirement, a second
retirement with different Gold purchases, and a longer campaign check after
the shorter tests are convincing. Faster simulation does not replace this.

### 11. Tune with controlled comparisons

For each proposed change, state the problem and predicted effect first. Change
one related group of numbers at a time. Keep an unchanged baseline and use
the same input schedules, seeds/random tapes and stopping rules for candidates.
Do not modify the real player save or game assets just to run an experiment.

Start with the 36-run smoke sweep, then mechanical checks, then the expanded
matrix for promising variants. Repeat calibrated Unity replays and held-out
human sessions before accepting the result. Run relevant EditMode checks
whenever implementation or gameplay data changes.

Each review should deliver: baseline versus candidate charts; the slowest and
fastest sensible paths; longest stalls; source income by stage; first-retirement
builds; Gold progression; exact proposed asset changes; and limitations.

## Player-feel improvements after the measurements

Prioritize a clear boosted-RPM indicator, a visible explanation of the active
shooter/case constraint, readable next-goal information and additional first-use
object guidance. Keep the visual feedback concentrated on the relevant object;
do not restore the removed bottom-of-screen money/progress messages.

Then check purchase feedback, milestone feedback, affordability colours,
late-stack readability and high-speed motion at representative progression
states. More features, more ranks and additional prestige content should follow
a validated need rather than compensate for an unmeasured economy.

## Decisions for the review

1. Adopt an early first-retirement build at EUR 15,000 as the current direction,
   or retain the earlier nearly-complete-tree requirement and redesign around it.
2. Use 30-40 minutes as an initial first-retirement hypothesis, subject to real
   sessions, rather than selecting a time because the old report happened to fit.
3. Implement the local recorder and simulator verification first, then review
   measured results before another broad price pass.

Only this planning document was added for this review. No gameplay settings,
prices, save data or scenes changed. The current-asset audit was executed;
the expanded runs, recorder, optimizer and campaign tests above are proposed
work and have not been executed.
