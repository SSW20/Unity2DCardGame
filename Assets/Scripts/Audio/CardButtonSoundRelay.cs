using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardButtonSoundRelay : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && button.interactable)
            CardSoundController.PlayUIHover();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && button.interactable)
            CardSoundController.PlayUIClick();
    }
}
