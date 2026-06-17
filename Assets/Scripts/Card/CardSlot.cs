using UnityEngine;

public class CardSlot : MonoBehaviour
{
    public bool IsOccupied { get; private set; }
    private GameObject currentCard;

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