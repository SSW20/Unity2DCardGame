using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SpecialSelectPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup gameBoardCanvasGroup;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform specialSlotContainer;
    [SerializeField] private Vector2 candidateCardSize = new Vector2(180f, 260f);
    [SerializeField] private float flipDuration = 0.35f;

    public bool isActive { get; private set; }
    public Action<CardUI> OnSpecialSelected;
    private bool isSelecting;

    private void Awake()
    {
        isActive = false;
        gameObject.SetActive(false);
    }

    public void Show(List<(string name, string desc)> options)
    {
        foreach (Transform child in cardContainer) Destroy(child.gameObject);

        isSelecting = false;
        foreach (var option in options)
        {
            GameObject card = Instantiate(cardPrefab, cardContainer);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            if (cardRect != null) cardRect.sizeDelta = candidateCardSize;

            CardUI cardUI = card.GetComponent<CardUI>();
            cardUI.cardType = CardType.Special;
            cardUI.cardName = option.name;
            cardUI.cardDescription = option.desc;
            cardUI.onClicked = OnCardSelected;
            cardUI.SetSpecialFace(false);

            CardDragManager dragManager = card.GetComponent<CardDragManager>();
            if (dragManager != null) Destroy(dragManager);
        }

        gameObject.SetActive(true);
        isActive = true;
        if (gameBoardCanvasGroup != null)
        {
            gameBoardCanvasGroup.interactable = false;
            gameBoardCanvasGroup.blocksRaycasts = false;
        }
    }

    private void OnCardSelected(CardUI selected)
    {
        if (isSelecting) return;
        CardSlot emptySlot = FindEmptySpecialSlot();
        if (emptySlot == null)
        {
            Debug.Log("No empty special slot is available.");
            return;
        }

        isSelecting = true;
        StartCoroutine(SelectRoutine(selected, emptySlot));
    }

    private IEnumerator SelectRoutine(CardUI selected, CardSlot emptySlot)
    {
        selected.onClicked = null;
        Tween flip = selected.FlipSpecialFaceUp(flipDuration);
        if (flip != null) yield return flip.WaitForCompletion();

        GameObject placedCard = Instantiate(selected.gameObject, emptySlot.transform);
        RectTransform rt = placedCard.GetComponent<RectTransform>();
        // 슬롯을 가득 채워, 슬롯 크기가 바뀌어도 특전 카드가 항상 동일한 크기를 유지한다.
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;

        CardUI placedUI = placedCard.GetComponent<CardUI>();
        placedUI.cardType = CardType.Special;
        placedUI.onClicked = null;
        placedUI.SetSpecialFace(true);
        CardDragManager dragManager = placedCard.GetComponent<CardDragManager>();
        if (dragManager != null) Destroy(dragManager);
        emptySlot.SetCard(placedCard);

        Hide();
        OnSpecialSelected?.Invoke(placedUI);
        isSelecting = false;
    }

    private CardSlot FindEmptySpecialSlot()
    {
        foreach (Transform child in specialSlotContainer)
        {
            CardSlot slot = child.GetComponent<CardSlot>();
            if (slot != null && !slot.IsOccupied) return slot;
        }
        return null;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        if (gameBoardCanvasGroup != null)
        {
            gameBoardCanvasGroup.interactable = true;
            gameBoardCanvasGroup.blocksRaycasts = true;
        }
        isActive = false;
    }
}
