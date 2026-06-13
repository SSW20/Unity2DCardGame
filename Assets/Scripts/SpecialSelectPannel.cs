using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class SpecialSelectPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup gameBoardCanvasGroup;
    [SerializeField] public bool isActive = false;


    public void Awake()
    {
        Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        isActive = true;
        gameBoardCanvasGroup.interactable = false;
        gameBoardCanvasGroup.blocksRaycasts = false;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        isActive = false;
        gameBoardCanvasGroup.interactable = true;
        gameBoardCanvasGroup.blocksRaycasts = true;
    }
}