using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class GameInputManager : MonoBehaviour
{
    [SerializeField] private GameTurnManager gameTurnManager;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform handPanel;
    [SerializeField] private Transform deckPanel;
    [SerializeField] private Transform graveyardPanel;

    [SerializeField] private HandLayoutManager handLayoutManager;

    [SerializeField] private CardManager cardManager;

    [SerializeField] private Transform fieldSlotContainer;

    [SerializeField] private AIController aiController;
    [SerializeField] private CardHoverManager cardHoverManager;

    [SerializeField] private TextMeshProUGUI playerScoreText;
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Graveyard Viewer")]
    [SerializeField, Range(0f, 1f)] private float graveyardOverlayOpacity = 0.9f;
    [SerializeField] private Vector2 graveyardViewerCardSize = new Vector2(140f, 200f);
    [SerializeField] private Vector2 graveyardViewerSpacing = new Vector2(24f, 24f);
    [SerializeField] private int graveyardViewerMaxColumns = 10;

    [Header("Score Information")]
    [SerializeField] private Button scoreInfoButton;
    [SerializeField] private TMP_FontAsset scoreInfoFont;
    [SerializeField, Range(0f, 1f)] private float scoreInfoOverlayOpacity = 0.94f;
    [SerializeField] private float scoreInfoTitleFontSize = 44f;
    [SerializeField] private float scoreInfoBodyFontSize = 26f;

    private bool isPlayerTurn = false;
    private Coroutine drawCardsCoroutine;
    private bool scoreTextWarningShown;

    private CardUI lastHovered;
    private int lastFieldCount = -1;
    private int lastGraveCount = -1;
    private GameObject graveyardOverlay;
    private GameObject scoreInfoOverlay;

    void Start()
    {
        ResolveScoreText();
        if (scoreInfoButton != null)
            scoreInfoButton.onClick.AddListener(ShowScoreInformation);
        else
            Debug.LogWarning("ScoreInfoButton is not assigned in GameInputManager.");
    }

    void Update()
    {
        if (graveyardOverlay != null || scoreInfoOverlay != null)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                HideActiveOverlay();

            return;
        }

        if (Input.GetMouseButtonDown(0) && IsPointerOverPlayerGraveyard())
        {
            ShowGraveyardViewer();
            return;
        }

        UpdateScorePreview();

        if (Input.GetKeyDown(KeyCode.G))
        {
            gameTurnManager?.ShowDebugFinalResult();
        }
    }



    private void UpdateScorePreview()
    {
        TextMeshProUGUI valueText = ResolveScoreText();
        if (valueText == null) return;

        CardUI hovered = cardHoverManager?.CurrentHovered;
        int fieldCount = cardManager.fieldList.Count;
        int graveCount = cardManager.graveList.Count;

        if (hovered == lastHovered && fieldCount == lastFieldCount && graveCount == lastGraveCount)
            return;

        lastHovered = hovered;
        lastFieldCount = fieldCount;
        lastGraveCount = graveCount;

        List<PokerCardData> pool = new List<PokerCardData>();
        pool.AddRange(cardManager.fieldList);
        pool.AddRange(cardManager.graveList);

        // 무덤 카드 특전의 미리보기 계산에는
        // 기존 무덤이 아니라 현재 필드에 놓인 카드만 사용한다.
        List<PokerCardData> previewFieldCards =
            new List<PokerCardData>(cardManager.fieldList);

        int emptySlots = GetEmptyPlayerSlots();

        if (hovered != null && hovered.cardType == CardType.Hand)
        {
            pool.Add(hovered.pokerCardData);
            previewFieldCards.Add(hovered.pokerCardData);
            emptySlots = Mathf.Max(0, emptySlots - 1); // 호버 카드가 슬롯 하나 차지한다고 가정
        }

        SettlementResult result = ScoreEvaluator.EvaluateAll(pool);
        result.newGraveCardCount =
            ScoreEvaluator.CountUnusedFieldCards(
                previewFieldCards,
                result.usedCards);

        float score = cardManager.CalculateScore(result, emptySlots);

        valueText.text = Mathf.RoundToInt(score).ToString();
    }

    private bool IsPointerOverPlayerGraveyard()
    {
        if (graveyardPanel == null || cardManager == null || cardManager.graveList.Count == 0)
            return false;

        RectTransform graveyardRect = graveyardPanel as RectTransform;
        if (graveyardRect == null)
            return false;

        Canvas canvas = graveyardPanel.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(
            graveyardRect,
            Input.mousePosition,
            eventCamera);
    }

    private void ShowGraveyardViewer()
    {
        if (graveyardOverlay != null || cardManager == null || cardManager.graveList.Count == 0)
            return;

        Canvas canvas = graveyardPanel != null
            ? graveyardPanel.GetComponentInParent<Canvas>()
            : null;
        if (canvas == null)
        {
            Debug.LogWarning("Player graveyard Canvas could not be found.");
            return;
        }

        graveyardOverlay = new GameObject(
            "PlayerGraveyardOverlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        graveyardOverlay.transform.SetParent(canvas.transform, false);
        graveyardOverlay.transform.SetAsLastSibling();
        cardHoverManager?.SetSuspended(true);
        gameTurnManager?.SetPlayerActionButtonsBlocked(true);

        RectTransform overlayRect = graveyardOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image background = graveyardOverlay.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, graveyardOverlayOpacity);

        Button closeButton = graveyardOverlay.GetComponent<Button>();
        closeButton.transition = Selectable.Transition.None;
        closeButton.onClick.AddListener(HideGraveyardViewer);

        GameObject contentObject = new GameObject(
            "Cards",
            typeof(RectTransform),
            typeof(GridLayoutGroup));
        contentObject.transform.SetParent(graveyardOverlay.transform, false);

        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.05f, 0.08f);
        contentRect.anchorMax = new Vector2(0.95f, 0.92f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        GridLayoutGroup grid = contentObject.GetComponent<GridLayoutGroup>();
        grid.cellSize = graveyardViewerCardSize;
        grid.spacing = graveyardViewerSpacing;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(
            1,
            Mathf.Min(graveyardViewerMaxColumns, cardManager.graveList.Count));

        foreach (PokerCardData graveCard in cardManager.graveList)
        {
            GameObject card = Instantiate(cardPrefab, contentObject.transform);
            card.name = "GraveyardCard";

            RectTransform cardRect = card.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.localScale = Vector3.one;
                cardRect.localRotation = Quaternion.identity;
            }

            CardUI cardUI = card.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.useAnimation = false;
                cardUI.cardType = CardType.Deck;
                cardUI.SetPokerData(graveCard);
                cardUI.FlipCard(true);
            }

            CardDragManager dragManager = card.GetComponent<CardDragManager>();
            if (dragManager != null)
            {
                dragManager.enabled = false;
                Destroy(dragManager);
            }
        }
    }

    private void HideGraveyardViewer()
    {
        if (graveyardOverlay == null)
            return;

        Destroy(graveyardOverlay);
        graveyardOverlay = null;
        ResumeCardHoverIfNoOverlay();
    }

    private void ShowScoreInformation()
    {
        if (scoreInfoOverlay != null)
            return;

        Canvas canvas = scoreInfoButton != null
            ? scoreInfoButton.GetComponentInParent<Canvas>()
            : null;
        if (canvas == null)
        {
            Debug.LogWarning("Score information Canvas could not be found.");
            return;
        }

        scoreInfoOverlay = new GameObject(
            "ScoreInformationOverlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        scoreInfoOverlay.transform.SetParent(canvas.transform, false);
        scoreInfoOverlay.transform.SetAsLastSibling();
        cardHoverManager?.SetSuspended(true);
        gameTurnManager?.SetPlayerActionButtonsBlocked(true);

        RectTransform overlayRect = scoreInfoOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image background = scoreInfoOverlay.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, scoreInfoOverlayOpacity);

        Button closeButton = scoreInfoOverlay.GetComponent<Button>();
        closeButton.transition = Selectable.Transition.None;
        closeButton.onClick.AddListener(HideScoreInformation);

        TMP_FontAsset font = ResolveScoreInfoFont();
        CreateScoreInfoText(
            scoreInfoOverlay.transform,
            "Title",
            "점수 계산 정보",
            new Vector2(0.08f, 0.85f),
            new Vector2(0.92f, 0.95f),
            scoreInfoTitleFontSize,
            TextAlignmentOptions.Center,
            font);
        CreateScoreInfoText(
            scoreInfoOverlay.transform,
            "Body",
            GetScoreInformationText(),
            new Vector2(0.1f, 0.08f),
            new Vector2(0.9f, 0.84f),
            scoreInfoBodyFontSize,
            TextAlignmentOptions.TopLeft,
            font);
        CreateScoreInfoText(
            scoreInfoOverlay.transform,
            "CloseHint",
            "검은 여백을 누르거나 ESC를 누르면 닫힙니다.",
            new Vector2(0.52f, 0.015f),
            new Vector2(0.98f, 0.065f),
            scoreInfoBodyFontSize * 0.8f,
            TextAlignmentOptions.BottomRight,
            font);
        CreateJokerInformation(scoreInfoOverlay.transform, font);
    }

    private void CreateJokerInformation(Transform parent, TMP_FontAsset font)
    {
        if (cardPrefab == null)
            return;

        GameObject jokerCard = Instantiate(cardPrefab, parent);
        jokerCard.name = "JokerCardPreview";

        RectTransform cardRect = jokerCard.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            cardRect.anchorMin = new Vector2(0.72f, 0.75f);
            cardRect.anchorMax = new Vector2(0.72f, 0.75f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = graveyardViewerCardSize * 0.8f;
            cardRect.localScale = Vector3.one;
            cardRect.localRotation = Quaternion.identity;
        }

        CardUI cardUI = jokerCard.GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.useAnimation = false;
            cardUI.cardType = CardType.Deck;

            if (cardManager != null && cardManager.JokerSprite != null)
            {
                cardUI.SetPokerData(PokerCardData.CreateJoker(cardManager.JokerSprite));
                cardUI.FlipCard(true);
            }
            else
            {
                cardUI.FlipCard(false);
            }
        }

        CardDragManager dragManager = jokerCard.GetComponent<CardDragManager>();
        if (dragManager != null)
        {
            dragManager.enabled = false;
            Destroy(dragManager);
        }

        CreateScoreInfoText(
            parent,
            "JokerDescription",
            "<size=115%><b>조커 카드</b></size>\n" +
            "코스트: 10\n" +
            "이번 턴에 손패의 조커를 모두 내지 않으면\n강제로 STOP됩니다.",
            new Vector2(0.77f, 0.67f),
            new Vector2(0.97f, 0.84f),
            scoreInfoBodyFontSize * 0.85f,
            TextAlignmentOptions.MidlineLeft,
            font);
    }

    private void CreateScoreInfoText(
        Transform parent,
        string objectName,
        string content,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize,
        TextAlignmentOptions alignment,
        TMP_FontAsset font)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
    }

    private TMP_FontAsset ResolveScoreInfoFont()
    {
        if (scoreInfoFont != null)
            return scoreInfoFont;

        TextMeshProUGUI[] sceneTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in sceneTexts)
        {
            if (text != null
                && text.font != null
                && text.font.name.ToLowerInvariant().Contains("neodgm"))
            {
                scoreInfoFont = text.font;
                return scoreInfoFont;
            }
        }

        TextMeshProUGUI templateText = scoreInfoButton != null
            ? scoreInfoButton.GetComponentInChildren<TextMeshProUGUI>(true)
            : null;
        scoreInfoFont = templateText != null ? templateText.font : null;
        return scoreInfoFont;
    }

    private string GetScoreInformationText()
    {
        return
            "<b>카드 숫자</b>  A=1 / J=11 / Q=12 / K=13\n\n" +
            "<size=115%><b>조합 조건</b></size>\n" +
            "트리플: 같은 숫자 3장    포카드: 같은 숫자 4장\n" +
            "스트레이트: 서로 이어지는 숫자 4장 이상\n\n" +
            "<size=115%><b>트리플·포카드 점수</b></size>\n" +
            "트리플: (조합 카드 숫자 합 + 10) × 3\n" +
            "포카드: (조합 카드 숫자 합 + 10) × 8\n\n" +
            "<size=115%><b>스트레이트 점수</b></size>\n" +
            "조합 카드 숫자 합 × 족보 상수\n" +
            "4연속 ×3 / 5연속 ×4 / 6연속 ×5 / 7연속 이상 ×8\n" +
            "여러 스트레이트가 있으면 각각 계산해서 더합니다.\n\n" +
            "<size=115%><b>특전 상세</b></size>\n" +
            "1. <b>실전압축 슬롯</b>\n" +
            "   상세: 점수 + 빈 슬롯 수 × 50점. 빈 슬롯 수만큼 추가 점수를 얻습니다.\n" +
            "2. <b>파묘</b>\n" +
            "   상세: 점수 + 무덤 카드 수 × 20점. 무덤의 카드가 많을수록 추가 점수를 얻습니다.\n" +
            "3. <b>같은 숫자 수집가</b>\n" +
            "   상세: 포카드 코스트에 +20을 적용하고 포카드 족보 상수는 ×8, 트리플은 ×4로 변경합니다.\n" +
            "4. <b>공세</b>\n" +
            "   상세: 상대보다 먼저 STOP 상태에 진입하면 +100점을 얻습니다.\n" +
            "5. <b>연속의 달인</b>\n" +
            "   상세: 스트레이트 코스트 평균이 6 이하이면 4/5/6연속 상수를 ×4/×5/×6으로 변경합니다.\n" +
            "   평균이 7 이상이면 코스트 보정값 +20을 적용하며, 7연속 상수는 변경하지 않습니다.\n\n" +
            "<b>결산:</b> 조합에 사용된 카드는 덱으로, 사용되지 않은 필드 카드는 무덤으로 이동합니다.\n" +
            "<b>턴 종료:</b> 남은 손패를 덱으로 되돌리고 다음 턴으로 넘깁니다. 상대가 이미 결산했다면 플레이어 턴을 다시 시작합니다.";
    }

    private void HideScoreInformation()
    {
        if (scoreInfoOverlay == null)
            return;

        Destroy(scoreInfoOverlay);
        scoreInfoOverlay = null;
        ResumeCardHoverIfNoOverlay();
    }

    private void ResumeCardHoverIfNoOverlay()
    {
        if (graveyardOverlay == null && scoreInfoOverlay == null)
        {
            cardHoverManager?.SetSuspended(false);
            gameTurnManager?.SetPlayerActionButtonsBlocked(false);
        }
    }

    private void HideActiveOverlay()
    {
        if (graveyardOverlay != null)
            HideGraveyardViewer();
        if (scoreInfoOverlay != null)
            HideScoreInformation();
    }

    private TextMeshProUGUI ResolveScoreText()
    {
        if (scoreText != null)
            return scoreText;

        if (playerScoreText != null && playerScoreText.transform.parent != null)
        {
            TextMeshProUGUI[] texts =
                playerScoreText.transform.parent.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in texts)
            {
                if (text != null && text.gameObject.name == "ScoreText")
                {
                    scoreText = text;
                    return scoreText;
                }
            }
        }

        if (!scoreTextWarningShown)
        {
            Debug.LogWarning("ScorePannel 아래의 ScoreText가 연결되지 않았습니다.");
            scoreTextWarningShown = true;
        }

        return null;
    }

    private int GetEmptyPlayerSlots()
    {
        int count = 0;
        CardSlot[] allSlots = fieldSlotContainer.GetComponentsInChildren<CardSlot>();
        foreach (var slot in allSlots)
        {
            if (slot.owner == SlotOwner.Player
                && slot.category == SlotCategory.Field
                && !slot.IsOccupied)
            {
                count++;
            }
        }
        return count;
    }

    // 플레이어 턴 시작
    public void StartPlayerTurn()
    {
        if (isPlayerTurn) return;

        isPlayerTurn = true;

        // 코스트 초기화
        cardManager.ResetPlayerCost();

        // 카드 드로우
        CancelPendingDraw();
        drawCardsCoroutine = StartCoroutine(DrawCards());
    }

    private IEnumerator DrawCards(int count = 5)
    {
        for (int i = 0; i < count; i++)
        {
            if (!isPlayerTurn)
                break;

            AddCardToHand();
            yield return new WaitForSeconds(0.1f);
        }

        drawCardsCoroutine = null;
    }

    private void CancelPendingDraw()
    {
        if (drawCardsCoroutine == null)
            return;

        StopCoroutine(drawCardsCoroutine);
        drawCardsCoroutine = null;
    }

    private void AddCardToHand()
    {
        GameObject newCard = Instantiate(cardPrefab, handPanel.transform);
        PokerCardData drawnCard = cardManager.DrawCard(cardManager.pokerDeck);

        CardUI cardUI = newCard.GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.SetPokerData(drawnCard);

            // 카드 앞면 표시 설정 해주는 곳 --> 앞면은 필드, 핸드 말고 더 있어야되는 이유가 있나? 
            cardUI.FlipCard(true);
        }

        CardDragManager cardDragManager = newCard.GetComponent<CardDragManager>();
        if (cardDragManager != null)
        {
            // CardManager와 GameInputManager를 자동 연결합니다.
            // 카드 프리팹 Inspector에서 별도로 연결할 필요가 없습니다.
            cardDragManager.Initialize(cardManager, this);
        }

        // 덱 월드 좌표 → handPanel 로컬 좌표로 변환해서 시작 위치 설정
        newCard.transform.localPosition = handPanel.InverseTransformPoint(deckPanel.position);
        newCard.transform.localScale = Vector3.one;

        handLayoutManager.UpdateLayout();
    }

    /// <summary>
    /// 플레이어 카드가 필드에 정상 배치된 직후 호출됩니다.
    /// 플레이어의 일반 필드 슬롯이 모두 찼으면 즉시 강제 Stop합니다.
    /// </summary>
    public void NotifyPlayerCardPlaced()
    {
        if (!isPlayerTurn)
            return;

        if (GetEmptyPlayerSlots() > 0)
            return;

        Debug.LogWarning("플레이어 필드 슬롯이 모두 찼습니다. 강제 Stop을 요청합니다.");
        gameTurnManager?.ForcePlayerStopBecauseFieldIsFull();
    }

    // 플레이어 턴 종료 → AI 턴으로
    //질문: 혹시 한 라운드에 턴 종료 횟수를 제한두는건 어떤가요? 제한이 없으면 좋은거 뜰  때까지 카드 안내고 턴 엔드만 할 수 있지 않나요?
    public void OnTurnEnd()
    {
        if (gameTurnManager != null)
            gameTurnManager.OnPlayerTurnEndButton();
    }

    public void CleanupPlayerHandAfterTurn()
    {
        isPlayerTurn = false;
        CancelPendingDraw();

        RemoveCardFromHand();
        cardManager.RemoveCardAll(cardManager.pokerDeck);
    }

    /// <summary>
    /// AI 고유 특전: 플레이어 필드의 일반 카드 중 무작위 1장을 덱으로 되돌린다.
    /// 조커와 특전 카드는 후보에서 제외한다.
    /// </summary>
    public IEnumerator RemoveRandomNormalFieldCardToDeck(
        float moveDuration,
        System.Action<bool> onCompleted)
    {
        List<CardSlot> candidates = new List<CardSlot>();
        CardSlot[] slots = fieldSlotContainer.GetComponentsInChildren<CardSlot>();

        foreach (CardSlot slot in slots)
        {
            if (slot.owner != SlotOwner.Player
                || slot.category != SlotCategory.Field
                || !slot.IsOccupied
                || slot.CurrentCardObject == null)
            {
                continue;
            }

            CardUI cardUI = slot.CurrentCardObject.GetComponent<CardUI>();
            if (cardUI == null || cardUI.pokerCardData.IsJoker)
                continue;

            candidates.Add(slot);
        }

        if (candidates.Count == 0)
        {
            onCompleted?.Invoke(false);
            yield break;
        }

        CardSlot selectedSlot = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        GameObject selectedCardObject = selectedSlot.CurrentCardObject;
        CardUI selectedCardUI = selectedCardObject.GetComponent<CardUI>();
        PokerCardData selectedCardData = selectedCardUI.pokerCardData;

        bool moved = cardManager.MoveCard(
            selectedCardData,
            cardManager.fieldList,
            cardManager.pokerDeck);

        if (!moved)
        {
            Debug.LogWarning("AI 특전 대상 카드의 데이터를 필드에서 찾지 못했습니다.");
            onCompleted?.Invoke(false);
            yield break;
        }

        cardManager.ShuffleDeck(cardManager.pokerDeck);
        selectedSlot.ClearSlot();

        selectedCardUI.SetHover(false);
        selectedCardUI.useAnimation = false;

        CardDragManager dragManager = selectedCardObject.GetComponent<CardDragManager>();
        if (dragManager != null)
            dragManager.enabled = false;

        float duration = Mathf.Max(0.01f, moveDuration);
        Sequence seq = DOTween.Sequence();
        seq.Append(selectedCardObject.transform.DOMove(deckPanel.position, duration).SetEase(Ease.InCubic));
        seq.Join(selectedCardObject.transform.DOScale(Vector3.zero, duration));
        yield return seq.WaitForCompletion();

        Destroy(selectedCardObject);
        onCompleted?.Invoke(true);
    }

    private void RemoveCardFromHand()
    {
        foreach (Transform child in handPanel)
        {
            GameObject card = child.gameObject;

            CardUI cardUI = card.GetComponent<CardUI>();
            if (cardUI != null)
                cardUI.useAnimation = false;

            Sequence seq = DOTween.Sequence();
            seq.Append(card.transform.DOScale(Vector3.zero, 0.3f));
            seq.Join(card.transform.DOMove(deckPanel.position, 0.3f).SetEase(Ease.InCubic));
            seq.AppendCallback(() => Destroy(card));
        }
    }



    // 결산 페이즈를 여기서 호출하는데, GameTurnManager에서도 결산페이즈를 호출하고 있음, 중복 계산으로 오류 발생 확률이 높아져요.
    // 지금 이 함수는 점수 계산과 UI 업데이트를 담당하는데, GameTurnManager에서 실제로 결산페이즈를 시작하는게 좋지 않나....
    // 어떻게 구조를 생각했냐면 GameTurnManager에서 결산페이즈를 시작하고, GameTurnManager는 현재 턴이 누구인지 알 수 있으니 그 값을 여기다가 넘겨주고 
    // 여기서는 UI 업데이트만 담당하는게 좋을듯요.
    public void OnFinish()
    {
        // 데이터 처리 + 점수 계산
        SettlementResult result = cardManager.Settle();
        int blankSlots = GetEmptyAISlots().Count;
        float score = cardManager.CalculateScore(result, blankSlots);

        // TODO: score를 실제 UI(점수판)에 반영
        TextMeshProUGUI valueText = ResolveScoreText();
        if (valueText != null)
        {
            valueText.text = Mathf.RoundToInt(score).ToString();
        }

        // 게임 종료 처리
        RemoveCardFromHand();
        cardManager.RemoveCardAll(cardManager.pokerDeck);

        foreach (Transform slotTransform in fieldSlotContainer)
        {
            CardSlot slot = slotTransform.GetComponent<CardSlot>();
            if (slot == null || !slot.IsOccupied) continue;

            GameObject fieldCard = slot.CurrentCardObject;
            CardUI cardUI = fieldCard.GetComponent<CardUI>();
            bool wasUsed = cardUI != null && result.usedCards.Contains(cardUI.pokerCardData);

            Vector3 targetPos = wasUsed ? deckPanel.position : graveyardPanel.position;
            AnimateAndDestroy(fieldCard, targetPos);

            slot.ClearSlot();
        }
        UpdateGraveVisual();

    }

    private void AnimateAndDestroy(GameObject card, Vector3 targetPos)
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(card.transform.DOMove(targetPos, 0.3f));
        seq.Join(card.transform.DOScale(Vector3.zero, 0.3f));
        seq.AppendCallback(() => Destroy(card));
    }

    private List<GameObject> graveVisualStack = new List<GameObject>();

    public void UpdateGraveVisual()
    {
        int actualCount = cardManager.graveList.Count;
        int visualCount = actualCount == 0 ? 0 : (actualCount / 2) + 1;

        foreach (var obj in graveVisualStack)
            Destroy(obj);
        graveVisualStack.Clear();

        for (int i = 0; i < visualCount; i++)
        {
            GameObject card = Instantiate(cardPrefab, graveyardPanel);
            RectTransform rt = card.GetComponent<RectTransform>();

            if (i == 0)
                rt.localRotation = Quaternion.identity;
            else
                rt.localRotation = Quaternion.Euler(0, 0, Random.Range(-5f, 5f));

            rt.localScale = Vector3.one;

            CardUI cardUI = card.GetComponent<CardUI>();
            if (cardUI != null) cardUI.useAnimation = false;

            CardDragManager dragManager = card.GetComponent<CardDragManager>();
            if (dragManager != null) Destroy(dragManager);

            graveVisualStack.Add(card);
        }
    }

    private List<CardSlot> GetEmptyAISlots()
    {
        List<CardSlot> result = new List<CardSlot>();
        CardSlot[] allSlots = fieldSlotContainer.GetComponentsInChildren<CardSlot>();
        foreach (var slot in allSlots)
        {
            if (!slot.IsOccupied && slot.owner == SlotOwner.Enemy)
                result.Add(slot);
        }
        return result;
    }
}
