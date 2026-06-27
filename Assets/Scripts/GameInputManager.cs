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
                cardUI.isAnimation = true;

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
        PokerCard drawnCard = cardManager.DrawCard(cardManager.pokerDeck);

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
        // 1. 시각적 카드 오브젝트 정리 (필드 슬롯에 떠있는 카드들 제거)
        foreach (Transform slotTransform in fieldSlotContainer)
        {
            CardSlot slot = slotTransform.GetComponent<CardSlot>();
            if (slot != null && slot.IsOccupied)
            {
                slot.ClearSlot();
            }
        }

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
    }
}
