using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameInputManager : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform handPanel;
    [SerializeField] private Transform deckPanel;
    [SerializeField] private Transform graveyardPanel;

    [SerializeField] private HandLayoutManager handLayoutManager;
    [SerializeField] private GameOverPannel gameOverPannel;

    [SerializeField] private SpecialSelectPanel specialSelectPanel;

    [SerializeField] private CardManager cardManager;

    [SerializeField] private Transform fieldSlotContainer;   // 필드 슬롯들의 부모


    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DrawCards());
    }

    private IEnumerator DrawCards(int count = 5)
    {
        for (int i = 0; i < count; i++)
        {
            AddCardToHand();
            yield return new WaitForSeconds(0.1f); // 0.1초 딜레이
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.V))
        {
            StartCoroutine(DrawCards());
        }

        //  if (Input.GetKeyDown(KeyCode.B))
        // {
        //     RemoveCardFromHand();
        // }

         if (Input.GetKeyDown(KeyCode.G))
        {
            gameOverPannel.Show();
        }
         if(Input.GetKeyDown(KeyCode.H))
        {
            gameOverPannel.Hide();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            specialSelectPanel.Show(new List<(string, string)>
            {
                ("화염 강화", "다음 카드의 데미지가 2배"),
                ("얼음 방패", "받는 피해를 50% 감소"),
                ("연쇄 공격", "다음 턴 카드를 2장 더 사용")
            });
        }

        if (Input.GetKeyDown(KeyCode.Y))
            specialSelectPanel.Hide();

            
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

        cardManager.RemoveCardAll(cardManager.pokerDeck);
    }



    private void AddCardToHand()
    {
        GameObject newCard = Instantiate(cardPrefab, handPanel.transform);
        PokerCardData drawnCard = cardManager.DrawCard(cardManager.pokerDeck);

        CardUI cardUI = newCard.GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.SetPokerData(drawnCard);
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

    public void ButtonClicked()
    {
        Debug.Log("Button was clicked!");
    }

    public void OnTurnEnd()
    {
        RemoveCardFromHand();

        cardManager.RemoveCardAll(cardManager.pokerDeck);
    }



    public void OnFinish()
    {
        // 2. 데이터 처리 + 점수 계산
        SettlementResult result = cardManager.Settle();
        int score = cardManager.CalculateScore(result);

        Debug.Log($"결산 점수: {score} " +
                $"(Triples:[{string.Join(",", result.tripleRanks)}], " +
                $"FourOfAKinds:[{string.Join(",", result.fourOfAKindRanks)}], " +
                $"Straights:[{string.Join(",", result.straightDetails)}])");

        // TODO: score를 실제 UI(점수판)에 반영

        // 3. 게임 종료 처리
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

        // 기존 시각 스택 정리
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
}
