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
    [SerializeField] private Transform aiHandPanel;
    [SerializeField] private Transform aiGraveyardPanel;
    [SerializeField] private HandLayoutManager aiHandLayoutManager;

    private List<GameObject> aiHandObjects = new List<GameObject>();
    private List<GameObject> aiGraveVisualStack = new List<GameObject>();

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

        // 1. 카드 5장 뽑기 + 손패 UI 생성 (뒷면)
        for (int i = 0; i < 5; i++)
        {
            if (aiCardManager.pokerDeck.Count == 0) break;
            PokerCardData drawn = aiCardManager.DrawCard(aiCardManager.pokerDeck);
            yield return StartCoroutine(DrawCardToHand(drawn, delayScale));
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

            // 손패에서 해당 카드 오브젝트 찾기
            GameObject cardObj = FindHandCardObject(card);
            if (cardObj == null) continue;

            aiHandObjects.Remove(cardObj);

            // 부모를 슬롯으로 먼저 변경해야 UpdateHandLayout이 이 카드 트윈을 건드리지 않음
            cardObj.transform.SetParent(slot.transform, true);
            UpdateHandLayout();

            // 앞면으로 전환 후 슬롯으로 직행
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.FlipCard(true);
                cardUI.useAnimation = false; // AI 필드 카드 호버 비활성화
            }
            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            yield return cardObj.transform.DOMove(slot.transform.position, 0.3f * delayScale).WaitForCompletion();
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.identity;

            slot.SetCard(cardObj);
            aiCardManager.MoveCard(card, aiCardManager.fieldList);

            yield return new WaitForSeconds(perCardDelay * delayScale);
        }

        Debug.Log($"AI placed {placedCount} cards (Remaining cost: {aiCardManager.GetAICurrentCost()}/{aiCardManager.GetMaxCost()})");

        // 4. 안 쓴 손패 전부 덱으로 반납
        yield return StartCoroutine(DiscardAIHand(delayScale));

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

    // AI 손패를 덱으로 반납 + 손패 UI 제거
    private IEnumerator DiscardAIHand(float delayScale)
    {
        List<GameObject> toDiscard = new List<GameObject>(aiHandObjects);
        aiHandObjects.Clear();

        foreach (var obj in toDiscard)
        {
            if (obj == null) continue;
            CardUI cardUI = obj.GetComponent<CardUI>();
            if (cardUI != null) cardUI.useAnimation = false;

            Sequence seq = DOTween.Sequence();
            seq.Append(obj.transform.DOMove(aiDeckPanel.position, 0.2f).SetEase(Ease.InCubic));
            seq.Join(obj.transform.DOScale(Vector3.zero, 0.2f));
            seq.AppendCallback(() => Destroy(obj));

            yield return new WaitForSeconds(0.05f * delayScale);
        }

        aiCardManager.RemoveCardAll(aiCardManager.pokerDeck);
        UpdateAIGraveVisual();
    }


    // 덱 → 손패 애니메이션 (뒷면)
    private IEnumerator DrawCardToHand(PokerCardData card, float delayScale)
    {
        if (aiHandPanel == null) yield break;

        GameObject cardObj = Instantiate(cardPrefab, aiHandPanel);

        CardUI cardUI = cardObj.GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.SetPokerData(card);
            cardUI.FlipCard(false); // 뒷면
        }

        CardDragManager drag = cardObj.GetComponent<CardDragManager>();
        if (drag != null) Destroy(drag);

        // 플레이어와 동일하게 덱 위치에서 시작
        cardObj.transform.position = aiDeckPanel.position;
        cardObj.transform.localScale = Vector3.one;

        aiHandObjects.Add(cardObj);

        // 손패 부채꼴 위치로 이동
        UpdateHandLayout();

        yield return new WaitForSeconds(0.1f * delayScale);
    }

    // 카드 앞면 뒤집기 (스케일로 플립 효과)
    private IEnumerator FlipToFront(CardUI cardUI, float duration)
    {
        Transform t = cardUI.transform;
        yield return t.DOScaleX(0f, duration * 0.5f).WaitForCompletion();
        cardUI.FlipCard(true);
        yield return t.DOScaleX(1f, duration * 0.5f).WaitForCompletion();
    }

    public void UpdateAIGraveVisual()
    {
        int actualCount = aiCardManager.graveList.Count;
        int visualCount = actualCount == 0 ? 0 : (actualCount / 2) + 1;

        foreach (var obj in aiGraveVisualStack)
            Destroy(obj);
        aiGraveVisualStack.Clear();

        if (aiGraveyardPanel == null) return;

        for (int i = 0; i < visualCount; i++)
        {
            GameObject card = Instantiate(cardPrefab, aiGraveyardPanel);
            RectTransform rt = card.GetComponent<RectTransform>();

            rt.localRotation = i == 0
                ? Quaternion.identity
                : Quaternion.Euler(0, 0, UnityEngine.Random.Range(-5f, 5f));
            rt.localScale = Vector3.one;

            CardUI cardUI = card.GetComponent<CardUI>();
            if (cardUI != null) cardUI.useAnimation = false;

            CardDragManager drag = card.GetComponent<CardDragManager>();
            if (drag != null) Destroy(drag);

            aiGraveVisualStack.Add(card);
        }
    }

    // 손패에서 PokerCardData로 오브젝트 찾기
    private GameObject FindHandCardObject(PokerCardData card)
    {
        foreach (var obj in aiHandObjects)
        {
            if (obj == null) continue;
            CardUI ui = obj.GetComponent<CardUI>();
            if (ui != null && ui.pokerCardData.suit == card.suit && ui.pokerCardData.rank == card.rank)
                return obj;
        }
        return null;
    }

    private void UpdateHandLayout()
    {
        if (aiHandLayoutManager != null)
            aiHandLayoutManager.UpdateLayout();
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