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
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 0.9f, 0.5f);

    private Vector3 originalScale;
    private Vector3 targetScale;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * animSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
        cardImage.color = hoverColor;
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
        cardImage.color = normalColor;
    }
}