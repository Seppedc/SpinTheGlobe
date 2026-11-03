using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// The case folder on the desk is the case's progress bar. It starts empty and flat; every clue a pin
    /// brings in slides into it as a sheet, photo or memo, so the folder visibly thickens and the clues peek
    /// out from under the cover. When the case is stamped the contents slide out and the folder is empty again.
    /// Uses the authored folder model (Generated_CaseFiles) and the desk's evidence photos/memos as templates;
    /// the loose evidence on the desk is hidden, so at the start the folder is the only file on the table.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CaseFolderVisual : MonoBehaviour
    {
        enum Kind { Paper, Photo, Memo }
        static readonly Kind[] Pattern = { Kind.Paper, Kind.Photo, Kind.Paper, Kind.Memo, Kind.Paper, Kind.Paper, Kind.Photo, Kind.Memo };

        /// <summary>World-space height each clue adds to the stack.</summary>
        public float itemThickness = 0.008f;
        /// <summary>The stack stops growing after this many clues (later clues still slide in, on top).</summary>
        public int maxVisualItems = 24;
        public float slideSeconds = 0.35f;
        public bool logLayout;

        readonly List<Transform> items = new List<Transform>();
        readonly List<(Transform t, Vector3 basePos)> cover = new List<(Transform, Vector3)>();
        readonly List<GameObject> paperTemplates = new List<GameObject>(), photoTemplates = new List<GameObject>(), memoTemplates = new List<GameObject>();
        Transform folder, clip, runtimeRoot;
        Vector3 up, right, forward, stackBase, clipBase, expose, lateral;
        float exposeExtent, lateralExtent, sideSign = 1;
        Bounds backBounds;
        float coverLift, coverTarget, emptyDrop;
        int level;
        Coroutine clearing;

        public Transform Folder => folder;
        public bool IsValid => folder;

        /// <summary>Top centre of the folder (for the desk label).</summary>
        public Vector3 LabelAnchor => folder ? stackBase + up * (Mathf.Max(coverLift, 0) + 0.03f) : Vector3.zero;

        public void Initialize(Transform folderRoot)
        {
            folder = folderRoot;
            if (!folder) return;
            runtimeRoot = new GameObject("Case folder contents").transform;
            runtimeRoot.SetParent(transform, false);

            up = folder.up; right = folder.right; forward = folder.forward;
            if (Vector3.Dot(up, Vector3.up) < 0.5f) { up = Vector3.up; right = Vector3.right; forward = Vector3.forward; }

            Transform back = null, frontCover = null;
            foreach (var t in folder.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                if (n.StartsWith("Folder_BackCover")) back = t;
                else if (n.StartsWith("Folder_FrontCover")) frontCover = t;
                if (n.StartsWith("Paper_Layer"))
                {
                    if (paperTemplates.Count == 0) paperTemplates.Add(t.gameObject);
                    t.gameObject.SetActive(false);
                }
                else if (n.StartsWith("Sheet_") || n.StartsWith("Document_Rule")) t.gameObject.SetActive(false);   // print on the top sheet
                else if (n.StartsWith("PaperClip")) { clip = t; clipBase = t.position; }
                else if (n.StartsWith("Folder_FrontCover") || n.StartsWith("Folder_SpineFold") || n.StartsWith("Scored_Fold") || n.StartsWith("Cover_"))
                    cover.Add((t, t.position));
            }

            backBounds = back ? ClickRouter.RendererBounds(back, folder.position) : ClickRouter.RendererBounds(folder, folder.position);
            stackBase = new Vector3(backBounds.center.x, backBounds.max.y, backBounds.center.z);
            // Empty folder: the cover lies on the back cover instead of on the authored paper block.
            if (frontCover)
            {
                var fb = ClickRouter.RendererBounds(frontCover, frontCover.position);
                emptyDrop = Mathf.Max(0, fb.min.y - backBounds.max.y - 0.0005f);
            }

            // Clues stick out on the side the cover leaves open (the top margin of the authored sheets), so they stay visible.
            expose = forward;
            if (frontCover)
            {
                var fb = ClickRouter.RendererBounds(frontCover, frontCover.position);
                var d = Vector3.ProjectOnPlane(backBounds.center - fb.center, up);
                if (d.sqrMagnitude > 1e-6f) expose = d.normalized;
            }
            lateral = Vector3.Cross(up, expose).normalized;
            exposeExtent = HalfLength(expose);
            // Clues stick out on the side that faces the middle of the desk.
            sideSign = Vector3.Dot(lateral, new Vector3(-stackBase.x, 0, 0)) >= 0 ? 1 : -1;
            lateralExtent = HalfLength(lateral);

            // The "SOLVED" mark sits on the cover, so it rides with it.
            var solved = ClickRouter.FindByName("SOLVED indication (appears after stamping)", "SOLVED indication");
            if (solved) cover.Add((solved, solved.position));

            CollectDeskEvidence();
            if (logLayout)
                Debug.Log($"[CaseFolder] papers {paperTemplates.Count}, photos {photoTemplates.Count}, memos {memoTemplates.Count}; base {stackBase:F2}, size {backBounds.size:F2}, expose {expose:F2} ({exposeExtent:F2}), lateral ({lateralExtent:F2}), emptyDrop {emptyDrop:F3}, cover parts {cover.Count}");
            ApplyCover(0);
            if (clip) clip.gameObject.SetActive(false);
        }

        /// <summary>The loose evidence on the desk becomes the templates for clues and is hidden from the table.</summary>
        void CollectDeskEvidence()
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string n = t.name;
                if (n.StartsWith("EvidencePhoto_")) { photoTemplates.Add(t.gameObject); t.gameObject.SetActive(false); }
                else if (n.StartsWith("EvidenceMemo_")) { memoTemplates.Add(t.gameObject); t.gameObject.SetActive(false); }
                else if (n.StartsWith("MemoParchmentBacking_")) t.gameObject.SetActive(false);
                else if (n.StartsWith("Manila file ")) { var r = t.GetComponent<Renderer>(); if (r) r.enabled = false; }
            }
            photoTemplates.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            memoTemplates.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        /// <summary>Makes the clickable case-file box match the folder, whatever way the folder is turned.</summary>
        public void FitClickCollider(Transform clickRoot)
        {
            if (!folder || !clickRoot) return;
            var box = clickRoot.GetComponent<BoxCollider>();
            if (!box) return;
            var b = ClickRouter.RendererBounds(folder, folder.position);
            b.Expand(new Vector3(0.05f, 0.25f, 0.05f));
            box.center = clickRoot.InverseTransformPoint(b.center);
            var s = clickRoot.lossyScale;
            box.size = new Vector3(b.size.x / Mathf.Max(1e-4f, Mathf.Abs(s.x)), b.size.y / Mathf.Max(1e-4f, Mathf.Abs(s.y)), b.size.z / Mathf.Max(1e-4f, Mathf.Abs(s.z)));
        }

        // ------------------------------------------------------------------ public API
        /// <summary>Shows exactly <paramref name="count"/> clues for case <paramref name="caseLevel"/>; new ones slide in when animated.</summary>
        public void SetCount(int count, int caseLevel, bool animate)
        {
            if (!folder) return;
            if (caseLevel != level || count < items.Count) { ClearImmediate(); level = caseLevel; }
            while (items.Count < count) AddItem(items.Count, animate && items.Count == count - 1);
            coverTarget = StackHeight(items.Count);
            if (!animate) ApplyCover(coverTarget);
            if (clip) clip.gameObject.SetActive(items.Count > 0);
        }

        /// <summary>Case approved: the contents slide out of the folder, which is empty afterwards.</summary>
        public void ClearAnimated()
        {
            if (!folder) return;
            if (clearing != null) StopCoroutine(clearing);
            clearing = StartCoroutine(ClearRoutine());
        }

        // ------------------------------------------------------------------ items
        float StackHeight(int count) => Mathf.Min(count, maxVisualItems) * itemThickness;

        void AddItem(int index, bool animate)
        {
            var kind = Pattern[(index + level) % Pattern.Length];
            var templates = kind == Kind.Photo ? photoTemplates : kind == Kind.Memo ? memoTemplates : paperTemplates;
            if (templates.Count == 0) { templates = paperTemplates; kind = Kind.Paper; }
            if (templates.Count == 0) return;
            var template = templates[(index * 7 + level * 3) % templates.Count];

            var go = Instantiate(template, runtimeRoot, true);
            go.name = kind + " clue " + (index + 1);
            go.SetActive(true);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(mb);
            var t = go.transform;

            // Deterministic scatter so a reload looks the same: papers stay mostly aligned, photos and memos
            // stick out of the top and side edges so the clues are visible under the cover.
            float h = Hash(index, level);
            float yaw, sx, sz;
            // sx: sideways across the folder, sz: towards the open edge (fractions of the folder's half size).
            switch (kind)
            {
                case Kind.Photo: yaw = Mathf.Lerp(-14, 14, h); sx = Mathf.Lerp(-0.5f, 0.5f, Hash(index + 11, level)); sz = Mathf.Lerp(0.45f, 0.75f, Hash(index + 23, level)); break;
                case Kind.Memo: yaw = Mathf.Lerp(-9, 9, h); sx = Mathf.Lerp(-0.45f, 0.45f, Hash(index + 5, level)); sz = Mathf.Lerp(0.35f, 0.6f, Hash(index + 31, level)); break;
                default: yaw = Mathf.Lerp(-2.5f, 2.5f, h); sx = Mathf.Lerp(-0.03f, 0.03f, Hash(index + 3, level)); sz = Mathf.Lerp(0.0f, 0.06f, Hash(index + 7, level)); break;
            }
            if (kind != Kind.Paper) FitInside(t, 0.62f);
            t.rotation = Quaternion.AngleAxis(yaw, up) * t.rotation;

            var b = ClickRouter.RendererBounds(t, t.position);
            Vector3 centerOffset = t.position - b.center;
            float layer = StackHeight(index) + Mathf.Min(index, maxVisualItems) * 0.0001f;
            float along = sz * exposeExtent, side = sx * lateralExtent;
            if (kind != Kind.Paper)
            {
                // Photos and memos poke out of the side edge that faces the middle of the desk, where the camera sees them,
                // so the later sheets on top don't hide them.
                float halfSide = Mathf.Abs(lateral.x) * b.extents.x + Mathf.Abs(lateral.z) * b.extents.z;
                side = sideSign * (lateralExtent - halfSide + lateralExtent * Mathf.Lerp(0.12f, 0.32f, Hash(index + 41, level)));
                along = Mathf.Lerp(-0.45f, 0.45f, Hash(index + 23, level)) * exposeExtent;
            }
            Vector3 targetCenter = stackBase + up * (layer + b.extents.y) + lateral * side + expose * along;
            Vector3 target = targetCenter + centerOffset;
            items.Add(t);
            if (logLayout) Debug.Log($"[CaseFolder] {go.name} from {template.name}: size {b.size:F2} → {target:F2}");

            if (animate) StartCoroutine(Slide(t, target + lateral * sideSign * lateralExtent * 1.2f + up * 0.12f, target));
            else t.position = target;
        }

        float HalfLength(Vector3 dir) => Mathf.Abs(dir.x) * backBounds.extents.x + Mathf.Abs(dir.z) * backBounds.extents.z;

        /// <summary>Scales a photo/memo down if it is bigger than <paramref name="fraction"/> of the folder.</summary>
        void FitInside(Transform t, float fraction)
        {
            var b = ClickRouter.RendererBounds(t, t.position);
            float k = Mathf.Min(1, fraction * backBounds.size.x / Mathf.Max(0.001f, b.size.x), fraction * backBounds.size.z / Mathf.Max(0.001f, b.size.z));
            if (k < 1) t.localScale *= k;
        }

        IEnumerator Slide(Transform t, Vector3 from, Vector3 to)
        {
            t.position = from;
            float startLift = coverLift;
            for (float k = 0; k < 1; k += Time.deltaTime / slideSeconds)
            {
                if (!t) yield break;
                float e = 1 - (1 - k) * (1 - k) * (1 - k);
                t.position = Vector3.Lerp(from, to, e) + up * Mathf.Sin(k * Mathf.PI) * 0.04f;
                ApplyCover(Mathf.Lerp(startLift, coverTarget, e) + Mathf.Sin(k * Mathf.PI) * 0.03f);   // the cover lifts as the clue slides under it
                yield return null;
            }
            if (t) t.position = to;
            ApplyCover(coverTarget);
        }

        IEnumerator ClearRoutine()
        {
            var leaving = new List<Transform>(items);
            items.Clear();
            var starts = new List<Vector3>();
            foreach (var t in leaving) starts.Add(t ? t.position : Vector3.zero);
            float lift = coverLift;
            for (float k = 0; k < 1; k += Time.deltaTime / 0.6f)
            {
                float e = k * k;
                for (int i = 0; i < leaving.Count; i++)
                    if (leaving[i]) leaving[i].position = starts[i] + lateral * e * lateralExtent * 2.8f + up * Mathf.Sin(k * Mathf.PI) * 0.05f;
                ApplyCover(Mathf.Lerp(lift, 0, e));
                yield return null;
            }
            foreach (var t in leaving) if (t) Destroy(t.gameObject);
            coverTarget = 0;
            ApplyCover(0);
            if (clip) clip.gameObject.SetActive(false);
            clearing = null;
        }

        void ClearImmediate()
        {
            if (clearing != null) { StopCoroutine(clearing); clearing = null; }
            foreach (var t in items) if (t) Destroy(t.gameObject);
            items.Clear();
            coverTarget = 0;
            ApplyCover(0);
        }

        void ApplyCover(float lift)
        {
            coverLift = lift;
            Vector3 delta = up * (lift - emptyDrop);
            foreach (var (t, basePos) in cover) if (t) t.position = basePos + delta;
            if (clip) clip.position = clipBase + delta;
        }

        static float Hash(int a, int b)
        {
            unchecked
            {
                uint h = (uint)(a * 374761393 + b * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }
    }
}
