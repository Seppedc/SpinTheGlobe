"""Behavioral checks for the offline economy experiments (no Unity dependency)."""
import copy
import unittest

from economy_model import Economy, cost
from economy_playtest import Playtest, case_strength, integrate


class PlaytestTests(unittest.TestCase):
    def setUp(self):
        self.data = copy.deepcopy(Economy().data)

    def test_idle_without_motor_earns_nothing(self):
        test = Playtest(self.data, idle=True)
        for _ in range(200):
            test.tick(purchasing=False)
        self.assertEqual(0, test.earned)

    def test_whiskey_boost_requires_permanent_unlock(self):
        locked = Playtest(self.data)
        unlocked = Playtest(self.data, shop={'unlock_whiskey': 1})
        for _ in range(100):
            locked.tick(purchasing=False)
            unlocked.tick(purchasing=False)
        self.assertEqual(0, locked.boost_end)
        self.assertGreater(unlocked.boost_end, unlocked.time)
        self.assertLess(locked.rotation_limits(True)[0], unlocked.rotation_limits(True)[0])
        self.assertNotIn('whiskey_duration', [locked.model.nodes[i]['id'] for i, price in
                         locked.model.candidates(locked.ranks, locked.shop, locked.s)])

    def test_less_friction_retains_speed_without_adding_drag_power(self):
        speeds = []
        for rank in (0, 5, 10):
            test = Playtest(self.data, idle=True)
            test.ranks[test.model.index['globe_drag']] = rank
            test.stats()
            test.velocity = 90
            for _ in range(20):
                test.tick(purchasing=False)
            self.assertEqual(1, test.s[102])
            self.assertAlmostEqual(.85**rank, test.s[107])
            speeds.append(test.velocity)
        self.assertLess(speeds[0], speeds[1])
        self.assertLess(speeds[1], speeds[2])
        self.assertLess(speeds[2], 90)

    def test_friction_integration_is_independent_of_step_size(self):
        velocity, total = 140., 0.
        for _ in range(100):
            velocity, angle = integrate(velocity, .01, .9875, 30, 180)
            total += angle
        expected_velocity, expected_angle = integrate(140, 1, .9875, 30, 180)
        self.assertAlmostEqual(expected_velocity, velocity, places=9)
        self.assertAlmostEqual(expected_angle, total, places=9)

    def test_saving_target_is_retained_when_unaffordable(self):
        test = Playtest(self.data)
        test.target = test.model.index['root']
        price = cost(test.model.nodes[test.target]['cost'], 0)
        test.money = price - 1
        test.buy()
        self.assertEqual(test.model.index['root'], test.target)
        self.assertEqual(0, test.rank('root'))
        test.money = price
        test.buy()
        self.assertEqual(1, test.rank('root'))
        self.assertAlmostEqual(0, test.money)

    def test_earnings_reconcile_with_wallet_and_spending(self):
        test = Playtest(self.data)
        result = test.run(15)
        spent = sum(row['price'] for row in result['purchases'])
        self.assertAlmostEqual(test.earned, sum(test.sources.values()))
        self.assertAlmostEqual(test.money, self.b_start() + test.earned - spent)
        self.assertTrue(result['purchases'])

    def b_start(self):
        return self.data['balance']['startingMoney']

    def test_busy_pin_cannot_be_relaunched_before_finishing_flight(self):
        test = Playtest(self.data, idle=True)
        for node_id in ('root', 'pin_01', 'pin_autoloader'):
            test.ranks[test.model.index[node_id]] = 1
        test.ranks[test.model.index['shooter_cooldown']] = 40
        test.stats()
        for _ in range(5):
            test.tick(purchasing=False)
        self.assertEqual(1, test.events['shots'])
        self.assertEqual(0, test.events['pins'])

    def test_locked_case_cannot_pay(self):
        test = Playtest(self.data)
        for node_id in ('root', 'pin_01', 'pin_autoloader'):
            test.ranks[test.model.index[node_id]] = 1
        test.stats()
        for _ in range(2400):
            test.tick(purchasing=False)
        self.assertGreater(test.events['pins'], 0)
        self.assertEqual(0, test.events['cases'])

    def test_case_strength_tier_boundaries(self):
        self.assertEqual(0, case_strength(self.data['balance'], 14))
        self.assertEqual(1, case_strength(self.data['balance'], 15))
        self.assertEqual(1, case_strength(self.data['balance'], 44))
        self.assertEqual(2, case_strength(self.data['balance'], 45))


if __name__ == '__main__':
    unittest.main()
