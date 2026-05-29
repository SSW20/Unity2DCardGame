using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
public class HandHoverManager : MonoBehaviour
{
    private CardUI[] cards;
    private CardUI currentHovered;

    private float enterDist = 40f;
    private float exitDist = 150f;
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
            float minDist = float.MaxValue; 

    foreach (CardUI card in cards)
    {
        Vector2 cardScreenPos = RectTransformUtility.WorldToScreenPoint(
            null,
            card.transform.position
        );

        float dist = Vector2.Distance(mousePos, cardScreenPos);
        float threshold = card.bIsHover ? exitDist : enterDist;
        if (dist < minDist && threshold < minDist)
        {
            minDist = dist;
            nearest = card;
        }
    }

    return nearest;
    }
}