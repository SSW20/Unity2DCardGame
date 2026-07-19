using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverPannel : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeSpeed = 3f;
    [SerializeField] private float flipDuration = 0.2f;
    [SerializeField] private float moveDuration = 0.45f;

    private RectTransform dealerCardRect;
    private Image dealerCardImage;
    private Image dealerPortraitImage;
    private Button dealerCardButton;
    private CanvasGroup resultGroup;
    private Text outcomeText;
    private Text playerScoreText;
    private Text dealerScoreText;
    private Text differenceText;
    private Text clickGuideText;
    private Font koreanFont;

    private Sprite[] resultDealerFrames;
    private float resultFrameDuration;
    private Coroutine expressionCoroutine;
    private bool showRequested;
    private bool revealed;

    private static readonly Vector2 DealerCardSize = new Vector2(400f, 538f);
    private static readonly Vector2 RevealedDealerPosition = new Vector2(-260f, 0f);

    private void Start()
    {
        if (showRequested)
            return;

        EnsureCanvasGroup();
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void Show(
        bool playerWon,
        int playerScore,
        int aiScore,
        Sprite[] dealerFrames,
        float secondsPerFrame)
    {
        showRequested = true;
        gameObject.SetActive(true);
        EnsureResultUI();

        revealed = false;
        resultDealerFrames = dealerFrames;
        resultFrameDuration = Mathf.Max(0.01f, secondsPerFrame);

        if (expressionCoroutine != null)
        {
            StopCoroutine(expressionCoroutine);
            expressionCoroutine = null;
        }

        dealerCardRect.anchoredPosition = Vector2.zero;
        dealerCardRect.localScale = Vector3.one;
        dealerCardImage.sprite = null;
        dealerCardImage.color = Color.black;
        dealerPortraitImage.sprite = null;
        dealerPortraitImage.color = Color.clear;
        dealerCardButton.interactable = true;

        clickGuideText.gameObject.SetActive(true);
        resultGroup.alpha = 0f;
        resultGroup.interactable = false;
        resultGroup.blocksRaycasts = false;
        outcomeText.text = playerWon ? "YOU WIN" : "YOU LOSE";
        playerScoreText.text = playerScore.ToString("N0");
        dealerScoreText.text = aiScore.ToString("N0");
        differenceText.text = $"최종 점수 차이 {FormatSignedScore(playerScore - aiScore)}";

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        Time.timeScale = 0f;

        StopAllCoroutines();
        StartCoroutine(FadeCanvas(0f, 1f, 1f / Mathf.Max(0.01f, fadeSpeed)));
    }

    // 기존 Inspector 이벤트가 남아 있어도 오류가 나지 않도록 유지한다.
    public void Show()
    {
        showRequested = true;
        gameObject.SetActive(true);
        EnsureCanvasGroup();
        Time.timeScale = 0f;
        StartCoroutine(FadeCanvas(canvasGroup.alpha, 1f, 1f / Mathf.Max(0.01f, fadeSpeed)));
    }

    public void Hide()
    {
        if (gameObject.activeInHierarchy)
            StartCoroutine(HideRoutine());
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void EnsureResultUI()
    {
        EnsureCanvasGroup();
        if (dealerCardRect != null)
            return;

        foreach (Transform child in transform)
            child.gameObject.SetActive(false);

        koreanFont = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 48)
            ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Image backdrop = CreateImage("Result Backdrop", transform, new Color(0f, 0f, 0f, 0.96f));
        StretchToParent(backdrop.rectTransform);

        GameObject cardObject = new GameObject(
            "Dealer Result Card",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        cardObject.transform.SetParent(transform, false);

        dealerCardRect = cardObject.GetComponent<RectTransform>();
        dealerCardRect.anchorMin = new Vector2(0.5f, 0.5f);
        dealerCardRect.anchorMax = new Vector2(0.5f, 0.5f);
        dealerCardRect.pivot = new Vector2(0.5f, 0.5f);
        dealerCardRect.sizeDelta = DealerCardSize;

        dealerCardImage = cardObject.GetComponent<Image>();
        dealerCardImage.color = Color.black;

        dealerPortraitImage = CreateImage("Dealer Portrait", cardObject.transform, Color.clear);
        StretchToParent(dealerPortraitImage.rectTransform);
        dealerPortraitImage.preserveAspect = true;
        dealerPortraitImage.raycastTarget = false;

        dealerCardButton = cardObject.GetComponent<Button>();
        dealerCardButton.targetGraphic = dealerCardImage;
        dealerCardButton.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = dealerCardButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.pressedColor = new Color(0.62f, 0.62f, 0.62f, 1f);
        dealerCardButton.colors = colors;
        dealerCardButton.onClick.AddListener(RevealResult);

        clickGuideText = CreateText(
            "Click Guide",
            transform,
            "검은 카드를 클릭하세요",
            30,
            TextAnchor.MiddleCenter);
        RectTransform guideRect = clickGuideText.rectTransform;
        guideRect.anchorMin = new Vector2(0.5f, 0.5f);
        guideRect.anchorMax = new Vector2(0.5f, 0.5f);
        guideRect.anchoredPosition = new Vector2(0f, -310f);
        guideRect.sizeDelta = new Vector2(600f, 60f);

        GameObject resultObject = new GameObject("Final Settlement", typeof(RectTransform), typeof(CanvasGroup));
        resultObject.transform.SetParent(transform, false);
        RectTransform resultRect = resultObject.GetComponent<RectTransform>();
        resultRect.anchorMin = new Vector2(0.5f, 0.5f);
        resultRect.anchorMax = new Vector2(0.5f, 0.5f);
        resultRect.pivot = new Vector2(0.5f, 0.5f);
        resultRect.anchoredPosition = new Vector2(245f, 20f);
        resultRect.sizeDelta = new Vector2(520f, 600f);
        resultGroup = resultObject.GetComponent<CanvasGroup>();

        Image resultBackground = CreateImage(
            "Settlement Background",
            resultObject.transform,
            new Color(0f, 0f, 0f, 0.72f));
        StretchToParent(resultBackground.rectTransform);
        resultBackground.raycastTarget = false;

        CreatePositionedText(
            "Title", resultObject.transform, "최종 결산",
            new Vector2(0f, 225f), new Vector2(500f, 65f), 44);

        outcomeText = CreatePositionedText(
            "Outcome", resultObject.transform, string.Empty,
            new Vector2(0f, 135f), new Vector2(500f, 65f), 42);

        CreatePositionedText(
            "Player Label", resultObject.transform, "PLAYER",
            new Vector2(-120f, 45f), new Vector2(220f, 50f), 31);
        CreatePositionedText(
            "Dealer Label", resultObject.transform, "DEALER",
            new Vector2(120f, 45f), new Vector2(220f, 50f), 31);

        playerScoreText = CreatePositionedText(
            "Player Score", resultObject.transform, string.Empty,
            new Vector2(-120f, -15f), new Vector2(220f, 55f), 38);
        dealerScoreText = CreatePositionedText(
            "Dealer Score", resultObject.transform, string.Empty,
            new Vector2(120f, -15f), new Vector2(220f, 55f), 38);

        differenceText = CreatePositionedText(
            "Score Difference", resultObject.transform, string.Empty,
            new Vector2(0f, -105f), new Vector2(500f, 60f), 31);

        CreateResultButton(
            "Retry Button",
            resultObject.transform,
            "다시 도전",
            new Vector2(-110f, -220f),
            RestartGame);
        CreateResultButton(
            "Main Menu Button",
            resultObject.transform,
            "메인 메뉴",
            new Vector2(110f, -220f),
            GoToMainMenu);
    }

    private static string FormatSignedScore(int score)
    {
        return score > 0 ? $"+{score:N0}" : score.ToString("N0");
    }

    private void RevealResult()
    {
        if (revealed)
            return;

        revealed = true;
        dealerCardButton.interactable = false;
        clickGuideText.gameObject.SetActive(false);
        StartCoroutine(RevealRoutine());
    }

    private IEnumerator RevealRoutine()
    {
        yield return ScaleX(dealerCardRect, 1f, 0f, flipDuration);

        dealerPortraitImage.color = Color.white;
        if (resultDealerFrames != null && resultDealerFrames.Length > 0)
        {
            dealerPortraitImage.sprite = resultDealerFrames[0];
            expressionCoroutine = StartCoroutine(PlayDealerFrames());
        }

        yield return ScaleX(dealerCardRect, 0f, 1f, flipDuration);
        yield return MoveCard(Vector2.zero, RevealedDealerPosition, moveDuration);

        resultGroup.interactable = true;
        resultGroup.blocksRaycasts = true;
        yield return FadeGroup(resultGroup, 0f, 1f, 0.35f);
    }

    private IEnumerator PlayDealerFrames()
    {
        int frameIndex = 0;

        while (resultDealerFrames != null && resultDealerFrames.Length > 0)
        {
            Sprite frame = resultDealerFrames[frameIndex];
            if (frame != null)
                dealerPortraitImage.sprite = frame;

            frameIndex = (frameIndex + 1) % resultDealerFrames.Length;
            yield return WaitUnscaled(resultFrameDuration);
        }
    }

    private IEnumerator ScaleX(RectTransform target, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            Vector3 scale = target.localScale;
            scale.x = Mathf.Lerp(from, to, t);
            target.localScale = scale;
            yield return null;
        }

        Vector3 finalScale = target.localScale;
        finalScale.x = to;
        target.localScale = finalScale;
    }

    private IEnumerator MoveCard(Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)));
            dealerCardRect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }

        dealerCardRect.anchoredPosition = to;
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        yield return FadeGroup(canvasGroup, from, to, duration);
    }

    private IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        group.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / Mathf.Max(0.01f, duration));
            yield return null;
        }

        group.alpha = to;
    }

    private IEnumerator WaitUnscaled(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator HideRoutine()
    {
        yield return FadeGroup(canvasGroup, canvasGroup.alpha, 0f, 1f / Mathf.Max(0.01f, fadeSpeed));
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        showRequested = false;
        gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    private Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private Text CreateText(string objectName, Transform parent, string value, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = koreanFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Normal;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private Text CreatePositionedText(
        string objectName,
        Transform parent,
        string value,
        Vector2 position,
        Vector2 size,
        int fontSize)
    {
        Text text = CreateText(objectName, parent, value, fontSize, TextAnchor.MiddleCenter);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        text.fontStyle = FontStyle.Bold;
        return text;
    }

    private void CreateResultButton(
        string objectName,
        Transform parent,
        string label,
        Vector2 position,
        UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(205f, 78f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.22f, 0.02f, 0.02f, 0.96f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        Text text = CreateText("Label", buttonObject.transform, label, 30, TextAnchor.MiddleCenter);
        StretchToParent(text.rectTransform);
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
