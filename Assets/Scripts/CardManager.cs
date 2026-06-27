using System.Collections.Generic;
using UnityEngine;

public enum CardSuit { Spade, Heart, Diamond, Club }

public enum CardRank { Ace = 1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }

[System.Serializable]
public struct PokerCardData
{
    public CardSuit suit;
    public CardRank rank;

    public PokerCardData(CardSuit suit, CardRank rank)
    {
        this.suit = suit;
        this.rank = rank;
    }
}

public class CardManager : MonoBehaviour
{
    [Header("코스트 시스템")]
    [SerializeField] private int maxCostPerTurn = 21;

    private int playerCurrentCost;
    private int aiCurrentCost;

    public List<PokerCardData> pokerDeck = new List<PokerCardData>();
    public List<PokerCardData> playerHand = new List<PokerCardData>();

    public List<PokerCardData> fieldList = new List<PokerCardData>();  
    public List<PokerCardData> graveList = new List<PokerCardData>();   
    public List<PokerCardData> specialList = new List<PokerCardData>();  


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
                PokerCardData newCard = new PokerCardData((CardSuit)s, (CardRank)r);
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
            Debug.Log($"플레이어 카드 배치 (코스트 {cardCost}): {playerCurrentCost}/{maxCostPerTurn}");
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

        foreach (var card in fieldSnapshot)
        {
            if (result.usedCards.Contains(card))
                pokerDeck.Add(card);
            else
                graveList.Add(card);
        }

        ShuffleDeck(pokerDeck);

        return result;
    }

    public int CalculateScore(SettlementResult result)
    {
        int score = 0;
        foreach (int rank in result.tripleRanks)      score += rank;
        foreach (int rank in result.fourOfAKindRanks) score += rank;
        foreach (var (high, count) in result.straightDetails) score += count + high;
        return score;
    }
}