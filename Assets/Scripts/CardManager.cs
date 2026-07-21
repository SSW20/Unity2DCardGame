using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CardSuit { Spade, Heart, Diamond, Club }

public enum CardKind
{
    Normal,
    Joker
}

public enum CardRank { Ace = 1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }

[System.Serializable]
public struct PokerCardData
{
    public const int JokerCost = 10;

    public CardKind kind;
    public CardSuit suit;
    public CardRank rank;
    public Sprite sprite;

    public bool IsJoker => kind == CardKind.Joker;
    public int Cost => IsJoker ? JokerCost : (int)rank;

    public PokerCardData(CardSuit suit, CardRank rank, Sprite sprite)
    {
        kind = CardKind.Normal;
        this.suit = suit;
        this.rank = rank;
        this.sprite = sprite;
    }

    public static PokerCardData CreateJoker(Sprite sprite)
    {
        return new PokerCardData
        {
            kind = CardKind.Joker,
            // 조커는 족보 계산에서 제외되므로 suit/rank 값은 사용하지 않는다.
            suit = CardSuit.Spade,
            rank = CardRank.Ace,
            sprite = sprite
        };
    }
}

public class CardManager : MonoBehaviour
{
    public const int MaxPerkCount = 3;

    [Header("코스트 시스템")]
    [SerializeField] private int maxCostPerTurn = 21;

    [Header("조커 카드")]
    [Tooltip("플레이어 CardManager에서만 켜세요. AI에 조커를 추가할 때는 AI CardManager에서도 켤 수 있습니다.")]
    [SerializeField] private bool includeJokersInDeck = false;
    [SerializeField, Min(0)] private int jokerCount = 5;
    [SerializeField] private Sprite jokerSprite;

    [Header("특전 시스템")]
    [SerializeField] private List<PerkType> ownedPerks = new List<PerkType>();

    [Header("특전 밸런스 - Inspector에서 수정")]
    [Tooltip("실전압축 슬롯: 빈 슬롯 1칸당 추가 점수")]
    [SerializeField, Min(0f)] private float compressedSlotPointsPerEmptySlot = 10f;

    [Tooltip("파묘: 이번 결산에서 무덤으로 간 카드 1장당 추가 점수")]
    [SerializeField, Min(0f)] private float graveRobbingPointsPerCard = 10f;

    [Tooltip("같은 숫자 수집가: 트리플 코스트 보정값")]
    [SerializeField] private float collectorTripleCostCorrection = 25f;
    [Tooltip("같은 숫자 수집가: 트리플 족보 배율")]
    [SerializeField, Min(0f)] private float collectorTripleMultiplier = 4f;
    [Tooltip("같은 숫자 수집가: 포카드 코스트 보정값")]
    [SerializeField] private float collectorFourCostCorrection = 25f;
    [Tooltip("같은 숫자 수집가: 포카드 족보 배율")]
    [SerializeField, Min(0f)] private float collectorFourMultiplier = 8f;

    [Tooltip("공세: 상대보다 먼저 STOP했을 때 추가 점수")]
    [SerializeField, Min(0f)] private float offensiveStopBonus = 100f;

    [Tooltip("연속의 달인: 저코스트 스트레이트로 판정하는 평균 상한")]
    [SerializeField] private float straightMasterLowAverageMax = 6f;
    [Tooltip("연속의 달인: 고코스트 스트레이트로 판정하는 평균 하한")]
    [SerializeField] private float straightMasterHighAverageMin = 7f;
    [Tooltip("연속의 달인: 평균이 낮을 때 4연속 배율")]
    [SerializeField, Min(0f)] private float straightMasterFourMultiplier = 4f;
    [Tooltip("연속의 달인: 평균이 낮을 때 5연속 배율")]
    [SerializeField, Min(0f)] private float straightMasterFiveMultiplier = 5f;
    [Tooltip("연속의 달인: 평균이 낮을 때 6연속 배율")]
    [SerializeField, Min(0f)] private float straightMasterSixMultiplier = 6f;
    [Tooltip("연속의 달인: 평균이 높을 때 코스트 합에 더하는 보정값")]
    [SerializeField] private float straightMasterHighCostCorrection = 20f;

