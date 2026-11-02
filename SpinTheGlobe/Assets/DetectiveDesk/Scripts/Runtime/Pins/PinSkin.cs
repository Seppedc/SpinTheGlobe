using System;
using System.Collections.Generic;
using UnityEngine;

namespace DetectiveDesk
{
    /// <summary>Swaps only the collectible pin's enamel material slot.</summary>
    [DisallowMultipleComponent]
    public sealed class PinSkin : MonoBehaviour
    {
        public const int SkinCount = 12;
        public static readonly string[] SkinNames =
        {
            "Ruby", "Sapphire", "Emerald", "Amber", "Amethyst", "Ivory",
            "Obsidian", "Turquoise", "Copper", "Gold", "Silver", "Midnight"
        };

        [SerializeField, Range(0, SkinCount - 1)] int skinIndex;
        [SerializeField] Renderer enamelHeadRenderer;
        [SerializeField] int enamelMaterialSlot;
        [SerializeField] Material[] skinMaterials = new Material[SkinCount];

        public int SkinIndex => skinIndex;
        public Renderer EnamelHeadRenderer => ResolveEnamelHead();
        public IReadOnlyList<Material> SkinMaterials => skinMaterials;

        void Awake() { ApplySkin(skinIndex, true); }

        void Reset()
        {
            ResolveEnamelHead();
#if UNITY_EDITOR
            for (int i = 0; i < SkinCount; i++)
                skinMaterials[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/DetectiveDesk/Art/Props/CollectiblePin/Materials/Skins/" + SkinNames[i] + ".mat");
#endif
            ApplySkin(skinIndex, false);
        }

        /// <summary>0=Ruby, 1=Sapphire, ... 11=Midnight. Does not mutate a Material asset.</summary>
        public void SetSkin(int index)
        {
            if (index < 0 || index >= SkinCount)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Pin skin must be between 0 and 11.");
            ApplySkin(index, true);
        }

        bool ApplySkin(int index, bool reportFailure)
        {
            var head = ResolveEnamelHead();
            if (!head || skinMaterials == null || skinMaterials.Length != SkinCount || !skinMaterials[index])
            {
                if (reportFailure) Debug.LogWarning("PinSkin needs its enamel head and all 12 skin materials assigned.", this);
                return false;
            }

            var slots = head.sharedMaterials;
            if (enamelMaterialSlot < 0 || enamelMaterialSlot >= slots.Length)
            {
                if (reportFailure) Debug.LogWarning("PinSkin enamel material slot is invalid.", this);
                return false;
            }

            // sharedMaterials returns an array copy. Assigning this renderer's slot
            // changes its reference, never the shared material's color or properties.
            if (slots[enamelMaterialSlot] != skinMaterials[index])
            {
                slots[enamelMaterialSlot] = skinMaterials[index];
                head.sharedMaterials = slots;
            }
            skinIndex = index;
            return true;
        }

        Renderer ResolveEnamelHead()
        {
            if (enamelHeadRenderer && enamelHeadRenderer.transform.IsChildOf(transform) && IsEnamelHead(enamelHeadRenderer))
                return enamelHeadRenderer;

            enamelHeadRenderer = null;
            foreach (var candidate in GetComponentsInChildren<Renderer>(true))
            {
                if (!IsEnamelHead(candidate)) continue;
                // Refuse ambiguous matches instead of recoloring an arbitrary component.
                if (enamelHeadRenderer) { enamelHeadRenderer = null; return null; }
                enamelHeadRenderer = candidate;
            }

            if (enamelHeadRenderer)
            {
                var slots = enamelHeadRenderer.sharedMaterials;
                enamelMaterialSlot = 0;
                for (int i = 0; i < slots.Length; i++)
                    if (slots[i] && slots[i].name.StartsWith("MAT_Pin_Enamel", StringComparison.Ordinal))
                        enamelMaterialSlot = i;
            }
            return enamelHeadRenderer;
        }

        static bool IsEnamelHead(Renderer candidate)
        {
            // The collar/bezel also contain "enamel" in their names: do not match that alone.
            string objectName = candidate.name;
            return objectName.IndexOf("RECOLOR THIS", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   objectName.IndexOf("Enamel head", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void OnValidate()
        {
            skinIndex = Mathf.Clamp(skinIndex, 0, SkinCount - 1);
#if UNITY_EDITOR
            // Material assignment is deferred out of Unity's asset-loading validation callback.
            UnityEditor.EditorApplication.delayCall -= PreviewValidatedSkin;
            UnityEditor.EditorApplication.delayCall += PreviewValidatedSkin;
#endif
        }

#if UNITY_EDITOR
        void PreviewValidatedSkin()
        {
            if (!this || Application.isPlaying) return;
            ApplySkin(skinIndex, false);
        }

        void OnDestroy() { UnityEditor.EditorApplication.delayCall -= PreviewValidatedSkin; }
#endif
    }
}
