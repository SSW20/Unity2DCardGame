using System.Collections.Generic;

public enum PerkType
{
    // 기존 enum 순서를 유지해 Unity 직렬화 값이 갑자기 뒤바뀌지 않도록 번호를 고정한다.
    CompressedSlots = 0,      // 실전압축 슬롯
    GraveRobbing = 1,        // 파묘
    SameNumberCollector = 2, // 같은 숫자 수집가
    Offensive = 3,           // 공세
    StraightMaster = 4       // 연속의 달인
}

public static class PerkCatalog
{
    public static readonly PerkType[] All =
    {
        PerkType.CompressedSlots,
        PerkType.GraveRobbing,
        PerkType.SameNumberCollector,
        PerkType.Offensive,
        PerkType.StraightMaster
    };

    public static string GetShortDescription(PerkType perk)
    {
        switch (perk)
        {
            case PerkType.CompressedSlots:
                return "빈 슬롯이 많을수록 추가 점수를 얻습니다.";

            case PerkType.GraveRobbing:
                return "무덤의 카드 수에 따라 추가 점수를 얻습니다.";

            case PerkType.SameNumberCollector:
                return "트리플과 포카드 점수를 강화합니다.";

            case PerkType.Offensive:
                return "상대보다 먼저 멈추면 추가 점수를 얻습니다.";

            case PerkType.StraightMaster:
                return "스트레이트 점수를 강화합니다.";

            default:
                return string.Empty;
        }
    }

    public static string GetName(PerkType perk)
    {
        switch (perk)
        {
            case PerkType.CompressedSlots:
                return "실전압축 슬롯";
            case PerkType.GraveRobbing:
                return "파묘";
            case PerkType.SameNumberCollector:
                return "같은 숫자 수집가";
            case PerkType.Offensive:
                return "공세";
            case PerkType.StraightMaster:
                return "연속의 달인";
            default:
                return perk.ToString();
        }
    }

    public static string GetDescription(PerkType perk)
    {
        switch (perk)
        {
            case PerkType.CompressedSlots:
                return "빈 슬롯 1칸당 50점을 추가로 얻습니다.";

            case PerkType.GraveRobbing:
                return "결산 후 무덤에 남는 카드 1장당 20점을 추가로 얻습니다.";

            case PerkType.SameNumberCollector:
                return "트리플은 (코스트 합 + 10) × 4, 포카드는 (코스트 합 + 20) × 8로 계산합니다.";

            case PerkType.Offensive:
                return "상대보다 먼저 STOP 상태에 진입하면 100점을 추가로 얻습니다.";

            case PerkType.StraightMaster:
                return "스트레이트 평균이 6 이하이면 4·5·6연속 배율을 향상시킵니다. 평균이 7 이상이면 코스트 합에 20을 더합니다.";

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
