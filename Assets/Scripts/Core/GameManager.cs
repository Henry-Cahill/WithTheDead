using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CloneZ
{
    public enum GameState { Playing, GameOver }

    /// <summary>Owns global game state: score, waves, and win/lose flow.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Playing;
        public int ZombieKills { get; private set; }
        public int Wave { get; private set; }
        public float TimeSurvived { get; private set; }
        public string CauseOfDeath { get; private set; } = "";
        public PlayerStats Player { get; internal set; }

        public event Action<int> OnZombieKilled;
        public event Action<int> OnWaveChanged;
        public event Action OnGameOver;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Ground items are drawn instanced from the item store rather than by scene objects of
            // their own, so the world needs exactly one renderer driving that.
            if (GroundItemRenderer.Instance == null) gameObject.AddComponent<GroundItemRenderer>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // A raid can't be quick-saved, and after a death in one the raid report (not R) moves on.
            bool raid = GameSession.IsExtraction;
            if (State == GameState.Playing)
            {
                TimeSurvived += Time.deltaTime;
                if (!raid && Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame) QuickSave();
            }
            else if (!raid && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                Restart();
            }
        }

        public void ReportZombieKill()
        {
            ZombieKills++;
            OnZombieKilled?.Invoke(ZombieKills);
        }

        public void SetWave(int wave)
        {
            Wave = wave;
            OnWaveChanged?.Invoke(wave);
        }

        /// <summary>Restores the run's score/progress from a saved game.</summary>
        public void RestoreProgress(int kills, int wave, float timeSurvived)
        {
            ZombieKills = Mathf.Max(0, kills);
            Wave = Mathf.Max(0, wave);
            TimeSurvived = Mathf.Max(0f, timeSurvived);
            OnZombieKilled?.Invoke(ZombieKills);
            OnWaveChanged?.Invoke(Wave);
        }

        public void TriggerGameOver(string cause)
        {
            if (State == GameState.GameOver) return;
            State = GameState.GameOver;
            CauseOfDeath = cause;
            // Permadeath: the wave run is over, so its save is gone. A raid never touches that save
            // (the raid's own kit is forfeited by the ExtractionManager instead).
            if (!GameSession.IsExtraction) SaveSystem.Delete();
            OnGameOver?.Invoke();
        }

        public void Restart()
        {
            // Reloading the scene lets the bootstrapper rebuild the world from scratch.
            NoiseSystem.Clear();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Writes the current run to disk (F5, and automatically on quit/pause). No-op when
        /// the survivor is dead so a finished run can't be re-saved over its own deletion, and during an
        /// extraction raid (raids are never saved — the kit carried out is kept by <see cref="RaidLoadout"/>).</summary>
        public void QuickSave()
        {
            if (GameSession.IsExtraction) return;
            if (State != GameState.Playing || Player == null || Player.IsDead) return;
            SaveSystem.Save(SaveSystem.Capture());
        }

        private void OnApplicationQuit() => QuickSave();

        private void OnApplicationPause(bool paused)
        {
            if (paused) QuickSave();
        }
    }
}
