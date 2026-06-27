using System.Collections.Generic;
using UnityEngine;

public enum CardSuit { Spade, Heart, Diamond, Club }

public enum CardRank { Ace = 1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }

[System.Serializable]
public struct PokerCard
{
    public CardSuit suit;
    public CardRank rank;

    public PokerCard(CardSuit suit, CardRank rank)
    {
        this.suit = suit;
        this.rank = rank;
    }
}

public class CardManager : MonoBehaviour
{
    public List<PokerCard> pokerDeck = new List<PokerCard>();
    public List<PokerCard> playerHand = new List<PokerCard>();

    public List<PokerCard> fieldList = new List<PokerCard>();  
    public List<PokerCard> graveList = new List<PokerCard>();   
    public List<PokerCard> specialList = new List<PokerCard>();  
    void Awake()
    {
        GeneratePokerDeck();
        ShuffleDeck(pokerDeck);
    }

    void GeneratePokerDeck()
    {
        pokerDeck.Clear();

        for (int s = 0; s < 4; s++)
        {
            for (int r = 1; r <= 13; r++)
            {
                PokerCard newCard = new PokerCard((CardSuit)s, (CardRank)r);
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

    public PokerCard DrawCard(List<PokerCard> deck)
    {
        // 덱에서 카드가 부족할 일은 없음
        PokerCard drawnCard = deck[0];
        deck.RemoveAt(0);
        playerHand.Add(drawnCard);
        return drawnCard;
    }

    public void RemoveCardAll(List<PokerCard> deck)
    {
        // 덱에서 카드가 부족할 일은 없음
        List<PokerCard> tempPlayerHand = new List<PokerCard>();
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

    public bool MoveCard(PokerCard card, List<PokerCard> destination)
    {
        if (pokerDeck.Remove(card))   { destination.Add(card); return true; }
        if (playerHand.Remove(card))  { destination.Add(card); return true; }
        if (fieldList.Remove(card))   { destination.Add(card); return true; }
        if (graveList.Remove(card))   { destination.Add(card); return true; }

        return false;
    }

    public SettlementResult Settle()
    {
        List<PokerCard> pool = new List<PokerCard>();
        pool.AddRange(fieldList);
        pool.AddRange(graveList);

        SettlementResult result = ScoreEvaluator.EvaluateAll(pool);

        // 1. 기존 무덤 → 무조건 덱으로
        // TODO: 애니메이션 
        pokerDeck.AddRange(graveList);
        graveList.Clear();

        // 2. 기존 필드 → 점수에 참여했으면 덱, 안 했으면 새 무덤 
        // TODO: 애니메이션 
        List<PokerCard> fieldSnapshot = new List<PokerCard>(fieldList);
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