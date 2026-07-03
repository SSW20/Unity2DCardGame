using UnityEngine;
using TMPro;

public class CardDescPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private float spacing = 20f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0f;
    }

    public void Show(CardUI card)
    {
        Debug.Log($"Show called: {card.cardName}");
        nameText.text = card.cardName;
        descText.text = card.cardDescription;

        PositionNextTo(card.GetComponent<RectTransform>());
        canvasGroup.alpha = 1f;
    }

    public void Hide()
    {
        if(canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void PositionNextTo(RectTransform cardRect)
    {
        float cardHalfWidth = cardRect.rect.width * 0.5f * cardRect.lossyScale.x;
        float panelHalfWidth = rectTransform.rect.width * 0.5f;

        Vector3 offset = cardRect.right * (cardHalfWidth + panelHalfWidth + spacing);
        rectTransform.position = cardRect.position + offset;
    }
}