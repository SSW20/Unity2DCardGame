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
    [Header("코스트 시스템")]
    [SerializeField] private int maxCostPerTurn = 21;

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

    public float CalculateScore(SettlementResult result, int emptySlotCount)
    {
        float total = 0f;

        //  트리플 / 포카드 점수
        int trp = 1 + result.triples.Count + result.fourOfAKinds.Count * 2;
        float trpMultiplier = trp * trp;

        // 트리플/포카드에 사용된 카드 코스트 합산
        float costTrp = 0f;
        foreach (var group in result.triples)
            foreach (var card in group) costTrp += (int)card.rank;
        foreach (var group in result.fourOfAKinds)
            foreach (var card in group) costTrp += (int)card.rank;

        // trpScore 계산
        if (result.triples.Count > 0 || result.fourOfAKinds.Count > 0)
            total += (costTrp * 0.1f + 15f) * trpMultiplier * ((emptySlotCount + 1) * 0.6f);


        //스트레이트 점수 
        foreach (var straight in result.straights)
        {
            int card = straight.Count;  // 스트레이트 카드 수

            // sym = 스트레이트 카드 중 가장 많은 suit 개수, 3 미만이면 2 고정
            Dictionary<CardSuit, int> suitCount = new Dictionary<CardSuit, int>();
            foreach (var c in straight)
            {
                if (!suitCount.ContainsKey(c.suit)) suitCount[c.suit] = 0;
                suitCount[c.suit]++;
            }
            int sym = 2;
            foreach (var cnt in suitCount.Values)
                if (cnt > sym) sym = cnt;

            // str 계산
            float str = card * (1f + (sym - 2) * 0.2f);

            // costStr = 스트레이트 카드 코스트 합산
            float costStr = 0f;
            foreach (var c in straight) costStr += (int)c.rank;

            // strScore 계산
            total += (costStr * 0.6f * str) * ((emptySlotCount + 1) * 0.7f);
        }

        return total;
    }
}