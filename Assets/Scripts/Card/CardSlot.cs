using UnityEngine;

public enum SlotCategory
{
    Field,    // 필드에 카드 내는 슬롯 (Hand → Field)
    Special   // 특전 카드 배치 슬롯 (OnCardSelected를 통해서만 채워짐)
}

public enum SlotOwner
{
    Player,
    Enemy
}

public class CardSlot : MonoBehaviour
{
    [SerializeField] public SlotCategory category = SlotCategory.Field;
    [SerializeField] public SlotOwner owner = SlotOwner.Player;

    [SerializeField] public bool IsOccupied = false;
    public RectTransform RectTransform => (RectTransform)transform;
    public GameObject CurrentCardObject => currentCard;

    private GameObject currentCard;

    public bool CanAcceptDrag(CardUI card)
    {
        if (IsOccupied) return false;
        if (owner != SlotOwner.Player) return false;        // 상대 슬롯엔 드래그로 못 놓음
        if (category != SlotCategory.Field) return false;   // Special 슬롯은 드래그 대상 아님
        if (card.cardType == CardType.Special) return false; // 특전 후보 카드는 드래그 대상 아님

        return true;
    }

    public void SetCard(GameObject card)
    {
        currentCard = card;
        IsOccupied = true;
    }

    public void ClearSlot()
    {
        currentCard = null;
        IsOccupied = false;
    }
}