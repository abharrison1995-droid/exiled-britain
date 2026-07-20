using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ExiledAlvaston.UI;
using ExiledAlvaston.Dialogue;
using ExiledAlvaston.Combat;
using ExiledAlvaston.Vibe;
using TMPro;

public class UISetup : EditorWindow
{
    [MenuItem("Tools/UI/Generate Exiled HUD")]
    public static void GenerateUI()
    {
        EnsureEventSystem();

        GameObject canvasGO = GameObject.Find("UICanvas");
        if (canvasGO != null) Object.DestroyImmediate(canvasGO);

        canvasGO = new GameObject("UICanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        UIManager uiManager = canvasGO.AddComponent<UIManager>();
        InventoryController invController = canvasGO.AddComponent<InventoryController>();
        DialogueManager diagManager = canvasGO.AddComponent<DialogueManager>();

        // ========== HUD ==========
        GameObject hud = CreatePanel("HUDPanel", canvasGO.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true);

        // Top-left: portrait + level + red HP + blue mana
        GameObject topLeft = CreatePanel("TopLeftPortraits", hud.transform,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -16), new Vector2(320, 110), true);
        topLeft.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

        GameObject portrait = CreateImage("PlayerPortrait", topLeft.transform,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(88, 88),
            new Color(0.4f, 0.35f, 0.28f, 1f));

        GameObject levelBadge = CreateImage("LevelBadge", portrait.transform,
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(4, 4), new Vector2(28, 28),
            EKVibe.LevelBadge);
        TextMeshProUGUI levelTxt = CreateTMP("LevelText", levelBadge.transform, "1",
            Vector2.zero, Vector2.one, EKVibe.TextDark, 18, TextAlignmentOptions.Center);

        GameObject hpTrack = CreateImage("HPTrack", topLeft.transform,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(100, -18), new Vector2(200, 18),
            new Color(0.15f, 0.1f, 0.1f, 0.85f));
        GameObject hpFill = CreateImage("HPFill", hpTrack.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, EKVibe.HealthBar);
        Image hpImg = hpFill.GetComponent<Image>();
        hpImg.type = Image.Type.Filled;
        hpImg.fillMethod = Image.FillMethod.Horizontal;
        hpImg.fillAmount = 1f;

        GameObject mpTrack = CreateImage("MPTrack", topLeft.transform,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(100, -48), new Vector2(200, 18),
            new Color(0.1f, 0.1f, 0.2f, 0.85f));
        GameObject mpFill = CreateImage("MPFill", mpTrack.transform,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, EKVibe.ManaBar);
        Image mpImg = mpFill.GetComponent<Image>();
        mpImg.type = Image.Type.Filled;
        mpImg.fillMethod = Image.FillMethod.Horizontal;
        mpImg.fillAmount = 1f;

        // Top-center: combat log
        GameObject combatLog = CreatePanel("CombatLog", hud.transform,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(520, 100), true);
        TextMeshProUGUI logTxt = CreateTMP("CombatLogText", combatLog.transform, "",
            Vector2.zero, Vector2.one, EKVibe.CombatLogText, 16, TextAlignmentOptions.Top);
        logTxt.enableWordWrapping = true;

        // Top-right: map / bag shortcut
        GameObject topRight = CreateImage("MapBagShortcut", hud.transform,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -16), new Vector2(72, 72),
            EKVibe.ParchmentDark);
        topRight.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
        topRight.AddComponent<Button>();
        var bagAction = topRight.AddComponent<HUDActionButton>();
        bagAction.Kind = HUDActionButton.ActionKind.Inventory;
        CreateTMP("BagLabel", topRight.transform, "Bag", Vector2.zero, Vector2.one,
            EKVibe.TextLight, 18, TextAlignmentOptions.Center);

