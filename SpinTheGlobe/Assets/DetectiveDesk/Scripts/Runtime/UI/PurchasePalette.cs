using DetectiveDesk.Progression;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    public enum PurchaseVisualState { Unavailable, Available, Maxed }

    /// <summary>One purchase-state palette for money skills and Gold shop upgrades.</summary>
    public readonly struct PurchaseColors
    {
        public readonly Color fill, border, icon, text, price;
        public PurchaseColors(Color fill, Color border, Color icon, Color text, Color price)
        {
            this.fill = fill; this.border = border; this.icon = icon; this.text = text; this.price = price;
        }
    }

    public static class PurchasePalette
    {
        static readonly PurchaseColors Unavailable = new PurchaseColors(
            new Color(.05f, .046f, .042f), new Color(.39f, .38f, .35f),
            new Color(.57f, .56f, .53f), new Color(.66f, .64f, .60f), new Color(.66f, .64f, .60f));
        static readonly PurchaseColors Available = new PurchaseColors(
            new Color(.18f, .115f, .065f), new Color(.66f, .46f, .26f),
            new Color(.85f, .65f, .42f), UITheme.Cream, new Color(.85f, .65f, .42f));
        static readonly PurchaseColors Maxed = new PurchaseColors(
            new Color(.40f, .30f, .12f), UITheme.GoldBright, UITheme.GoldBright, UITheme.GoldBright, UITheme.GoldBright);

        public static PurchaseVisualState State(bool maxed, bool available) => maxed
            ? PurchaseVisualState.Maxed : available ? PurchaseVisualState.Available : PurchaseVisualState.Unavailable;
        public static PurchaseVisualState State(NodeStatus status) => State(status == NodeStatus.Maxed, status == NodeStatus.Available);
        public static PurchaseColors For(NodeStatus status) => For(State(status));
        public static PurchaseColors For(PurchaseVisualState state) => state == PurchaseVisualState.Maxed
            ? Maxed : state == PurchaseVisualState.Available ? Available : Unavailable;
    }
}
