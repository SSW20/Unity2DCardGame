using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class SpecialSelectPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup gameBoardCanvasGroup;
    [SerializeField] private Transform cardContainer;     // 후보 카드 컨테이너
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform specialSlotContainer; // SpecialCardHorizontalBottom

    public bool isActive = false;
    public void Awake()
    {
        isActive = false;
        gameObject.SetActive(false);
    }


    public void Show(List<(string name, string desc)> options)
    {
        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        foreach (var option in options)
        {
            GameObject card = Instantiate(cardPrefab, cardContainer);
            CardUI cardUI = card.GetComponent<CardUI>();
            cardUI.cardType = CardType.Special;
            cardUI.cardName = option.name;
            cardUI.cardDescription = option.desc;
            cardUI.onClicked = OnCardSelected;

            CardDragManager dragManager = card.GetComponent<CardDragManager>();
            if (dragManager != null)
            {
                
            Destroy(dragManager);
            }
        }

        gameObject.SetActive(true);
        isActive = true;

        if (gameBoardCanvasGroup != null)
        {
            gameBoardCanvasGroup.interactable = true;
            gameBoardCanvasGroup.blocksRaycasts = true;
        }
    }

    private void OnCardSelected(CardUI selected)
    {
        // 빈 슬롯 찾기
        CardSlot emptySlot = FindEmptySpecialSlot();
        if (emptySlot != null)
        {
            // 선택된 카드를 복사해서 슬롯에 배치
            GameObject placedCard = Instantiate(selected.gameObject, emptySlot.transform);

            RectTransform rt = placedCard.GetComponent<RectTransform>();
RectTransform slotRt = emptySlot.RectTransform;


rt.anchorMin = new Vector2(0.5f, 0.5f);
rt.anchorMax = new Vector2(0.5f, 0.5f);
rt.pivot = new Vector2(0.5f, 0.5f);
rt.anchoredPosition = Vector2.zero;
rt.localRotation = Quaternion.identity;
rt.localScale = Vector3.one;

            CardUI placedUI = placedCard.GetComponent<CardUI>();
            placedUI.cardType = CardType.Field;
            placedUI.onClicked = null;

            emptySlot.SetCard(placedCard);
        }
        else
        {
            Debug.Log("특전 슬롯이 가득 찼습니다");
        }

        Hide();
    }

    private CardSlot FindEmptySpecialSlot()
    {
        foreach (Transform child in specialSlotContainer)
        {
            CardSlot slot = child.GetComponent<CardSlot>();
            if (slot != null && !slot.IsOccupied)
                return slot;
        }
        return null;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        gameBoardCanvasGroup.interactable = true;
        gameBoardCanvasGroup.blocksRaycasts = true;
        isActive = false;
    }
}