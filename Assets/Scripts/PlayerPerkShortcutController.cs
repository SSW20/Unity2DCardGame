using TMPro;
using UnityEngine;

public class PlayerPerkShortcutController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CardManager playerCardManager;
    [SerializeField] private SpecialSelectPanel specialSelectPanel;

    [Header("Optional Debug UI")]
    [SerializeField] private TMP_Text playerPerkText;

    [Header("Debug")]
    [SerializeField] private bool enableNumberKeySelection;

    private void Start()
    {
        RefreshPerkText();
    }

    private void Update()
    {
        if (!enableNumberKeySelection)
            return;

        // 기존 게임 시작 전 특전 선택창이 열려 있을 때는 단축키를 받지 않는다.
        if (specialSelectPanel != null && specialSelectPanel.isActive)
            return;

        if (Pressed(KeyCode.Alpha1, KeyCode.Keypad1))
            SelectPerk(PerkType.CompressedSlots);

        if (Pressed(KeyCode.Alpha2, KeyCode.Keypad2))
            SelectPerk(PerkType.GraveRobbing);

        if (Pressed(KeyCode.Alpha3, KeyCode.Keypad3))
            SelectPerk(PerkType.SameNumberCollector);

        if (Pressed(KeyCode.Alpha4, KeyCode.Keypad4))
            SelectPerk(PerkType.Offensive);

        if (Pressed(KeyCode.Alpha5, KeyCode.Keypad5))
            SelectPerk(PerkType.StraightMaster);
    }

    private bool Pressed(KeyCode mainKey, KeyCode keypadKey)
    {
        return Input.GetKeyDown(mainKey) || Input.GetKeyDown(keypadKey);
    }

    private void SelectPerk(PerkType perk)
    {
        if (playerCardManager == null)
        {
            Debug.LogError("[Player Perk] Player CardManager가 연결되지 않았습니다.");
            return;
        }

        if (playerCardManager.TryAddPerk(perk))
        {
            Debug.Log($"[Player Perk] {PerkCatalog.GetName(perk)} 획득");
        }
        else if (playerCardManager.HasPerk(perk))
        {
            Debug.LogWarning($"[Player Perk] 이미 보유 중: {PerkCatalog.GetName(perk)}");
        }
        else
        {
            Debug.LogWarning(
                $"[Player Perk] 최대 {CardManager.MaxPerkCount}개까지만 보유할 수 있습니다.");
        }

        RefreshPerkText();
    }

    private void RefreshPerkText()
    {
        if (playerPerkText == null || playerCardManager == null)
            return;

        playerPerkText.text =
            $"Player Perks\n{PerkCatalog.JoinNames(playerCardManager.OwnedPerks)}";
    }
}
