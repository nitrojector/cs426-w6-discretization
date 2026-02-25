using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    // Hardcoded level order (scene names must be in Build Settings).
    // You can also use scene paths, but SceneManager.LoadScene(string) typically uses the name.
    [SerializeField] private string[] _levels = new[]
    {
        "level0",
    };

    [SerializeField] private string _winSceneName = "Win"; // optional: where to go on win

    public int CurrentLevelIndex { get; private set; } = 0;
    public int LevelCount => _levels?.Length ?? 0;

    public event Action<int, string> OnLevelLoaded; // (index, sceneName)
    public event Action<int, string> OnLevelReset; // (index, sceneName)
    public event Action OnWinReached;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // If you want it to auto-sync to whatever scene is currently open on first boot:
        SyncIndexToActiveSceneIfPossible();
    }

    private void SyncIndexToActiveSceneIfPossible()
    {
        string active = SceneManager.GetActiveScene().name;
        int idx = IndexOfLevel(active);
        if (idx >= 0) CurrentLevelIndex = idx;
    }

    public void LoadCurrentLevel()
    {
        EnsureValidLevels();
        string scene = _levels[CurrentLevelIndex];
        SceneManager.LoadScene(scene);
        OnLevelLoaded?.Invoke(CurrentLevelIndex, scene);
    }

    public void LoadLevel(int index)
    {
        EnsureValidLevels();

        if (index < 0 || index >= _levels.Length)
            throw new ArgumentOutOfRangeException(nameof(index),
                $"Index {index} out of range [0..{_levels.Length - 1}]");

        CurrentLevelIndex = index;

        string scene = _levels[CurrentLevelIndex];
        SceneManager.LoadScene(scene);
        OnLevelLoaded?.Invoke(CurrentLevelIndex, scene);
    }

    public void ResetLevel()
    {
        EnsureValidLevels();

        string scene = _levels[CurrentLevelIndex];
        SceneManager.LoadScene(scene);
        OnLevelReset?.Invoke(CurrentLevelIndex, scene);
    }

    /// <summary>
    /// Advances to the next level. If already at last level, triggers win.
    /// </summary>
    public void NextLevel()
    {
        EnsureValidLevels();

        if (CurrentLevelIndex + 1 >= _levels.Length)
        {
            OnWin();
            return;
        }

        CurrentLevelIndex++;
        string scene = _levels[CurrentLevelIndex];
        SceneManager.LoadScene(scene);
        OnLevelLoaded?.Invoke(CurrentLevelIndex, scene);
    }

    /// <summary>
    /// Called when NextLevel is invoked on the last level.
    /// </summary>
    public void OnWin()
    {
        OnWinReached?.Invoke();

        // Optional: load a win scene if provided.
        if (!string.IsNullOrWhiteSpace(_winSceneName))
        {
            SceneManager.LoadScene(_winSceneName);
        }
    }

    private int IndexOfLevel(string sceneName)
    {
        if (_levels == null) return -1;
        for (int i = 0; i < _levels.Length; i++)
        {
            if (string.Equals(_levels[i], sceneName, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    private void EnsureValidLevels()
    {
        if (_levels == null || _levels.Length == 0)
            throw new InvalidOperationException("LevelManager has no levels configured.");
    }
}