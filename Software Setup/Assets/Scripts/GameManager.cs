using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI; // for button label update

public class GameManager : MonoBehaviour
{
    public static GameManager I;

    // Persist next mode choice across scene reloads
    private static bool modeInitialized = false;
    public static bool nextModeTimeTrial = false;

    [Header("References")]
    public Transform playerBall;
    public TextMeshProUGUI scoreText;      // visible in Normal Mode only
    public TextMeshProUGUI timerText;      // visible in Time Trial only
    public TextMeshProUGUI remainingText;  // visible in Time Trial only
    public TextMeshProUGUI messageText;    // intro + win/lose text
    public GameObject messagePanel;        // NEW: parent object containing background + messageText
    public CameraShake cameraShake;        // optional

    [Header("Modes")]
    public bool timeTrialMode = false;     // Inspector default for first boot

    [Header("Score Settings (Normal Mode)")]
    public int score = 0;
    public int targetScore = 20;
    public bool winOnClearBoard = true;

    [Header("Time Trial Settings")]
    public float hazardTimePenalty = 2.0f;     // seconds added on hazard
    public string bestTimePlayerPrefKey = "BestTime_01";

    [Header("Hazard Settings (Normal Mode)")]
    public float hazardInvulnTime = 1.0f; // brief invulnerability after hazard hit
    public bool gameOverAtZero = false;
    public int hazardPenaltyDefault = 3;

    [Header("Lose Condition")]
    public float fallY = -5f;

    // pickup tracking
    private int totalPickups = 0;
    private int remainingPickups = 0;

    // runtime state
    private bool ended = false;
    private bool messageHiddenAfterStart = false; // hide intro on first input
    private bool timerRunning = false;
    private float elapsed = 0f;

    // hazard lock for Normal Mode
    private bool hazardLocked = false;
    private float hazardLockTimer = 0f;

    void Awake()
    {
        I = this;

        // On first boot, adopt Inspector setting as default.
        // Afterwards, use the persisted choice across reloads.
        if (!modeInitialized)
        {
            nextModeTimeTrial = timeTrialMode;
            modeInitialized = true;
        }
        timeTrialMode = nextModeTimeTrial;
    }

    void Start()
    {
        if (!cameraShake && Camera.main)
            cameraShake = Camera.main.GetComponent<CameraShake>();

        UpdateAllUI();
        WriteIntroIfNeeded(); // shows panel + intro text

        // Initial UI visibility based on mode
        if (scoreText) scoreText.gameObject.SetActive(!timeTrialMode);
        if (timerText) timerText.gameObject.SetActive(timeTrialMode);
        if (remainingText) remainingText.gameObject.SetActive(timeTrialMode);

        UpdateModeButtonLabel();
    }

    void Update()
    {
        // reload manually
        if (Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        // Hide intro panel on first input, start timer
        if (!messageHiddenAfterStart)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f || Input.anyKeyDown)
            {
                if (messagePanel) messagePanel.SetActive(false); // NEW: hide whole panel
                messageHiddenAfterStart = true;
                timerRunning = true;
            }
        }

        // hazard invulnerability (Normal Mode only)
        if (!timeTrialMode && hazardLocked)
        {
            hazardLockTimer -= Time.unscaledDeltaTime;
            if (hazardLockTimer <= 0f) hazardLocked = false;
        }

        // timer tick
        if (!ended && timerRunning)
        {
            elapsed += Time.deltaTime;
            UpdateTimerUI();
        }

