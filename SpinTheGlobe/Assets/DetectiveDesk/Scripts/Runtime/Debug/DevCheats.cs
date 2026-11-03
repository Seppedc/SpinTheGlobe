using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Testing shortcuts, only in the Editor and development builds:
    /// F1 +€1,000 (Shift: +€100,000) · F2 +1 ice, paper ball and statue piece · F3 complete and gild all statues
    /// · F4 +10 gold · F5 fill the case file · F6 show the whole skill tree · F7 whiskey glass almost full (no cooldown) · F8 slow motion on/off (×0.15).
    /// </summary>
    public sealed class DevCheats : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        GameManager game;

        void Awake() { game = GetComponent<GameManager>(); }

        void Update()
        {
            if (!game) return;
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (Input.GetKeyDown(KeyCode.F1)) { game.Credit(shift ? 100000 : 1000); game.ShowHint("[dev] money added"); }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                game.Inventory.AddIce(); game.Inventory.AddPaperBall();
                if (game.figurines != null) foreach (var f in game.figurines) if (f && !f.IsComplete) { f.AddPiece(); break; }
                game.ShowHint("[dev] ice cube, paper ball and statue piece added");
            }
            if (Input.GetKeyDown(KeyCode.F3) && game.figurines != null)
            {
                foreach (var f in game.figurines) if (f) { while (!f.IsComplete) f.AddPiece(); f.AddGold(1); }
                game.ShowHint("[dev] statues completed and gilded");
            }
            if (Input.GetKeyDown(KeyCode.F4)) { game.CreditGold(10); game.ShowHint("[dev] gold added"); }
            if (Input.GetKeyDown(KeyCode.F5) && game.Cases) game.Cases.DevFill();
            if (Input.GetKeyDown(KeyCode.F7) && game.Whiskey) game.Whiskey.DevAlmostFull();
            if (Input.GetKeyDown(KeyCode.F8)) { Time.timeScale = Time.timeScale < 0.99f ? 1f : 0.15f; game.ShowHint("[dev] time ×" + Time.timeScale.ToString("0.##")); }
            if (Input.GetKeyDown(KeyCode.F6) && game.TreeView)
            {
                game.TreeView.RevealAll = !game.TreeView.RevealAll;
                game.ShowHint("[dev] skill tree: " + (game.TreeView.RevealAll ? "showing all nodes" : "normal reveal"));
            }
        }
#endif
    }
}
