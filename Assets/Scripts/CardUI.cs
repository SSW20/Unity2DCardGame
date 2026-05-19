using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Card Animation")]
    [SerializeField] private bool UseAnimation = true;

    [Header("Scale")]
    [SerializeField] private float hoverScale = 1.5f;
    [SerializeField] private float animSpeed = 10f;

    [Header("Highlight")]
    [SerializeField] private Image cardImage;
    [SerializeField] private Color hoverColor = new Color(1f, 0.9f, 0.5f);

    private Vector3 originalScale;
    private Vector3 targetScale;
    private Color originalColor;  // 원래 색을 Start에서 저장

    void Start()
    {
        if (cardImage == null)
            cardImage = GetComponent<Image>();

        originalScale = transform.localScale;
        targetScale = originalScale;
        originalColor = cardImage.color;  // 원래 색 저장
    }

    void Update()
    {
        if (!UseAnimation) return;
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * animSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!UseAnimation) return;
        targetScale = originalScale * hoverScale;
        cardImage.color = hoverColor;

        Canvas cardCanvas = GetComponent<Canvas>();
        cardCanvas.sortingOrder = 10;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!UseAnimation) return;
    targetScale = originalScale;
    cardImage.color = originalColor;

    Canvas cardCanvas = GetComponent<Canvas>();
    cardCanvas.sortingOrder = 0;
    }
}