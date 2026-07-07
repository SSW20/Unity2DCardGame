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

    [Header("Buttons")]
    [SerializeField] private Button stopButton;
    [SerializeField] private Button turnEndButton;

    private TurnOwner currentTurn;
    private GamePhase currentPhase;

    private bool playerStopped = false;
    private bool aiStopped = false;

    private int playerTotalScore = 0;
    private int aiTotalScore = 0;

    private void Start()
    {
        SetupButtons();
        StartGame();
    }

    public void StartGame()
    {
        playerTotalScore = 0;
        aiTotalScore = 0;

        UpdateScoreUI();
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

    // TODO : 결산페이즈에서의 딜레이가 필요
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


    // 질문 : 이 함수를 통해 점수 차 누적 값을 빨간색 슬라이드에 더하는 거면 디버그 보다는 실제로 값을 조정하는 게 좋을듯요.
    
    // 실제로 값을 조정한다는 말이 무슨 뜻인가요? 
    private void ApplyRoundScore(float playerRoundScore, float aiRoundScore)
    {
        int diff = Mathf.RoundToInt(Mathf.Abs(playerRoundScore - aiRoundScore));

        if (playerRoundScore > aiRoundScore)
        {
            playerTotalScore += diff;//이런 값을 얘기하는 건가요?
            Debug.Log($"Player: {diff} points added");
        }
        else if (aiRoundScore > playerRoundScore)
        {
            aiTotalScore += diff;
            Debug.Log($"AI: {diff} points added");
        }
        else
        {
            Debug.Log("No points added.");
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

            Debug.Log("Player wins");

            if (gameOverPannel != null)
                gameOverPannel.Show();

            return;
        }

        if (aiTotalScore >= targetScore)
        {
            currentPhase = GamePhase.GameOver;
            UpdatePhaseUI();

            Debug.Log("AI win");

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
    private void UpdateScoreUI()
    {
        if (playerTotalScoreText != null)
            playerTotalScoreText.text = $"Player: {playerTotalScore}";

        if (aiTotalScoreText != null)
            aiTotalScoreText.text = $"AI: {aiTotalScore}";
    }

    // 질문 : 이 값이 현재 턴의 상태를 나타내는 값이라면 어디에? 
    private void UpdatePhaseUI()
    {
        if (phaseText != null)
            phaseText.text = currentPhase.ToString();
    }

    // 질문 : 각 페이즈마다 해야되는 일들 
    // 예를들어 AI턴일 떄 플레이어의 카드움직임을 막고, 버튼을 비활성화 시킨다던지, 플레이어의 턴일 떄 AI의 행동을 막고
    // 플레이어가 먼저 결산 시 AI에게 배속을 걸고
    // 결산 페이즈일 떄는 양쪽 다 조작을 막는게 좋겟죠?
    
    //ai 배속은 제가 했어요
    //다른 건 다 동의해요. 근데 ai 턴일 때는 어짜피 플레이어의 손에는 아무런 카드가 없으니 플레이어의 카드 움직임을 막는 건 추가할 필요 없지 않을까요?
    private void Update()
    {
        if (currentPhase != GamePhase.PlayerTurn) return;

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