    public IReadOnlyList<PerkType> OwnedPerks => ownedPerks;

    private int playerCurrentCost;
    private int aiCurrentCost;

    public List<PokerCardData> pokerDeck = new List<PokerCardData>();
    public List<PokerCardData> playerHand = new List<PokerCardData>();

    public List<PokerCardData> fieldList = new List<PokerCardData>();
    public List<PokerCardData> graveList = new List<PokerCardData>();
    public List<PokerCardData> specialList = new List<PokerCardData>();

    [SerializeField] private TextMeshProUGUI playerCostText;

    public CardImageData cardImageData;

    public Sprite JokerSprite => jokerSprite;
    public bool IncludesJokersInDeck => includeJokersInDeck;
    public int JokerCount => jokerCount;

    void Awake()
    {
        GeneratePokerDeck();
        ShuffleDeck(pokerDeck);

        playerCurrentCost = maxCostPerTurn;
        aiCurrentCost = maxCostPerTurn;
    }

    void GeneratePokerDeck()
    {
        pokerDeck.Clear();

        for (int s = 0; s < 4; s++)
        {
            for (int r = 1; r <= 13; r++)
            {
                PokerCardData newCard = new PokerCardData(
                    (CardSuit)s,
                    (CardRank)r,
                    cardImageData.GetSprite((CardSuit)s, (CardRank)r));
                pokerDeck.Add(newCard);
            }
        }

        if (includeJokersInDeck)
        {
            if (jokerSprite == null)
                Debug.LogWarning($"{name}: Joker Sprite가 연결되지 않았습니다.");

            for (int i = 0; i < jokerCount; i++)
                pokerDeck.Add(PokerCardData.CreateJoker(jokerSprite));
        }
    }

    public void ShuffleDeck<T>(List<T> deck)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    public PokerCardData DrawCard(List<PokerCardData> deck)
    {
        // 덱에서 카드가 부족할 일은 없음
        PokerCardData drawnCard = deck[0];
        deck.RemoveAt(0);
        playerHand.Add(drawnCard);
        return drawnCard;
    }

    public void RemoveCardAll(List<PokerCardData> deck)
    {
        // 덱에서 카드가 부족할 일은 없음
        List<PokerCardData> tempPlayerHand = new List<PokerCardData>();
        foreach (var card in playerHand)
        {
            tempPlayerHand.Add(card);
        }

        playerHand.Clear();
        foreach (var card in tempPlayerHand)
        {
            pokerDeck.Add(card);
        }

        ShuffleDeck(pokerDeck);
    }

    // ===== 코스트 시스템 =====

    public void ResetPlayerCost()
    {
        playerCurrentCost = maxCostPerTurn;
        Debug.Log($"플레이어 코스트 초기화: {playerCurrentCost}/{maxCostPerTurn}");
        if (playerCostText != null)
        {
            playerCostText.text = playerCurrentCost.ToString();
        }
    }

    public void ResetAICost()
    {
        aiCurrentCost = maxCostPerTurn;
        Debug.Log($"AI 코스트 초기화: {aiCurrentCost}/{maxCostPerTurn}");
    }

    public bool CanPlayerAffordCard(PokerCardData card)
    {
        int cardCost = card.Cost;
        return playerCurrentCost >= cardCost;
    }

    public bool CanAIAffordCard(PokerCardData card)
    {
        int cardCost = card.Cost;
        return aiCurrentCost >= cardCost;
    }

    public bool SpendPlayerCost(PokerCardData card)
    {
        int cardCost = card.Cost;
        if (playerCurrentCost >= cardCost)
        {
            playerCurrentCost -= cardCost;
            if (playerCostText != null)
                playerCostText.text = playerCurrentCost.ToString();
            return true;
        }
        Debug.Log($"코스트 부족! 필요: {cardCost}, 보유: {playerCurrentCost}");
        return false;
    }

    public bool SpendAICost(PokerCardData card)
    {
        int cardCost = card.Cost;
        if (aiCurrentCost >= cardCost)
        {
            aiCurrentCost -= cardCost;
            Debug.Log($"AI card (cost {cardCost}): {aiCurrentCost}/{maxCostPerTurn}");
            return true;
        }
        return false;
    }

