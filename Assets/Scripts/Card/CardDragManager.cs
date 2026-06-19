using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CardDragManager : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private Transform originalParent;
    private Vector3 originalPosition;
    private int originalSiblingIndex;
    private HandLayoutManager handLayoutManager;



    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalPosition = rectTransform.position;
        originalSiblingIndex = transform.GetSiblingIndex();

        // 원래 부모(HandPanel)에서 HandLayoutManager 찾아두기
        handLayoutManager = originalParent.GetComponent<HandLayoutManager>();
        CardUI cardUI = GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.isDragging = true;
            cardUI.SetHover(false);        // 드래그 시작 시 자기 자신 호버 강제 해제
            // cardUI.SetScaleImmediate();
        }



        canvasGroup.blocksRaycasts = false;

        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();

        rectTransform.position = eventData.position;
        rectTransform.localRotation = Quaternion.identity;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        CardSlot targetSlot = GetSlotUnderPointer(eventData);
        CardUI cardUI = GetComponent<CardUI>();
        if (targetSlot != null && targetSlot.CanAcceptDrag(cardUI))
        {
            // 슬롯에 배치
            transform.SetParent(targetSlot.transform, false);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
            targetSlot.SetCard(gameObject);

            if (cardUI != null)
            {
                cardUI.cardType = CardType.Field;
                cardUI.SetHover(false);
            }

            // 더 이상 드래그 안 되게
            this.enabled = false;

            // 손패 재정렬
            if (handLayoutManager != null)
                handLayoutManager.UpdateLayout();
        }
        else
        {
            // 원래 자리로 복귀
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSiblingIndex);
            rectTransform.position = originalPosition;
            if (handLayoutManager != null)
                handLayoutManager.UpdateLayout(); 
        }
    }

    private CardSlot GetSlotUnderPointer(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            CardSlot slot = result.gameObject.GetComponent<CardSlot>();
            if (slot != null) return slot;
        }
        return null;
    }
}