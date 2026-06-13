using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOverPannel : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeSpeed = 0.3f;
  
    void Start()
    {
        canvasGroup.alpha = 0.0f;
        gameObject.SetActive(false);
    }
    public void Show()
    {
        gameObject.SetActive(true);
        StartCoroutine(FadeIn());
    }

    public void Hide()
    {
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeIn()
    {
        Time.timeScale = 0f;
        canvasGroup.alpha = 0.0f;
        while(canvasGroup.alpha < 1.0f)
        {
            canvasGroup.alpha += Time.unscaledDeltaTime * fadeSpeed;
            yield return null;
        }
        canvasGroup.alpha = 1.0f;
    }

    private IEnumerator FadeOut()
    {
        canvasGroup.alpha = 1.0f;
        while(canvasGroup.alpha > 0.0f)
        {
            canvasGroup.alpha -= Time.unscaledDeltaTime * fadeSpeed;
            yield return null;
        }
        canvasGroup.alpha = 0.0f;
        gameObject.SetActive(false);
        Time.timeScale = 1f;
    }
}
