using UnityEngine;
using UnityEngine.UI;

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
            transform.SetAsLastSibling();
        }
        else
        {
            targetScale = originalScale;
            // cardImage.color = originalColor;
            // cardCanvas.sortingOrder = 0; 
            transform.SetSiblingIndex(originalIndex);
        }
    }
}