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

    [Header("Highlight")]
    [SerializeField] private Image cardImage;
    [SerializeField] private Color hoverColor = new Color(1f, 0.9f, 0.5f);

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
        if (!useAnimation || cardType == CardType.Deck || cardType == CardType.Special) return;

        if (isHover)
        {
            transform.DOKill(true);   
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
              transform.SetLocalPositionAndRotation(homeLocalPosition, homeLocalRotation);
            //   transform.SetSiblingIndex(originalIndex); // 원래 위치로 복원
              transform.SetSiblingIndex(homeSiblingIndex);
            }
            bIsHover = false;
        }
    }

    public void SetPokerData(PokerCardData card)
    {
        pokerCardData = card;
    }

}