        // fall lose
        if (!ended && playerBall && playerBall.position.y < fallY)
            GameOver("You fell! Press R to restart.");
    }

    // --- Mode toggle that reloads the scene cleanly ---
    public void ToggleModeAndReload()
    {
        nextModeTimeTrial = !timeTrialMode; // flip and persist
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // -------------------------
    // Public API (called by Spawner / Pickup / Hazard)
    // -------------------------
    public void RegisterTotalPickups(int count)
    {
        totalPickups = Mathf.Max(0, count);
        remainingPickups = totalPickups;
        UpdateRemainingUI();
    }

    public void OnPickupConsumed()
    {
        if (remainingPickups > 0) remainingPickups--;
        UpdateRemainingUI();

        if (timeTrialMode)
        {
            if (!ended && remainingPickups == 0)
                WinTimeTrial();
        }
        else
        {
            if (!ended && winOnClearBoard && remainingPickups == 0)
                Win("Perfect Clear! Press R to play again.");
        }
    }

    public void AddScore(int add = 1)
    {
        if (ended) return;
        if (timeTrialMode) return; // ignore score in Time Trial

        score = Mathf.Max(0, score + add);
        UpdateScoreUI();

        if (score >= targetScore)
            Win("You Win! Press R to play again.");
    }

    public void OnHazardHit(int penalty)
    {
        if (ended) return;

        if (timeTrialMode)
        {
            elapsed += Mathf.Max(0f, hazardTimePenalty);
            UpdateTimerUI();
            if (cameraShake) cameraShake.TriggerShake(0.2f, 0.15f, 2.0f);
        }
        else
        {
            if (hazardLocked) return;

            int p = penalty != 0 ? Mathf.Abs(penalty) : Mathf.Abs(hazardPenaltyDefault);
            score = Mathf.Max(0, score - p);
            UpdateScoreUI();

            if (cameraShake) cameraShake.TriggerShake(0.25f, 0.15f, 2.5f);

            hazardLocked = true;
            hazardLockTimer = Mathf.Max(0.05f, hazardInvulnTime);

            if (gameOverAtZero && score <= 0)
                GameOver("Score depleted. Press R to restart.");
        }
    }

    // -------------------------
    // Win / Lose
    // -------------------------
    void WinTimeTrial()
    {
        ended = true;
        timerRunning = false;

        string timeStr = FormatTime(elapsed);
        string bestStr = "";

        float best = PlayerPrefs.GetFloat(bestTimePlayerPrefKey, -1f);
        if (best < 0f || elapsed < best)
        {
            PlayerPrefs.SetFloat(bestTimePlayerPrefKey, elapsed);
            PlayerPrefs.Save();
            bestStr = "\nNew Best!";
        }
        else
        {
            bestStr = $"\nBest: {FormatTime(best)}";
        }

        if (messagePanel) messagePanel.SetActive(true); // NEW: show panel again
        if (messageText) messageText.text = $"Perfect Clear! Time: {timeStr}{bestStr}\nPress R to play again.";
        Time.timeScale = 0f;
    }

    public void GameOver(string msg = "Game Over.")
    {
        ended = true;
        timerRunning = false;
        if (messagePanel) messagePanel.SetActive(true); // NEW: show panel again
        if (messageText) messageText.text = msg + " Press R to restart.";
        Time.timeScale = 0f;
    }

    public void Win(string msg = "You Win!")
    {
        ended = true;
        timerRunning = false;
        if (messagePanel) messagePanel.SetActive(true); // NEW: show panel again
        if (messageText) messageText.text = msg;
        Time.timeScale = 0f;
    }

    // -------------------------
    // UI helpers
    // -------------------------
    void UpdateAllUI()
    {
        UpdateScoreUI();
        UpdateTimerUI();
        UpdateRemainingUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText) scoreText.text = $"Score: {score}/{targetScore}";
    }

    void UpdateTimerUI()
    {
        if (!timerText) return;
        if (!timeTrialMode)
        {
            timerText.text = "";
            return;
        }
        timerText.text = $"Time: {FormatTime(elapsed)}";
    }

    void UpdateRemainingUI()
    {
        if (!remainingText) return;
        if (!timeTrialMode || totalPickups <= 0)
        {
            remainingText.text = timeTrialMode ? "Remaining: 0/0" : "";
            return;
        }
        remainingText.text = $"Remaining: {remainingPickups}/{totalPickups}";
    }

    void WriteIntroIfNeeded()
    {
        if (!messageText) return;

        // ensure panel visible at start
        if (messagePanel) messagePanel.SetActive(true);

        if (!timeTrialMode)
        {
            messageText.text =
                "Mode: Normal\nReach the target score or clear all pickups.\nWASD/Arrows to tilt. Hold Shift for precision. Press R to restart.";
        }
        else
        {
            messageText.text =
                "Mode: Time Trial\nClear all pickups as fast as possible!\nWASD/Arrows to tilt. Hold Shift for precision. Press R to restart.";
        }
    }

    void UpdateModeButtonLabel()
    {
        GameObject btn = GameObject.Find("ModeToggleButton");
        if (!btn) return;
        TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt) txt.text = timeTrialMode ? "Mode: Time Trial" : "Mode: Normal";
    }

    string FormatTime(float t)
    {
        if (t < 0f) t = 0f;
        int minutes = Mathf.FloorToInt(t / 60f);
        float seconds = t - minutes * 60f;
        return $"{minutes:00}:{seconds:00.00}";
    }
}
