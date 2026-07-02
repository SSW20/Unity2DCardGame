using System.Collections.Generic;

public struct SettlementResult
{
    public List<List<PokerCardData>> triples;
    public List<List<PokerCardData>> fourOfAKinds;
    public List<List<PokerCardData>> straights;   

    public HashSet<PokerCardData> usedCards;
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
            usedCards = new HashSet<PokerCardData>()
        };

        Dictionary<int, List<PokerCardData>> rankGroups = BuildRankGroups(pool);

        EvaluateTriple(rankGroups, ref result);
        EvaluateFourOfAKind(rankGroups, ref result);
        EvaluateStraight(pool, ref result);

        return result;
    }

    private static Dictionary<int, List<PokerCardData>> BuildRankGroups(List<PokerCardData> pool)
    {
        var rankGroups = new Dictionary<int, List<PokerCardData>>();
        foreach (var c in pool)
        {
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
                // 트리플 카드 목록 전체를 저장
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
                // 포카드 카드 목록 전체를 저장
                result.fourOfAKinds.Add(new List<PokerCardData>(kvp.Value));
                foreach (var c in kvp.Value) result.usedCards.Add(c);
            }
        }
    }

    private static void EvaluateStraight(List<PokerCardData> pool, ref SettlementResult result)
    {
        Dictionary<int, PokerCardData> rep = new Dictionary<int, PokerCardData>();
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
                    // 런에 해당하는 카드 목록을 PokerCardData 전체로 저장
                    List<PokerCardData> run = new List<PokerCardData>();
                    for (int j = runStart; j < i; j++)
                    {
                        run.Add(rep[sorted[j]]);
                        result.usedCards.Add(rep[sorted[j]]);
                    }
                    result.straights.Add(run);
                }
                runStart = i;
            }
        }
    }
}