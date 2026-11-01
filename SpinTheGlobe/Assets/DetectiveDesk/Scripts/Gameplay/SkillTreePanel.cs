using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    public sealed class SkillTreePanel : MonoBehaviour
    {
        public GameManager game;
        public SkillTreeDefinition definition;
        public GameObject engineering, restorations;
        public SkillTreeNodeView[] nodeViews;
        public SkillTreeLinkGraphic[] connections;
        public ScrollRect engineeringScroll, restorationScroll;
        public Text wallet, summary, detailTitle, detailEffect, detailRequirements, detailPrice, detailState, buyCaption;
        public Button buyButton;
        public Image engineeringTab, restorationTab;
        [SerializeField] string selectedId = "speed_01";
        [SerializeField] int currentTab;
        double cachedMoney = double.NaN;
        public string SelectedId => selectedId;
        public int CurrentTab => currentTab;
        public SkillTreeDefinition Definition => game && game.skillTree ? game.skillTree : definition;
        void OnEnable()
        {
            if (!game) game = GetComponentInParent<GameManager>();
            if (!game) game = FindFirstObjectByType<GameManager>();
            if (game) game.Changed += Refresh;
            ApplyTab(); Refresh(); RevealSelection();
        }
        void OnDisable() { if (game) game.Changed -= Refresh; }
        void Update() { if (game && cachedMoney != game.Money) Refresh(); } // Also observes preserved legacy case rewards.
        public void ShowEngineering() { currentTab = 0; ApplyTab(); EnsureSelection(); Refresh(); }
        public void ShowRestorations() { currentTab = 1; ApplyTab(); EnsureSelection(); Refresh(); }
        public void FocusFigurine(int index)
        {
            currentTab = 1;
            int rank = game && game.figurines != null && index >= 0 && index < game.figurines.Length && game.figurines[index]
                ? Mathf.Clamp(game.figurines[index].Tier + 1, 1, 3) : 1;
            selectedId = "figure_" + Mathf.Clamp(index, 0, 2) + "_" + rank;
            ApplyTab(); Refresh(); RevealSelection();
        }
        public void SelectNode(string id)
        {
            var node = Definition ? Definition.Find(id) : null;
            if (node == null) return;
            selectedId = id; currentTab = node.tab; ApplyTab(); Refresh();
        }
        void RevealSelection()
        {
            var scroll = currentTab == 0 ? engineeringScroll : restorationScroll;
            if (!scroll || !scroll.content || nodeViews == null) return;
            Canvas.ForceUpdateCanvases();
            foreach (var view in nodeViews) if (view && view.nodeId == selectedId)
            {
                float y = -((RectTransform)view.transform).anchoredPosition.y;
                float height = scroll.viewport.rect.height;
                float offset = scroll.content.anchoredPosition.y;
                if (y - 55 < offset || y + 55 > offset + height)
                    scroll.content.anchoredPosition = new Vector2(0, Mathf.Clamp(y - height * .5f, 0, Mathf.Max(0, scroll.content.rect.height - height)));
                break;
            }
        }
        public bool TryPurchaseSelected()
        {
            bool result = game && game.PurchaseSkill(selectedId);
            Refresh(); return result;
        }
        public void PurchaseSelected() { TryPurchaseSelected(); }
        void EnsureSelection()
        {
            var selected = Definition ? Definition.Find(selectedId) : null;
            if (selected != null && selected.tab == currentTab) return;
            if (Definition && Definition.nodes != null) foreach (var node in Definition.nodes)
                if (node != null && node.tab == currentTab) { selectedId = node.id; return; }
        }
        void ApplyTab()
        {
            if (engineering) engineering.SetActive(currentTab == 0);
            if (restorations) restorations.SetActive(currentTab == 1);
            if (engineeringTab) engineeringTab.color = currentTab == 0 ? new Color(.30f,.22f,.10f) : new Color(.10f,.09f,.07f);
            if (restorationTab) restorationTab.color = currentTab == 1 ? new Color(.30f,.22f,.10f) : new Color(.10f,.09f,.07f);
        }
        public void Refresh()
        {
            if (!game || !Definition) return;
            cachedMoney = game.Money;
            if (wallet) wallet.text = "AVAILABLE  €" + game.Money.ToString("N2");
            if (summary) summary.text = "GLOBAL ×" + game.GlobalMultiplier.ToString("0.0") + "   /   CURRENT PROGRESS IS PRESERVED";
            if (nodeViews != null) foreach (var view in nodeViews) if (view) view.Refresh(game, Definition, selectedId);
            if (connections != null) foreach (var graphic in connections) if (graphic) graphic.SetVerticesDirty();
            var node = Definition.Find(selectedId);
            string reason; var status = Definition.State(game, node, out reason);
            if (detailTitle) detailTitle.text = node == null ? "SELECT A SKILL" : node.title;
            if (detailEffect) detailEffect.text = node == null ? "Choose a node to inspect its effect and prerequisites." : Definition.EffectText(game, node) + "\n\n" + node.description;
            if (detailRequirements) detailRequirements.text = Definition.Requirements(game, node);
            if (detailPrice) detailPrice.text = status == SkillNodeState.Owned ? "ALREADY PURCHASED" : node != null && GameplayMath.Finite(Definition.Cost(game, node)) ? "COST  €" + Definition.Cost(game, node).ToString("N0") : "";
            if (detailState) detailState.text = reason;
            if (buyCaption) buyCaption.text = status == SkillNodeState.Owned ? "OWNED" : status == SkillNodeState.Available ? "UNLOCK SKILL" : status == SkillNodeState.Unaffordable ? "NEED CASH" : "LOCKED";
            if (buyButton) buyButton.interactable = status == SkillNodeState.Available;
        }
    }
}
