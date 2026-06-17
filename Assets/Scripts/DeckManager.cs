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

    public override string ToString()
    {
        return $"{suit}_{rank}";
    }
}

public class DeckManager : MonoBehaviour
{
    public List<PokerCard> pokerDeck = new List<PokerCard>();
    public List<PokerCard> playerHand = new List<PokerCard>();

    void Start()
    {
        GeneratePokerDeck();

        ShuffleDeck(pokerDeck);

        for (int i = 0; i < 5; i++)
        {
            DrawCard(pokerDeck);
        }

        string handResult = string.Join(", ", playerHand);
        Debug.Log("현재 내 포커 손패: [ " + handResult + " ]");
        Debug.Log("남은 덱 카드 수: " + pokerDeck.Count);
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
        Debug.Log("52장의 포커 덱이 생성되었습니다.");
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
        Debug.Log("덱을 무작위로 섞었습니다.");
    }

    public void DrawCard(List<PokerCard> deck)
    {
        if (deck.Count > 0)
        {
            PokerCard drawnCard = deck[0];
            deck.RemoveAt(0);
            playerHand.Add(drawnCard);

            Debug.Log($"♣ {drawnCard.ToString()} 카드를 뽑았습니다!");
        }
        else
        {
            Debug.LogWarning("덱에 카드가 없습니다!");
        }
    }
}