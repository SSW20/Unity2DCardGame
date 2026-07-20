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
        EnsureInitialized();
    }

    private void EnsureInitialized()
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
        EnsureInitialized();
        if (card == null || rectTransform == null || canvasGroup == null) return;

        Debug.Log($"Show called: {card.cardName}");
        if (nameText != null)
        {
            nameText.alignment = TextAlignmentOptions.Top;
            nameText.text = card.cardName;

            if (descText != null)
            {
                descText.alignment = TextAlignmentOptions.Top;
                descText.text = card.cardDescription;
            }
        }
        else if (descText != null)
        {
            // 현재 씬은 이름 전용 TMP가 없으므로 하나의 텍스트 안에서
            // 이름과 설명을 위에서부터 가운데 정렬로 표시한다.
            descText.alignment = TextAlignmentOptions.Top;
            descText.text =
                $"<align=center><size=115%><nobr><b>{card.cardName}</b></nobr></size></align>\n\n" +
                $"<align=center>{card.cardDescription}</align>";
        }

        PositionNextTo(card.transform as RectTransform);
        canvasGroup.alpha = 1f;
    }

    public void Hide()
    {
        if(canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void PositionNextTo(RectTransform cardRect)
    {
        if (cardRect == null || rectTransform == null) return;

        float cardHalfWidth = cardRect.rect.width * 0.5f * cardRect.lossyScale.x;
        float panelHalfWidth = rectTransform.rect.width * 0.5f;

        Vector3 offset = cardRect.right * (cardHalfWidth + panelHalfWidth + spacing);
        rectTransform.position = cardRect.position + offset;
    }
}
