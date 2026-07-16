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
    [SerializeField] private GameOverPannel gameOverPannel;

    [SerializeField] private SpecialSelectPanel specialSelectPanel;

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

    private bool isPlayerTurn = false;
    private bool scoreTextWarningShown;

    private CardUI lastHovered;
    private int lastFieldCount = -1;
    private int lastGraveCount = -1;
    private GameObject graveyardOverlay;

    void Start()
    {
        ResolveScoreText();
    }

    void Update()
    {
        if (graveyardOverlay != null)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                HideGraveyardViewer();

            return;
        }

        if (Input.GetMouseButtonDown(0) && IsPointerOverPlayerGraveyard())
        {
            ShowGraveyardViewer();
            return;
        }

        UpdateScorePreview();

        if (Input.GetKeyDown(KeyCode.V))
        {
            if (gameTurnManager != null)
                gameTurnManager.StartGame();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            gameOverPannel.Show();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            gameOverPannel.Hide();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            specialSelectPanel.Show(new List<(string, string)>
            {
                ("Flame Boost", "Next card damage x2"),
                ("Ice Shield", "Reduce damage by 50%"),
                ("Chain Attack", "Use 2 additional cards next turn"),
                ("Lucky Draw", "Draw one extra card at the start of your turn"),
                ("Second Wind", "Recover once when your score falls behind")
            });
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            specialSelectPanel.Hide();
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
            if (slot.owner == SlotOwner.Player && !slot.IsOccupied)
                count++;
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
        StartCoroutine(DrawCards());
    }

    private IEnumerator DrawCards(int count = 5)
    {
        for (int i = 0; i < count; i++)
        {
            AddCardToHand();
            yield return new WaitForSeconds(0.1f);
        }
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
            cardDragManager.cardManager = cardManager;
        }

        // 덱 월드 좌표 → handPanel 로컬 좌표로 변환해서 시작 위치 설정
        newCard.transform.localPosition = handPanel.InverseTransformPoint(deckPanel.position);
        newCard.transform.localScale = Vector3.one;

        handLayoutManager.UpdateLayout();
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

        RemoveCardFromHand();
        cardManager.RemoveCardAll(cardManager.pokerDeck);
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
