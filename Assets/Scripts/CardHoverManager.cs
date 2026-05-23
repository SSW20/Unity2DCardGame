using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
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
                // 마우스 아래 UI 오브젝트 감지
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // 감지된 것 중 CardUI 있는지 확인
        CardUI nearest = null;
        foreach (RaycastResult result in results)
        {
            CardUI card = result.gameObject.GetComponent<CardUI>();
            if (card != null)
            {
                nearest = card;
                break;
            }
        }

        if (nearest == currentHovered) return;

        if (currentHovered != null)
            currentHovered.SetHover(false);

        currentHovered = nearest;

        if (currentHovered != null)
            currentHovered.SetHover(true);

    }

    private CardUI GetNearestCardUnderMouse()
    {
            Vector2 mousePos = Input.mousePosition;
            CardUI nearest = null;
            float minDist = 30f; 

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