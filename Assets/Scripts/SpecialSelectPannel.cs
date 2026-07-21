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

    [Header("Perk Card Faces")]
    [SerializeField] private Sprite sameNumberCollectorSprite;
    [SerializeField] private Sprite offensiveSprite;
    [SerializeField] private Sprite compressedSlotSprite;
    [SerializeField] private Sprite graveRobbingSprite;
    [SerializeField] private Sprite straightMasterSprite;

    public bool isActive { get; private set; }
    private bool isSelecting;
    private readonly Dictionary<CardUI, PerkType> perkCandidates = new Dictionary<CardUI, PerkType>();
    private Func<PerkType, bool> trySelectPerk;
    private Action<PerkType> onPerkSelectionCompleted;

    private void Awake()
    {
        isActive = false;
        gameObject.SetActive(false);
    }

    public void Show(List<(string name, string desc)> options)
    {
        if (options == null || !PrepareToShow())
            return;

        foreach (var option in options)
            CreateCandidateCard(option.name, option.desc);

        ActivatePanel();
    }

    public bool ShowPerkOptions(
        IReadOnlyList<PerkType> perks,
        Func<PerkType, bool> trySelect,
        Action<PerkType> selectionCompleted)
    {
        if (perks == null || trySelect == null || !PrepareToShow())
            return false;

        trySelectPerk = trySelect;
        onPerkSelectionCompleted = selectionCompleted;

        foreach (PerkType perk in perks)
        {
            CardUI card = CreateCandidateCard(
                PerkCatalog.GetName(perk),
                PerkCatalog.GetDescription(perk),
                GetPerkSprite(perk));
            if (card != null)
                perkCandidates[card] = perk;
        }

        if (perkCandidates.Count == 0)
            return false;

        ActivatePanel();
        return true;
    }

    public bool TryDisplayPerk(PerkType perk, SlotOwner owner)
    {
        CardSlot emptySlot = FindEmptySpecialSlot(owner);
        return TryPlacePerkCard(
            cardPrefab,
            emptySlot,
            PerkCatalog.GetName(perk),
            PerkCatalog.GetDescription(perk),
            GetPerkSprite(perk));
    }

    private bool PrepareToShow()
    {
        if (cardContainer == null || cardPrefab == null || specialSlotContainer == null)
        {
            Debug.LogWarning("SpecialSelectPanel is missing a required reference.");
            return false;
        }

        if (FindEmptySpecialSlot() == null)
        {
            Debug.LogWarning("SpecialSelectPanel has no available special slot.");
            return false;
        }

        foreach (Transform child in cardContainer)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        isSelecting = false;
        perkCandidates.Clear();
        trySelectPerk = null;
        onPerkSelectionCompleted = null;
        return true;
    }

    private CardUI CreateCandidateCard(
        string name,
        string description,
        Sprite frontSprite = null)
    {
        GameObject card = Instantiate(cardPrefab, cardContainer);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        if (cardRect != null)
            cardRect.sizeDelta = candidateCardSize;

        CardUI cardUI = card.GetComponent<CardUI>();
        if (cardUI == null)
        {
            Destroy(card);
            return null;
        }

        cardUI.cardType = CardType.Special;
        cardUI.cardName = name;
        cardUI.cardDescription = description;
        cardUI.onClicked = OnCardSelected;
        cardUI.SetSpecialFrontImage(frontSprite);
        cardUI.SetSpecialFace(false);

        CardDragManager dragManager = card.GetComponent<CardDragManager>();
        if (dragManager != null)
            Destroy(dragManager);

        return cardUI;
    }

    private void ActivatePanel()
    {
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

        PerkType selectedPerk;
        if (perkCandidates.TryGetValue(selected, out selectedPerk)
            && !trySelectPerk(selectedPerk))
        {
            Debug.LogWarning($"Could not add perk: {PerkCatalog.GetName(selectedPerk)}");
            return;
        }

        isSelecting = true;
        CardSoundController.PlayUIClick();
        StartCoroutine(SelectRoutine(selected, emptySlot, selectedPerk));
    }

    private IEnumerator SelectRoutine(CardUI selected, CardSlot emptySlot, PerkType selectedPerk)
    {
        selected.onClicked = null;
        Tween flip = selected.FlipSpecialFaceUp(flipDuration);
        if (flip != null) yield return flip.WaitForCompletion();

        TryPlacePerkCard(
            selected.gameObject,
            emptySlot,
            selected.cardName,
            selected.cardDescription,
            selected.SpecialFrontImage);

        bool selectedPerkCandidate = perkCandidates.ContainsKey(selected);

        if (selectedPerkCandidate)
            onPerkSelectionCompleted?.Invoke(selectedPerk);

        isSelecting = false;
        Hide();
    }

    private bool TryPlacePerkCard(
        GameObject source,
        CardSlot slot,
        string name,
        string description,
        Sprite frontSprite = null)
    {
        if (source == null || slot == null)
            return false;

        GameObject placedCard = Instantiate(source, slot.transform);
        CardUI placedUI = placedCard.GetComponent<CardUI>();
        if (placedUI == null)
        {
            Destroy(placedCard);
            return false;
        }

        placedUI.cardType = CardType.Special;
        placedUI.cardName = name;
        placedUI.cardDescription = description;
        placedUI.onClicked = null;
        placedUI.SetSpecialFrontImage(frontSprite);
        placedUI.SetSpecialFace(true);

        RectTransform rect = placedCard.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        CardDragManager dragManager = placedCard.GetComponent<CardDragManager>();
        if (dragManager != null)
            Destroy(dragManager);

        slot.SetCard(placedCard);
        return true;
    }

    private Sprite GetPerkSprite(PerkType perk)
    {
        switch (perk)
        {
            case PerkType.TripleCostBoost:
                return sameNumberCollectorSprite;
            case PerkType.HighScoreBonus:
                return offensiveSprite;
            case PerkType.EmptySlotBoost:
                return compressedSlotSprite;
            case PerkType.GraveCardBonus:
                return graveRobbingSprite;
            case PerkType.StraightBoost:
                return straightMasterSprite;
            default:
                return null;
        }
    }

    private CardSlot FindEmptySpecialSlot()
    {
        return FindEmptySpecialSlot(SlotOwner.Player);
    }

    private CardSlot FindEmptySpecialSlot(SlotOwner owner)
    {
        CardSlot[] slots = owner == SlotOwner.Player && specialSlotContainer != null
            ? specialSlotContainer.GetComponentsInChildren<CardSlot>()
            : FindObjectsOfType<CardSlot>();

        foreach (CardSlot slot in slots)
        {
            if (slot.category == SlotCategory.Special
                && slot.owner == owner
                && !slot.IsOccupied)
            {
                return slot;
            }
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
