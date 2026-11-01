using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    public sealed class SkillTreeNodeView : MonoBehaviour
    {
        public string nodeId;
        public SkillTreePanel owner;
        public Button button;
        public Image background, border, selection;
        public Text title, state, cost;
        public SkillNodeState CurrentState { get; private set; }
        public void Select() { if (owner) owner.SelectNode(nodeId); }
        public void Refresh(GameManager game, SkillTreeDefinition definition, string selectedId)
        {
            SkillNode node = definition ? definition.Find(nodeId) : null;
            string reason; CurrentState = definition ? definition.State(game, node, out reason) : SkillNodeState.Unavailable;
            bool owned = CurrentState == SkillNodeState.Owned, ready = CurrentState == SkillNodeState.Available;
            if (title) title.text = node == null ? "Unavailable" : node.title;
            if (state) state.text = owned ? "OWNED" : ready ? "AVAILABLE" : CurrentState == SkillNodeState.Unaffordable ? "NEED CASH" : "LOCKED";
            if (cost) cost.text = owned ? "PURCHASED" : node != null && definition && GameplayMath.Finite(definition.Cost(game, node)) ? "€" + definition.Cost(game, node).ToString("N0") : "—";
            if (button) button.interactable = node != null; // Locked nodes remain inspectable.
            if (background) background.color = owned ? new Color(.12f,.16f,.12f,.98f) : ready ? new Color(.20f,.145f,.075f,.98f) : new Color(.065f,.059f,.052f,.98f);
            if (border) border.color = owned ? new Color(.67f,.55f,.27f) : ready ? new Color(.90f,.67f,.28f) : new Color(.25f,.22f,.17f);
            if (selection) selection.enabled = nodeId == selectedId;
            if (title) title.color = owned || ready ? new Color(.94f,.83f,.62f) : new Color(.58f,.55f,.47f);
            if (state) state.color = owned ? new Color(.64f,.79f,.54f) : ready ? new Color(.96f,.73f,.33f) : new Color(.56f,.51f,.43f);
        }
    }
}
