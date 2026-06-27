using System.Collections.Generic;

public struct SettlementResult
{
    public List<int> tripleRanks;
    public List<int> fourOfAKindRanks;
    public List<(int high, int count)> straightDetails;   // 점수 계산용으로 카드 수도 같이 보관

    public HashSet<PokerCard> usedCards;
}

public static class ScoreEvaluator
{
    public static SettlementResult EvaluateAll(List<PokerCard> pool)
    {
        var result = new SettlementResult
        {
            tripleRanks = new List<int>(),
            fourOfAKindRanks = new List<int>(),
            straightDetails = new List<(int, int)>(),
            usedCards = new HashSet<PokerCard>()
        };

        Dictionary<int, List<PokerCard>> rankGroups = BuildRankGroups(pool);

        EvaluateTriple(rankGroups, ref result);
        EvaluateFourOfAKind(rankGroups, ref result);
        EvaluateStraight(pool, ref result);

        return result;
    }

    private static Dictionary<int, List<PokerCard>> BuildRankGroups(List<PokerCard> pool)
    {
        var rankGroups = new Dictionary<int, List<PokerCard>>();
        foreach (var c in pool)
        {
            int r = (int)c.rank;
            if (!rankGroups.ContainsKey(r)) rankGroups[r] = new List<PokerCard>();
            rankGroups[r].Add(c);
        }
        return rankGroups;
    }

    private static void EvaluateTriple(Dictionary<int, List<PokerCard>> rankGroups, ref SettlementResult result)
    {
        foreach (var kvp in rankGroups)
        {
            if (kvp.Value.Count == 3)
            {
                result.tripleRanks.Add(kvp.Key);
                foreach (var c in kvp.Value) result.usedCards.Add(c);
            }
        }
        result.tripleRanks.Sort((a, b) => b.CompareTo(a));
    }

    private static void EvaluateFourOfAKind(Dictionary<int, List<PokerCard>> rankGroups, ref SettlementResult result)
    {
        foreach (var kvp in rankGroups)
        {
            if (kvp.Value.Count == 4)
            {
                result.fourOfAKindRanks.Add(kvp.Key);
                foreach (var c in kvp.Value) result.usedCards.Add(c);
            }
        }
        result.fourOfAKindRanks.Sort((a, b) => b.CompareTo(a));
    }

    private static void EvaluateStraight(List<PokerCard> pool, ref SettlementResult result)
    {
        Dictionary<int, PokerCard> rep = new Dictionary<int, PokerCard>();
        foreach (var c in pool)
        {
            int r = (int)c.rank;
            if (!rep.ContainsKey(r)) rep[r] = c;
        }

        List<int> sorted = new List<int>(rep.Keys);
        sorted.Sort();

        int runStart = 0;
        int n = sorted.Count;

        for (int i = 1; i <= n; i++)
        {
            bool broken = (i == n) || (sorted[i] != sorted[i - 1] + 1);
            if (broken)
            {
                int length = i - runStart;
                if (length >= 4)
                {
                    int high = sorted[i - 1];
                    result.straightDetails.Add((high, length));
                    for (int j = runStart; j < i; j++)
                        result.usedCards.Add(rep[sorted[j]]);
                }
                runStart = i;
            }
        }
        result.straightDetails.Sort((a, b) => b.high.CompareTo(a.high));
    }
}