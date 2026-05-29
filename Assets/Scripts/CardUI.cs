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
    [SerializeField] private float yOffset = 40f;
    [SerializeField] public bool isAnimation = false;

    [Header("Highlight")]
    [SerializeField] private Image cardImage;
    [SerializeField] private Color hoverColor = new Color(1f, 0.9f, 0.5f);

    [Header("Card Type")]
    [SerializeField] public CardType cardType;

    public bool bIsHover = false;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private Color originalColor;

    private int originalIndex;

    private Quaternion originalRotation;

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
            originalRotation = transform.localRotation;
            targetScale = originalScale * hoverScale;
            // cardImage.color = hoverColor;
            if(cardType == CardType.Hand)
            {
              transform.SetLocalPositionAndRotation(transform.localPosition + Vector3.up * yOffset, Quaternion.identity);
              transform.SetSiblingIndex(100); // 최상위로 이동
            }
            bIsHover = true;
        }
        else
        {
            targetScale = originalScale;
            // cardImage.color = originalColor;
            // cardCanvas.sortingOrder = 0; 
            if(cardType == CardType.Hand)
            {
              transform.SetLocalPositionAndRotation(transform.localPosition - Vector3.up * yOffset, originalRotation);
              transform.SetSiblingIndex(originalIndex); // 원래 위치로 복원
            }
            bIsHover = false;
        }
    }
}