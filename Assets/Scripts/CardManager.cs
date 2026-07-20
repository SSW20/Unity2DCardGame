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

    public float CalculateScore(SettlementResult result, int emptySlotCount)
    {
        float tripleAndStraightScore = 0f;

        // 보유 특전에 따라 점수식의 계수를 결정한다.
        // ===== 트리플 / 포카드 점수 =====
        foreach (var triple in result.triples)
            tripleAndStraightScore += (GetRankCost(triple) + 15f) * 4f;

        foreach (var fourOfAKind in result.fourOfAKinds)
            tripleAndStraightScore += (GetRankCost(fourOfAKind) + 10f) * 8f;

        // ===== 스트레이트 점수 =====
        foreach (var straight in result.straights)
        {
            int cardCount = straight.Count;
            float costStr = 0f;

            foreach (var card in straight)
            {
                if (!card.IsJoker)
                    costStr += (int)card.rank;
            }

            float strScore = costStr * GetStraightMultiplier(cardCount);

            // 스트레이트 강화:
            // 카드가 N장이면 1.2^N 배율을 적용한다.
            if (HasPerk(PerkType.StraightBoost))
                strScore *= Mathf.Pow(1.2f, cardCount);

            tripleAndStraightScore += strScore;
        }

        // 고득점 보너스:
        // 트리플 + 스트레이트 점수의 합이 100 이상일 때만 10% 증가한다.
        if (HasPerk(PerkType.HighScoreBonus)
            && tripleAndStraightScore >= 100f)
        {
            tripleAndStraightScore *= 1.1f;
        }

        float total = tripleAndStraightScore;

        // 무덤 카드 보너스:
        // 기존 무덤 카드가 아니라 이번 결산에서 새로 무덤으로 간 카드만 계산한다.
        if (HasPerk(PerkType.GraveCardBonus))
            total += result.newGraveCardCount * 20f;

        return total;
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
