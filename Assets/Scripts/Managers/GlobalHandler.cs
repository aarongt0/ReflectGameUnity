using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GlobalHandler : MonoBehaviour
{
    public static GlobalHandler I;

    public Dictionary<string, MoveAsset> MoveByHitboxId = new();
    private HashSet<string> uniqueHitboxIds;

    public GameObject p1;
    public GameObject p2;

    public GameObject botPrefab;

    private float START_DELAY = 1f;

    [SerializeField] UIManager ui;

    public bool hasBot = false;

    public Transform[] SpawnPoints;
    public int playerIndex = 0;

    // Hardcoded -- make dynamic if/when customization is implemented
    private string PlayerOneName = "Blue Player"; 
    private string PlayerTwoName = "Red Player"; 

    private Color p1Color = Color.blue;
    private Color p2Color = Color.red;

    private bool isFrozen = false;


    public static event Action RestartGame;
    public static event Action EndGame;
    public static event Action GameStart;

    private float END_TIME = 3f;

    private void Log(string msg)
    {
        string newMsg = $"[GlobalHandler]: {msg}";
        Debug.Log(newMsg);
    }
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        if (hasBot) return;

        Log("Player Joined");
        playerInput.transform.position = SpawnPoints[playerIndex].transform.position;
        if (playerIndex == 0)
        {
            Log("Attempting to assign player one");
            p1 = playerInput.gameObject;
            p1.GetComponentInChildren<SpriteRenderer>().color = p1Color;
            p1.name = PlayerOneName;
            ui.leftColor = p1Color;
            ui.p1Respawn = p1.GetComponent<PlayerRespawn>();
        }
        else if (playerIndex == 1 && !hasBot)
        {
            p2 = playerInput.gameObject;
            p2.GetComponentInChildren<SpriteRenderer>().color = p2Color;
            p2.name = PlayerTwoName;
            ui.rightColor = p2Color;
            ui.p2Respawn = p2.GetComponent<PlayerRespawn>();

            if (p1 != null && p2 != null)
            {
                Log("Both Players Joined, Initializing Game");
                InitPlayers();
            }
        }
        playerIndex++;
        ui.playerCount = playerIndex;
        ui.UpdateJoinUI();
    }

    private void OnEnable()
    {
        PlayerRespawn.PlayerLoss += HandlePlayerLoss;
    }

    private void OnDisable()
    {
        PlayerRespawn.PlayerLoss -= HandlePlayerLoss;
    }

    private void HandlePlayerLoss()
    {
        bool p1Win = p1.GetComponent<PlayerRespawn>().stocks > p2.GetComponent<PlayerRespawn>().stocks;
        string winnerName;
        Color winnerColor;

        if (p1Win)
        {
            winnerName = p1.name;
            winnerColor = p1Color;
        }
        else
        {
            winnerName = p2.name;
            winnerColor = p2Color;
        }
        Log($"{winnerName} Wins!");

        ui.winnerName = winnerName;
        ui.winnerColor = winnerColor;
        StartCoroutine(EndGameRoutine());
    }

    IEnumerator EndGameRoutine()
    {
        EndGame?.Invoke();

        p2.GetComponent<PlayerController>().movementLocked = true;
        p1.GetComponent<PlayerController>().movementLocked = true;


        ToggleTimeFreeze();

        yield return new WaitForSecondsRealtime(END_TIME); 

        ToggleTimeFreeze();

        // Reset Player spawns and unlock them

        p1.GetComponent<PlayerRespawn>().ForceRespawn();
        p2.GetComponent<PlayerRespawn>().ForceRespawn();

        RestartGame?.Invoke();

        StartCoroutine(StartGameRoutine());
    }

    IEnumerator StartGameRoutine()
    {
        yield return new WaitForSecondsRealtime(START_DELAY);

        StartGame();
    }


    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

    }

    private void Start()
    {
        if (hasBot)
        {
            InitPlayers();
        }

        Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
        Cursor.visible = false;
    }

    private void InitPlayers()
    {
        uniqueHitboxIds = new();

        if (p1 == null || p2 == null)
        {
            Log("ONE OR MORE PLAYERS NOT ASSIGNED!!!");
            return;
        }

        CreateHitboxDict(p1); CreateHitboxDict(p2);
        Log("Hitbox Dicts created");

        ui.p1Health = p1.GetComponent<Health>();
        ui.p2Health = p2.GetComponent<Health>();

        p1.GetComponent<PlayerCombat>().moveByHitboxId = MoveByHitboxId;
        p2.GetComponent<PlayerCombat>().moveByHitboxId = MoveByHitboxId;

        p1.GetComponent<PlayerRespawn>().spawnPoint = SpawnPoints[0];
        p2.GetComponent<PlayerRespawn>().spawnPoint = SpawnPoints[1];

        p1.GetComponentInChildren<Hurtbox>().id = PlayerOneName;
        p2.GetComponentInChildren<Hurtbox>().id = PlayerTwoName;

        p1.GetComponent<PlayerCombat>().Team = PlayerOneName;
        p2.GetComponent<PlayerCombat>().Team = PlayerTwoName;



        Log("All preparations done, starting game...");
        StartCoroutine(StartGameRoutine());
    }

    private void CreateHitboxDict(GameObject player) // Initialize a hitbox dict given both player's equipped moves
    {
        if (player == null) return;
        var pcs = player.GetComponent<PlayerCombat>();
        if (pcs == null) return;

        var moveSets = pcs.GetMoveSets();

        foreach (MoveSet set in moveSets)
        {
            if (set == null) continue;
            foreach (MoveAsset mAsset in set.moveList)
            {
                foreach (HitboxEvent e in mAsset.hitboxEvents)
                {
                    if (!uniqueHitboxIds.Contains(e.hitboxId))
                    {
                        uniqueHitboxIds.Add(e.hitboxId);
                        MoveByHitboxId[e.hitboxId] = mAsset;
                    }
                }
            }
        }
    }

    private void StartGame()
    {
        GameStart?.Invoke();
        p2.GetComponent<PlayerController>().movementLocked = false;
        p1.GetComponent<PlayerController>().movementLocked = false;

        Log("Set both players movementLocked to false");
        return;
    }
    public void ToggleTimeFreeze()
    {
        isFrozen = !isFrozen;

        if (isFrozen)
        {
            Time.timeScale = 0f; // Freezes time
        }
        else
        {
            Time.timeScale = 1f; // Resumes time
        }
    }
}
