using UnityEngine;

public class HandHoverManager : MonoBehaviour
{
    private CardUI[] cards;
    private CardUI currentHovered;

    void Start()
    {
        cards = FindObjectsOfType<CardUI>();
    }

    void Update()
    {
        CardUI nearest = GetNearestCardUnderMouse();

        if (nearest == currentHovered) return;

        if (currentHovered != null)
            currentHovered.SetHover(false);

        currentHovered = nearest;

        if (currentHovered != null)
            currentHovered.SetHover(true);

        Debug.Log(currentHovered != null ? $"Hovering: {currentHovered.name}" : "Not hovering any card");
    }

    private CardUI GetNearestCardUnderMouse()
    {
            Vector2 mousePos = Input.mousePosition;
            CardUI nearest = null;
            float minDist = 60f; // 이 거리 안에 있어야 호버 인정 (값 조절 가능)

    foreach (CardUI card in cards)
    {
        Vector2 cardScreenPos = RectTransformUtility.WorldToScreenPoint(
            null,
            card.transform.position
        );

        float dist = Vector2.Distance(mousePos, cardScreenPos);

        if (dist < minDist)
        {
            minDist = dist;
            nearest = card;
        }
    }

    return nearest;
    }
}