    public int GetPlayerCurrentCost()
    {
        return playerCurrentCost;
    }

    public int GetAICurrentCost()
    {
        return aiCurrentCost;
    }

    public int GetMaxCost()
    {
        return maxCostPerTurn;
    }

    // ===== 특전 시스템 =====

    public bool HasPerk(PerkType perk)
    {
        return ownedPerks.Contains(perk);
    }

    public bool TryAddPerk(PerkType perk)
    {
        if (ownedPerks.Contains(perk))
            return false;

        if (ownedPerks.Count >= MaxPerkCount)
            return false;

        ownedPerks.Add(perk);
        return true;
    }

    public void ResetPerks()
    {
        ownedPerks.Clear();
    }

    public bool MoveCard(
        PokerCardData card,
        List<PokerCardData> source,
        List<PokerCardData> destination)
    {
        if (source == null || destination == null || ReferenceEquals(source, destination))
            return false;

        int cardIndex = source.IndexOf(card);
        if (cardIndex < 0)
            return false;

        PokerCardData movedCard = source[cardIndex];
        source.RemoveAt(cardIndex);
        destination.Add(movedCard);
        return true;
    }

    // 기존 호출과의 호환용이다. 동일한 조커가 여러 장이므로 새 코드에서는
    // 출발 목록을 명시하는 MoveCard(card, source, destination)를 사용한다.
    public bool MoveCard(PokerCardData card, List<PokerCardData> destination)
    {
        if (MoveCard(card, playerHand, destination)) return true;
        if (MoveCard(card, fieldList, destination)) return true;
        if (MoveCard(card, graveList, destination)) return true;
        if (MoveCard(card, pokerDeck, destination)) return true;
        return false;
    }

    public bool HasJokerInHand()
    {
        foreach (PokerCardData card in playerHand)
        {
            if (card.IsJoker)
                return true;
        }

        return false;
    }

    public int CountJokersInHand()
    {
        int count = 0;
        foreach (PokerCardData card in playerHand)
        {
            if (card.IsJoker)
                count++;
        }

        return count;
    }

    public SettlementResult Settle()
    {
        List<PokerCardData> pool = new List<PokerCardData>();
        pool.AddRange(fieldList);
        pool.AddRange(graveList);

        SettlementResult result = ScoreEvaluator.EvaluateAll(pool);

        // 1. 기존 무덤 → 무조건 덱으로
        // TODO: 애니메이션 
        pokerDeck.AddRange(graveList);
        graveList.Clear();

        // 2. 기존 필드 → 점수에 참여했으면 덱, 안 했으면 새 무덤 
        // TODO: 애니메이션 
        List<PokerCardData> fieldSnapshot = new List<PokerCardData>(fieldList);
        fieldList.Clear();

        result.newGraveCardCount = 0;

        foreach (var card in fieldSnapshot)
        {
            // 조커는 족보와 점수에 참여하지 않으며, 결산 후 항상 덱으로 돌아간다.
            if (card.IsJoker)
            {
                pokerDeck.Add(card);
            }
            else if (result.usedCards.Contains(card))
            {
                pokerDeck.Add(card);
            }
            else
            {
                graveList.Add(card);
                result.newGraveCardCount++;
            }
        }

        ShuffleDeck(pokerDeck);

        return result;
    }

