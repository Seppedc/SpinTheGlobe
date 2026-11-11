using System.Collections.Generic;
using DetectiveDesk.Core;
using DetectiveDesk.Save;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Coordinates the physical paper ball pile on the desk, smooth camera transitions
    /// between the desk and the basketball hoop, mid-air hover aiming with physics trajectory arc,
    /// and hoop scoring.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PaperBallThrower : MonoBehaviour
    {
        public enum HoopMode { Desk, TransitioningToHoop, InHoopMode, TransitioningToDesk }

        public Transform basketTarget;
        public Transform throwOrigin;
        public Transform paperVisual;
        public Transform scoreAnchor;
        public Transform pileAnchor;
        public PaperBallInstance paperBallPrefab;

        [SerializeField] Canvas scoreCanvas;
        [SerializeField] TextMeshProUGUI scoreLabel;
        public Canvas ScoreCanvas => scoreCanvas;
        public long ScoredShots { get; private set; }

        [SerializeField] Canvas aimCanvas;
        [SerializeField] TextMeshProUGUI hint;
        [SerializeField] Button backButton;

        public const int MaxVisualPileCount = 12;
        readonly List<PaperBallInstance> activePile = new List<PaperBallInstance>(MaxVisualPileCount);
        public IReadOnlyList<PaperBallInstance> ActivePile => activePile;

        GameManager game;
        Camera activeCamera;
        public Camera ActiveCamera
        {
            get => activeCamera ? activeCamera : (activeCamera = game && game.GameCamera ? game.GameCamera : Camera.main);
            set => activeCamera = value;
        }

        // Camera positions and rotations
        readonly Vector3 defaultDeskCamPos = new Vector3(0f, 2.38f, -5.8f);
        readonly Quaternion defaultDeskCamRot = Quaternion.Euler(4.986f, 0f, 0f);
        readonly Vector3 defaultHoopCamPos = new Vector3(3.90f, 2.65f, -4.80f);
        readonly Quaternion defaultHoopCamRot = Quaternion.Euler(8.0f, 16.5f, 0f);
        Vector3 camStartPos, camEndPos;
        Quaternion camStartRot, camEndRot;
        float camTransitionTimer = -1f;
        const float CamTransitionDuration = 0.42f;

        HoopMode currentMode = HoopMode.Desk;
        public HoopMode CurrentMode => currentMode;

        // Hover ball & Trajectory
        GameObject hoverBall;
        LineRenderer trajectoryLine;
        bool isDragging;
        Vector2 dragStartMouse;
        Vector3 lastComputedVelocity;
        bool waitingForShotResult;
        [Tooltip("Seconds after shooting before spawning the next ball or returning to desk.")]
        public float postShotWaitSeconds = 1.5f;
        float shotResultTimer;

        [Header("Celebration & Visual Feedback")]
        [SerializeField] Light hoopLight;
        float hoopLightBaseIntensity = 2.4f;
        float hoopLightFlashTimer;
        float scorePlatePunchTimer;
        RectTransform scorePlateRect;
        Image scorePlateImg;
        Color scorePlateBaseColor;
        bool hasScoredThisShot;

        // Floating 3D Swish Popup
        Canvas swishCanvas;
        TextMeshProUGUI swishLabel;
        RectTransform swishRect;
        float swishTimer;

        // Legacy / Test compatibility fields
        float legacyElapsed, resultRemaining;
        int aimStartFrame;
        Vector3 legacyStart, legacyDestination, visualScale;
        bool legacyScored;
        bool legacyIsAiming;
        bool legacyIsThrowing;

        public bool IsAiming => currentMode == HoopMode.InHoopMode || currentMode == HoopMode.TransitioningToHoop || legacyIsAiming;
        public bool IsThrowing => legacyIsThrowing || waitingForShotResult || currentMode == HoopMode.TransitioningToDesk;

        public Canvas CreateLayout(GameManager owner)
        {
            game = owner;
            if (!scoreCanvas && scoreAnchor)
            {
                scoreCanvas = UIFactory.CreateCanvas("Paper Hoop Score", 15);
                scoreCanvas.renderMode = RenderMode.WorldSpace;
                var rect = (RectTransform)scoreCanvas.transform;
                rect.sizeDelta = new Vector2(400, 110);
                rect.localScale = Vector3.one * .0028f;
                rect.position = scoreAnchor.position;
                rect.rotation = scoreAnchor.rotation;
                var plate = UIFactory.NewPanel("Score plate", rect);
                plate.raycastTarget = false;
                plate.rectTransform.Stretch();
                scorePlateRect = rect;
                scorePlateImg = plate.GetComponent<Image>();
                scorePlateBaseColor = scorePlateImg ? scorePlateImg.color : new Color(0.12f, 0.08f, 0.05f, 0.90f);
                scoreLabel = UIFactory.NewTitle("Scored", plate.transform, "SCORED 0", 38, TextAnchor.MiddleCenter, UITheme.Gold);
                scoreLabel.rectTransform.Stretch(10);
                RefreshScore();
            }
            else if (scoreCanvas && scoreAnchor)
            {
                scoreCanvas.transform.position = scoreAnchor.position;
                scoreCanvas.transform.rotation = scoreAnchor.rotation;
                RefreshScore();
            }

            if (!swishCanvas && basketTarget)
            {
                swishCanvas = UIFactory.CreateCanvas("Swish Celebration", 25);
                swishCanvas.renderMode = RenderMode.WorldSpace;
                swishRect = (RectTransform)swishCanvas.transform;
                swishRect.sizeDelta = new Vector2(400, 100);
                swishRect.localScale = Vector3.one * 0.0035f;
                swishRect.position = basketTarget.position + Vector3.up * 0.12f;
                swishLabel = UIFactory.NewTitle("Swish Label", swishRect, "★ SWISH! +1 ★", 52, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.35f, 1f));
                swishLabel.rectTransform.Stretch();

                // Dark outline for crisp high-contrast readability
                var mat = new Material(swishLabel.fontSharedMaterial);
                mat.EnableKeyword("OUTLINE_ON");
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.28f);
                mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.08f, 0.04f, 0.02f, 1f));
                swishLabel.fontSharedMaterial = mat;

                swishCanvas.gameObject.SetActive(false);
            }

            if (!aimCanvas)
            {
                aimCanvas = UIFactory.CreateCanvas("Paper Ball Aim", 110);

                // Instructions banner
                var panel = UIFactory.NewPanel("Instructions", aimCanvas.transform);
                panel.raycastTarget = false;
                panel.rectTransform.Place(new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 65), new Vector2(920, 75));
                hint = UIFactory.NewText("Hint", panel.transform, "Drag to aim & release to shoot · Right-click or Esc to return", 23, TextAnchor.MiddleCenter, UITheme.Cream);
                hint.rectTransform.Stretch(10);

                // Back to desk button in top left
                var backObj = new GameObject("Back Button", typeof(RectTransform), typeof(Image), typeof(Button));
                backObj.transform.SetParent(aimCanvas.transform, false);
                var backRect = (RectTransform)backObj.transform;
                backRect.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(25, -25), new Vector2(200, 48));
                var backImg = backObj.GetComponent<Image>();
                backImg.color = new Color(0.12f, 0.08f, 0.05f, 0.90f);
                backButton = backObj.GetComponent<Button>();
                backButton.targetGraphic = backImg;
                var colors = backButton.colors;
                colors.highlightedColor = new Color(0.25f, 0.18f, 0.12f, 1.0f);
                colors.pressedColor = new Color(0.35f, 0.25f, 0.15f, 1.0f);
                backButton.colors = colors;
                backButton.onClick.AddListener(ReturnToDesk);

                var backLabel = UIFactory.NewText("Label", backRect, "◀ BACK TO DESK", 18, TextAnchor.MiddleCenter, UITheme.Gold);
                backLabel.rectTransform.Stretch();

                aimCanvas.gameObject.SetActive(false);
            }

            return aimCanvas;
        }

        public void Initialize(GameManager owner)
        {
            game = owner;
            activeCamera = game && game.GameCamera ? game.GameCamera : Camera.main;
            CreateLayout(owner);
            currentMode = HoopMode.Desk;
            legacyIsThrowing = legacyIsAiming = false;
            waitingForShotResult = isDragging = false;

            if (paperVisual) { visualScale = paperVisual.localScale; paperVisual.gameObject.SetActive(false); }
            if (aimCanvas) aimCanvas.gameObject.SetActive(false);

            EnsurePileAnchor();
            EnsureHoverBall();
            EnsureTrajectoryLine();

            if (!hoopLight)
            {
                var hoop = GameObject.Find("Paper Basketball Hoop");
                if (hoop) hoopLight = hoop.GetComponentInChildren<Light>();
            }
            if (hoopLight) hoopLightBaseIntensity = hoopLight.intensity;

            if (game && game.Inventory != null)
            {
                game.Inventory.Changed -= OnInventoryChanged;
                game.Inventory.Changed += OnInventoryChanged;
                SyncPileWithInventory();
            }
        }

        void EnsurePileAnchor()
        {
            if (!pileAnchor)
            {
                var existing = GameObject.Find("Paper pile anchor");
                if (existing) pileAnchor = existing.transform;
                else
                {
                    var anchor = new GameObject("Paper pile anchor");
                    anchor.transform.SetParent(transform, false);
                    anchor.transform.position = new Vector3(0.45f, 0.82f, -0.85f);
                    pileAnchor = anchor.transform;
                }
            }
        }

        void EnsureHoverBall()
        {
            if (hoverBall) return;
            if (paperBallPrefab)
            {
                hoverBall = Instantiate(paperBallPrefab.gameObject, transform);
                hoverBall.name = "Hover Paper Ball (Aiming)";
                var rb = hoverBall.GetComponent<Rigidbody>();
                if (rb) rb.isKinematic = true;
                var col = hoverBall.GetComponent<Collider>();
                if (col) col.enabled = false;
                var instance = hoverBall.GetComponent<PaperBallInstance>();
                if (instance) SafeDestroy(instance);
                hoverBall.transform.localScale = Vector3.one * 1.0f;
                hoverBall.SetActive(false);
            }
            else if (paperVisual)
            {
                hoverBall = Instantiate(paperVisual.gameObject, transform);
                hoverBall.name = "Hover Paper Ball (Aiming)";
                hoverBall.transform.localScale = Vector3.one * 1.0f;
                hoverBall.SetActive(false);
            }
        }

        void EnsureTrajectoryLine()
        {
            if (trajectoryLine) return;
            var lineObj = new GameObject("Trajectory Arc");
            lineObj.transform.SetParent(transform, false);
            trajectoryLine = lineObj.AddComponent<LineRenderer>();

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            trajectoryLine.material = mat;
            trajectoryLine.startWidth = 0.042f;
            trajectoryLine.endWidth = 0.018f;
            trajectoryLine.useWorldSpace = true;
            trajectoryLine.positionCount = 35;
            trajectoryLine.numCapVertices = 4;
            trajectoryLine.numCornerVertices = 4;
            trajectoryLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trajectoryLine.receiveShadows = false;
            trajectoryLine.startColor = trajectoryLine.endColor = new Color(1.0f, 0.88f, 0.50f, 0.75f);
            lineObj.SetActive(false);
        }

        void OnInventoryChanged()
        {
            SyncPileWithInventory();
        }

        public void SyncPileWithInventory()
        {
            if (!game || game.Inventory == null || !paperBallPrefab) return;

            // Remove destroyed or thrown balls from active list
            for (int i = activePile.Count - 1; i >= 0; i--)
            {
                if (!activePile[i] || activePile[i].CurrentState != PaperBallInstance.State.InPile)
                {
                    activePile.RemoveAt(i);
                }
            }

            int invCount = game.Inventory.PaperBalls;
            int desiredCount = Mathf.Clamp(invCount, 0, MaxVisualPileCount);

            // Spawn balls to match inventory up to pile visual cap
            Vector3 center = pileAnchor ? pileAnchor.position : new Vector3(0.45f, 0.82f, -0.85f);
            while (activePile.Count < desiredCount)
            {
                int idx = activePile.Count;
                Vector3 spawnOffset = new Vector3(
                    Random.Range(-0.16f, 0.16f),
                    0.08f + idx * 0.045f,
                    Random.Range(-0.14f, 0.14f)
                );
                var ball = Instantiate(paperBallPrefab, center + spawnOffset, Random.rotation);
                ball.name = "Desk Paper Ball " + (idx + 1);
                ball.Owner = this;
                ball.transform.localScale = Vector3.one * 1.0f;
                activePile.Add(ball);
            }

            // If inventory reduced without throwing, trim excess
            while (activePile.Count > desiredCount)
            {
                int last = activePile.Count - 1;
                if (activePile[last]) SafeDestroy(activePile[last].gameObject);
                activePile.RemoveAt(last);
            }
        }

        static void SafeDestroy(Object obj)
        {
            if (!obj) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        [ContextMenu("Spawn 1 Paper Ball (P / F5)")]
        public void SpawnTestPaperBall()
        {
            if (game && game.Inventory != null)
            {
                game.Inventory.AddPaperBall(1);
            }
            else
            {
                EnsurePileAnchor();
                Vector3 center = pileAnchor ? pileAnchor.position : new Vector3(0.28f, 0.82f, -0.50f);
                if (paperBallPrefab)
                {
                    var ball = Instantiate(paperBallPrefab, center + Vector3.up * 0.25f, Random.rotation);
                    ball.name = "Test Paper Ball " + (activePile.Count + 1);
                    ball.Owner = this;
                    ball.transform.localScale = Vector3.one * 1.0f;
                    activePile.Add(ball);
                }
            }
        }

        [ContextMenu("Spawn 5 Paper Balls (F6)")]
        public void SpawnFiveTestPaperBalls()
        {
            for (int i = 0; i < 5; i++) SpawnTestPaperBall();
        }

        public void Use()
        {
            if (currentMode == HoopMode.Desk)
            {
                EnterHoopShootingMode();
            }
            else if (currentMode == HoopMode.InHoopMode)
            {
                ReturnToDesk();
            }
            else
            {
                if (legacyIsAiming) CancelAim(); else BeginAim();
            }
        }

        public void EnterHoopShootingMode()
        {
            if (game && game.Inventory != null && game.Inventory.PaperBalls <= 0)
            {
                ShowHint("No paper balls! Spin the globe to find cases & earn paper balls.");
                resultRemaining = 2.0f;
                return;
            }

            var cam = ActiveCamera;
            if (!cam) return;

            camStartPos = cam.transform.position;
            camStartRot = cam.transform.rotation;
            camEndPos = defaultHoopCamPos;
            camEndRot = defaultHoopCamRot;
            camTransitionTimer = 0f;
            currentMode = HoopMode.TransitioningToHoop;
            isDragging = false;
            waitingForShotResult = false;

            if (hoverBall) hoverBall.SetActive(false);
            if (trajectoryLine) trajectoryLine.gameObject.SetActive(false);

            ShowHint("Transitioning to hoop...");
        }

        public void ReturnToDesk()
        {
            if (currentMode == HoopMode.Desk) return;

            var cam = ActiveCamera;
            if (!cam) return;

            camStartPos = cam.transform.position;
            camStartRot = cam.transform.rotation;
            camEndPos = defaultDeskCamPos;
            camEndRot = defaultDeskCamRot;
            camTransitionTimer = 0f;
            currentMode = HoopMode.TransitioningToDesk;
            isDragging = false;
            waitingForShotResult = false;

            if (hoverBall) hoverBall.SetActive(false);
            if (trajectoryLine) trajectoryLine.gameObject.SetActive(false);

            ShowHint("Returning to desk...");
        }

        // Backward compatibility for existing unit tests
        public bool BeginAim()
        {
            if (!game || IsThrowing || IsAiming || game.WorldInputBlocked || !basketTarget || !throwOrigin || !paperVisual || game.Inventory.PaperBalls <= 0) return false;
            legacyIsAiming = true;
            aimStartFrame = Time.frameCount;
            resultRemaining = 0;
            paperVisual.position = throwOrigin.position;
            paperVisual.localScale = visualScale;
            paperVisual.gameObject.SetActive(true);
            ShowHint("Aim at the hoop - Click to throw - Right-click / Esc to cancel");
            return true;
        }

        public void CancelAim()
        {
            if (currentMode == HoopMode.InHoopMode || currentMode == HoopMode.TransitioningToHoop)
            {
                ReturnToDesk();
                return;
            }

            if (!legacyIsAiming) return;
            legacyIsAiming = false;
            if (paperVisual) paperVisual.gameObject.SetActive(false);
            if (aimCanvas) aimCanvas.gameObject.SetActive(false);
        }

        public bool TryThrowAt(Vector3 point)
        {
            if (!legacyIsAiming || legacyIsThrowing || !basketTarget || !paperVisual || !throwOrigin) return false;
            if (!float.IsFinite(point.x) || !float.IsFinite(point.y) || !float.IsFinite(point.z)) return false;
            point.y = basketTarget.position.y;
            if (Vector3.Distance(throwOrigin.position, point) > (game ? game.Balance.paperBallMaxAimDistance : 15f)) return false;
            if (game && !game.Inventory.TryUsePaperBall()) { CancelAim(); return false; }
            legacyStart = throwOrigin.position;
            legacyDestination = point;
            legacyElapsed = 0;
            legacyIsAiming = false;
            legacyIsThrowing = true;
            legacyScored = Vector3.Distance(legacyDestination, basketTarget.position) <= (game ? game.Balance.paperBallBasketRadius : 0.20f);
            ShowHint("...");
            if (game && game.audioManager) game.audioManager.Click();
            return true;
        }

        void Update()
        {
            if (!activeCamera) activeCamera = game && game.GameCamera ? game.GameCamera : Camera.main;

            // 1. Camera transitions
            if (camTransitionTimer >= 0f)
            {
                AdvanceCameraTransition(Time.unscaledDeltaTime);
            }

            // 2. Desk mode pile interaction
            if (currentMode == HoopMode.Desk && (!game || !game.WorldInputBlocked))
            {
                HandleDeskPileInput();
            }

            // 3. Hoop shooting mode
            if (currentMode == HoopMode.InHoopMode)
            {
                UpdateHoopShootingMode();
            }

            // 4. Legacy flight update
            AdvanceFlight(Time.unscaledDeltaTime);

            // 5. Result banner timeout
            if (currentMode == HoopMode.Desk && !legacyIsAiming && !legacyIsThrowing && resultRemaining > 0)
            {
                resultRemaining -= Time.unscaledDeltaTime;
                if (resultRemaining <= 0 && aimCanvas) aimCanvas.gameObject.SetActive(false);
            }

            // 6. Score celebration animations
            UpdateCelebrations();
        }

        void UpdateCelebrations()
        {
            float dt = Time.unscaledDeltaTime;

            // Score plate punch and golden glow animation
            if (scorePlatePunchTimer > 0f)
            {
                scorePlatePunchTimer -= dt;
                float t = Mathf.Clamp01(scorePlatePunchTimer / 0.5f);
                float scale = 0.0028f * (1f + Mathf.Sin(t * Mathf.PI) * 0.40f);
                if (scorePlateRect) scorePlateRect.localScale = Vector3.one * scale;
                if (scorePlateImg) scorePlateImg.color = Color.Lerp(scorePlateBaseColor, new Color(0.90f, 0.70f, 0.18f, 1f), t);
                if (scoreLabel) scoreLabel.color = Color.Lerp(UITheme.Gold, Color.white, t);
            }

            // Floating 3D swish popup animation
            if (swishTimer > 0f && swishRect && swishLabel)
            {
                swishTimer -= dt;
                float progress = 1f - Mathf.Clamp01(swishTimer / 1.5f);
                swishRect.position += Vector3.up * (dt * 0.45f);
                var cam = ActiveCamera;
                if (cam) swishRect.rotation = cam.transform.rotation;

                float alpha = Mathf.Clamp01(swishTimer / 0.4f);
                swishLabel.color = new Color(1f, 0.92f, 0.35f, alpha);
                float scaleMod = 1f + Mathf.Sin(Mathf.Clamp01(progress * 4f) * Mathf.PI * 0.5f) * 0.45f;
                swishRect.localScale = Vector3.one * (0.0035f * scaleMod);

                if (swishTimer <= 0f && swishCanvas)
                {
                    swishCanvas.gameObject.SetActive(false);
                }
            }

            // Hoop light flash animation
            if (hoopLightFlashTimer > 0f)
            {
                hoopLightFlashTimer -= dt;
                float t = Mathf.Clamp01(hoopLightFlashTimer / 0.45f);
                if (hoopLight) hoopLight.intensity = hoopLightBaseIntensity + t * 4.5f;
            }
        }

        public void AdvanceCameraTransition(float dt)
        {
            if (camTransitionTimer < 0f) return;
            var cam = ActiveCamera;

            camTransitionTimer += Mathf.Max(0.0001f, dt);
            float t = Mathf.Clamp01(camTransitionTimer / CamTransitionDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (cam)
            {
                cam.transform.position = Vector3.Lerp(camStartPos, camEndPos, smoothT);
                cam.transform.rotation = Quaternion.Slerp(camStartRot, camEndRot, smoothT);
            }

            if (t >= 1f)
            {
                camTransitionTimer = -1f;
                if (currentMode == HoopMode.TransitioningToHoop)
                {
                    currentMode = HoopMode.InHoopMode;
                    OnEnteredHoopMode();
                }
                else if (currentMode == HoopMode.TransitioningToDesk)
                {
                    currentMode = HoopMode.Desk;
                    if (aimCanvas) aimCanvas.gameObject.SetActive(false);
                }
            }
        }

        void OnEnteredHoopMode()
        {
            EnsureHoverBall();
            EnsureTrajectoryLine();

            if (hoverBall)
            {
                PositionHoverBall();
                hoverBall.SetActive(true);
            }

            ShowHint("Drag to aim & release to shoot · Right-click or Esc to return");
        }

        Vector3 GetBaseHoverPosition()
        {
            var cam = ActiveCamera;
            if (!cam) return new Vector3(5.62f, 1.62f, -3.95f);
            return cam.transform.position + cam.transform.forward * 1.75f + cam.transform.right * 0.28f + cam.transform.up * -0.42f;
        }

        void PositionHoverBall()
        {
            if (!hoverBall) return;
            Vector3 basePos = GetBaseHoverPosition();
            float idleBob = Mathf.Sin(Time.time * 2.8f) * 0.015f;
            hoverBall.transform.position = basePos + Vector3.up * idleBob;
            hoverBall.transform.Rotate(Vector3.up, 18f * Time.deltaTime, Space.World);
        }

        void UpdateHoopShootingMode()
        {
            // Cancel / Return to desk with Escape or Right-click
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                if (isDragging)
                {
                    isDragging = false;
                    if (trajectoryLine) trajectoryLine.gameObject.SetActive(false);
                    ShowHint("Aim canceled. Drag to aim · Right-click or Esc to return");
                    return;
                }
                ReturnToDesk();
                return;
            }

            // Post-shot cooldown & auto-reset
            if (waitingForShotResult)
            {
                shotResultTimer -= Time.deltaTime;
                if (shotResultTimer <= 0f)
                {
                    waitingForShotResult = false;
                    if (game && game.Inventory != null && game.Inventory.PaperBalls > 0)
                    {
                        if (hoverBall)
                        {
                            PositionHoverBall();
                            hoverBall.SetActive(true);
                        }
                        if (hasScoredThisShot)
                        {
                            ShowHint("Next ball ready! Drag to shoot · Right-click or Esc to return");
                        }
                        else
                        {
                            ShowHint("Missed — adjust aim & try again! (Right-click or Esc to return)");
                        }
                    }
                    else
                    {
                        ShowHint(hasScoredThisShot ? "Final shot scored! Returning to desk..." : "Out of paper balls! Returning to desk...");
                        ReturnToDesk();
                    }
                }
                return;
            }

            // If not dragging, idle hover ball
            if (!isDragging)
            {
                PositionHoverBall();

                // Mouse down on hover ball or screen to start dragging
                if (Input.GetMouseButtonDown(0) && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
                {
                    isDragging = true;
                    dragStartMouse = Input.mousePosition;
                    if (trajectoryLine) trajectoryLine.gameObject.SetActive(true);
                }
            }
            else
            {
                // Dragging in progress: calculate aim and render trajectory
                UpdateDragAim();

                if (Input.GetMouseButtonUp(0))
                {
                    ExecuteHoopThrow();
                }
            }
        }

        public void UpdateDragAim()
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 delta = mousePos - dragStartMouse;

            Vector3 baseHoverPos = GetBaseHoverPosition();
            Vector3 targetPos = basketTarget ? basketTarget.position : new Vector3(5.70f, 1.28f, 1.27f);

            // Baseline ballistic velocity to hit hoop center from hover position
            float flightTime = 0.86f;
            float g = Mathf.Abs(Physics.gravity.y);
            float baseVz = (targetPos.z - baseHoverPos.z) / flightTime;
            float baseVy = (targetPos.y - baseHoverPos.y + 0.5f * g * flightTime * flightTime) / flightTime;
            float baseVx = (targetPos.x - baseHoverPos.x) / flightTime;

            float screenH = Mathf.Max(100f, Screen.height);
            float screenW = Mathf.Max(100f, Screen.width);

            // Full-range directional aim:
            // Drag UP (delta.y > 0) -> Higher, loftier, deeper arc
            // Drag DOWN (delta.y < 0) -> Flatter, lower, smaller arc
            // Drag LEFT / RIGHT -> Steer trajectory
            float forwardFactor = Mathf.Clamp(delta.y / (screenH * 0.35f), -0.75f, 1.25f);
            float steerFactor = Mathf.Clamp(delta.x / (screenW * 0.30f), -1.25f, 1.25f);

            float vx = baseVx + steerFactor * 4.8f;
            float vy = Mathf.Max(0.8f, baseVy + forwardFactor * 4.2f);
            float vz = Mathf.Max(2.0f, baseVz + forwardFactor * 5.2f);

            lastComputedVelocity = new Vector3(vx, vy, vz);

            // Visual ball movement following cursor drag
            if (hoverBall)
            {
                Vector3 dragOffset = new Vector3(steerFactor * 0.12f, forwardFactor * 0.10f, forwardFactor * 0.14f);
                hoverBall.transform.position = baseHoverPos + dragOffset;
            }

            // Draw trajectory arc
            if (trajectoryLine && trajectoryLine.gameObject.activeSelf)
            {
                Vector3 origin = hoverBall ? hoverBall.transform.position : baseHoverPos;
                bool onTarget = false;
                for (int i = 0; i < trajectoryLine.positionCount; i++)
                {
                    float t = i * 0.030f;
                    Vector3 pt = origin + lastComputedVelocity * t + 0.5f * Physics.gravity * t * t;
                    trajectoryLine.SetPosition(i, pt);

                    if (Mathf.Abs(pt.y - targetPos.y) < 0.28f && Vector3.Distance(new Vector3(pt.x, 0, pt.z), new Vector3(targetPos.x, 0, targetPos.z)) < 0.24f)
                    {
                        onTarget = true;
                    }
                }

                // Vibrant green-gold when on target, soft amber otherwise
                Color lineColor = onTarget ? new Color(0.28f, 1.0f, 0.45f, 0.95f) : new Color(1.0f, 0.88f, 0.50f, 0.75f);
                trajectoryLine.startColor = trajectoryLine.endColor = lineColor;
            }
        }

        void ExecuteHoopThrow()
        {
            isDragging = false;
            if (trajectoryLine) trajectoryLine.gameObject.SetActive(false);

            if (!paperBallPrefab)
            {
                ShowHint("Missing paper ball prefab!");
                return;
            }

            if (game && game.Inventory != null && game.Inventory.PaperBalls <= 0)
            {
                ShowHint("Out of paper balls!");
                ReturnToDesk();
                return;
            }

            Vector3 spawnPos = hoverBall ? hoverBall.transform.position : GetBaseHoverPosition();
            var launchedBall = Instantiate(paperBallPrefab, spawnPos, Random.rotation);
            launchedBall.name = "Thrown Paper Ball";
            launchedBall.Owner = this;
            launchedBall.transform.localScale = Vector3.one * 1.0f;

            // Add backspin torque for realistic basketball flight
            Vector3 backspin = new Vector3(-9.0f, Random.Range(-2.5f, 2.5f), Random.Range(-2.5f, 2.5f));
            launchedBall.Launch(lastComputedVelocity, backspin);

            if (hoverBall) hoverBall.SetActive(false);

            hasScoredThisShot = false;
            waitingForShotResult = true;
            shotResultTimer = postShotWaitSeconds;

            ShowHint("Ball in flight...");
            SyncPileWithInventory();
        }

        void HandleDeskPileInput()
        {
            if (!activeCamera) return;
            if (Input.GetMouseButtonDown(0) && !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
            {
                var ray = activeCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 20f))
                {
                    var ball = hit.collider.GetComponentInParent<PaperBallInstance>();
                    if (!ball && hit.rigidbody) ball = hit.rigidbody.GetComponent<PaperBallInstance>();
                    if (ball && ball.CurrentState == PaperBallInstance.State.InPile)
                    {
                        EnterHoopShootingMode();
                        return;
                    }

                    Vector3 center = pileAnchor ? pileAnchor.position : new Vector3(0.45f, 0.82f, -0.85f);
                    if (Vector3.Distance(hit.point, center) < 0.50f && activePile.Count > 0)
                    {
                        EnterHoopShootingMode();
                    }
                }
            }
        }

        public void OnBallThrown(PaperBallInstance ball)
        {
            if (activePile.Contains(ball)) activePile.Remove(ball);
            if (game && game.Inventory != null) game.Inventory.TryUsePaperBall();
            if (game && game.audioManager) game.audioManager.Launch();
            SyncPileWithInventory();
        }

        public void OnHoopScored(PaperBallInstance ball = null)
        {
            if (ScoredShots < long.MaxValue) ScoredShots++;
            RefreshScore();
            EventBus.Publish(new PaperShotScored(ScoredShots));
            if (game && game.audioManager) game.audioManager.Payout();
            if (Application.isPlaying && game && game.useSaveFile) game.SaveGame();

            hasScoredThisShot = true;
            scorePlatePunchTimer = 0.5f;
            hoopLightFlashTimer = 0.45f;

            if (swishCanvas && basketTarget)
            {
                swishTimer = 1.5f;
                swishRect.position = basketTarget.position + Vector3.up * 0.12f;
                var cam = ActiveCamera;
                if (cam) swishRect.rotation = cam.transform.rotation;
                swishCanvas.gameObject.SetActive(true);
            }

            ShowHint("★ SWISH! SCORED +1! ★");
        }

        public void PlayBounceSound(Vector3 position)
        {
            if (game && game.audioManager) game.audioManager.Click();
        }

        public void AdvanceFlight(float seconds)
        {
            if (!legacyIsThrowing) return;
            if (!paperVisual) { legacyIsThrowing = false; return; }
            legacyElapsed += Mathf.Max(0, seconds);
            float duration = Mathf.Max(.1f, game ? game.Balance.paperBallFlightSeconds : 1.1f);
            float t = Mathf.Clamp01(legacyElapsed / duration);
            float arc = game ? game.Balance.paperBallArcHeight : 1.3f;
            paperVisual.position = Vector3.Lerp(legacyStart, legacyDestination, t) + Vector3.up * (4 * arc * t * (1 - t));
            paperVisual.rotation = Quaternion.Euler(t * 720, t * 360, t * 270);
            if (t < 1) return;
            float dropDuration = Mathf.Max(.1f, game ? game.Balance.paperBallDropSeconds : 0.25f);
            float drop = Mathf.Clamp01((legacyElapsed - duration) / dropDuration);
            paperVisual.position = legacyDestination + Vector3.down * (.5f * drop * drop);
            if (drop < 1) return;
            paperVisual.gameObject.SetActive(false);
            legacyIsThrowing = false;
            if (legacyScored)
            {
                OnHoopScored();
            }
            ShowHint(legacyScored ? "Scored!" : "Missed - try again");
            resultRemaining = game ? game.Balance.paperBallResultSeconds : 1.5f;
        }

        public void LoadState(SaveData data)
        {
            ScoredShots = System.Math.Max(0, data.paperShotsScored);
            RefreshScore();
            SyncPileWithInventory();
        }

        public void SaveState(SaveData data)
        {
            data.paperShotsScored = ScoredShots;
        }

        void RefreshScore()
        {
            if (scoreLabel) scoreLabel.text = "SCORED " + ScoredShots.ToString("N0");
        }

        void LateUpdate()
        {
            if (!scoreCanvas || !scoreAnchor) return;
            scoreCanvas.transform.position = scoreAnchor.position;
            scoreCanvas.transform.rotation = scoreAnchor.rotation;
        }

        void ShowHint(string text)
        {
            if (!aimCanvas || !hint) return;
            hint.text = text;
            aimCanvas.gameObject.SetActive(true);
        }

        void OnDisable()
        {
            currentMode = HoopMode.Desk;
            legacyIsAiming = legacyIsThrowing = false;
            if (paperVisual) paperVisual.gameObject.SetActive(false);
            if (aimCanvas) aimCanvas.gameObject.SetActive(false);
            if (hoverBall) hoverBall.SetActive(false);
            if (trajectoryLine) trajectoryLine.gameObject.SetActive(false);
            if (game && game.Inventory != null)
            {
                game.Inventory.Changed -= OnInventoryChanged;
            }
        }
    }
}
