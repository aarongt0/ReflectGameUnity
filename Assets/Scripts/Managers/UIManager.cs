using UnityEngine;
using UnityEngine.UIElements;
using System;

public class UIManager : MonoBehaviour
{
    public Health p1Health;
    public Health p2Health;
    public PlayerRespawn p1Respawn;
    public PlayerRespawn p2Respawn;

    public Color leftColor;
    public Color rightColor;

    public Color winnerColor;

    public string winnerName = "";

    public int playerCount = 0;

    // Join UI elems
    [SerializeField] UIDocument joinUi;
    Label joinText;

    // Play UI elems
    [SerializeField] UIDocument playUi;
    Label textLeft;
    Label textRight;
    Label livesLeft;
    Label livesRight;

    // GameOver UI elems
    [SerializeField] UIDocument gameOverUi;
    Label winnerText;

    public string p1hp;
    public string p2hp;
    public enum GameState
    {
        Joining,
        Playing,
        GameOver
    }

    public GameState currentState;

    private void Start()
    {
        // Joining Components
        joinText = joinUi.rootVisualElement.Q<Label>("Join");

        // Playing Components
        VisualElement playRoot = playUi.rootVisualElement;
        textLeft = playRoot.Q<Label>("TextLeft");
        textRight = playRoot.Q<Label>("TextRight");
        livesLeft = playRoot.Q<Label>("LivesLeft");
        livesRight = playRoot.Q<Label>("LivesRight");

        // GameOver Componenets
        VisualElement gameOverRoot = gameOverUi.rootVisualElement;
        winnerText = gameOverRoot.Q<Label>("Winner");

        currentState = GameState.Joining;
        SwitchGameState(currentState);
    }
    private void Log(string msg)
    {
        Debug.Log($"[UI Manager]: {msg}");
    }

    private void OnEnable()
    {
        GlobalHandler.EndGame += HandleGameOver;
        GlobalHandler.RestartGame += HandleGameRestart;
        GlobalHandler.GameStart += HandleGameStart;
        Health.UpdateUI += HandleUpdateUI;
    }

    private void OnDisable()
    {
        GlobalHandler.EndGame -= HandleGameOver;
        GlobalHandler.RestartGame -= HandleGameRestart;
        GlobalHandler.GameStart -= HandleGameStart;
        Health.UpdateUI -= HandleUpdateUI;
    }
    private void UpdatePlayingUi()
    {
        p1hp = p1Health.healthPercent.ToString("F0");
        p2hp = p2Health.healthPercent.ToString("F0");
        textLeft.text = $"%{p1hp}";
        textRight.text = $"%{p2hp}";

        string leftStocks = "";
        string rightStocks = "";

        for (int i = 0; i < p1Respawn.stocks; i++)
        {
            leftStocks += ".";
        }
        for (int i = 0; i < p2Respawn.stocks; i++)
        {
            rightStocks += ".";
        }

        livesLeft.text = leftStocks;
        livesRight.text = rightStocks;

        Log($"P1 Stocks: {leftStocks}");
        Log($"P2 Stocks: {rightStocks}");
    }

    private void UpdateWinnerUI()
    {
        winnerText.style.color = winnerColor;
        winnerText.text = $"{winnerName} Wins!";
    }


    public void UpdateJoinUI()
    {
        joinText.text = $"Press any button to join ({playerCount.ToString()}/2)";
    }

    private void SetPlayUI()
    {
        textRight.style.color = rightColor;
        textLeft.style.color = leftColor;
        livesLeft.style.color = leftColor;
        livesRight.style.color = rightColor;

        UpdatePlayingUi();
    }

    private void HandleUpdateUI()
    {
        if (currentState == GameState.Playing) UpdatePlayingUi();
    }

    // Hide all UIs, show switched one
    public void SwitchGameState(GameState gameState)
    {
        if (currentState == gameState)
            Log("Attempted to switch game state to current game state.");

        SetVisible(joinUi, false);
        SetVisible(playUi, false);
        SetVisible(gameOverUi, false);

        switch (gameState)
        {
            case GameState.Joining:
                SetVisible(joinUi, true);
                UpdateJoinUI();
                break;
            case GameState.Playing:
                SetVisible(playUi, true);
                SetPlayUI();
                break;
            case GameState.GameOver:
                SetVisible(gameOverUi, true);
                UpdateWinnerUI();
                break;
            default:
                Log("Unknown Game State");
                break;
        }

        currentState = gameState;
        Log($"Game State set to: {currentState}");
    }
    private void HandleGameStart()
    {
        SwitchGameState(GameState.Playing);
    }
    private void HandleGameOver()
    {
        SwitchGameState(GameState.GameOver);
    }

    private void HandleGameRestart()
    {
        winnerName = "";
        SwitchGameState(GameState.Playing);
    }
    private void SetVisible(UIDocument doc, bool visible)
    {
        doc.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