    public float CalculateScore(
        SettlementResult result,
        int emptySlotCount,
        bool stoppedBeforeOpponent = false)
    {
        float total = 0f;
        bool hasCollector = HasPerk(PerkType.TripleCostBoost);
        bool hasStraightMaster = HasPerk(PerkType.StraightBoost);

        // ===== 트리플 / 포카드 점수 =====
        foreach (List<PokerCardData> triple in result.triples)
        {
            float correction = hasCollector ? collectorTripleCostCorrection : 15f;
            float multiplier = hasCollector ? collectorTripleMultiplier : 4f;
            total += (GetRankCost(triple) + correction) * multiplier;
        }

        foreach (List<PokerCardData> fourOfAKind in result.fourOfAKinds)
        {
            float correction = hasCollector ? collectorFourCostCorrection : 10f;
            float multiplier = hasCollector ? collectorFourMultiplier : 8f;
            total += (GetRankCost(fourOfAKind) + correction) * multiplier;
        }

        // ===== 스트레이트 점수 =====
        foreach (List<PokerCardData> straight in result.straights)
        {
            int cardCount = straight.Count;
            if (cardCount <= 0)
                continue;

            float cost = GetRankCost(straight);
            float multiplier = GetStraightMultiplier(cardCount);

            if (hasStraightMaster)
            {
                float averageCost = cost / cardCount;
                if (averageCost <= straightMasterLowAverageMax)
                    multiplier = GetStraightMasterLowMultiplier(cardCount);
                else if (averageCost >= straightMasterHighAverageMin)
                    cost += straightMasterHighCostCorrection;
            }

            total += cost * multiplier;
        }

        if (HasPerk(PerkType.EmptySlotBoost))
            total += Mathf.Max(0, emptySlotCount) * compressedSlotPointsPerEmptySlot;

        if (HasPerk(PerkType.GraveCardBonus))
            total += result.newGraveCardCount * graveRobbingPointsPerCard;

        if (HasPerk(PerkType.HighScoreBonus) && stoppedBeforeOpponent)
            total += offensiveStopBonus;

        return total;
    }

    public string GetPerkDescription(PerkType perk)
    {
        switch (perk)
        {
            case PerkType.EmptySlotBoost:
                return $"빈 슬롯 1칸당 {FormatRuleNumber(compressedSlotPointsPerEmptySlot)}점을 추가로 얻습니다.";
            case PerkType.GraveCardBonus:
                return $"이번 결산에서 무덤으로 간 카드 1장당 {FormatRuleNumber(graveRobbingPointsPerCard)}점을 추가로 얻습니다.";
            case PerkType.TripleCostBoost:
                return "트리플은 (코스트 합 + "
                    + FormatRuleNumber(collectorTripleCostCorrection) + ") × "
                    + FormatRuleNumber(collectorTripleMultiplier)
                    + ", 포카드는 (코스트 합 + "
                    + FormatRuleNumber(collectorFourCostCorrection) + ") × "
                    + FormatRuleNumber(collectorFourMultiplier) + "로 계산합니다.";
            case PerkType.HighScoreBonus:
                return $"상대보다 먼저 STOP 상태에 진입하면 {FormatRuleNumber(offensiveStopBonus)}점을 추가로 얻습니다.";
            case PerkType.StraightBoost:
                return "스트레이트 평균이 "
                    + FormatRuleNumber(straightMasterLowAverageMax)
                    + " 이하이면 4·5·6연속 배율을 각각 ×"
                    + FormatRuleNumber(straightMasterFourMultiplier) + "·×"
                    + FormatRuleNumber(straightMasterFiveMultiplier) + "·×"
                    + FormatRuleNumber(straightMasterSixMultiplier)
                    + "으로 적용합니다. 평균이 "
                    + FormatRuleNumber(straightMasterHighAverageMin)
                    + " 이상이면 코스트 합에 "
                    + FormatRuleNumber(straightMasterHighCostCorrection) + "을 더합니다.";
            default:
                return PerkCatalog.GetDescription(perk);
        }
    }

    private float GetStraightMasterLowMultiplier(int cardCount)
    {
        switch (cardCount)
        {
            case 4: return straightMasterFourMultiplier;
            case 5: return straightMasterFiveMultiplier;
            case 6: return straightMasterSixMultiplier;
            default: return GetStraightMultiplier(cardCount);
        }
    }

    private static string FormatRuleNumber(float value)
    {
        return value.ToString("0.##");
    }

    private static float GetRankCost(List<PokerCardData> cards)
    {
        float cost = 0f;
        foreach (PokerCardData card in cards)
        {
            if (!card.IsJoker)
                cost += (int)card.rank;
        }

        return cost;
    }

    private static float GetStraightMultiplier(int cardCount)
    {
        switch (cardCount)
        {
            case 4: return 3f;
            case 5: return 4f;
            case 6: return 5f;
            default: return cardCount >= 7 ? 8f : 0f;
        }
    }
}
