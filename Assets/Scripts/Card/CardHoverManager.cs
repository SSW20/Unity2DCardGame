using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CardHoverManager : MonoBehaviour
{
    private CardUI currentHovered;
    public CardUI CurrentHovered => currentHovered;
    private List<CardUI> hitHandCards = new List<CardUI>();

    private float enterDist = 40f;
    private float exitDist = 150f;

    [SerializeField] private SpecialSelectPanel specialSelectPanel;
    [SerializeField] private CardDescPanel descriptionPanel;
    private bool isSuspended;


    void Start()
    {
        // Hand 타입 카드만 따로 저장 (거리 기반 처리용)
        var all = FindObjectsOfType<CardUI>();
    }

    void Update()
    {
        if (isSuspended)
            return;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        CardUI nearest = null;

        foreach (RaycastResult result in results)
        {
            CardUI card = result.gameObject.GetComponent<CardUI>();
            if (card == null) continue;
            if (card.isDragging) continue;
            if (card.cardType == CardType.Field || card.cardType == CardType.Deck) continue;

            if (card.cardType == CardType.Special)
            {
                nearest = card;
                hitHandCards.Clear();
                break;
            }
            else
            {
               hitHandCards.Add(card);
            }
        }

        if(hitHandCards.Count > 0)
        {
            nearest = GetNearestHandCard();
        }
        if (specialSelectPanel != null
            && specialSelectPanel.isActive
            && (nearest == null || nearest.cardType != CardType.Special))
        {
            nearest = null;
            hitHandCards.Clear();
        }


        if (nearest == currentHovered) return;

        if (currentHovered != null)
            currentHovered.SetHover(false);

        currentHovered = nearest;

        if (currentHovered != null)
            currentHovered.SetHover(true);

        if (descriptionPanel != null && currentHovered != null && currentHovered.cardType == CardType.Special)
            descriptionPanel.Show(currentHovered);
        else if (descriptionPanel != null)
            descriptionPanel.Hide();

    }

    public void SetSuspended(bool suspended)
    {
        if (isSuspended == suspended)
            return;

        isSuspended = suspended;
        hitHandCards.Clear();

        if (currentHovered != null)
        {
            currentHovered.SetHover(false);
            currentHovered = null;
        }

        if (descriptionPanel != null)
            descriptionPanel.Hide();
    }

    private CardUI GetNearestHandCard()
    {
        Vector2 mousePos = Input.mousePosition;
        CardUI nearest = null;
        float minDist = float.MaxValue;

        foreach (CardUI card in hitHandCards)
        {
            if (card == null) continue;
            Vector2 cardScreenPos = RectTransformUtility.WorldToScreenPoint(
                null,
                card.transform.position
            );

            float dist = Vector2.Distance(mousePos, cardScreenPos);
            float threshold = card.bIsHover ? exitDist : enterDist;

            if (dist < threshold && dist < minDist)
            {
                minDist = dist;
                nearest = card;
            }
        }

        return nearest;
    } 
}
