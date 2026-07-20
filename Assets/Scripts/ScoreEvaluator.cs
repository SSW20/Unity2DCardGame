using System.Collections.Generic;

public struct SettlementResult
{
    public List<List<PokerCardData>> triples;
    public List<List<PokerCardData>> fourOfAKinds;
    public List<List<PokerCardData>> straights;

    public HashSet<PokerCardData> usedCards;

    // 이번 결산에서 기존 필드 카드 중 족보에 사용되지 않아
    // 새로 무덤으로 이동하는 카드 수
    public int newGraveCardCount;
}

public static class ScoreEvaluator
{
    public static SettlementResult EvaluateAll(List<PokerCardData> pool)
    {
        var result = new SettlementResult
        {
            triples = new List<List<PokerCardData>>(),
            fourOfAKinds = new List<List<PokerCardData>>(),
            straights = new List<List<PokerCardData>>(),
            usedCards = new HashSet<PokerCardData>(),
            newGraveCardCount = 0
        };

        Dictionary<int, List<PokerCardData>> rankGroups = BuildRankGroups(pool);

        EvaluateTriple(rankGroups, ref result);
        EvaluateFourOfAKind(rankGroups, ref result);
        EvaluateStraight(rankGroups, ref result);

        return result;
    }

    private static Dictionary<int, List<PokerCardData>> BuildRankGroups(List<PokerCardData> pool)
    {
        var rankGroups = new Dictionary<int, List<PokerCardData>>();
        foreach (var c in pool)
        {
            // 조커는 슬롯만 차지하며 어떠한 족보에도 참여하지 않는다.
            if (c.IsJoker)
                continue;

            int r = (int)c.rank;
            if (!rankGroups.ContainsKey(r)) rankGroups[r] = new List<PokerCardData>();
            rankGroups[r].Add(c);
        }
        return rankGroups;
    }

    private static void EvaluateTriple(Dictionary<int, List<PokerCardData>> rankGroups, ref SettlementResult result)
    {
        foreach (var kvp in rankGroups)
        {
            if (kvp.Value.Count == 3)
            {
                result.triples.Add(new List<PokerCardData>(kvp.Value));
                foreach (var c in kvp.Value) result.usedCards.Add(c);
            }
        }
    }

    private static void EvaluateFourOfAKind(Dictionary<int, List<PokerCardData>> rankGroups, ref SettlementResult result)
    {
        foreach (var kvp in rankGroups)
        {
            if (kvp.Value.Count == 4)
            {
                result.fourOfAKinds.Add(new List<PokerCardData>(kvp.Value));
                foreach (var c in kvp.Value) result.usedCards.Add(c);
            }
        }
    }

    private static void EvaluateStraight(Dictionary<int, List<PokerCardData>> rankGroups, ref SettlementResult result)
    {
        Dictionary<int, int> usedIndex = new Dictionary<int, int>();
        foreach (var key in rankGroups.Keys) usedIndex[key] = 0;

        bool foundStraight = true;

        while (foundStraight)
        {
            foundStraight = false;

            List<int> sorted = new List<int>();
            foreach (var key in rankGroups.Keys)
                if (usedIndex[key] < rankGroups[key].Count) sorted.Add(key);
            sorted.Sort();

            int n = sorted.Count;
            int runStart = 0;

            for (int i = 1; i <= n; i++)
            {
                bool broken = (i == n) || (sorted[i] != sorted[i - 1] + 1);
                if (broken)
                {
                    int length = i - runStart;
                    if (length >= 4)
                    {
                        // 스트레이트 발견 
                        List<PokerCardData> newStraight = new List<PokerCardData>();
                        for (int j = runStart; j < i; j++)
                        {
                            PokerCardData card = rankGroups[sorted[j]][usedIndex[sorted[j]]];
                            newStraight.Add(card);
                            result.usedCards.Add(card);
                            usedIndex[sorted[j]]++;
                        }
                        result.straights.Add(newStraight);
                        foundStraight = true;
                    }
                    runStart = i;
                }
            }
        }
    }

    public static int CountUnusedFieldCards(
        List<PokerCardData> fieldCards,
        HashSet<PokerCardData> usedCards)
    {
        if (fieldCards == null || fieldCards.Count == 0)
            return 0;

        int count = 0;

        foreach (PokerCardData card in fieldCards)
        {
            // 조커는 결산 후 덱으로 돌아가므로 새 무덤 카드 수에 포함하지 않는다.
            if (card.IsJoker)
                continue;

            if (usedCards == null || !usedCards.Contains(card))
                count++;
        }

        return count;
    }
}
