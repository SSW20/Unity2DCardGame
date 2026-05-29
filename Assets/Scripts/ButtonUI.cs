using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class ButtonUI : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] public UnityEvent onButtonClick;

    [Header("Visual")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.8f);

    private Button button;

    void Start()
    {
        button = GetComponent<Button>();

        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.pressedColor = pressedColor;
        button.colors = colors;

        button.onClick.AddListener(() => onButtonClick?.Invoke());
    }
}