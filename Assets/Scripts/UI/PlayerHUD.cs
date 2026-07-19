using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 HUD - 런타임 자동 생성.
/// 캐릭터 좌측: HP 게이지 + E/R 스킬
/// 캐릭터 우측: CP 게이지 + RClick/L+R 스킬
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    private RectTransform hpFillRect;
    private Text hpText;
    private RectTransform chakraFillRect;
    private Text chakraText;
    private Image[] skillCooldownFills = new Image[4];
    private Text[] skillCooldownTexts = new Text[4];
    private GameObject gameOverPanel;
    private GameObject dungeonClearPanel;

    private PlayerStats playerStats;
    private SkillManager skillManager;
    private DungeonManager dungeonManager;
    private PlayerController playerController;

    private Image dashCooldownFill;
    private Text dashCooldownText;

    private Canvas canvas;

    // 게이지 설정
    [Header("게이지 설정")]
    public float gaugeWidth = 35f;
    public float gaugeHeight = 300f;
    public float gaugeOffsetX = 220f;      // 화면 중앙에서 좌우 거리
    public float gaugeBottomY = 200f;      // 하단 여백

    // 스킬 슬롯 설정
    [Header("스킬 슬롯 UI")]
    public float slotSize = 50f;
    public float slotGap = 8f;
    public float skillCooldownFontSize = 14f;
    public float skillLabelFontSize = 13f;
    public float labelOffsetX = 5f;        // 텍스트와 아이콘 간격
    public float labelWidth = 150f;        // 스킬 텍스트 가로 폭

    [Header("스킬 이름")]
    public string skill0Name = "우클릭";
    public string skill1Name = "L+R";
    public string skill2Name = "E키";
    public string skill3Name = "R키";

    [Header("조준점 (Crosshair)")]
    public bool showCrosshair = true;
    public float crosshairCenterDotSize = 3f;   // 중앙 점 크기
    public float crosshairBracketSize = 12f;    // 괄호 크기
    public float crosshairBracketThickness = 2f;// 괄호 두께
    public float crosshairGap = 8f;             // 중앙점과 괄호 간격
    public Color crosshairColor = new Color(1f, 1f, 1f, 0.8f);

    void Start()
    {
        CreateCanvas();
        CreateHPGauge();
        CreateChakraGauge();
        CreateSkillSlots();
        CreateDashSlot();
        CreateCrosshair();
        CreateGameOverPanel();
        CreateDungeonClearPanel();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerStats = playerObj.GetComponent<PlayerStats>();
            skillManager = playerObj.GetComponent<SkillManager>();
            playerController = playerObj.GetComponent<PlayerController>();
        }

        dungeonManager = FindObjectOfType<DungeonManager>();

        if (playerStats != null)
        {
            playerStats.OnHPChanged += UpdateHP;
            playerStats.OnChakraChanged += UpdateChakra;
            playerStats.OnGameOver += ShowGameOver;
            UpdateHP(playerStats.currentHP, playerStats.maxHP);
            UpdateChakra(playerStats.currentChakra, playerStats.maxChakra);
        }

        if (dungeonManager != null)
        {
            dungeonManager.OnDungeonCleared += ShowDungeonClear;
        }
    }

    void Update()
    {
        if (skillManager != null)
        {
            for (int i = 0; i < 4; i++)
            {
                float percent = skillManager.GetCooldownPercent(i);
                if (skillCooldownFills[i] != null)
                    skillCooldownFills[i].fillAmount = percent;

                if (skillCooldownTexts[i] != null)
                {
                    float remaining = skillManager.GetCooldownRemaining(i);
                    skillCooldownTexts[i].text = remaining > 0f ? remaining.ToString("F1") : "";
                }
            }
        }

        if (playerController != null)
        {
            if (dashCooldownFill != null)
                dashCooldownFill.fillAmount = playerController.GetDashCooldownPercent();
            if (dashCooldownText != null)
            {
                float remaining = playerController.GetDashCooldownRemaining();
                dashCooldownText.text = remaining > 0f ? remaining.ToString("F1") : "";
            }
        }
    }

    // ===== UI 자동 생성 =====

    void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("HUD_Canvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();
    }

    void CreateHPGauge()
    {
        // HP 게이지 - 화면 중앙 기준 좌측
        GameObject hpContainer = new GameObject("HP_Gauge");
        hpContainer.transform.SetParent(canvas.transform, false);
        RectTransform containerRect = hpContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0);
        containerRect.anchorMax = new Vector2(0.5f, 0);
        containerRect.pivot = new Vector2(0.5f, 0);
        containerRect.anchoredPosition = new Vector2(-gaugeOffsetX, gaugeBottomY);
        containerRect.sizeDelta = new Vector2(gaugeWidth, gaugeHeight);

        // 테두리
        CreateUIImage("HP_Border", containerRect,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(gaugeWidth + 6, gaugeHeight + 6), new Color(0.1f, 0.1f, 0.1f, 0.9f));

        // 내부 배경 (회색 - HP가 줄면 이 색이 보임)
        CreateUIImage("HP_Background", containerRect,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(gaugeWidth, gaugeHeight), new Color(0.4f, 0.4f, 0.4f, 0.8f));

        // HP Fill (초록색, 앵커 기반 아래→위)
        GameObject hpFillObj = CreateUIImage("HP_Fill", containerRect,
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero,
            Vector2.zero, new Color(0.2f, 0.85f, 0.2f, 1f));
        hpFillRect = hpFillObj.GetComponent<RectTransform>();
        hpFillRect.anchorMin = new Vector2(0, 0);
        hpFillRect.anchorMax = new Vector2(1, 1);
        hpFillRect.offsetMin = Vector2.zero;
        hpFillRect.offsetMax = Vector2.zero;

        // HP 텍스트 (게이지 하단)
        hpText = CreateUIText("HP_Text", containerRect,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -30),
            new Vector2(140, 30), "HP 1000", 18, Color.white);
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.fontStyle = FontStyle.Bold;
    }

    void CreateChakraGauge()
    {
        // CP 게이지 - 화면 중앙 기준 우측
        GameObject chakraContainer = new GameObject("Chakra_Gauge");
        chakraContainer.transform.SetParent(canvas.transform, false);
        RectTransform containerRect = chakraContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0);
        containerRect.anchorMax = new Vector2(0.5f, 0);
        containerRect.pivot = new Vector2(0.5f, 0);
        containerRect.anchoredPosition = new Vector2(gaugeOffsetX, gaugeBottomY);
        containerRect.sizeDelta = new Vector2(gaugeWidth, gaugeHeight);

        // 테두리
        CreateUIImage("Chakra_Border", containerRect,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(gaugeWidth + 6, gaugeHeight + 6), new Color(0.1f, 0.1f, 0.1f, 0.9f));

        // 내부 배경 (회색 - CP가 줄면 이 색이 보임)
        CreateUIImage("Chakra_Background", containerRect,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(gaugeWidth, gaugeHeight), new Color(0.4f, 0.4f, 0.4f, 0.8f));

        // CP Fill (파란색, 앵커 기반 아래→위)
        GameObject chakraFillObj = CreateUIImage("Chakra_Fill", containerRect,
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero,
            Vector2.zero, new Color(0.2f, 0.4f, 0.95f, 1f));
        chakraFillRect = chakraFillObj.GetComponent<RectTransform>();
        chakraFillRect.anchorMin = new Vector2(0, 0);
        chakraFillRect.anchorMax = new Vector2(1, 1);
        chakraFillRect.offsetMin = Vector2.zero;
        chakraFillRect.offsetMax = Vector2.zero;

        // CP 텍스트 (게이지 하단)
        chakraText = CreateUIText("Chakra_Text", containerRect,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -30),
            new Vector2(140, 30), "CP 500", 18, Color.white);
        chakraText.alignment = TextAnchor.MiddleCenter;
        chakraText.fontStyle = FontStyle.Bold;
    }

    void CreateSkillSlots()
    {
        // === 좌측: HP 게이지 왼쪽에 E(아래), R(위) ===
        // SkillManager 슬롯: 0=RClick, 1=L+R, 2=E, 3=R
        float leftGaugeCenter = -gaugeOffsetX;
        float leftSlotX = leftGaugeCenter - gaugeWidth / 2f - slotSize / 2f - 15f;

        float topY = gaugeBottomY + gaugeHeight;

        // R (슬롯 인덱스 3) - 상단 (텍스트 왼쪽)
        CreateSkillSlot(3, skill3Name, leftSlotX, topY - slotSize, slotSize, false);
        // E (슬롯 인덱스 2) - 그 아래 (텍스트 왼쪽)
        CreateSkillSlot(2, skill2Name, leftSlotX, topY - slotSize * 2 - slotGap, slotSize, false);
        // Shift 슬롯은 CreateDashSlot()에서 E 아래에 생성

        // === 우측: CP 게이지 오른쪽에 상단 정렬 ===
        float rightGaugeCenter = gaugeOffsetX;
        float rightSlotX = rightGaugeCenter + gaugeWidth / 2f + slotSize / 2f + 15f;

        // L+R (슬롯 인덱스 1) - 상단 (텍스트 오른쪽)
        CreateSkillSlot(1, skill1Name, rightSlotX, topY - slotSize, slotSize, true);
        // RClick (슬롯 인덱스 0) - 그 아래 (텍스트 오른쪽)
        CreateSkillSlot(0, skill0Name, rightSlotX, topY - slotSize * 2 - slotGap, slotSize, true);
    }

    void CreateDashSlot()
    {
        float leftGaugeCenter = -gaugeOffsetX;
        float leftSlotX = leftGaugeCenter - gaugeWidth / 2f - slotSize / 2f - 15f;
        float topY = gaugeBottomY + gaugeHeight;
        float yPos = topY - slotSize * 3 - slotGap * 2;

        GameObject container = new GameObject("SkillContainer_Shift");
        container.transform.SetParent(canvas.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0);
        containerRect.anchorMax = new Vector2(0.5f, 0);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(leftSlotX, yPos);
        containerRect.sizeDelta = new Vector2(slotSize + labelWidth, slotSize);

        // 슬롯 배경
        GameObject slot = CreateUIImage("Icon", container.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(slotSize, slotSize), new Color(0.15f, 0.15f, 0.15f, 0.85f));

        // 테두리
        GameObject border = CreateUIImage("Border", slot.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(slotSize + 3, slotSize + 3), new Color(0.3f, 0.3f, 0.3f, 0.9f));
        border.transform.SetAsFirstSibling();

        // 쿨타임 오버레이
        GameObject coolFill = CreateUIImage("CooldownFill", slot.transform,
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero,
            Vector2.zero, new Color(0f, 0f, 0f, 0.7f));
        RectTransform cfRect = coolFill.GetComponent<RectTransform>();
        cfRect.offsetMin = Vector2.zero;
        cfRect.offsetMax = Vector2.zero;
        dashCooldownFill = coolFill.GetComponent<Image>();
        dashCooldownFill.type = Image.Type.Filled;
        dashCooldownFill.fillMethod = Image.FillMethod.Radial360;
        dashCooldownFill.fillAmount = 0f;

        // 쿨타임 텍스트
        dashCooldownText = CreateUIText("CooldownText", slot.transform,
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero,
            "", (int)skillCooldownFontSize, Color.white);
        dashCooldownText.alignment = TextAnchor.MiddleCenter;
        dashCooldownText.fontStyle = FontStyle.Bold;

        // 키 라벨 (아이콘 왼쪽)
        Text keyLabel = CreateUIText("KeyLabel", container.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-slotSize / 2f - labelOffsetX - labelWidth / 2f, 0),
            new Vector2(labelWidth, 50), "Shift", (int)skillLabelFontSize, new Color(0.9f, 0.9f, 0.9f, 1f));
        keyLabel.alignment = TextAnchor.MiddleRight;
        keyLabel.fontStyle = FontStyle.Bold;
    }

    void CreateSkillSlot(int index, string label, float xPos, float yPos, float size, bool textOnRight)
    {
        // 컨테이너 생성 (아이콘 + 텍스트를 포함)
        GameObject container = new GameObject($"SkillContainer_{label}");
        container.transform.SetParent(canvas.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0);
        containerRect.anchorMax = new Vector2(0.5f, 0);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(xPos, yPos);
        containerRect.sizeDelta = new Vector2(size + labelWidth, size);

        // 슬롯 배경 (아이콘)
        GameObject slot = CreateUIImage("Icon", container.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(size, size), new Color(0.15f, 0.15f, 0.15f, 0.85f));

        // 테두리
        GameObject border = CreateUIImage("Border", slot.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(size + 3, size + 3), new Color(0.3f, 0.3f, 0.3f, 0.9f));
        border.transform.SetAsFirstSibling();

        // 쿨타임 오버레이
        GameObject coolFill = CreateUIImage("CooldownFill", slot.transform,
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero,
            Vector2.zero, new Color(0f, 0f, 0f, 0.7f));
        RectTransform cfRect = coolFill.GetComponent<RectTransform>();
        cfRect.offsetMin = Vector2.zero;
        cfRect.offsetMax = Vector2.zero;
        skillCooldownFills[index] = coolFill.GetComponent<Image>();
        skillCooldownFills[index].type = Image.Type.Filled;
        skillCooldownFills[index].fillMethod = Image.FillMethod.Radial360;
        skillCooldownFills[index].fillAmount = 0f;

        // 쿨타임 텍스트 (아이콘 내부)
        skillCooldownTexts[index] = CreateUIText("CooldownText", slot.transform,
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero,
            "", (int)skillCooldownFontSize, Color.white);
        skillCooldownTexts[index].alignment = TextAnchor.MiddleCenter;
        skillCooldownTexts[index].fontStyle = FontStyle.Bold;

        // 키 라벨 (컨테이너 내부)
        Text keyLabel;
        if (textOnRight)
        {
            // 우측: 아이콘 오른쪽에 텍스트
            keyLabel = CreateUIText("KeyLabel", container.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(size / 2f + labelOffsetX + labelWidth / 2f, 0),
                new Vector2(labelWidth, 50), label, (int)skillLabelFontSize, new Color(0.9f, 0.9f, 0.9f, 1f));
            keyLabel.alignment = TextAnchor.MiddleLeft;
        }
        else
        {
            // 좌측: 아이콘 왼쪽에 텍스트
            keyLabel = CreateUIText("KeyLabel", container.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-size / 2f - labelOffsetX - labelWidth / 2f, 0),
                new Vector2(labelWidth, 50), label, (int)skillLabelFontSize, new Color(0.9f, 0.9f, 0.9f, 1f));
            keyLabel.alignment = TextAnchor.MiddleRight;
        }
        keyLabel.fontStyle = FontStyle.Bold;
    }

    void CreateCrosshair()
    {
        if (!showCrosshair) return;

        // 조준점 컨테이너 (화면 중앙)
        GameObject crosshairContainer = new GameObject("Crosshair");
        crosshairContainer.transform.SetParent(canvas.transform, false);
        RectTransform containerRect = crosshairContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = new Vector2(100, 100);

        // 중앙 점
        GameObject centerDot = CreateUIImage("CenterDot", crosshairContainer.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(crosshairCenterDotSize, crosshairCenterDotSize), crosshairColor);
        centerDot.GetComponent<Image>().raycastTarget = false;

        float distance = crosshairCenterDotSize / 2f + crosshairGap + crosshairBracketSize / 2f;

        // 좌측 괄호 (
        CreateBracket("LeftBracket", crosshairContainer.transform,
            new Vector2(-distance, 0), new Vector2(crosshairBracketThickness, crosshairBracketSize));

        // 우측 괄호 )
        CreateBracket("RightBracket", crosshairContainer.transform,
            new Vector2(distance, 0), new Vector2(crosshairBracketThickness, crosshairBracketSize));

        // 상단 괄호
        CreateBracket("TopBracket", crosshairContainer.transform,
            new Vector2(0, distance), new Vector2(crosshairBracketSize, crosshairBracketThickness));

        // 하단 괄호
        CreateBracket("BottomBracket", crosshairContainer.transform,
            new Vector2(0, -distance), new Vector2(crosshairBracketSize, crosshairBracketThickness));
    }

    void CreateBracket(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject bracket = CreateUIImage(name, parent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position,
            size, crosshairColor);
        bracket.GetComponent<Image>().raycastTarget = false;
    }

    void CreateGameOverPanel()
    {
        gameOverPanel = CreatePanel("GameOverPanel", "GAME OVER", new Color(0.5f, 0f, 0f, 0.8f));
        gameOverPanel.SetActive(false);
    }

    void CreateDungeonClearPanel()
    {
        // 전체 화면 패널 없이 텍스트만 화면 중앙에 크게 표시
        dungeonClearPanel = new GameObject("VictoryPanel");
        dungeonClearPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = dungeonClearPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(1000, 220);

        // 텍스트 가독성을 위한 그림자 (약간 아래·오른쪽으로 오프셋)
        CreateUIText("VictoryShadow", dungeonClearPanel.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(8f, -8f),
            new Vector2(1200, 400), "VICTORY", 240, new Color(0f, 0f, 0f, 0.7f));

        // 메인 VICTORY 텍스트 (골드 컬러)
        Text victoryText = CreateUIText("VictoryText", dungeonClearPanel.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(1200, 400), "VICTORY", 240, new Color(1f, 0.88f, 0.1f, 1f));
        victoryText.fontStyle = FontStyle.Bold;

        dungeonClearPanel.SetActive(false);
    }

    // ===== 헬퍼 함수 =====

    GameObject CreateUIImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        Image img = obj.AddComponent<Image>();
        img.color = color;
        return obj;
    }

    Text CreateUIText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta, string text, int fontSize, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        Text t = obj.AddComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.color = color;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        return t;
    }

    GameObject CreatePanel(string name, string message, Color bgColor)
    {
        GameObject panel = CreateUIImage(name, canvas.transform,
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero,
            Vector2.zero, bgColor);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Text txt = CreateUIText("Message", panel.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(600, 100), message, 48, Color.white);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontStyle = FontStyle.Bold;

        return panel;
    }

    // ===== 업데이트 =====

    void UpdateHP(float current, float max)
    {
        if (hpFillRect != null)
            hpFillRect.anchorMax = new Vector2(1, current / max);
        if (hpText != null)
            hpText.text = $"HP {(int)current}";
    }

    void UpdateChakra(float current, float max)
    {
        if (chakraFillRect != null)
            chakraFillRect.anchorMax = new Vector2(1, current / max);
        if (chakraText != null)
            chakraText.text = $"CP {(int)current}";
    }

    void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ShowDungeonClear()
    {
        if (dungeonClearPanel != null)
            StartCoroutine(ShowVictoryThenHide());
    }

    IEnumerator ShowVictoryThenHide()
    {
        dungeonClearPanel.SetActive(true);
        yield return new WaitForSeconds(3f);
        dungeonClearPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnHPChanged -= UpdateHP;
            playerStats.OnChakraChanged -= UpdateChakra;
            playerStats.OnGameOver -= ShowGameOver;
        }
        if (dungeonManager != null)
        {
            dungeonManager.OnDungeonCleared -= ShowDungeonClear;
        }
    }
}
