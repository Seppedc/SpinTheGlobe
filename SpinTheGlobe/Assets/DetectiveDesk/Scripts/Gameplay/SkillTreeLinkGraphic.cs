using System;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    /// <summary>UI-native connector lines; no external textures or custom shader dependency.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SkillTreeLinkGraphic : MaskableGraphic
    {
        [Serializable] public sealed class Link
        {
            public RectTransform source, target;
            public string sourceId, targetId;
        }
        public SkillTreePanel owner;
        public Link[] links = Array.Empty<Link>();
        public float thickness = 2;
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear(); if (links == null) return;
            foreach (var link in links)
            {
                if (link == null || !link.source || !link.target) continue;
                Vector2 a = transform.InverseTransformPoint(link.source.TransformPoint(new Vector3(0, link.source.rect.yMin, 0)));
                Vector2 b = transform.InverseTransformPoint(link.target.TransformPoint(new Vector3(0, link.target.rect.yMax, 0)));
                Vector2 direction = b - a; if (direction.sqrMagnitude < .001f) continue;
                Vector2 normal = new Vector2(-direction.y, direction.x).normalized * thickness * .5f;
                Color tint = new Color(.24f,.19f,.13f,.8f);
                var definition = owner ? owner.Definition : null;
                if (definition && owner.game)
                {
                    var target = definition.Find(link.targetId);
                    string reason; var state = definition.State(owner.game, target, out reason);
                    if (state == SkillNodeState.Owned) tint = new Color(.66f,.51f,.23f,.95f);
                    else if (state == SkillNodeState.Available || state == SkillNodeState.Unaffordable) tint = new Color(.52f,.36f,.15f,.9f);
                }
                int offset = helper.currentVertCount;
                helper.AddVert(a - normal, tint, Vector2.zero); helper.AddVert(a + normal, tint, Vector2.zero);
                helper.AddVert(b + normal, tint, Vector2.zero); helper.AddVert(b - normal, tint, Vector2.zero);
                helper.AddTriangle(offset, offset + 1, offset + 2); helper.AddTriangle(offset, offset + 2, offset + 3);
            }
        }
    }
}
