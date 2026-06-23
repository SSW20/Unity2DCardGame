using System.Collections.Generic;
using UnityEngine;

// 카드 52장 만들기
[System.Serializable]
public class PokerCard
{
    public string suit; // 문양 (Spade, Heart, Diamond, Club)
    public string rank; // 숫자 (Ace, 2~10, Jack, Queen, King)

    public PokerCard(string suit, string rank)
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
    public int currentTurn = 1; // 현재 턴 번호

    void Start()
    {
        // 시작 시 덱 만들기/ 5장 뽑기(1턴)
        GeneratePokerDeck();
        ShuffleDeck(pokerDeck);

        for (int i = 0; i < 5; i++)
        {
            DrawCard();
        }

        Debug.Log($"=== ☀️ 턴 {currentTurn} 시작 ===");
        PrintCurrentStatus();
    }

    // 포커 52장 생성
    public void GeneratePokerDeck()
    {
        pokerDeck.Clear();
        string[] suits = { "Spade", "Heart", "Diamond", "Club" };
        string[] ranks = { "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King" };

        foreach (string suit in suits)
        {
            foreach (string rank in ranks)
            {
                pokerDeck.Add(new PokerCard(suit, rank));
            }
        }
    }

    // 셔플
    public void ShuffleDeck(List<PokerCard> deck)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            PokerCard temp = deck[i];
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    // 덱에 다시 돌려놓기
    public void EndTurn()
    {
        Debug.Log($"=== 🌙 턴 {currentTurn} 종료 ===");

      
        if (playerHand.Count > 0)
        {
            pokerDeck.AddRange(playerHand);
            playerHand.Clear();           
            Debug.Log("🔄 손에 남은 카드를 모두 덱으로 다시 돌려보냈습니다.");
        }

        // 덱 다시 섞기
        ShuffleDeck(pokerDeck);
        Debug.Log("🎲 덱을 새로 한 번 더 섞었습니다.");

        // 턴 증가
        currentTurn++;
        Debug.Log($"=== ☀️ 턴 {currentTurn} 시작 ===");

        // 새 카드 5장 뽑기
        for (int i = 0; i < 5; i++)
        {
            if (pokerDeck.Count > 0)
            {
                DrawCard();
            }
        }

        PrintCurrentStatus();
    }

    // 5. 카드 한 장을 뽑는 함수
    public void DrawCard()
    {
        if (pokerDeck.Count > 0)
        {
            PokerCard drawnCard = pokerDeck[0];
            pokerDeck.RemoveAt(0);
            playerHand.Add(drawnCard);
        }
    }

    // 가시성을 위한 메세지 출력(나중에 없애도 됨)
    void PrintCurrentStatus()
    {
        string handResult = string.Join(", ", playerHand);
        Debug.Log($"[현재 내 손패]: {handResult}");
        Debug.Log($"📦 남은 덱 카드 수: {pokerDeck.Count}장");
    }

    // 테스트용 턴 넘기기  
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            EndTurn();
        }
    }
}