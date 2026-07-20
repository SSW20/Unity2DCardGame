using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public enum TurnOwner
{
    Player,
    AI
}

public enum GamePhase
{
    Stop,
    PlayerTurn,
    AITurn,
    Settlement,
    GameOver
}

public class GameTurnManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private GameInputManager gameInputManager;
    [SerializeField] private AIController aiController;
    [SerializeField] private CardManager playerCardManager;
    [SerializeField] private CardManager aiCardManager;

    [Header("Pre-game Special Selection")]
    [SerializeField] private SpecialSelectPanel specialSelectPanel;

    //[Header("Field Slot Containers")]
    //[SerializeField] private Transform playerFieldSlotContainer;
    //[SerializeField] private Transform aiFieldSlotContainer;

    [Header("Game Over")]
    [SerializeField] private GameOverPannel gameOverPannel;
    [SerializeField] private int targetScore = 100;

    [Header("UI")]
    [SerializeField] private TMP_Text playerTotalScoreText;
    [SerializeField] private TMP_Text aiTotalScoreText;
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private Slider playerScoreSlider;
    [SerializeField] private Slider aiScoreSlider;

    [Header("Perk Debug UI (Optional)")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text aiPerkText;

    [Header("Buttons")]
    [SerializeField] private Button stopButton;
    [SerializeField] private Button turnEndButton;

    [Header("Forced Stop Messages")]
    [SerializeField] private float jokerForcedStopMessageDuration = 1.5f;
    [SerializeField] private float fieldFullForcedStopMessageDuration = 1.5f;

    [Header("Turn Banner")]
    [SerializeField] private GameObject turnBannerObject;
    [SerializeField] private Image turnBannerImage;
    [SerializeField] private TMP_Text turnBannerText;
    [SerializeField] private Sprite turnBannerFrameSprite;
    [SerializeField] private float turnBannerDuration = 2f;
    [SerializeField] private float fastTurnBannerDuration = 1f;

    [Header("Dealer Expression")]
    [SerializeField] private Image dealerImage;
    [SerializeField] private Sprite dealerDefaultSprite;
    [SerializeField] private Sprite dealerSmileSprite;
    [SerializeField] private Sprite dealerAnnoyedSprite;
    [SerializeField] private Sprite[] dealerSmileFrames;
    [SerializeField] private float dealerSmileFrameDuration = 0.15f;
    [SerializeField] private Sprite[] dealerAnnoyedFrames;
    [SerializeField] private float dealerAnnoyedFrameDuration = 0.15f;

    [Header("Settlement Score Clash")]
    [SerializeField] private SettlementScoreClashUI scoreClashUI;

    // TODO : 디버그용 임시 코드, 실제 점수 계산이 확인되면 제거할 것
    [Header("Debug")]
    [SerializeField] private bool debugUseRandomClashScore = false;
    [SerializeField] private Vector2 debugRandomScoreRange = new Vector2(50f, 500f);

    private TurnOwner currentTurn;
    private GamePhase currentPhase;

    private bool playerStopped = false;
    private bool aiStopped = false;
    private bool playerActionButtonsBlocked;

    private int playerTotalScore = 0;
    private int aiTotalScore = 0;
    private int currentRound = 0;
    private Text readableTurnBannerText;
    private Coroutine dealerExpressionCoroutine;

    public int CurrentRound => currentRound;

    private void Awake()
    {
        CreateReadableTurnBannerText();
    }

    private void Start()
    {
        FindGameOverPanelIfNeeded();
        SetupButtons();

        if (turnBannerObject != null)
            turnBannerObject.SetActive(false);

        BeginGameSetup();
    }

    private void BeginGameSetup()
    {
        // 씬이 처음 시작될 때 플레이어 특전과
        // 이전 AI 랜덤 특전 데이터를 모두 초기화한다.
        if (playerCardManager != null)
            playerCardManager.ResetPerks();

        if (aiCardManager != null)
            aiCardManager.ResetPerks();

        StartGame();
    }

    public void StartGame()
    {
        playerTotalScore = 0;
        aiTotalScore = 0;
        currentRound = 0;

        // 특전 목록은 BeginGameSetup()에서 한 번만 초기화한다.
        UpdateScoreUI();
        UpdatePerkDebugUI();
        StartNewRound();
    }

    private void SetupButtons()
    {
        if (stopButton != null)
        {
            stopButton.onClick = new Button.ButtonClickedEvent();
            stopButton.onClick.AddListener(OnPlayerStopButton);
        }

        if (turnEndButton != null)
        {
            turnEndButton.onClick = new Button.ButtonClickedEvent();
            turnEndButton.onClick.AddListener(OnPlayerTurnEndButton);
        }
    }

    private void StartNewRound()
    {
        playerStopped = false;
        aiStopped = false;

        currentRound++;

        if (aiController != null)
            aiController.ResetRoundState();

        UpdatePerkDebugUI();

        currentTurn = TurnOwner.Player;
        ShowPlayerPerkSelectionOrStartTurn();
    }

    private void ShowPlayerPerkSelectionOrStartTurn()
    {
        if (playerCardManager == null
            || playerCardManager.OwnedPerks.Count >= CardManager.MaxPerkCount
            || specialSelectPanel == null)
        {
            StartPlayerTurnAfterPerkSelection();
            return;
        }

        System.Collections.Generic.List<PerkType> candidates =
            new System.Collections.Generic.List<PerkType>();
        foreach (PerkType perk in PerkCatalog.All)
        {
            if (!playerCardManager.HasPerk(perk))
                candidates.Add(perk);
        }

        if (candidates.Count == 0)
        {
            StartPlayerTurnAfterPerkSelection();
            return;
        }

        bool shown = specialSelectPanel.ShowPerkOptions(
            candidates,
            TryAddPlayerPerk,
            _ =>
            {
                StartPlayerTurnAfterPerkSelection();
            });

        if (!shown)
            StartPlayerTurnAfterPerkSelection();
    }

    private bool TryAddPlayerPerk(PerkType perk)
    {
        if (playerCardManager == null || !playerCardManager.TryAddPerk(perk))
            return false;

        Debug.Log($"[Player Perk] Round {currentRound}: {PerkCatalog.GetName(perk)} acquired");
        return true;
    }

    private void StartPlayerTurnAfterPerkSelection()
    {
        UpdatePerkDebugUI();
        StartPlayerTurn();
    }

    private void StartPlayerTurn()
    {
        if (currentPhase == GamePhase.GameOver) return;

        if (playerStopped)
        {
            GoToNextTurn();
            return;
        }

        StartCoroutine(StartPlayerTurnRoutine());
    }

    private IEnumerator StartPlayerTurnRoutine()
    {
        currentPhase = GamePhase.Stop;
        UpdatePhaseUI();

        float duration = aiStopped ? fastTurnBannerDuration : turnBannerDuration;
        yield return ShowTurnBanner("플레이어 턴", duration);

        currentPhase = GamePhase.PlayerTurn;
        UpdatePhaseUI();

        gameInputManager.StartPlayerTurn();
    }

    private void StartAITurn()
    {
        if (currentPhase == GamePhase.GameOver) return;

        if (aiStopped)
        {
            GoToNextTurn();
            return;
        }

        StartCoroutine(StartAITurnRoutine());
    }

    private IEnumerator StartAITurnRoutine()
    {
        currentPhase = GamePhase.Stop;
        UpdatePhaseUI();

        ResetDealerExpression();

        bool fastMode = playerStopped;
        float duration = fastMode ? fastTurnBannerDuration : turnBannerDuration;
        yield return ShowTurnBanner("AI 턴", duration);

        currentPhase = GamePhase.AITurn;
        UpdatePhaseUI();

        aiController.TakeTurn(
            OnAITurnFinished,
            fastMode,
            allowFieldRemovalPerk: !playerStopped);
    }

    private IEnumerator ShowTurnBanner(string message, float duration)
    {
        if (turnBannerObject == null) yield break;

        if (turnBannerImage != null && turnBannerFrameSprite != null)
            turnBannerImage.sprite = turnBannerFrameSprite;

        if (readableTurnBannerText != null)
            readableTurnBannerText.text = message;
        else if (turnBannerText != null)
            turnBannerText.text = message;

        turnBannerObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        turnBannerObject.SetActive(false);
    }

    private void CreateReadableTurnBannerText()
    {
        if (turnBannerObject == null) return;

        // 현재 TMP 폰트에는 한글 글리프가 없어 Windows의 한글 시스템 폰트를 사용한다.
        if (turnBannerText != null) turnBannerText.gameObject.SetActive(false);

        GameObject textObject = new GameObject("Turn Banner Korean Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(turnBannerObject.transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(40f, 25f);
        rect.offsetMax = new Vector2(-40f, -25f);

        readableTurnBannerText = textObject.GetComponent<Text>();
        readableTurnBannerText.font = Font.CreateDynamicFontFromOSFont("Malgun Gothic", 44)
            ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        readableTurnBannerText.fontSize = 44;
        readableTurnBannerText.fontStyle = FontStyle.Bold;
        readableTurnBannerText.alignment = TextAnchor.MiddleCenter;
        readableTurnBannerText.color = Color.white;
        readableTurnBannerText.raycastTarget = false;
    }

    public void OnPlayerTurnEndButton()
    {
        if (currentPhase != GamePhase.PlayerTurn || playerActionButtonsBlocked) return;

        // 같은 프레임의 연속 입력도 다시 진입하지 못하도록 즉시 버튼을 잠근다.
        currentPhase = GamePhase.Stop;
        UpdatePhaseUI();

        if (playerCardManager != null && playerCardManager.HasJokerInHand())
        {
            StartCoroutine(ForcePlayerStopBecauseOfJoker());
            return;
        }

        gameInputManager.CleanupPlayerHandAfterTurn();

        currentTurn = TurnOwner.Player;
        GoToNextTurn();
    }

    private IEnumerator ForcePlayerStopBecauseOfJoker()
    {
        currentPhase = GamePhase.Stop;
        playerStopped = true;
        UpdatePhaseUI();

        int jokerCount = playerCardManager != null
            ? playerCardManager.CountJokersInHand()
            : 0;

        Debug.LogWarning(
            $"손패에 조커 {jokerCount}장이 남아 있어 플레이어가 강제로 Stop됩니다.");

        gameInputManager.CleanupPlayerHandAfterTurn();

        yield return ShowTurnBanner(
            "조커 미사용: 강제 STOP",
            jokerForcedStopMessageDuration);

        currentTurn = TurnOwner.Player;
        GoToNextTurn();
    }

    /// <summary>
    /// 플레이어의 일반 필드 슬롯이 모두 찼을 때 호출합니다.
    /// 마지막 카드를 놓는 즉시 남은 손패를 덱으로 돌려보내고 강제 Stop합니다.
    /// </summary>
    public void ForcePlayerStopBecauseFieldIsFull()
    {
        if (currentPhase != GamePhase.PlayerTurn
            || playerActionButtonsBlocked
            || playerStopped)
        {
            return;
        }

        StartCoroutine(ForcePlayerStopBecauseFieldIsFullRoutine());
    }

    private IEnumerator ForcePlayerStopBecauseFieldIsFullRoutine()
    {
        // 코루틴은 첫 yield 전까지 즉시 실행되므로,
        // 중복 호출이 들어와도 이후 호출은 PlayerTurn 조건을 통과하지 못합니다.
        currentPhase = GamePhase.Stop;
        playerStopped = true;
        UpdatePhaseUI();

        Debug.LogWarning("플레이어 필드 슬롯이 모두 차서 강제로 Stop됩니다.");

        gameInputManager.CleanupPlayerHandAfterTurn();

        yield return ShowTurnBanner(
            "필드 슬롯 가득 참: 강제 STOP",
            fieldFullForcedStopMessageDuration);

        currentTurn = TurnOwner.Player;
        GoToNextTurn();
    }

    public void OnPlayerStopButton()
    {
        if (currentPhase != GamePhase.PlayerTurn || playerActionButtonsBlocked) return;

        playerStopped = true;

        gameInputManager.CleanupPlayerHandAfterTurn();

        currentTurn = TurnOwner.Player;
        GoToNextTurn();
    }

    private void OnAITurnFinished(bool aiChoseStop)
    {
        if (currentPhase != GamePhase.AITurn) return;

        if (aiChoseStop)
            aiStopped = true;

        currentTurn = TurnOwner.AI;
        GoToNextTurn();
    }

    private void GoToNextTurn()
    {
        if (playerStopped && aiStopped)
        {
            StartSettlementPhase();
            return;
        }

        if (currentTurn == TurnOwner.Player)
        {
            if (!aiStopped)
                StartAITurn();
            else
                StartPlayerTurn();
        }
        else
        {
            if (!playerStopped)
                StartPlayerTurn();
            else
                StartAITurn();
        }
    }

    private void StartSettlementPhase()
    {
        StartCoroutine(StartSettlementPhaseRoutine());
    }

    private IEnumerator StartSettlementPhaseRoutine()
    {
        currentPhase = GamePhase.Stop;
        UpdatePhaseUI();

        yield return ShowTurnBanner("정산 중", turnBannerDuration);

        currentPhase = GamePhase.Settlement;
        UpdatePhaseUI();

        int playerEmptySlots = GetEmptySlots(SlotOwner.Player);
        int aiEmptySlots = GetEmptySlots(SlotOwner.Enemy);

        SettlementResult playerResult = playerCardManager.Settle();
        SettlementResult aiResult = aiCardManager.Settle();

        float playerRoundScore = playerCardManager.CalculateScore(playerResult, playerEmptySlots);
        float aiRoundScore = aiCardManager.CalculateScore(aiResult, aiEmptySlots);

        Debug.Log($"Player Round Score: {playerRoundScore}");
        Debug.Log($"AI Round Score: {aiRoundScore}");

        ClearFieldVisuals(SlotOwner.Player);
        ClearFieldVisuals(SlotOwner.Enemy);

        gameInputManager.UpdateGraveVisual();
        aiController.UpdateAIGraveVisual();

        if (scoreClashUI != null)
        {
            float clashPlayerScore = playerRoundScore;
            float clashAiScore = aiRoundScore;

            // 디버그용 임시 코드 : 실제 점수가 0이거나 계산이 안 될 때도 연출을 확인할 수 있게 랜덤값으로 대체
            if (debugUseRandomClashScore)
            {
                clashPlayerScore = UnityEngine.Random.Range(debugRandomScoreRange.x, debugRandomScoreRange.y);
                clashAiScore = UnityEngine.Random.Range(debugRandomScoreRange.x, debugRandomScoreRange.y);
                Debug.Log($"[Debug] Clash test scores - Player: {clashPlayerScore:F0}, AI: {clashAiScore:F0}");
            }

            bool clashDone = false;
            scoreClashUI.PlayClash(clashPlayerScore, clashAiScore, () => clashDone = true);
            yield return new WaitUntil(() => clashDone);
        }

        ApplyRoundScore(playerRoundScore, aiRoundScore);
    }


    // 질문 : 이 함수를 통해 점수 차 누적 값을 빨간색 슬라이드에 더하는 거면 디버그 보다는 실제로 값을 조정하는 게 좋을듯요.

    // 실제로 값을 조정한다는 말이 무슨 뜻인가요? 
    private void ApplyRoundScore(float playerRoundScore, float aiRoundScore)
    {
        int diff = Mathf.RoundToInt(Mathf.Abs(playerRoundScore - aiRoundScore));

        if (playerRoundScore > aiRoundScore)
        {
            SetDealerExpression(dealerAnnoyedSprite);
            playerTotalScore += diff;//이런 값을 얘기하는 건가요?
            Debug.Log($"Player: {diff} points added");
        }
        else if (aiRoundScore > playerRoundScore)
        {
            SetDealerExpression(dealerSmileSprite);
            aiTotalScore += diff;
            Debug.Log($"AI: {diff} points added");
        }
        else
        {
            ResetDealerExpression();
            Debug.Log("No points added.");
        }

        UpdateScoreUI();
        CheckGameOver();
    }

    private void SetDealerExpression(Sprite expression)
    {
        StopDealerExpressionAnimation();

        if (dealerImage == null || expression == null)
        {
            Debug.LogWarning("Dealer expression is not assigned in GameTurnManager.");
            return;
        }

        if (expression == dealerAnnoyedSprite
            && dealerAnnoyedFrames != null
            && dealerAnnoyedFrames.Length > 0)
        {
            dealerExpressionCoroutine = StartCoroutine(PlayDealerExpressionAnimation(
                dealerAnnoyedFrames,
                dealerAnnoyedFrameDuration));
            return;
        }

        if (expression == dealerSmileSprite
            && dealerSmileFrames != null
            && dealerSmileFrames.Length > 0)
        {
            dealerExpressionCoroutine = StartCoroutine(PlayDealerExpressionAnimation(
                dealerSmileFrames,
                dealerSmileFrameDuration));
            return;
        }

        dealerImage.sprite = expression;
    }

    private IEnumerator PlayDealerExpressionAnimation(Sprite[] frames, float secondsPerFrame)
    {
        int frameIndex = 0;
        float frameDuration = Mathf.Max(0.01f, secondsPerFrame);

        while (true)
        {
            Sprite frame = frames[frameIndex];
            if (frame != null)
                dealerImage.sprite = frame;

            frameIndex = (frameIndex + 1) % frames.Length;
            yield return new WaitForSeconds(frameDuration);
        }
    }

    private void StopDealerExpressionAnimation()
    {
        if (dealerExpressionCoroutine == null)
            return;

        StopCoroutine(dealerExpressionCoroutine);
        dealerExpressionCoroutine = null;
    }

    private void ResetDealerExpression()
    {
        SetDealerExpression(dealerDefaultSprite);
    }

    private void CheckGameOver()
    {
        if (playerTotalScore >= targetScore)
        {
            currentPhase = GamePhase.GameOver;
            UpdatePhaseUI();

            Debug.Log("Player wins");

            ShowFinalResult(true);

            return;
        }

        if (aiTotalScore >= targetScore)
        {
            currentPhase = GamePhase.GameOver;
            UpdatePhaseUI();

            Debug.Log("AI win");

            ShowFinalResult(false);

            return;
        }

        StartNewRound();
    }

    private void FindGameOverPanelIfNeeded()
    {
        if (gameOverPannel != null)
            return;

        GameOverPannel[] panels = Resources.FindObjectsOfTypeAll<GameOverPannel>();
        foreach (GameOverPannel panel in panels)
        {
            if (panel != null && panel.gameObject.scene.IsValid())
            {
                gameOverPannel = panel;
                return;
            }
        }
    }

    private void ShowFinalResult(bool playerWon)
    {
        FindGameOverPanelIfNeeded();
        if (gameOverPannel == null)
        {
            Debug.LogWarning("GameOverPannel was not found in the active scene.");
            return;
        }

        Sprite[] resultFrames = playerWon
            ? dealerAnnoyedFrames
            : dealerSmileFrames;
        float resultFrameDuration = playerWon
            ? dealerAnnoyedFrameDuration
            : dealerSmileFrameDuration;

        gameOverPannel.Show(
            playerWon,
            playerTotalScore,
            aiTotalScore,
            resultFrames,
            resultFrameDuration);
    }

    public void ShowDebugFinalResult()
    {
        if (currentPhase == GamePhase.GameOver)
            return;

        currentPhase = GamePhase.GameOver;
        UpdatePhaseUI();

        bool playerWon = playerTotalScore >= aiTotalScore;
        ShowFinalResult(playerWon);
    }

    private int GetEmptySlots(SlotOwner owner)
    {
        int count = 0;

        CardSlot[] slots = FindObjectsOfType<CardSlot>();

        foreach (CardSlot slot in slots)
        {
            if (slot.category != SlotCategory.Field) continue;
            if (slot.owner != owner) continue;

            if (!slot.IsOccupied)
                count++;
        }

        return count;
    }

    // TODO : 애니메이션 수정 필요, 카드가 사라질때 덱으로 돌아가는 애니메이션이 필요
    private void ClearFieldVisuals(SlotOwner owner)
    {
        CardSlot[] slots = FindObjectsOfType<CardSlot>();

        foreach (CardSlot slot in slots)
        {
            if (slot.category != SlotCategory.Field) continue;
            if (slot.owner != owner) continue;
            if (!slot.IsOccupied) continue;

            GameObject cardObj = slot.CurrentCardObject;

            if (cardObj != null)
                Destroy(cardObj);

            slot.ClearSlot();
        }
    }

    // 질문 : 이 값이 목표치에 대한 누적값 == 빨간색 슬라이드 바의 값인지?
    //저는 그렇게 생각했어요. 아니면 백분율로 변환해서 표현할까요?
    private void UpdatePerkDebugUI()
    {
        if (roundText != null)
            roundText.text = $"Round {currentRound}";

        if (aiPerkText != null)
        {
            aiPerkText.text =
                $"AI Perk\n{AIController.FieldRemovalPerkName}";
        }
    }

    private void UpdateScoreUI()
    {
        if (playerTotalScoreText != null)
            playerTotalScoreText.text = $"Player: {playerTotalScore}";

        if (aiTotalScoreText != null)
            aiTotalScoreText.text = $"AI: {aiTotalScore}";

        if (playerScoreSlider != null)
            playerScoreSlider.value = Mathf.Clamp01((float)playerTotalScore / targetScore);

        if (aiScoreSlider != null)
            aiScoreSlider.value = Mathf.Clamp01((float)aiTotalScore / targetScore);
    }

    // 질문 : 이 값이 현재 턴의 상태를 나타내는 값이라면 어디에? 
    private void UpdatePhaseUI()
    {
        if (phaseText != null)
            phaseText.text = currentPhase.ToString();

        UpdatePlayerActionButtons();
    }

    public void SetPlayerActionButtonsBlocked(bool blocked)
    {
        playerActionButtonsBlocked = blocked;
        UpdatePlayerActionButtons();
    }

    private void UpdatePlayerActionButtons()
    {
        bool interactable =
            currentPhase == GamePhase.PlayerTurn && !playerActionButtonsBlocked;

        if (stopButton != null)
            stopButton.interactable = interactable;
        if (turnEndButton != null)
            turnEndButton.interactable = interactable;
    }

    // 질문 : 각 페이즈마다 해야되는 일들 
    // 예를들어 AI턴일 떄 플레이어의 카드움직임을 막고, 버튼을 비활성화 시킨다던지, 플레이어의 턴일 떄 AI의 행동을 막고
    // 플레이어가 먼저 결산 시 AI에게 배속을 걸고
    // 결산 페이즈일 떄는 양쪽 다 조작을 막는게 좋겟죠?

    //ai 배속은 제가 했어요
    //다른 건 다 동의해요. 근데 ai 턴일 때는 어짜피 플레이어의 손에는 아무런 카드가 없으니 플레이어의 카드 움직임을 막는 건 추가할 필요 없지 않을까요?
    private void Update()
    {
        if (currentPhase != GamePhase.PlayerTurn || playerActionButtonsBlocked) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Player: Turn End");
            OnPlayerTurnEndButton();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("Player: Stop");
            OnPlayerStopButton();
        }
    }
}
