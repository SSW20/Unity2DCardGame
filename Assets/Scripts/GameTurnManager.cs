using TMPro;
using UnityEngine;

public enum TurnOwner
{
    Player,
    AI
}

public enum GamePhase
{
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

    private TurnOwner currentTurn;
    private GamePhase currentPhase;

    private bool playerStopped = false;
    private bool aiStopped = false;

    private int playerTotalScore = 0;
    private int aiTotalScore = 0;

    private void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        playerTotalScore = 0;
        aiTotalScore = 0;

        UpdateScoreUI();
        StartNewRound();
    }

    private void StartNewRound()
    {
        playerStopped = false;
        aiStopped = false;

        if (aiController != null)
            aiController.ResetRoundState();

        currentTurn = TurnOwner.Player;
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

        currentPhase = GamePhase.AITurn;
        UpdatePhaseUI();

        bool fastMode = playerStopped;
        aiController.TakeTurn(OnAITurnFinished, fastMode);
    }

    public void OnPlayerTurnEndButton()
    {
        if (currentPhase != GamePhase.PlayerTurn) return;

        gameInputManager.CleanupPlayerHandAfterTurn();

        currentTurn = TurnOwner.Player;
        GoToNextTurn();
    }

    public void OnPlayerStopButton()
    {
        if (currentPhase != GamePhase.PlayerTurn) return;

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

        ApplyRoundScore(playerRoundScore, aiRoundScore);
    }

    private void ApplyRoundScore(float playerRoundScore, float aiRoundScore)
    {
        int diff = Mathf.RoundToInt(Mathf.Abs(playerRoundScore - aiRoundScore));

        if (playerRoundScore > aiRoundScore)
        {
            playerTotalScore += diff;
            Debug.Log($"플레이어가 {diff}점 획득");
        }
        else if (aiRoundScore > playerRoundScore)
        {
            aiTotalScore += diff;
            Debug.Log($"AI가 {diff}점 획득");
        }
        else
        {
            Debug.Log("동점입니다. 점수 획득 없음");
        }

        UpdateScoreUI();
        CheckGameOver();
    }

    private void CheckGameOver()
    {
        if (playerTotalScore >= targetScore)
        {
            currentPhase = GamePhase.GameOver;
            UpdatePhaseUI();

            Debug.Log("플레이어 승리");

            if (gameOverPannel != null)
                gameOverPannel.Show();

            return;
        }

        if (aiTotalScore >= targetScore)
        {
            currentPhase = GamePhase.GameOver;
            UpdatePhaseUI();

            Debug.Log("플레이어 패배");

            if (gameOverPannel != null)
                gameOverPannel.Show();

            return;
        }

        StartNewRound();
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

    private void UpdateScoreUI()
    {
        if (playerTotalScoreText != null)
            playerTotalScoreText.text = $"Player: {playerTotalScore}";

        if (aiTotalScoreText != null)
            aiTotalScoreText.text = $"AI: {aiTotalScore}";
    }

    private void UpdatePhaseUI()
    {
        if (phaseText != null)
            phaseText.text = currentPhase.ToString();
    }

    private void Update()
    {
        if (currentPhase != GamePhase.PlayerTurn) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("테스트 입력: Turn End");
            OnPlayerTurnEndButton();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("테스트 입력: Stop");
            OnPlayerStopButton();
        }
    }
}