using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class AIController : MonoBehaviour
{
    [Header("AI 전용 데이터")]
    [SerializeField] private CardManager aiCardManager;

    [Header("AI 필드 슬롯")]
    [SerializeField] private Transform aiFieldSlotContainer;

    [Header("시각 요소")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform aiDeckPanel;

    [Header("AI 타이밍")]
    [SerializeField] private float thinkingDelay = 1.0f;       // 카드 뽑은 후 ~ 배치 시작 전
    [SerializeField] private float perCardDelay = 0.4f;          // 카드 한 장 배치할 때마다 간격
    [SerializeField] private float beforeSettleDelay = 0.8f;     // 배치 끝난 후 ~ 결산 판단 전

    [Header("결산 랜덤 가중치")]
    [SerializeField] private float baseSettleChance = 0.2f;
    [SerializeField] private float fullFieldBonus = 0.3f;
    [SerializeField] private float emptyHandPenalty = 0.15f;
    [SerializeField] private float longTurnBonus = 0.4f;
    [SerializeField] private int longTurnThreshold = 5;

    private int turnsSinceLastSettle = 0;

    public void TakeTurn()
    {
        StartCoroutine(TakeTurnRoutine());
    }

    private IEnumerator TakeTurnRoutine()
    {
        turnsSinceLastSettle++;

        // 1. 카드 5장 뽑기
        for (int i = 0; i < 5; i++)
        {
            if (aiCardManager.pokerDeck.Count == 0) break;
            aiCardManager.DrawCard(aiCardManager.pokerDeck);
        }

        yield return new WaitForSeconds(thinkingDelay);   // ★ 0.3f → thinkingDelay로 변경

        // 2. 빈 슬롯 확인, 필드+손패 합쳐서 최적 조합 탐색
        List<CardSlot> emptySlots = GetEmptyAISlots();
        List<PokerCardData> aiHand = new List<PokerCardData>(aiCardManager.playerHand);
        int maxPlay = Mathf.Min(emptySlots.Count, aiHand.Count);

        List<PokerCardData> bestPlay = FindBestCombination(aiHand, maxPlay);

        // 3. 배치 애니메이션
        int slotIndex = 0;
        foreach (var card in bestPlay)
        {
            CardSlot slot = emptySlots[slotIndex];
            slotIndex++;

            GameObject cardObj = Instantiate(cardPrefab, slot.transform);
            cardObj.transform.position = aiDeckPanel.position;
            cardObj.transform.localScale = Vector3.one;

            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null) cardUI.SetPokerData(card);

            CardDragManager dragManager = cardObj.GetComponent<CardDragManager>();
            if (dragManager != null) Destroy(dragManager);

            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Sequence seq = DOTween.Sequence();
            seq.Append(cardObj.transform.DOMove(slot.transform.position, 0.3f));
            seq.AppendCallback(() =>
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localRotation = Quaternion.identity;
            });

            slot.SetCard(cardObj);
            aiCardManager.MoveCard(card, aiCardManager.fieldList);

            yield return new WaitForSeconds(perCardDelay);   // ★ 0.15f → perCardDelay로 변경
        }

        Debug.Log($"AI가 {bestPlay.Count}장 배치함: {string.Join(", ", bestPlay)}");

        // 4. 안 쓴 손패 전부 덱으로 반납
        DiscardAIHand();

        // 5. 결산할지 랜덤 판단
        yield return new WaitForSeconds(beforeSettleDelay);   // ★ 0.3f → beforeSettleDelay로 변경
        TryRandomSettle(emptySlots.Count - bestPlay.Count);
    }

    // ===== ★ 수정 2: GetComponentsInChildren로 계층 깊이 무관하게 안전하게 조회 =====
    private List<CardSlot> GetEmptyAISlots()
    {
        List<CardSlot> result = new List<CardSlot>();
        CardSlot[] allSlots = aiFieldSlotContainer.GetComponentsInChildren<CardSlot>();
        foreach (var slot in allSlots)
        {
            if (!slot.IsOccupied && slot.owner == SlotOwner.Enemy)
                result.Add(slot);
        }
        return result;
    }

    private void DiscardAIHand()
    {
        aiCardManager.RemoveCardAll(aiCardManager.pokerDeck);
    }

    private void TryRandomSettle(int remainingEmptySlots)
    {
        float chance = baseSettleChance;

        bool fieldNearlyFull = remainingEmptySlots <= 1;
        bool fieldHasNothing = aiCardManager.fieldList.Count == 0;

        if (fieldNearlyFull) chance += fullFieldBonus;
        if (fieldHasNothing) chance -= emptyHandPenalty;
        if (turnsSinceLastSettle >= longTurnThreshold) chance += longTurnBonus;

        chance = Mathf.Clamp01(chance);

        bool willSettle = Random.value < chance;
        Debug.Log($"AI 결산 판단: 확률={chance:F2}, 결과={willSettle}");

        if (willSettle)
            DoSettle();
    }

    private void DoSettle()
    {
        SettlementResult result = aiCardManager.Settle();
        int score = aiCardManager.CalculateScore(result);

        Debug.Log($"AI 결산 점수: {score} " +
                  $"(Triples:[{string.Join(",", result.tripleRanks)}], " +
                  $"FourOfAKinds:[{string.Join(",", result.fourOfAKindRanks)}], " +
                  $"Straights:[{string.Join(",", result.straightDetails)}])");

        // TODO: HP 시스템 생기면 여기서 플레이어한테 데미지 적용
        // 예: playerHP.TakeDamage(score);

        // ===== ★ 수정 2: 여기도 GetComponentsInChildren로 안전하게 =====
        CardSlot[] allSlots = aiFieldSlotContainer.GetComponentsInChildren<CardSlot>();
        foreach (var slot in allSlots)
        {
            if (!slot.IsOccupied) continue;

            GameObject fieldCard = slot.CurrentCardObject;
            CardUI cardUI = fieldCard.GetComponent<CardUI>();
            bool wasUsed = cardUI != null && result.usedCards.Contains(cardUI.pokerCardData);

            Vector3 targetPos = aiDeckPanel.position;   // TODO: AI 무덤 위치 생기면 wasUsed로 분기

            Sequence seq = DOTween.Sequence();
            seq.Append(fieldCard.transform.DOMove(targetPos, 0.3f));
            seq.Join(fieldCard.transform.DOScale(Vector3.zero, 0.3f));
            seq.AppendCallback(() => Destroy(fieldCard));

            slot.ClearSlot();
        }

        turnsSinceLastSettle = 0;
    }

    // ===== ★ 수정 1: 필드에 이미 있는 카드 + 후보 조합을 합쳐서 평가 =====
    private List<PokerCardData> FindBestCombination(List<PokerCardData> aiHand, int maxCards)
    {
        List<PokerCardData> bestCombo = new List<PokerCardData>();
        int bestScore = -1;
        int n = aiHand.Count;

        for (int mask = 1; mask < (1 << n); mask++)
        {
            List<PokerCardData> combo = new List<PokerCardData>();
            for (int i = 0; i < n; i++)
                if ((mask & (1 << i)) != 0) combo.Add(aiHand[i]);

            if (combo.Count > maxCards) continue;

            // 필드에 이미 있는 카드 + 이번 후보 조합을 합쳐서 평가
            List<PokerCardData> testPool = new List<PokerCardData>(aiCardManager.fieldList);
            testPool.AddRange(combo);

            SettlementResult result = ScoreEvaluator.EvaluateAll(testPool);
            int score = aiCardManager.CalculateScore(result);

            if (score > bestScore)
            {
                bestScore = score;
                bestCombo = combo;
            }
        }

        return bestCombo;
    }
}