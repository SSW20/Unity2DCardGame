using System.Collections.Generic;

public enum PerkType
{
    TripleCostBoost,   // 트리플/포카드의 costTrp 계수를 0.1에서 0.5로 강화
    HighScoreBonus,    // 트리플 + 스트레이트 점수가 100 이상이면 10% 증가
    EmptySlotBoost,    // 빈 슬롯 보너스 계수를 0.8로 강화
    GraveCardBonus,    // 이번 결산에서 새로 무덤으로 간 카드 1장당 20점
    StraightBoost      // 스트레이트 카드 수만큼 1.2의 거듭제곱 배율 적용
}

public static class PerkCatalog
{
    public static readonly PerkType[] All =
    {
        PerkType.TripleCostBoost,
        PerkType.HighScoreBonus,
        PerkType.EmptySlotBoost,
        PerkType.GraveCardBonus,
        PerkType.StraightBoost
    };

    public static string GetName(PerkType perk)
    {
        switch (perk)
        {
            case PerkType.TripleCostBoost:
                return "같은 숫자 수집가";
            case PerkType.HighScoreBonus:
                return "공세";
            case PerkType.EmptySlotBoost:
                return "실전압축 슬롯";
            case PerkType.GraveCardBonus:
                return "파묘";
            case PerkType.StraightBoost:
                return "연속의 달인";
            default:
                return perk.ToString();
        }
    }

    public static string GetDescription(PerkType perk)
    {
        switch (perk)
        {
            case PerkType.TripleCostBoost:
                return "트리플과 포카드가 추가 점수를 얻습니다.";
            case PerkType.HighScoreBonus:
                return "상대방보다 빨리 멈출 경우 추가 점수를 얻습니다.";
            case PerkType.EmptySlotBoost:
                return "빈 슬롯 개수에 따른 추가 점수를 얻습니다.";
            case PerkType.GraveCardBonus:
                return "카드 무덤의 카드가 많을수록 추가 점수를 얻습니다.";
            case PerkType.StraightBoost:
                return "스트레이트를 강화합니다.";
            default:
                return string.Empty;
        }
    }

    public static string JoinNames(IReadOnlyList<PerkType> perks)
    {
        if (perks == null || perks.Count == 0)
            return "없음";

        List<string> names = new List<string>();
        for (int i = 0; i < perks.Count; i++)
            names.Add(GetName(perks[i]));

        return string.Join(", ", names);
    }
}
