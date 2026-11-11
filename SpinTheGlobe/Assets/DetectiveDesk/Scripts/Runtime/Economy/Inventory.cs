using System;
using UnityEngine;

namespace DetectiveDesk.Economy
{
    /// <summary>What a lootbox can drop. Explicit values: serialized. Append only.</summary>
    public enum LootKind
    {
        IceCube = 0,
        Gold = 1,
        StatuePiece = 2,
        PaperBall = 3,
        Money = 4,
        StatuePiece2 = 5,
        StatuePiece3 = 6,
        StatuePiece1 = 2,
    }

    public static class LootKindExtensions
    {
        public static bool IsStatuePiece(this LootKind kind) =>
            kind == LootKind.StatuePiece || kind == LootKind.StatuePiece2 || kind == LootKind.StatuePiece3;

        public static int StatueIndex(this LootKind kind) =>
            kind == LootKind.StatuePiece3 ? 2 : kind == LootKind.StatuePiece2 ? 1 : (kind == LootKind.StatuePiece ? 0 : -1);
    }

    /// <summary>Consumable items the player holds. Plain C#, saved with the game.</summary>
    [Serializable]
    public sealed class Inventory
    {
        [SerializeField] int iceCubes;
        [SerializeField] int paperBalls;
        [SerializeField] int sparePieces;

        public event Action Changed;

        public int IceCubes => iceCubes;
        public int PaperBalls => paperBalls;
        /// <summary>Statue pieces waiting for a pedestal (all statues complete when they dropped).</summary>
        public int SparePieces => sparePieces;

        public void AddIce(int n = 1) { iceCubes = Math.Max(0, iceCubes + n); Changed?.Invoke(); }
        public void AddPaperBall(int n = 1) { paperBalls = Math.Max(0, paperBalls + n); Changed?.Invoke(); }
        public void AddSparePiece(int n = 1) { sparePieces = Math.Max(0, sparePieces + n); Changed?.Invoke(); }

        public bool TryUseIce() { if (iceCubes <= 0) return false; iceCubes--; Changed?.Invoke(); return true; }
        public bool TryUsePaperBall() { if (paperBalls <= 0) return false; paperBalls--; Changed?.Invoke(); return true; }
        public bool TryUseSparePiece() { if (sparePieces <= 0) return false; sparePieces--; Changed?.Invoke(); return true; }

        public void Load(int ice, int paper, int spare)
        {
            iceCubes = Math.Max(0, ice);
            paperBalls = Math.Max(0, paper);
            sparePieces = Math.Max(0, spare);
            Changed?.Invoke();
        }
    }
}
