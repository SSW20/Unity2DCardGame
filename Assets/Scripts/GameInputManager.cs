using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;


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

    [SerializeField] private TextMeshProUGUI playerScoreText;

    private bool isPlayerTurn = false;

    void Start()
    {

    }

    void Update()
    {
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
                ("Chain Attack", "Use 2 additional cards next turn")
            });
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            specialSelectPanel.Hide();
        }
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

        newCard.transform.position = deckPanel.position;
        newCard.transform.localScale = Vector3.one;

        handLayoutManager.UpdateLayout();
    }

    // 플레이어 턴 종료 → AI 턴으로
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

    public void ButtonClicked()
    {
        Debug.Log("Button was clicked!");
    }

    public void OnFinish()
    {
        // 데이터 처리 + 점수 계산
        SettlementResult result = cardManager.Settle();
        int blankSlots = GetEmptyAISlots().Count;
        float score = cardManager.CalculateScore(result, blankSlots);

        Debug.Log($"Settlement score: {score}");

        // TODO: score를 실제 UI(점수판)에 반영
        if (playerScoreText != null)
        {
            playerScoreText.text = $"Score: {score}";
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

    private void UpdateGraveVisual()
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