using System.Collections.Generic;
using DetectiveDesk.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    public sealed partial class SkillTreeView
    {
        const float SectionSize = 64;

        [System.Serializable]
        sealed class SectionView
        {
            public string id;
            [System.NonSerialized] public SkillSectionData data;
            public RectTransform rect;
            public TextMeshProUGUI label;
            public Image rootLink, stem, bus;
        }

        [SerializeField] List<SectionView> authoredSections = new List<SectionView>();
        readonly Dictionary<string, SectionView> sectionViews = new Dictionary<string, SectionView>();

        void RestoreSections()
        {
            bool removed = false;
            for (int i = authoredSections.Count - 1; i >= 0; i--)
            {
                var view = authoredSections[i];
                bool exists = false;
                foreach (var data in game.Tree.Sections) if (data.id == view.id) { exists = true; break; }
                if (exists) continue;
                foreach (var component in new Component[] { view.rect, view.label, view.rootLink, view.stem, view.bus })
                    if (component)
                    {
                        if (Application.isPlaying) Destroy(component.gameObject);
                        else DestroyImmediate(component.gameObject);
                    }
                authoredSections.RemoveAt(i);
                removed = true;
            }
            sectionViews.Clear();
            foreach (var view in authoredSections)
                foreach (var data in game.Tree.Sections)
                    if (view.id == data.id && view.rect) { view.data = data; sectionViews[data.id] = view; break; }
            if (EnsureSections() || removed)
                foreach (var node in views.Values)
                    if (System.Array.Exists(node.data.prerequisites ?? System.Array.Empty<string>(), id => id == DefaultSkillTreeFactory.RootId)) RebuildNodeLinks(node);
            foreach (var section in sectionViews.Values) BindSectionInfo(section);
        }

        bool EnsureSections()
        {
            bool created = false;
            if (game.Tree.Sections.Count == 0) return false;
            var layer = content.Find("Sections") as RectTransform;
            if (!layer) layer = UIFactory.NewRect("Sections", content);
            foreach (var data in game.Tree.Sections)
            {
                if (sectionViews.ContainsKey(data.id)) continue;
                sectionViews[data.id] = CreateSection(data, layer);
                created = true;
            }
            authoredSections = new List<SectionView>(sectionViews.Values);
            return created;
        }

        SectionView CreateSection(SkillSectionData data, RectTransform parent)
        {
            var view = new SectionView { id = data.id, data = data };
            NodeView style = null;
            foreach (var node in views.Values)
                if (NodeIcon(node.data) == data.icon) { style = node; break; }
            var face = UIFactory.NewImage(data.id, parent, new Color(.065f, .055f, .035f));
            face.raycastTarget = true;
            view.rect = face.rectTransform.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), data.position, new Vector2(SectionSize, SectionSize));
            var frame = UIFactory.AddFrame(view.rect, false, .85f);
            if (style != null) frame.sprite = style.border.sprite;
            frame.raycastTarget = false;
            frame.color = UITheme.Gold;
            var icon = UIFactory.NewIcon(data.icon, view.rect, 30);
            if (style != null) icon.sprite = style.icon.sprite;
            icon.raycastTarget = false;
            icon.color = UITheme.Gold;
            icon.rectTransform.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.zero, new Vector2(30, 30));
            view.label = UIFactory.NewText("Section " + data.title, content.Find("Labels"),
                data.title.ToUpperInvariant(), 18, TextAnchor.UpperCenter, UITheme.Gold);
            if (style != null) view.label.font = style.label.font;
            view.label.fontStyle = FontStyles.Bold;
            view.label.raycastTarget = false;
            PlaceSectionLabel(view);
            var links = content.Find("Links") as RectTransform;
            view.rootLink = Link(links, Vector2.zero, data.position);
            view.stem = Link(links, data.position, new Vector2(data.position.x, BusY(view)));
            view.bus = Link(links, Vector2.zero, Vector2.zero);
            return view;
        }

        SectionView hoveredSection;

        void BindSectionInfo(SectionView section)
        {
            var widget = section.rect.GetComponent<SkillSectionWidget>();
            if (!widget) widget = section.rect.gameObject.AddComponent<SkillSectionWidget>();
            section.rect.GetComponent<Image>().raycastTarget = true;
            widget.Entered = () =>
            {
                hovered = null;
                hoveredSection = section;
                tooltipAge = 0;
                tooltip.gameObject.SetActive(true);
                FillTooltip();
                PlaceTooltip();
            };
            widget.Exited = () => { if (hoveredSection == section) HideTooltip(); };
        }

        void FillSectionTooltip()
        {
            var colors = PurchasePalette.For(PurchaseVisualState.Maxed);
            tooltip.GetComponent<Image>().color = colors.fill;
            tooltip.Find("Frame").GetComponent<Image>().color = colors.border;
            tooltip.Find("Rule").GetComponent<Image>().color = colors.border;
            tipTitle.text = hoveredSection.data.title;
            tipTitle.color = tipBody.color = colors.text;
            tipBody.text = hoveredSection.data.description;
            tipStats.gameObject.SetActive(false);
            tipRank.gameObject.SetActive(false);
            tipCost.gameObject.SetActive(false);
        }

        static void PlaceSectionLabel(SectionView view)
        {
            view.label.rectTransform.Place(new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(.5f, 1), view.rect.anchoredPosition - new Vector2(0, SectionSize * .5f + 7), new Vector2(150, 28));
            view.label.text = "<mark=#0b0a09E6 padding=\"6,6,2,2\">" + view.data.title.ToUpperInvariant() + "</mark>";
        }

        static float BusY(SectionView view) => view.rect.anchoredPosition.y
            + (view.data.upward ? view.data.connectorOffset : -view.data.connectorOffset);

        static void PlaceLink(Image image, Vector2 a, Vector2 b)
        {
            Vector2 d = b - a;
            image.rectTransform.anchoredPosition = (a + b) * .5f;
            image.rectTransform.sizeDelta = new Vector2(d.magnitude, 3);
            image.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }

        void RebuildNodeLinks(NodeView node)
        {
            foreach (var link in node.links) if (link)
            {
                if (Application.isPlaying) Destroy(link.gameObject);
                else DestroyImmediate(link.gameObject);
            }
            node.links.Clear();
            if (node.data.prerequisites == null) return;
            foreach (var id in node.data.prerequisites)
            {
                var parent = game.Tree.Find(id);
                if (!DrawLink(parent, node.data)) continue;
                var section = parent.IsRoot ? game.Tree.SectionFor(node.data) : null;
                Vector2 target = node.rect.anchoredPosition;
                Vector2 start = views.TryGetValue(id, out var parentView) ? parentView.rect.anchoredPosition : parent.position;
                if (section != null && sectionViews.TryGetValue(section.id, out var sectionView))
                    start = section.sharedConnector ? new Vector2(target.x, BusY(sectionView))
                        : sectionView.rect.anchoredPosition;
                node.links.Add(Link(content.Find("Links") as RectTransform, start, target));
            }
        }

        public void SynchronizeSections(GameManager owner)
        {
            CreateLayout(owner);
            foreach (var section in sectionViews.Values)
            {
                section.rect.anchoredPosition = section.data.position;
                PlaceSectionLabel(section);
            }
            foreach (var node in views.Values)
                if (game.Tree.SectionFor(node.data) != null) RebuildNodeLinks(node);
            RefreshSections();
        }

        void RefreshSections()
        {
            if (!views.TryGetValue(DefaultSkillTreeFactory.RootId, out var root)) return;
            foreach (var section in sectionViews.Values)
            {
                float y = BusY(section), min = section.rect.anchoredPosition.x, max = min;
                bool any = false;
                foreach (var node in views.Values)
                {
                    if (game.Tree.SectionFor(node.data) != section.data || !node.rect.gameObject.activeSelf) continue;
                    any = true;
                    Vector2 target = node.rect.anchoredPosition;
                    min = Mathf.Min(min, target.x);
                    max = Mathf.Max(max, target.x);
                    if (node.links.Count > 0) PlaceLink(node.links[0], section.data.sharedConnector
                        ? new Vector2(target.x, y) : section.rect.anchoredPosition, target);
                }
                PlaceLink(section.rootLink, root.rect.anchoredPosition, section.rect.anchoredPosition);
                PlaceLink(section.stem, section.rect.anchoredPosition, new Vector2(section.rect.anchoredPosition.x, y));
                PlaceLink(section.bus, new Vector2(min, y), new Vector2(max, y));
                section.rootLink.color = section.stem.color = section.bus.color = UITheme.GoldDim;
                section.rect.gameObject.SetActive(any);
                section.label.gameObject.SetActive(any);
                section.rootLink.gameObject.SetActive(any);
                section.stem.gameObject.SetActive(any && section.data.sharedConnector);
                section.bus.gameObject.SetActive(any && section.data.sharedConnector && max > min);
                if (!any && hoveredSection == section) HideTooltip();
            }
        }
    }
}
