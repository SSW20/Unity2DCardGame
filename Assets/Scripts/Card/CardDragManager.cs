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

    private Quaternion originalRotation;
    private int originalSiblingIndex;
    private HandLayoutManager handLayoutManager;

    public CardManager cardManager;



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
        originalRotation = rectTransform.localRotation;

        // 원래 부모(HandPanel)에서 HandLayoutManager 찾아두기
        handLayoutManager = originalParent.GetComponent<HandLayoutManager>();
        CardUI cardUI = GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.isDragging = true;
            cardUI.SetHover(false);        // 드래그 시작 시 자기 자신 호버 강제 해제
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
            // ===== ★ 코스트 체크 추가 =====
            if (cardUI != null && cardManager != null)
            {
                // 코스트 확인
                if (!cardManager.CanPlayerAffordCard(cardUI.pokerCardData))
                {
                    // 코스트 부족시 원래 위치로 복귀
                    Debug.Log("코스트 부족! 카드가 손패로 돌아갑니다.");
                    ReturnToHand(cardUI);
                    return;
                }

                // 코스트 소비
                if (!cardManager.SpendPlayerCost(cardUI.pokerCardData))
                {
                    // 코스트 소비 실패 → 원래 위치로 복귀
                    ReturnToHand(cardUI);
                    return;
                }
            }

            // 슬롯에 배치
            transform.SetParent(targetSlot.transform, false);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
            targetSlot.SetCard(gameObject);

            if (cardUI != null && cardManager != null)
                cardManager.MoveCard(cardUI.pokerCardData, cardManager.fieldList);   // Field로 보냄

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
            // 슬롯에 배치 실패 → 원래 위치로 복귀
            ReturnToHand(cardUI);
        }
    }

    /// <summary>
    /// 카드를 손패로 돌려보내기
    /// </summary>
    private void ReturnToHand(CardUI cardUI)
    {
        transform.SetParent(originalParent, true);
        transform.SetSiblingIndex(originalSiblingIndex);

        rectTransform.localPosition = cardUI.homeLocalPosition;
        rectTransform.localRotation = cardUI.homeLocalRotation;

        if (cardUI != null)
        {
            cardUI.isDragging = false;
        }

        if (handLayoutManager != null)
            handLayoutManager.UpdateLayout();
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