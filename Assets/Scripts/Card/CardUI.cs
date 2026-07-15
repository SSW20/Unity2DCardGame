using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public enum CardType
{
    Hand,    // 손패
    Special, // 특전
    Field,    // 필드
    Deck,     // 덱
}
public class CardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Card Animation")]
    [SerializeField] public bool useAnimation = true;

    [Header("Scale")]
    [SerializeField] private float hoverScale = 1.3f;
    [SerializeField] private float animSpeed = 10f;
    [SerializeField] private float yOffset = 40f;

    [Header("Sprite")]
    private Sprite cardFrontImage;
    [SerializeField] private Sprite cardBackImage;

    [Header("Card Type")]
    [SerializeField] public CardType cardType;
    [SerializeField] public PokerCardData pokerCardData;

    

    [Header("Special Card Data")]
    public string cardName;
    [TextArea] public string cardDescription;
    public System.Action<CardUI> onClicked;

    public bool bIsHover = false;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private Color originalColor;
    private Image cardImage;

    public Vector3 homeLocalPosition;
    public Quaternion homeLocalRotation;

    public int homeSiblingIndex; 

    public bool isDragging = false;

    private int originalIndex;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (cardType == CardType.Special)
        {
            onClicked?.Invoke(this);
        }
    }


    void Awake()
    {
        originalIndex = transform.GetSiblingIndex();
        originalScale = transform.localScale;
        targetScale = originalScale;
        cardImage = GetComponent<Image>();
        if (cardImage != null) originalColor = cardImage.color;
    }

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void Update()
    {
        if (!useAnimation) return;
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * animSpeed
        );
    }

    // HandHoverManager가 직접 호출
    public void SetHover(bool isHover)
    {
        if (!useAnimation || cardType == CardType.Deck) return;

        if (isHover)
        {
            transform.DOKill(true);   
            targetScale = originalScale * (cardType == CardType.Special ? 1.12f : hoverScale);
            if (cardType == CardType.Special && cardImage != null)
                cardImage.color = Color.Lerp(originalColor, Color.white, 0.35f);
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
            if (cardType == CardType.Special && cardImage != null) cardImage.color = originalColor;
            if(cardType == CardType.Hand)
            {
              transform.SetLocalPositionAndRotation(homeLocalPosition, homeLocalRotation);
              transform.SetSiblingIndex(homeSiblingIndex);
            }
            bIsHover = false;
        }
    }

    public void SetPokerData(PokerCardData card)
    {
        pokerCardData = card;
        cardFrontImage = pokerCardData.sprite;
    }

    public void FlipCard(bool faceUp)
    {
        if (cardImage != null && (faceUp ? cardFrontImage : cardBackImage) != null)
            cardImage.sprite = faceUp ? cardFrontImage : cardBackImage;
    }

    public void SetSpecialFace(bool faceUp)
    {
        if (cardImage == null) cardImage = GetComponent<Image>();
        if (cardImage != null) cardImage.color = faceUp ? new Color(0.96f, 0.82f, 0.42f) : new Color(0.22f, 0.18f, 0.32f);
        originalColor = cardImage != null ? cardImage.color : Color.white;
    }

    public Tween FlipSpecialFaceUp(float duration)
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null) return null;
        Sequence sequence = DOTween.Sequence();
        sequence.Append(rect.DOScaleX(0f, duration * 0.5f));
        sequence.AppendCallback(() => SetSpecialFace(true));
        sequence.Append(rect.DOScaleX(originalScale.x, duration * 0.5f));
        return sequence;
    }

}

