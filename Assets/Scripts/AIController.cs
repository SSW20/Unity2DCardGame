using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
public class AIController : MonoBehaviour
{
    [Header("AI Data")]
    [SerializeField] private CardManager aiCardManager;

    [Header("AI Field Slots")]
    [SerializeField] private Transform aiFieldSlotContainer;

    [Header("Visual")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform aiDeckPanel;

    [Header("AI Timing")]
    [SerializeField] private float thinkingDelay = 1.0f;
    [SerializeField] private float perCardDelay = 0.4f;
    [SerializeField] private float beforeSettleDelay = 0.8f;

    //배속값 조정
    [Header("Fast Mode")]
    [SerializeField] private float fastModeDelayScale = 0.5f;

    [Header("Settlement Random Weight")]
    [SerializeField] private float baseSettleChance = 0.2f;
    [SerializeField] private float fullFieldBonus = 0.3f;
    [SerializeField] private float emptyHandPenalty = 0.15f;
    [SerializeField] private float longTurnBonus = 0.4f;
    [SerializeField] private int longTurnThreshold = 5;

    private int turnsSinceLastSettle = 0;

    public void TakeTurn(Action<bool> onFinished, bool fastMode = false)
    {
        StartCoroutine(TakeTurnRoutine(onFinished, fastMode));
    }

    public void TakeTurn()
    {
        TakeTurn(null, false);
    }

    private IEnumerator TakeTurnRoutine(Action<bool> onFinished, bool fastMode)
    {
        float delayScale = fastMode ? fastModeDelayScale : 1f;
        turnsSinceLastSettle++;

        // AI 코스트 초기화
        aiCardManager.ResetAICost();

        // 1. 카드 5장 뽑기
        for (int i = 0; i < 5; i++)
        {
            if (aiCardManager.pokerDeck.Count == 0) break;
            aiCardManager.DrawCard(aiCardManager.pokerDeck);
        }

        yield return new WaitForSeconds(thinkingDelay * delayScale);

        // 2. 빈 슬롯 확인, 필드+손패 합쳐서 최적 조합 탐색 (코스트 고려)
        List<CardSlot> emptySlots = GetEmptyAISlots();
        List<PokerCardData> aiHand = new List<PokerCardData>(aiCardManager.playerHand);
        int maxPlay = Mathf.Min(emptySlots.Count, aiHand.Count);

        List<PokerCardData> bestPlay = FindBestCombination(aiHand, maxPlay);

        // 3. 배치 애니메이션
        int slotIndex = 0;
        int placedCount = 0;

        foreach (var card in bestPlay)
        {
            // AI 코스트 체크
            if (!aiCardManager.CanAIAffordCard(card))
            {
                Debug.Log($"AI: Not enough cost to place card {card.rank}");
                continue;
            }

            // AI 코스트 소비
            if (!aiCardManager.SpendAICost(card))
            {
                continue;
            }

            CardSlot slot = emptySlots[slotIndex];
            slotIndex++;
            placedCount++;

            GameObject cardObj = Instantiate(cardPrefab, slot.transform);
            cardObj.transform.position = aiDeckPanel.position;
            cardObj.transform.localScale = Vector3.one;

            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.SetPokerData(card);
                cardUI.FlipCard(true); // 카드 앞면 표시
            }

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

            yield return new WaitForSeconds(perCardDelay * delayScale);
        }

        Debug.Log($"AI placed {placedCount} cards (Remaining cost: {aiCardManager.GetAICurrentCost()}/{aiCardManager.GetMaxCost()})");
        // 4. 안 쓴 손패 전부 덱으로 반납
        DiscardAIHand();

        // 5. AI가 Turn End 할지 Stop 할지 결정
        yield return new WaitForSeconds(beforeSettleDelay * delayScale);

        int remainingEmptySlots = emptySlots.Count - placedCount;
        bool aiChoseStop = DecideStopOrTurnEnd(remainingEmptySlots);

        onFinished?.Invoke(aiChoseStop);
    }

    // 비어있는 AI 슬롯 찾기
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

    // AI 손패를 덱으로 반납
    private void DiscardAIHand()
    {
        aiCardManager.RemoveCardAll(aiCardManager.pokerDeck);
    }

    private bool DecideStopOrTurnEnd(int remainingEmptySlots)
    {
        float chance = baseSettleChance;

        bool fieldNearlyFull = remainingEmptySlots <= 1;
        bool fieldHasNothing = aiCardManager.fieldList.Count == 0;

        if (fieldNearlyFull) chance += fullFieldBonus;
        if (fieldHasNothing) chance -= emptyHandPenalty;
        if (turnsSinceLastSettle >= longTurnThreshold) chance += longTurnBonus;

        chance = Mathf.Clamp01(chance);

        bool willStop = UnityEngine.Random.value < chance;

        Debug.Log($"AI stop check: chance={chance:F2}, stop={willStop}");

        if (willStop)
            turnsSinceLastSettle = 0;

        return willStop;
    }

    public void ResetRoundState()
    {
        turnsSinceLastSettle = 0;
    }

    // 결산 확률 계산 및 판단
    /*private void TryRandomSettle(int remainingEmptySlots)
    {
        float chance = baseSettleChance;

        bool fieldNearlyFull = remainingEmptySlots <= 1;
        bool fieldHasNothing = aiCardManager.fieldList.Count == 0;

        if (fieldNearlyFull) chance += fullFieldBonus;
        if (fieldHasNothing) chance -= emptyHandPenalty;
        if (turnsSinceLastSettle >= longTurnThreshold) chance += longTurnBonus;

        chance = Mathf.Clamp01(chance);

        bool willSettle = Random.value < chance;
        Debug.Log($"AI settlement check: chance={chance:F2}, result={willSettle}");

        if (willSettle)
            DoSettle();
    }

    // 결산 수행
    private void DoSettle()
    {
        SettlementResult result = aiCardManager.Settle();

        int blankSlots = GetEmptyAISlots().Count;
        float score = aiCardManager.CalculateScore(result, blankSlots);

        Debug.Log($"AI settlement score: {score} ");

        // TODO: HP 시스템 생기면 여기서 플레이어한테 데미지 적용

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
    }*/

    // 필드에 이미 있는 카드 + 후보 조합을 합쳐서 평가 (코스트 고려)
    private List<PokerCardData> FindBestCombination(List<PokerCardData> aiHand, int maxCards)
    {
        List<PokerCardData> bestCombo = new List<PokerCardData>();
        float bestScore = -1;
        int n = aiHand.Count;
        int aiCurrentCost = aiCardManager.GetAICurrentCost();
        int maxAICost = aiCardManager.GetMaxCost();

        for (int mask = 1; mask < (1 << n); mask++)
        {
            List<PokerCardData> combo = new List<PokerCardData>();
            int totalCost = 0;

            for (int i = 0; i < n; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    combo.Add(aiHand[i]);
                    totalCost += (int)aiHand[i].rank;
                }
            }

            // 코스트 제약 체크
            if (combo.Count > maxCards) continue;
            if (totalCost > aiCurrentCost) continue;

            // 필드에 이미 있는 카드 + 이번 후보 조합을 합쳐서 평가
            List<PokerCardData> testPool = new List<PokerCardData>(aiCardManager.fieldList);
            testPool.AddRange(combo);

            SettlementResult result = ScoreEvaluator.EvaluateAll(testPool);
            int blankSlots = GetEmptyAISlots().Count - combo.Count; 
            float score = aiCardManager.CalculateScore(result, blankSlots);

            if (score > bestScore)
            {
                bestScore = score;
                bestCombo = combo;
            }
        }

        return bestCombo;
    }
}