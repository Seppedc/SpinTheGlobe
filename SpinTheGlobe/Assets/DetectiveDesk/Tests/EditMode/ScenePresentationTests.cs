using DetectiveDesk.Gameplay;
using DetectiveDesk.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk.Tests
{
    public sealed class ScenePresentationTests
    {
        GameObject root;
        GameManager game;
        GameBalance balance;
        SkillTreeAsset tree;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Scene presentation test");
            root.SetActive(false);
            game = root.AddComponent<GameManager>();
            balance = ScriptableObject.CreateInstance<GameBalance>();
            tree = ScriptableObject.CreateInstance<SkillTreeAsset>();
            tree.SetNodes(DefaultSkillTreeFactory.Create());
            tree.SetSections(DefaultSkillTreeFactory.CreateSections());
            game.balance = balance;
            game.skillTreeAsset = tree;
            game.useSaveFile = false;
            game.PrepareScenePresentation();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(balance);
            Object.DestroyImmediate(tree);
        }

        void CreateLayouts()
        {
            game.Hud.CreateLayout(game).transform.SetParent(root.transform, false);
            game.TreeView.CreateLayout(game).transform.SetParent(root.transform, false);
            game.Settings.CreateLayout(game).transform.SetParent(root.transform, false);
            game.ShopView.CreateLayout(game).transform.SetParent(root.transform, false);
            game.DeskInfo.CreateLayout(game).SetParent(root.transform, false);
        }

        [Test]
        public void AuthoredDrinkObjects_StartHidden_WithUnlockBindingsAndShopRow()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/DetectiveDesk/Scenes/DetectiveOffice.unity");
            try
            {
                WhiskeyGlass drink = null;
                GoldShopView shop = null;
                foreach (var sceneRoot in scene.GetRootGameObjects())
                {
                    if (!drink) drink = sceneRoot.GetComponentInChildren<WhiskeyGlass>(true);
                    if (!shop) shop = sceneRoot.GetComponentInChildren<GoldShopView>(true);
                }
                Assert.IsNotNull(drink);
                foreach (var prop in new[] { drink.glassModel, drink.glassInteraction, drink.glassRim,
                    drink.bottleModel, drink.bottleInteraction, drink.bottleCap })
                {
                    Assert.IsNotNull(prop, "hidden objects must retain their serialized unlock binding");
                    Assert.IsFalse(prop.gameObject.activeSelf);
                }
                Assert.IsNotNull(shop);
                var rows = new SerializedObject(shop).FindProperty("rows");
                int count = 0;
                for (int i = 0; i < rows.arraySize; i++)
                    if (rows.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == GoldShopCatalog.Whiskey) count++;
                Assert.AreEqual(1, count);
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void WhiskeyObjectsAndInteraction_AreLockedUntilPermanentPurchase()
        {
            var drink = root.AddComponent<WhiskeyGlass>();
            Transform Prop(string name)
            {
                var prop = new GameObject(name).transform;
                prop.SetParent(root.transform, false);
                prop.gameObject.AddComponent<BoxCollider>();
                return prop;
            }
            drink.glassModel = Prop("Test drink model");
            drink.glassInteraction = Prop("Test drink hit target");
            drink.glassRim = Prop("Test drink rim");
            drink.bottleModel = Prop("Test bottle model");
            drink.bottleInteraction = Prop("Test bottle hit target");
            drink.bottleCap = Prop("Test bottle cap");
            var props = new[] { drink.glassModel, drink.glassInteraction, drink.glassRim,
                drink.bottleModel, drink.bottleInteraction, drink.bottleCap };
            drink.Initialize(game);
            Assert.IsFalse(drink.Unlocked);
            foreach (var prop in props) Assert.IsFalse(prop.gameObject.activeSelf);
            drink.Sip();
            drink.DevAlmostFull();
            Assert.AreEqual(0, drink.Fill);
            Assert.IsFalse(drink.Boosting);
            game.Wallet.Add(DetectiveDesk.Economy.Currency.Gold, 10);
            Assert.IsTrue(game.PurchaseShopItem(GoldShopCatalog.Whiskey));
            drink.RefreshUnlock();
            foreach (var prop in props) Assert.IsTrue(prop.gameObject.activeSelf);
            Assert.IsTrue(drink.Unlocked);
            drink.Sip();
            Assert.Greater(drink.Fill, 0);
        }

        [Test]
        public void ExistingGoldCanvas_AddsMissingWhiskeyUnlockExactlyOnce()
        {
            CreateLayouts();
            var serialized = new SerializedObject(game.ShopView);
            var rows = serialized.FindProperty("rows");
            int expectedCount = rows.arraySize;
            int remove = -1;
            for (int i = 0; i < rows.arraySize; i++)
                if (rows.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == GoldShopCatalog.Whiskey) remove = i;
            Assert.GreaterOrEqual(remove, 0);
            var row = rows.GetArrayElementAtIndex(remove);
            var background = (Image)row.FindPropertyRelative("background").objectReferenceValue;
            Object.DestroyImmediate(background.gameObject);
            rows.DeleteArrayElementAtIndex(remove);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            game.ShopView.CreateLayout(game);
            game.ShopView.CreateLayout(game);
            serialized.Update();
            Assert.AreEqual(expectedCount, serialized.FindProperty("rows").arraySize);
        }

        [Test]
        public void FirstPinBoxOpening_WaitsForClosingTree_AndPausesWhenHidden()
        {
            CreateLayouts();
            game.Settings.Close();
            game.ShopView.Close();
            var coffer = new GameObject("Intro test coffer");
            coffer.transform.SetParent(root.transform, false);
            coffer.AddComponent<DeskInteractable>();
            var lid = new GameObject("Lid").transform;
            lid.SetParent(coffer.transform, false);
            Quaternion openPose = Quaternion.Euler(15, 0, 0);
            lid.localRotation = openPose;
            var socket = new GameObject("Intro test socket");
            socket.transform.SetParent(coffer.transform, false);
            game.pinSlots = new[] { socket.AddComponent<PinCofferSlot>() };
            var box = root.AddComponent<PinBox>();
            box.Initialize(game);
            Quaternion closedPose = lid.localRotation;
            game.Wallet.Add(DetectiveDesk.Economy.Currency.Money, 100);
            game.PurchaseSkill("root");
            game.TreeView.Open();
            Assert.IsTrue(game.PurchaseSkill("pin_01"));
            box.AdvanceLid(5, !game.WorldInputBlocked);
            Assert.Less(Quaternion.Angle(closedPose, lid.localRotation), .001f);
            Assert.IsTrue(game.Guidance.NeedsPinBox);
            game.TreeView.Close();
            box.AdvanceLid(5, false); // Switching to another app also leaves the first opening pending.
            Assert.Less(Quaternion.Angle(closedPose, lid.localRotation), .001f);
            box.AdvanceLid(box.lidSeconds * .5f, !game.WorldInputBlocked);
            Quaternion halfway = lid.localRotation;
            Assert.Greater(Quaternion.Angle(closedPose, halfway), 1);
            Assert.Greater(Quaternion.Angle(openPose, halfway), 1);
            game.TreeView.Open();
            box.AdvanceLid(5, !game.WorldInputBlocked);
            Assert.Less(Quaternion.Angle(halfway, lid.localRotation), .001f);
            Assert.IsFalse(game.Guidance.ShowPinShooter(true));
            game.TreeView.Close();
            box.AdvanceLid(box.lidSeconds, !game.WorldInputBlocked);
            Assert.Less(Quaternion.Angle(openPose, lid.localRotation), .001f);
            Assert.IsFalse(game.Guidance.NeedsPinBox);
            Assert.IsTrue(game.Guidance.ShowPinShooter(true));
        }

        [Test]
        public void CaseFolder_StopsGrowingAfterFive_AndContinuesReplacingTheTopClue()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var fixture = new GameObject("Capped folder test");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fixture, scene);
            try
            {
                var folder = new GameObject("Folder").transform;
                folder.SetParent(fixture.transform, false);
                void Part(string name, float height, Vector3 size)
                {
                    var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                    part.transform.SetParent(folder, false);
                    part.transform.localPosition = Vector3.up * height;
                    part.transform.localScale = size;
                    part.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                }
                Part("Folder_BackCover", 0, new Vector3(.4f, .005f, .3f));
                Part("Folder_FrontCover", .045f, new Vector3(.4f, .005f, .3f));
                Part("Paper_Layer", .02f, new Vector3(.35f, .004f, .25f));
                var visual = fixture.AddComponent<CaseFolderVisual>();
                visual.Initialize(folder);
                visual.SetCount(5, 0, false);
                var labelAtFive = visual.LabelAnchor;
                var stampAtFive = visual.StampPoint;
                var contents = fixture.transform.Find("Case folder contents");
                var topAtFive = contents.Find("Paper clue 5");
                float topHeight = topAtFive.position.y;
                visual.SetCount(17, 0, false);
                Assert.AreEqual(labelAtFive, visual.LabelAnchor, "label must not climb above the capped stack");
                Assert.AreEqual(stampAtFive, visual.StampPoint, "stamp height must also remain capped");
                Assert.AreEqual(5, contents.GetComponentsInChildren<MeshFilter>().Length, "avoid overlapping extra sheets");
                Assert.AreEqual(topHeight, contents.Find("Paper clue 17").position.y, 1e-6f);
                visual.SetCount(18, 0, false);
                Assert.IsNotNull(contents.Find("Paper clue 18"), "later arrivals still create a new clue");
                visual.SetCount(3, 0, false);
                Assert.AreEqual(3, contents.GetComponentsInChildren<MeshFilter>().Length, "restoring a smaller count rebuilds correctly");
                visual.SetCount(0, 1, false);
                Assert.AreEqual(0, contents.GetComponentsInChildren<MeshFilter>().Length, "next case starts empty");
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void GameplayGlobe_RendersOneSurface_WithoutOverlappingLegacyShell()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/DetectiveDesk/Scenes/DetectiveOffice.unity");
            try
            {
                GlobeController globe = null;
                foreach (var sceneRoot in scene.GetRootGameObjects())
                {
                    globe = sceneRoot.GetComponentInChildren<GlobeController>(true);
                    if (globe) break;
                }
                Assert.IsNotNull(globe);
                Assert.IsTrue(globe.rotor.Find("GlobeRework_Sphere").gameObject.activeSelf);
                Assert.IsFalse(globe.rotor.Find("Sphere_RotationPivot").gameObject.activeSelf, "legacy surface must not overlap the current globe");
                int shells = 0;
                foreach (var renderer in globe.rotor.GetComponentsInChildren<MeshRenderer>())
                    if (renderer.enabled && renderer.name == "Sphere_DarkMetalShell") shells++;
                Assert.AreEqual(1, shells, "overlapping shells cause flickering during rotation");
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void PinColors_UseStrengthInsteadOfSlot_AndReachGoldOnlyAtMaximum()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<PinSkin>("Assets/DetectiveDesk/Prefabs/Pins/CollectiblePin.prefab");
            var first = Object.Instantiate(prefab, root.transform);
            var second = Object.Instantiate(prefab, root.transform);
            var original = prefab.SkinMaterials[0].GetColor("_BaseColor");
            var seen = new System.Collections.Generic.HashSet<Color>();
            for (int rank = 0; rank <= 20; rank++)
            {
                game.Skills.Load(tree, new System.Collections.Generic.List<DetectiveDesk.Save.SkillRank>
                {
                    new DetectiveDesk.Save.SkillRank { id = "pin_01_durability", rank = rank },
                    new DetectiveDesk.Save.SkillRank { id = "pin_02_durability", rank = rank },
                });
                first.SetSkin(1);
                second.SetSkin(8);
                game.ApplyPinAppearance(first, 0);
                game.ApplyPinAppearance(second, 1);
                Assert.AreEqual(first.DisplayColor, second.DisplayColor, "same Strength, different slot");
                Assert.AreEqual(balance.pinStrengthColors[rank], first.DisplayColor);
                Assert.IsTrue(seen.Add(first.DisplayColor), "every Strength level has a distinct colour");
                if (rank < 20) Assert.AreNotEqual(balance.pinStrengthColors[20], first.DisplayColor);
                var properties = new MaterialPropertyBlock();
                var serialized = new SerializedObject(first);
                int slot = serialized.FindProperty("enamelMaterialSlot").intValue;
                first.EnamelHeadRenderer.GetPropertyBlock(properties, slot);
                Assert.Less(Vector4.Distance(first.DisplayColor, properties.GetColor("_BaseColor")), 1e-5f);
                Assert.AreEqual(rank == 20 ? balance.pinMaxStrengthMetallic : balance.pinEnamelMetallic, properties.GetFloat("_Metallic"));
                first.SetIlluminated(true);
                first.EnamelHeadRenderer.GetPropertyBlock(properties, slot);
                Assert.Less(Vector4.Distance(first.DisplayColor * .65f, properties.GetColor("_EmissionColor")), 1e-5f);
            }
            Assert.AreEqual(original, prefab.SkinMaterials[0].GetColor("_BaseColor"), "shared material was not recoloured");
        }

        [Test]
        public void PinBox_RefreshUpdatesExistingPinAfterStrengthPurchase()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<PinSkin>("Assets/DetectiveDesk/Prefabs/Pins/CollectiblePin.prefab");
            var socket = new GameObject("Strength test socket");
            socket.transform.SetParent(root.transform, false);
            var slot = socket.AddComponent<PinCofferSlot>();
            var serialized = new SerializedObject(slot);
            serialized.FindProperty("pinPrefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            game.pinSlots = new[] { slot };
            game.Wallet.Add(DetectiveDesk.Economy.Currency.Money, 1e6);
            Assert.IsTrue(game.PurchaseSkill("root"));
            Assert.IsTrue(game.PurchaseSkill("pin_01"));
            var box = root.AddComponent<PinBox>();
            box.Initialize(game);
            var occupant = slot.Occupant;
            Assert.AreEqual(balance.pinStrengthColors[0], occupant.DisplayColor);
            Assert.IsTrue(game.PurchaseSkill("pin_01_durability"));
            box.Refresh();
            Assert.AreSame(occupant, slot.Occupant);
            Assert.AreEqual(balance.pinStrengthColors[1], occupant.DisplayColor);
        }

        [Test]
        public void SkillColors_FollowNextPurchase_NotBranchOrPartialOwnership()
        {
            CreateLayouts();
            var canvas = game.TreeView.CreateLayout(game);
            void AssertStyle(string id, PurchaseVisualState expected)
            {
                var node = canvas.transform.Find("Backdrop/Viewport/Content/Nodes/" + id);
                var colors = PurchasePalette.For(expected);
                Assert.AreEqual(colors.fill, node.GetComponent<Image>().color, id + " fill");
                Assert.AreEqual(colors.border, node.Find("Frame").GetComponent<Image>().color, id + " border");
                node.GetComponent<SkillNodeWidget>().Entered(node.GetComponent<SkillNodeWidget>());
                var serialized = new SerializedObject(game.TreeView);
                var price = (TMPro.TextMeshProUGUI)serialized.FindProperty("tipCost").objectReferenceValue;
                Assert.AreEqual(colors.price, price.color, id + " price");
                var card = (RectTransform)serialized.FindProperty("tooltip").objectReferenceValue;
                Assert.AreEqual(colors.fill, card.GetComponent<Image>().color, id + " tooltip");
            }
            game.TreeView.Open();
            AssertStyle("root", PurchaseVisualState.Unavailable);
            game.Wallet.Add(DetectiveDesk.Economy.Currency.Money, 10000);
            game.TreeView.Open();
            AssertStyle("root", PurchaseVisualState.Available);
            Assert.IsTrue(game.PurchaseSkill("root"));
            Assert.IsTrue(game.PurchaseSkill("pin_01"));
            Assert.IsTrue(game.PurchaseSkill("globe_drag"));
            game.TreeView.Open();
            AssertStyle("globe_drag", PurchaseVisualState.Available);
            AssertStyle("pin_01_durability", PurchaseVisualState.Available);
            AssertStyle("root", PurchaseVisualState.Maxed);
            Assert.IsTrue(game.PurchaseSkill("globe_auto", 10));
            game.TreeView.Open();
            AssertStyle("globe_auto", PurchaseVisualState.Unavailable); // temporarily capped, not maxed
            game.Wallet.TrySpend(DetectiveDesk.Economy.Currency.Money, game.Wallet.Money);
            game.TreeView.Open();
            AssertStyle("globe_drag", PurchaseVisualState.Unavailable);
            game.Wallet.Add(DetectiveDesk.Economy.Currency.Money, 10000);
            Assert.IsTrue(game.PurchaseSkill("globe_drag", 10));
            game.TreeView.Open();
            AssertStyle("globe_drag", PurchaseVisualState.Maxed);
        }

        [Test]
        public void GoldShop_UsesTheSamePalette_AndDisabledOwnedButtonsStayGold()
        {
            CreateLayouts();
            var serialized = new SerializedObject(game.ShopView);
            var rows = serialized.FindProperty("rows");
            SerializedProperty row = null;
            for (int i = 0; i < rows.arraySize; i++)
            {
                var candidate = rows.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("id").stringValue == GoldShopCatalog.Statue1) { row = candidate; break; }
            }
            Assert.IsNotNull(row);
            var background = (Image)row.FindPropertyRelative("background").objectReferenceValue;
            var button = (Button)row.FindPropertyRelative("button").objectReferenceValue;
            var themed = button.GetComponent<ThemedButton>();
            game.ShopView.Open();
            Assert.AreEqual(PurchasePalette.For(PurchaseVisualState.Unavailable).fill, background.color);
            game.Wallet.Add(DetectiveDesk.Economy.Currency.Gold, game.Shop.NextCost(GoldShopCatalog.Find(GoldShopCatalog.Statue1)));
            game.ShopView.Open();
            Assert.AreEqual(PurchasePalette.For(PurchaseVisualState.Available).fill, background.color);
            themed.OnPointerEnter(null);
            Assert.AreNotEqual(PurchasePalette.For(PurchaseVisualState.Maxed).border, themed.frame.color);
            Assert.IsTrue(game.PurchaseShopItem(GoldShopCatalog.Statue1));
            game.ShopView.Open();
            themed.OnPointerExit(null);
            Assert.IsFalse(button.interactable);
            Assert.AreEqual(PurchasePalette.For(PurchaseVisualState.Maxed).fill, background.color);
            Assert.AreEqual(PurchasePalette.For(PurchaseVisualState.Maxed).border, themed.frame.color);
            Assert.AreEqual(PurchasePalette.For(PurchaseVisualState.Maxed).text, themed.label.color);
        }

        [Test]
        public void BulkControls_RebindWithoutDuplicates_AndTooltipMatchesPartialPurchase()
        {
            CreateLayouts();
            var canvas = game.TreeView.CreateLayout(game);
            var controls = canvas.transform.Find("Backdrop/Purchase Amount");
            Assert.AreEqual(3, controls.childCount);
            CreateLayouts();
            Assert.AreEqual(3, controls.childCount);
            controls.Find("Buy 10").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(10, game.TreeView.PurchaseAmount);
            game.Wallet.Add(DetectiveDesk.Economy.Currency.Money, 1000);
            Assert.IsTrue(game.PurchaseSkill("root"));
            Assert.IsTrue(game.PurchaseSkill("pin_01"));
            game.Wallet.TrySpend(DetectiveDesk.Economy.Currency.Money, game.Wallet.Money - 25);
            var widget = canvas.transform.Find("Backdrop/Viewport/Content/Nodes/shooter_cooldown").GetComponent<SkillNodeWidget>();
            widget.Entered(widget);
            var serialized = new SerializedObject(game.TreeView);
            var price = serialized.FindProperty("tipCost").objectReferenceValue as TMPro.TextMeshProUGUI;
            StringAssert.Contains("Buy 2 levels", price.text);
            StringAssert.Contains("25", price.text);
            widget.Clicked(widget);
            Assert.AreEqual(2, game.Skills.Rank("shooter_cooldown"));
            Assert.AreEqual(0, game.Wallet.Money);
        }

        [Test]
        public void GlobeHeading_FollowsUpgradeVisibility_IncludingPreviewAndReset()
        {
            CreateLayouts();
            var canvas = game.TreeView.CreateLayout(game);
            var heading = canvas.transform.Find("Backdrop/Viewport/Content/Sections/globe_section");
            var serialized = new SerializedObject(game.TreeView);
            var section = serialized.FindProperty("authoredSections").GetArrayElementAtIndex(0);
            void AssertVisible(bool visible)
            {
                Assert.AreEqual(visible, heading.gameObject.activeSelf);
                foreach (string field in new[] { "label", "rootLink", "stem", "bus" })
                    Assert.AreEqual(visible, ((Component)section.FindPropertyRelative(field).objectReferenceValue).gameObject.activeSelf, field);
            }
            game.TreeView.Open();
            AssertVisible(false);
            game.TreeView.PreviewLayout();
            AssertVisible(true);
            game.TreeView.Open();
            AssertVisible(false);
            game.Wallet.Add(DetectiveDesk.Economy.Currency.Money, 20);
            Assert.IsTrue(game.PurchaseSkill("root"));
            game.TreeView.Open();
            AssertVisible(true);
            game.Skills.Reset();
            game.Stats.Rebuild(tree, game.Skills);
            game.TreeView.Open();
            AssertVisible(false);
        }

        [Test]
        public void SectionHeadings_HaveNoPurchaseWidgets_AndDoNotDuplicateOnRebinding()
        {
            CreateLayouts();
            var canvas = game.TreeView.CreateLayout(game);
            var sections = canvas.transform.Find("Backdrop/Viewport/Content/Sections");
            Assert.AreEqual(4, sections.childCount);
            Assert.AreEqual(0, sections.GetComponentsInChildren<SkillNodeWidget>(true).Length);
            Assert.AreEqual(tree.Nodes.Count, canvas.GetComponentsInChildren<SkillNodeWidget>(true).Length);
            int before = canvas.GetComponentsInChildren<Transform>(true).Length;
            CreateLayouts();
            Assert.AreEqual(before, canvas.GetComponentsInChildren<Transform>(true).Length);
            foreach (var graphic in sections.GetComponentsInChildren<Graphic>(true))
                Assert.AreEqual(graphic.GetComponent<SkillSectionWidget>() != null, graphic.raycastTarget,
                    "only the information node face should receive pointer input");
        }

        [Test]
        public void DeskEntries_UseTheFreeDeskHub_WithoutChangingPurchasePrerequisites()
        {
            CreateLayouts();
            game.TreeView.PreviewLayout();
            var canvas = game.TreeView.CreateLayout(game);
            var content = canvas.transform.Find("Backdrop/Viewport/Content");
            Assert.IsNotNull(content.Find("Sections/desk_section"));
            foreach (var id in new[] { "loot_bulk", "whiskey_duration", "statue_gold_fill" })
            {
                var node = tree.Find(id);
                Assert.AreEqual("desk_section", tree.SectionFor(node).id);
                CollectionAssert.Contains(node.prerequisites, "root");
            }
        }

        [Test]
        public void FourInformationHubs_ShowDescriptionsWithoutPurchasing_AndRestoreSkillTooltips()
        {
            CreateLayouts();
            var canvas = game.TreeView.CreateLayout(game);
            var sections = canvas.transform.Find("Backdrop/Viewport/Content/Sections");
            game.TreeView.Open();
            foreach (Transform section in sections) Assert.IsFalse(section.gameObject.activeSelf);
            game.Wallet.Add(DetectiveDesk.Economy.Currency.Money, 10000);
            game.PurchaseSkill("root");
            game.TreeView.Open();
            double money = game.Wallet.Money;
            int owned = game.Skills.ToSave().Count;
            var serialized = new SerializedObject(game.TreeView);
            var title = (TMPro.TextMeshProUGUI)serialized.FindProperty("tipTitle").objectReferenceValue;
            var body = (TMPro.TextMeshProUGUI)serialized.FindProperty("tipBody").objectReferenceValue;
            var price = (TMPro.TextMeshProUGUI)serialized.FindProperty("tipCost").objectReferenceValue;
            foreach (var data in tree.Sections)
            {
                var section = sections.Find(data.id);
                Assert.IsTrue(section.gameObject.activeSelf);
                var widget = section.GetComponent<SkillSectionWidget>();
                widget.OnPointerEnter(null);
                widget.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(null)
                    { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left });
                Assert.AreEqual(data.title, title.text);
                Assert.AreEqual(data.description, body.text);
                Assert.IsFalse(price.gameObject.activeSelf);
                Assert.AreEqual(money, game.Wallet.Money);
                Assert.AreEqual(owned, game.Skills.ToSave().Count);
                widget.OnPointerExit(null);
            }
            var skill = canvas.transform.Find("Backdrop/Viewport/Content/Nodes/globe_auto").GetComponent<SkillNodeWidget>();
            skill.Entered(skill);
            Assert.IsTrue(price.gameObject.activeSelf, "purchase tooltip must recover after an info tooltip");
        }

        [Test]
        public void MovingAParent_UpdatesItsChildLinkWithoutMovingTheChild()
        {
            CreateLayouts();
            var canvas = game.TreeView.CreateLayout(game);
            var child = (RectTransform)canvas.transform.Find("Backdrop/Viewport/Content/Nodes/globe_multiplier");
            child.anchoredPosition = new Vector2(20, 650);
            var parent = tree.Find("globe_max_speed");
            parent.position += new Vector2(24, 0);
            game.TreeView.SynchronizeNodeLayout(game, parent.id);
            Assert.AreEqual(new Vector2(20, 650), child.anchoredPosition);
            var serialized = new SerializedObject(game.TreeView);
            var nodes = serialized.FindProperty("authoredNodes");
            for (int i = 0; i < nodes.arraySize; i++)
            {
                var node = nodes.GetArrayElementAtIndex(i);
                if (node.FindPropertyRelative("id").stringValue != "globe_multiplier") continue;
                var link = (node.FindPropertyRelative("links").GetArrayElementAtIndex(0).objectReferenceValue as Image).rectTransform;
                Assert.AreEqual((parent.position + child.anchoredPosition) * .5f, link.anchoredPosition);
                return;
            }
            Assert.Fail("Missing Spin Pay view.");
        }

        [Test]
        public void CreatingLayoutsTwice_ReusesObjectsAndPreservesAuthoredPositions()
        {
            CreateLayouts();
            int before = root.GetComponentsInChildren<Transform>(true).Length;
            var upgrades = game.Hud.Canvas.transform.Find("Upgrades");
            var authored = new Vector3(17, -31, 0);
            upgrades.localPosition = authored;
            CreateLayouts();
            Assert.AreEqual(before, root.GetComponentsInChildren<Transform>(true).Length);
            Assert.AreEqual(authored, upgrades.localPosition);
            Assert.AreEqual(0, game.Wallet.Money, "layout authoring does not load a saved wallet");
            Assert.AreEqual(0, game.Skills.Rank("root"), "layout authoring does not buy skills");
        }

        [Test]
        public void HudInitialization_BindsAuthoredButtonsWithoutRebuildingLayout()
        {
            CreateLayouts();
            var canvas = game.Hud.Canvas;
            int before = canvas.GetComponentsInChildren<Transform>(true).Length;
            var button = canvas.transform.Find("Upgrades");
            button.localPosition = new Vector3(123, 45, 0);
            game.Hud.Initialize(game);
            Assert.AreSame(canvas, game.Hud.Canvas);
            Assert.AreEqual(before, canvas.GetComponentsInChildren<Transform>(true).Length);
            Assert.AreEqual(new Vector3(123, 45, 0), button.localPosition);
            game.TreeView.Close();
            button.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(game.TreeView.IsOpen);
        }

        [Test]
        public void SerializedSkillTree_RestoresNodesAndBindsCallbacksAfterReload()
        {
            CreateLayouts();
            var canvas = game.TreeView.CreateLayout(game);
            int before = canvas.GetComponentsInChildren<Transform>(true).Length;
            var replacement = new GameObject("Reloaded skill tree");
            replacement.SetActive(false);
            replacement.transform.SetParent(root.transform, false);
            var restored = replacement.AddComponent<SkillTreeView>();
            EditorUtility.CopySerialized(game.TreeView, restored);
            Object.DestroyImmediate(game.TreeView);
            restored.Initialize(game);
            Assert.IsTrue(canvas == restored.CreateLayout(game), "the saved canvas reference must resolve to the original Unity object");
            Assert.AreEqual(before, canvas.GetComponentsInChildren<Transform>(true).Length);
            Assert.AreEqual(4, canvas.transform.Find("Backdrop/Viewport/Content/Sections").childCount);
            var nodes = canvas.GetComponentsInChildren<SkillNodeWidget>(true);
            Assert.AreEqual(tree.Nodes.Count, nodes.Length);
            foreach (var node in nodes)
            {
                Assert.IsNotNull(node.Clicked, node.nodeId);
                Assert.IsNotNull(node.Entered, node.nodeId);
                Assert.IsNotNull(node.Exited, node.nodeId);
            }
        }

        [Test]
        public void RebindingAButton_DoesNotDuplicateItsAction()
        {
            var button = new GameObject("Button", typeof(RectTransform), typeof(Button)).GetComponent<Button>();
            button.transform.SetParent(root.transform, false);
            int calls = 0;
            SceneViewBindings.Bind(button, () => calls++);
            SceneViewBindings.Bind(button, () => calls++);
            button.onClick.Invoke();
            Assert.AreEqual(1, calls);
        }

        [Test]
        public void SynchronizingLayouts_AddsMissingSkillsWithoutMovingOtherNodes()
        {
            var nodes = DefaultSkillTreeFactory.Create();
            nodes.RemoveAll(node => node.id == DefaultSkillTreeFactory.CaseUnlockId);
            tree.SetNodes(nodes);
            CreateLayouts();
            var canvas = game.TreeView.CreateLayout(game);
            var globe = (RectTransform)canvas.transform.Find("Backdrop/Viewport/Content/Nodes/globe_auto");
            globe.anchoredPosition = new Vector2(123, 456);
            var pin = (RectTransform)canvas.transform.Find("Backdrop/Viewport/Content/Nodes/pin_01");
            pin.anchoredPosition = new Vector2(432, 21);
            tree.SetNodes(DefaultSkillTreeFactory.Create());
            game.TreeView.SynchronizeBranchLayout(game, SkillBranch.CaseFile);
            game.TreeView.SynchronizeBranchLayout(game, SkillBranch.CaseFile);
            game.TreeView.SynchronizeNodeLayout(game, "shooter_cooldown", "pin_autoloader");
            Assert.AreEqual(tree.Nodes.Count, canvas.GetComponentsInChildren<SkillNodeWidget>(true).Length);
            Assert.AreEqual(new Vector2(123, 456), globe.anchoredPosition);
            Assert.AreEqual(new Vector2(432, 21), pin.anchoredPosition);
            var unlock = (RectTransform)canvas.transform.Find("Backdrop/Viewport/Content/Nodes/case_unlock");
            Assert.AreEqual(tree.Find(DefaultSkillTreeFactory.CaseUnlockId).position, unlock.anchoredPosition);
        }
    }
}
