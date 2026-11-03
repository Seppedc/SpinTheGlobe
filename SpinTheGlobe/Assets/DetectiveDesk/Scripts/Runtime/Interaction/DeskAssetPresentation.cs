using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk
{
    /// <summary>Connects authored archive and effects to existing gameplay, without granting rewards itself.</summary>
    public sealed class DeskAssetPresentation : MonoBehaviour
    {
        public DetectiveGame game;
        public GameObject archive;
        public Animation completion;
        public Texture2D[] evidenceTextures;
        public PinCofferSlot[] slots;
        public Button archiveButton, closeButton, previousEvidenceButton, nextEvidenceButton, previousPinsButton, nextPinsButton;
        public RawImage evidenceImage;
        public Text evidenceTitle, evidenceBody, evidenceHeader, pinHint, statusText, caseMetadata;
        public Text[] pinNames, pinNumbers;
        public Image[] pinHeads;
        public int EvidenceIndex { get; private set; }
        public int PinPage { get; private set; }
        public int CompletionPlayCount { get; private set; }
        public bool IsArchiveOpen => archive && archive.activeSelf;
        static readonly string[] Titles = { "Rainy passage", "Warehouse windows", "Unknown visitor", "Appointment note", "Timetable note", "Reminder note" };
        static readonly string[] Details = {
            "PHOTOGRAPH / PASSAGE\n\nA street lamp illuminates a rain-soaked passage. Compare it with the witness notes.",
            "PHOTOGRAPH / WAREHOUSE\n\nA row of windows above a warehouse entrance. The image can be replaced for a future case.",
            "PHOTOGRAPH / VISITOR\n\nA silhouetted visitor near a window. Identity is unconfirmed.",
            "MEMO / APPOINTMENT\n\nThursday, after closing.\nThe lamp was still warm.\nSomeone left in a hurry.\nAsk the night porter who had the spare key.",
            "MEMO / TIMETABLE\n\n9:10 - last tram\n9:25 - lights upstairs\n9:40 - rain began\n\nCompare the witness's time with the ledger.",
            "MEMO / REMINDER\n\nReturn the borrowed key.\nKeep the envelope dry.\nThere are two signatures. Only one matches.\nLook beneath the date."
        };

        void Awake()
        {
            archiveButton.onClick.AddListener(ToggleArchive);
            closeButton.onClick.AddListener(CloseArchive);
            previousEvidenceButton.onClick.AddListener(() => SelectEvidence(EvidenceIndex - 1));
            nextEvidenceButton.onClick.AddListener(() => SelectEvidence(EvidenceIndex + 1));
            previousPinsButton.onClick.AddListener(() => SetPinPage(PinPage - 1));
            nextPinsButton.onClick.AddListener(() => SetPinPage(PinPage + 1));
            CloseArchive();
            if (completion) { completion.playAutomatically = false; completion.gameObject.SetActive(false); }
        }

        void OnEnable()
        {
            if (game) game.CaseSolved += PlayCompletion;   // the archive pop-up is no longer opened from the desk
            foreach (var slot in slots) if (slot) slot.Selected += SlotSelected;
        }

        void OnDisable()
        {
            if (game) game.CaseSolved -= PlayCompletion;
            foreach (var slot in slots) if (slot) slot.Selected -= SlotSelected;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseArchive();
            if (IsArchiveOpen) RefreshPins();
        }

        public void ToggleArchive() { if (IsArchiveOpen) CloseArchive(); else OpenArchive(); }
        public void OpenArchive()
        {
            if (game) { if (game.settingsPanel) game.settingsPanel.SetActive(false); if (game.upgradesPanel) game.upgradesPanel.SetActive(false); }
            archive.SetActive(true);
            SelectEvidence(EvidenceIndex);
            SetPinPage(PinPage);
            RefreshStatus();
        }
        public void CloseArchive() { if (archive) archive.SetActive(false); }
        public void SelectEvidence(int index)
        {
            if (evidenceTextures == null || evidenceTextures.Length == 0) return;
            EvidenceIndex = (index % evidenceTextures.Length + evidenceTextures.Length) % evidenceTextures.Length;
            evidenceImage.texture = evidenceTextures[EvidenceIndex];
            evidenceTitle.text = Titles[EvidenceIndex];
            evidenceBody.text = Details[EvidenceIndex];
            evidenceHeader.text = "EVIDENCE / " + (EvidenceIndex + 1).ToString("00") + " OF " + evidenceTextures.Length.ToString("00");
        }
        public void InspectEvidence(int index) { OpenArchive(); SelectEvidence(index); }
        public void SetPinPage(int page) { PinPage = (page % 3 + 3) % 3; RefreshPins(); }
        public void RefreshPins()
        {
            int occupied = slots.Count(s => s && !s.IsEmpty);
            pinHint.text = occupied + " / 12 sockets occupied   /   page " + (PinPage + 1) + " of 3";
            for (int i = 0; i < pinNames.Length; i++)
            {
                var slot = slots.FirstOrDefault(s => s && s.SlotNumber == PinPage * 4 + i + 1);
                var pin = slot ? slot.Occupant : null;
                pinNumbers[i].text = (PinPage * 4 + i + 1).ToString("00");
                pinNames[i].text = pin ? PinSkin.SkinNames[pin.SkinIndex].ToUpperInvariant() : "EMPTY SOCKET";
                pinHeads[i].color = pin && pin.SkinMaterials[pin.SkinIndex] ? pin.SkinMaterials[pin.SkinIndex].GetColor("_BaseColor") : new Color(.23f, .26f, .23f);
            }
        }
        void SlotSelected(PinCofferSlot slot) { SetPinPage((slot.SlotNumber - 1) / 4); }
        void RefreshStatus()
        {
            bool solved = game && game.IsCaseSolved;
            statusText.text = solved ? "CASE SOLVED" : "ACTIVE CASE";
            caseMetadata.text = solved ? "SOLVED / +50 awarded" : "ACTIVE / 6 evidence items";
        }
        void PlayCompletion()
        {
            RefreshStatus();
            CloseArchive();
            if (!completion || !completion.clip) return;
            completion.gameObject.SetActive(true);
            completion.Stop();
            completion.GetComponent<CanvasGroup>().alpha = 0;
            completion.Play(completion.clip.name, PlayMode.StopAll);
            CompletionPlayCount++;
        }
    }
}
