using UnityEngine;
using UnityEngine.UI;
public enum CardType
{
    Hand,    // 손패
    Special, // 특전
    Field    // 필드
}
public class CardUI : MonoBehaviour
{
    [Header("Card Animation")]
    [SerializeField] private bool UseAnimation = true;

    [Header("Scale")]
    [SerializeField] private float hoverScale = 1.3f;
    [SerializeField] private float animSpeed = 10f;

    [Header("Highlight")]
    [SerializeField] private Image cardImage;
    [SerializeField] private Color hoverColor = new Color(1f, 0.9f, 0.5f);

    [Header("Card Type")]
    [SerializeField] private CardType cardType;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private Color originalColor;

    private int originalIndex;

    // private Canvas cardCanvas;

    void Awake()
    {
        // cardCanvas = GetComponent<Canvas>();
        originalIndex = transform.GetSiblingIndex();
    }

    void Start()
    {
        // if (cardImage == null)
        //     cardImage = GetComponent<Image>();

        originalScale = transform.localScale;
        targetScale = originalScale;
        // originalColor = cardImage.color;
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

    // HandHoverManager가 직접 호출
    public void SetHover(bool isHover)
    {
        if (!UseAnimation) return;

        if (isHover)
        {
            targetScale = originalScale * hoverScale;
            // cardImage.color = hoverColor;
            if(cardType == CardType.Hand)
            {
              transform.SetAsLastSibling();
            }
        }
        else
        {
            targetScale = originalScale;
            // cardImage.color = originalColor;
            // cardCanvas.sortingOrder = 0; 
            if(cardType == CardType.Hand)
            {
              transform.SetSiblingIndex(originalIndex);
            }
        }
    }
}