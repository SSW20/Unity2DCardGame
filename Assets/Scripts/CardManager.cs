using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CardSuit { Spade, Heart, Diamond, Club }

public enum CardRank { Ace = 1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }

[System.Serializable]
public struct PokerCardData
{
    public CardSuit suit;
    public CardRank rank;

    public Sprite sprite;
    
    public PokerCardData(CardSuit suit, CardRank rank, Sprite sprite)
    {
        this.suit = suit;
        this.rank = rank;
        this.sprite = sprite;
    }
}

public class CardManager : MonoBehaviour
{
    public const int MaxPerkCount = 3;

    [Header("코스트 시스템")]
    [SerializeField] private int maxCostPerTurn = 21;

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
                PokerCardData newCard = new PokerCardData((CardSuit)s, (CardRank)r, cardImageData.GetSprite((CardSuit)s, (CardRank)r));
                pokerDeck.Add(newCard);
            }
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
        foreach(var card in playerHand)
        {
            tempPlayerHand.Add(card);
        }
        
        playerHand.Clear();
        foreach(var card in tempPlayerHand)
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
        int cardCost = (int)card.rank;
        return playerCurrentCost >= cardCost;
    }

    public bool CanAIAffordCard(PokerCardData card)
    {
        int cardCost = (int)card.rank;
        return aiCurrentCost >= cardCost;
    }

    public bool SpendPlayerCost(PokerCardData card)
    {
        int cardCost = (int)card.rank;
        if (playerCurrentCost >= cardCost)
        {
            playerCurrentCost -= cardCost;
            playerCostText.text = playerCurrentCost.ToString();
            return true;
        }
        Debug.Log($"코스트 부족! 필요: {cardCost}, 보유: {playerCurrentCost}");
        return false;
    }

    public bool SpendAICost(PokerCardData card)
    {
        int cardCost = (int)card.rank;
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

    public bool TryAddRandomPerk(out PerkType selectedPerk)
    {
        List<PerkType> candidates = new List<PerkType>();

        foreach (PerkType perk in PerkCatalog.All)
        {
            if (!ownedPerks.Contains(perk))
                candidates.Add(perk);
        }

        if (ownedPerks.Count >= MaxPerkCount || candidates.Count == 0)
        {
            selectedPerk = default(PerkType);
            return false;
        }

        int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
        selectedPerk = candidates[randomIndex];

        return TryAddPerk(selectedPerk);
    }

    public void ResetPerks()
    {
        ownedPerks.Clear();
    }

    public bool MoveCard(PokerCardData card, List<PokerCardData> destination)
    {
        if (pokerDeck.Remove(card))   { destination.Add(card); return true; }
        if (playerHand.Remove(card))  { destination.Add(card); return true; }
        if (fieldList.Remove(card))   { destination.Add(card); return true; }
        if (graveList.Remove(card))   { destination.Add(card); return true; }

        return false;
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
            if (result.usedCards.Contains(card))
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
        float tripleCostCoefficient =
            HasPerk(PerkType.TripleCostBoost) ? 0.5f : 0.1f;

        float tripleEmptySlotBonus =
            HasPerk(PerkType.EmptySlotBoost) ? 0.8f : 0.6f;

        float straightEmptySlotBonus =
            HasPerk(PerkType.EmptySlotBoost) ? 0.8f : 0.7f;

        // ===== 트리플 / 포카드 점수 =====
        int trp = 1 + result.triples.Count + result.fourOfAKinds.Count * 2;
        float trpMultiplier = trp * trp;

        float costTrp = 0f;

        foreach (var group in result.triples)
        {
            foreach (var card in group)
                costTrp += (int)card.rank;
        }

        foreach (var group in result.fourOfAKinds)
        {
            foreach (var card in group)
                costTrp += (int)card.rank;
        }

        if (result.triples.Count > 0 || result.fourOfAKinds.Count > 0)
        {
            float trpScore =
                (costTrp * tripleCostCoefficient + 15f)
                * trpMultiplier
                * ((emptySlotCount + 1) * tripleEmptySlotBonus);

            tripleAndStraightScore += trpScore;
        }

        // ===== 스트레이트 점수 =====
        foreach (var straight in result.straights)
        {
            int cardCount = straight.Count;
            float str = cardCount;

            float costStr = 0f;

            foreach (var card in straight)
                costStr += (int)card.rank;

            float strScore =
                (costStr * 0.6f * str)
                * ((emptySlotCount + 1) * straightEmptySlotBonus);

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
}