        // Bottom-left: location/time + virtual joystick
        TextMeshProUGUI locTxt = CreateTMP("LocationTime", hud.transform,
            "Alvaston; Day 1, 8 AM",
            new Vector2(0, 0), new Vector2(0, 0), EKVibe.TextLight, 18, TextAlignmentOptions.BottomLeft);
        RectTransform locRT = locTxt.rectTransform;
        locRT.anchorMin = new Vector2(0, 0);
        locRT.anchorMax = new Vector2(0, 0);
        locRT.pivot = new Vector2(0, 0);
        locRT.anchoredPosition = new Vector2(20, 250);
        locRT.sizeDelta = new Vector2(420, 30);

        GameObject joyRoot = CreateImage("VirtualJoystick", hud.transform,
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 40),
            new Vector2(EKVibe.JoystickRadius * 2f, EKVibe.JoystickRadius * 2f),
            new Color(0.2f, 0.2f, 0.2f, 0.35f));
        joyRoot.GetComponent<RectTransform>().pivot = new Vector2(0, 0);
        joyRoot.GetComponent<Image>().raycastTarget = true;
        GameObject joyHandle = CreateImage("Handle", joyRoot.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(70, 70), new Color(0.85f, 0.85f, 0.85f, 0.55f));
        joyHandle.GetComponent<Image>().raycastTarget = false;
        VirtualJoystick joystick = joyRoot.AddComponent<VirtualJoystick>();
        joystick.Background = joyRoot.GetComponent<RectTransform>();
        joystick.Handle = joyHandle.GetComponent<RectTransform>();

        // Bottom-center: quick slots (5)
        GameObject quickSlots = CreatePanel("QuickSlots", hud.transform,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(380, EKVibe.QuickSlotSize), true);
        HorizontalLayoutGroup qhl = quickSlots.AddComponent<HorizontalLayoutGroup>();
        qhl.spacing = 10;
        qhl.childAlignment = TextAnchor.MiddleCenter;
        qhl.childControlWidth = true;
        qhl.childControlHeight = true;
        qhl.childForceExpandWidth = true;
        qhl.childForceExpandHeight = true;
        for (int i = 0; i < 5; i++)
        {
            GameObject slot = CreateImage($"QuickSlot{i}", quickSlots.transform,
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, EKVibe.SlotFrame);
        }

        // Bottom-right: attack cluster (large attack + skills)
        GameObject actionCluster = CreatePanel("ActionCluster", hud.transform,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-30, 30), new Vector2(220, 220), true);
        actionCluster.GetComponent<RectTransform>().pivot = new Vector2(1, 0);

        GameObject attackBtn = CreateImage("AttackButton", actionCluster.transform,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(0, 0),
            new Vector2(EKVibe.AttackButtonSize, EKVibe.AttackButtonSize), EKVibe.ButtonBrown);
        attackBtn.AddComponent<Button>();
        var atkAction = attackBtn.AddComponent<HUDActionButton>();
        atkAction.Kind = HUDActionButton.ActionKind.Attack;
        CreateTMP("AtkLabel", attackBtn.transform, "ATK", Vector2.zero, Vector2.one,
            EKVibe.TextLight, 22, TextAlignmentOptions.Center);

        string[] skillLabels = { "Heal", "Skill", "Sprint" };
        Vector2[] skillOffsets =
        {
            new Vector2(-110, 30),
            new Vector2(-40, 110),
            new Vector2(-150, 110)
        };
        for (int i = 0; i < 3; i++)
        {
            GameObject skill = CreateImage($"Skill{i}", actionCluster.transform,
                new Vector2(1, 0), new Vector2(1, 0), skillOffsets[i],
                new Vector2(EKVibe.SkillButtonSize, EKVibe.SkillButtonSize), EKVibe.ParchmentDark);
            skill.AddComponent<Button>();
            var skillAction = skill.AddComponent<HUDActionButton>();
            skillAction.Kind = HUDActionButton.ActionKind.Ability;
            skillAction.AbilityIndex = i;
            CreateTMP($"SkillLabel{i}", skill.transform, skillLabels[i], Vector2.zero, Vector2.one,
                EKVibe.TextLight, 14, TextAlignmentOptions.Center);
        }

        // Wire HUD refs
        uiManager.TopLeftPortraitPanel = topLeft.GetComponent<RectTransform>();
        uiManager.BottomCenterQuickSlotPanel = quickSlots.GetComponent<RectTransform>();
        uiManager.RightActionPanel = actionCluster.GetComponent<RectTransform>();
        uiManager.CombatLogPanel = combatLog.GetComponent<RectTransform>();
        uiManager.Joystick = joystick;
        uiManager.PlayerPortrait = portrait.GetComponent<Image>();
        uiManager.PlayerHealthFill = hpImg;
        uiManager.PlayerManaFill = mpImg;
        uiManager.LevelText = levelTxt;
        uiManager.LocationTimeText = locTxt;
        uiManager.CombatLogText = logTxt;
        uiManager.SetLocationTime("London", 1, "8 AM");
        uiManager.SetLevel(1);
        uiManager.LogCombat("Welcome to Discover England.");

        // Quest tracker (top-right under bag)
        GameObject questRoot = CreateImage("QuestTracker", hud.transform,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -100), new Vector2(280, 72),
            new Color(0.2f, 0.15f, 0.1f, 0.72f));
        questRoot.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
        questRoot.GetComponent<Image>().raycastTarget = false;
        TextMeshProUGUI questTitle = CreateTMP("QuestTitle", questRoot.transform, "",
            new Vector2(0.06f, 0.52f), new Vector2(0.94f, 0.95f), EKVibe.TextLight, 18, TextAlignmentOptions.Left);
        TextMeshProUGUI questObj = CreateTMP("QuestObjective", questRoot.transform, "",
            new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.52f), EKVibe.CombatLogText, 15, TextAlignmentOptions.TopLeft);
        questObj.enableWordWrapping = true;
        var questUi = hud.AddComponent<QuestTrackerUI>();
        questUi.Root = questRoot;
        questUi.TitleText = questTitle;
        questUi.ObjectiveText = questObj;
        questRoot.SetActive(false);

        // ========== Inventory (parchment 3-column) ==========
        GameObject invPanel = CreateImage("InventoryOverlay", canvasGO.transform,
            new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f), Vector2.zero, Vector2.zero,
            EKVibe.ParchmentPanel);
        invPanel.SetActive(false);

        // Left stats
        GameObject leftStats = CreatePanel("LeftStats", invPanel.transform,
            new Vector2(0, 0), new Vector2(0.28f, 1), Vector2.zero, Vector2.zero, true);
        leftStats.GetComponent<Image>().color = new Color(0, 0, 0, 0.08f);

        CreateTMP("CharName", leftStats.transform, "Hero, Adventurer",
            new Vector2(0.08f, 0.88f), new Vector2(0.92f, 0.98f), EKVibe.TextDark, 22, TextAlignmentOptions.Left);
        TextMeshProUGUI traits = CreateTMP("Traits", leftStats.transform,
            "STR  5\nEND  5\nAGI  5\nINT  5\nAWA  5\nPER  5",
            new Vector2(0.08f, 0.45f), new Vector2(0.92f, 0.85f), EKVibe.TextDark, 20, TextAlignmentOptions.TopLeft);
        TextMeshProUGUI resists = CreateTMP("Resistances", leftStats.transform,
            "Armor 0\nFire 0  Cold 0\nPoison 0  Magic 0",
            new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.42f), EKVibe.TextDark, 18, TextAlignmentOptions.TopLeft);

        // Nav buttons
        string[] navLabels = { "Journal", "Skills", "Reputation", "Details" };
        for (int i = 0; i < 4; i++)
        {
            GameObject nav = CreateImage($"Nav{i}", leftStats.transform,
                new Vector2(0.05f + i * 0.23f, 0.04f), new Vector2(0.26f + i * 0.23f, 0.14f),
                Vector2.zero, Vector2.zero, EKVibe.ButtonBrown);
            nav.AddComponent<Button>();
            CreateTMP($"NavTxt{i}", nav.transform, navLabels[i], Vector2.zero, Vector2.one,
                EKVibe.TextLight, 14, TextAlignmentOptions.Center);
        }

        // Center paper doll (ring of slots around character)
        GameObject center = CreatePanel("CenterPaperDoll", invPanel.transform,
            new Vector2(0.28f, 0.35f), new Vector2(0.68f, 1), Vector2.zero, Vector2.zero, true);

        GameObject dollSprite = CreateImage("CharacterSprite", center.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 120),
            new Color(0.55f, 0.5f, 0.4f, 1f));

        // 12 equip slots in a ring (EK paper-doll feel)
        Vector2[] slotPos =
        {
            new Vector2(0, 140), new Vector2(90, 100), new Vector2(120, 20),
            new Vector2(90, -60), new Vector2(0, -110), new Vector2(-90, -60),
            new Vector2(-120, 20), new Vector2(-90, 100),
            new Vector2(50, 40), new Vector2(-50, 40), new Vector2(50, -20), new Vector2(-50, -20)
        };
        for (int i = 0; i < 12; i++)
        {
            CreateImage($"EquipSlot{i}", center.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), slotPos[i],
                new Vector2(64, 64), EKVibe.SlotFrame);
        }

        // Tooltip under doll
        GameObject tooltip = CreateImage("ItemTooltip", invPanel.transform,
            new Vector2(0.3f, 0.05f), new Vector2(0.66f, 0.34f), Vector2.zero, Vector2.zero,
            EKVibe.ParchmentDark);
        CreateTMP("TooltipTitle", tooltip.transform, "Select an item",
            new Vector2(0.05f, 0.7f), new Vector2(0.95f, 0.95f), EKVibe.TextLight, 20, TextAlignmentOptions.Left);
        CreateTMP("TooltipBody", tooltip.transform, "Item details appear here.",
            new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.7f), EKVibe.TextLight, 16, TextAlignmentOptions.TopLeft);
        GameObject unequip = CreateImage("UnequipBtn", tooltip.transform,
            new Vector2(0.3f, 0.05f), new Vector2(0.7f, 0.22f), Vector2.zero, Vector2.zero, EKVibe.ButtonBrown);
        unequip.AddComponent<Button>();
        CreateTMP("UnequipTxt", unequip.transform, "Unequip", Vector2.zero, Vector2.one,
            EKVibe.TextLight, 18, TextAlignmentOptions.Center);

        // Right backpack 4x5
        GameObject right = CreatePanel("RightBackpack", invPanel.transform,
            new Vector2(0.68f, 0.15f), new Vector2(1, 1), Vector2.zero, Vector2.zero, true);
        GridLayoutGroup backpackGrid = right.AddComponent<GridLayoutGroup>();
        backpackGrid.cellSize = new Vector2(70, 70);
        backpackGrid.spacing = new Vector2(8, 8);
        backpackGrid.padding = new RectOffset(16, 16, 16, 16);
        backpackGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        backpackGrid.constraintCount = 4;
        for (int i = 0; i < 20; i++)
        {
            CreateImage($"BagSlot{i}", right.transform,
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, EKVibe.SlotEmpty);
        }

        TextMeshProUGUI gold = CreateTMP("GoldText", invPanel.transform, "0",
            new Vector2(0.72f, 0.08f), new Vector2(0.95f, 0.14f), EKVibe.TextDark, 22, TextAlignmentOptions.Right);

        GameObject backBtn = CreateImage("BackButton", invPanel.transform,
            new Vector2(0.78f, 0.02f), new Vector2(0.97f, 0.08f), Vector2.zero, Vector2.zero, EKVibe.ButtonBrown);
        backBtn.AddComponent<Button>();
        CreateTMP("BackTxt", backBtn.transform, "Back", Vector2.zero, Vector2.one,
            EKVibe.TextLight, 20, TextAlignmentOptions.Center);

        invController.InventoryUIPanel = invPanel;
        invController.BackpackGridContainer = right.transform;
        invController.CoreTraitsText = traits;
        invController.ResistancesText = resists;
        invController.TooltipPanel = tooltip;
        invController.TooltipTitle = tooltip.transform.Find("TooltipTitle")?.GetComponent<TextMeshProUGUI>();
        invController.TooltipBody = tooltip.transform.Find("TooltipBody")?.GetComponent<TextMeshProUGUI>();
        invController.UnequipButton = unequip.GetComponent<Button>();
        invController.CurrencyText = gold;
        invController.CharacterNameText = leftStats.transform.Find("CharName")?.GetComponent<TextMeshProUGUI>();

        // ========== Dialogue ==========
        GameObject diagPanel = CreateImage("DialogueOverlay", canvasGO.transform,
            new Vector2(0.15f, 0.02f), new Vector2(0.85f, 0.38f), Vector2.zero, Vector2.zero,
            EKVibe.ParchmentPanel);
        diagPanel.SetActive(false);

        CreateImage("SpeakerPortrait", diagPanel.transform,
            new Vector2(0, 0), new Vector2(0.18f, 1), new Vector2(8, 0), new Vector2(-8, -8),
            EKVibe.SlotFrame);
        TextMeshProUGUI diagText = CreateTMP("DialogueText", diagPanel.transform, "Hello there, traveler.",
            new Vector2(0.2f, 0.5f), new Vector2(0.98f, 0.95f), EKVibe.TextDark, 22, TextAlignmentOptions.TopLeft);
        GameObject diagChoices = CreatePanel("ChoicesContainer", diagPanel.transform,
            new Vector2(0.2f, 0.05f), new Vector2(0.98f, 0.48f), Vector2.zero, Vector2.zero, true);
        VerticalLayoutGroup vlg = diagChoices.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.spacing = 6;

        GameObject choicePrefab = CreateImage("ChoiceButtonPrefab", null,
            Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0, 40), EKVibe.ButtonBrown);
        choicePrefab.AddComponent<Button>();
        CreateTMP("ChoiceLabel", choicePrefab.transform, "1. (Option)", Vector2.zero, Vector2.one,
            EKVibe.TextLight, 18, TextAlignmentOptions.Left);

        diagManager.DialoguePanel = diagPanel;
        diagManager.ChoicesContainer = diagChoices.transform;
        diagManager.MainDialogueText = diagText;
        diagManager.ChoiceButtonPrefab = choicePrefab;

        // Hook joystick onto player combat if present
        var playerCombat = Object.FindObjectOfType<CombatController>();
        if (playerCombat != null)
            playerCombat.Joystick = joystick;

        var invBind = Object.FindObjectOfType<InventoryController>();
        var playerStats = AssetDatabase.LoadAssetAtPath<ExiledAlvaston.Data.CharacterData>("Assets/Data/PlayerStats.asset");
        if (invBind != null && playerStats != null)
            invBind.BindCharacter(playerStats);

        Debug.Log("EK vibe UI generated — joystick, action cluster, combat log, parchment inventory.");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta, bool transparent)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        Image img = go.AddComponent<Image>();
        img.color = transparent ? new Color(0, 0, 0, 0) : Color.white;
        img.raycastTarget = !transparent;
        return go;
    }

    private static GameObject CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        GameObject go = CreatePanel(name, parent, anchorMin, anchorMax, anchoredPos, sizeDelta, false);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = true;
        return go;
    }

    private static TextMeshProUGUI CreateTMP(string name, Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax, Color color, float size, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }
}
