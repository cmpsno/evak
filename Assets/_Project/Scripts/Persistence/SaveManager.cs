using System;
using UnityEngine;
using Campusano.Missions;

namespace Campusano.Persistence
{
    /// <summary>
    /// TICKET-EP1-01 v2. Per-episode save files + one global profile.
    /// Every checkpoint writes BOTH the episode key and the profile key.
    /// Keys: "Campusano_Ep{n}_Save", "Campusano_Profile".
    /// </summary>
    public class SaveManager : MonoBehaviour, ISaveManager
    {
        public static SaveManager Instance { get; private set; }

        public EpisodeSaveData CurrentEpisodeSave { get; private set; }
        public ProfileData CurrentProfile { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static string EpisodeKey(int episodeNumber) => $"Campusano_Ep{episodeNumber}_Save";
        public const string ProfileKey = "Campusano_Profile";

        public bool HasEpisodeSave(int episodeNumber)
        {
            return PlayerPrefs.HasKey(EpisodeKey(episodeNumber));
        }

        public void SaveEpisodeCheckpoint(int episodeNumber, string stepId, JobStateData snapshot)
        {
            var save = new EpisodeSaveData
            {
                episodeNumber = episodeNumber,
                currentStepId = stepId,
                jobStateSnapshot = snapshot,
                timestampTicks = DateTime.UtcNow.Ticks
            };
            PlayerPrefs.SetString(EpisodeKey(episodeNumber), JsonUtility.ToJson(save));
            CurrentEpisodeSave = save;

            // Every checkpoint also updates the profile (cumulative JobState).
            EnsureProfile();
            CurrentProfile.jobState = snapshot;
            CurrentProfile.lastPlayedTicks = save.timestampTicks;
            SaveProfile(CurrentProfile);

            PlayerPrefs.Save();
            Debug.Log($"[Save] Checkpoint ep{episodeNumber} @ {stepId} (+profile).");
        }

        public void SaveProfile(ProfileData profile)
        {
            CurrentProfile = profile ?? CurrentProfile;
            if (CurrentProfile == null) return;
            CurrentProfile.lastPlayedTicks = DateTime.UtcNow.Ticks;
            PlayerPrefs.SetString(ProfileKey, JsonUtility.ToJson(CurrentProfile));
            PlayerPrefs.Save();
        }

        public bool LoadEpisodeSave(int episodeNumber)
        {
            string key = EpisodeKey(episodeNumber);
            if (!PlayerPrefs.HasKey(key)) return false;
            try
            {
                CurrentEpisodeSave = JsonUtility.FromJson<EpisodeSaveData>(PlayerPrefs.GetString(key));
                return CurrentEpisodeSave != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Failed to parse {key}: {e.Message}");
                return false;
            }
        }

        public bool LoadProfile()
        {
            if (!PlayerPrefs.HasKey(ProfileKey))
            {
                CurrentProfile = NewProfile();
                return false;
            }
            try
            {
                CurrentProfile = JsonUtility.FromJson<ProfileData>(PlayerPrefs.GetString(ProfileKey));
                if (CurrentProfile == null) CurrentProfile = NewProfile();
                if (CurrentProfile.jobState == null) CurrentProfile.jobState = new JobStateData();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Failed to parse {ProfileKey}: {e.Message}");
                CurrentProfile = NewProfile();
                return false;
            }
        }

        public void ClearEpisodeSave(int episodeNumber)
        {
            PlayerPrefs.DeleteKey(EpisodeKey(episodeNumber));
            PlayerPrefs.Save();
        }

        public void ClearAll()
        {
            for (int ep = 1; ep <= 6; ep++) PlayerPrefs.DeleteKey(EpisodeKey(ep));
            PlayerPrefs.DeleteKey(ProfileKey);
            PlayerPrefs.Save();
            CurrentEpisodeSave = null;
            CurrentProfile = null;
        }

        /// <summary>
        /// Exact load flow (Section 2.4): profile -> episode save -> silent restore.
        /// Listeners fast-forward via OnStepRestored. OnStepChanged never fires.
        /// </summary>
        public bool LoadEpisode(int episodeNumber, MissionStateMachine missions)
        {
            LoadProfile(); // seeds JobState; creates empty profile if none
            if (!LoadEpisodeSave(episodeNumber)) return false;

            Ep1Step step;
            try { step = (Ep1Step)Enum.Parse(typeof(Ep1Step), CurrentEpisodeSave.currentStepId); }
            catch
            {
                Debug.LogError($"[Save] Unknown step id '{CurrentEpisodeSave.currentStepId}' in ep{episodeNumber} save.");
                return false;
            }

            missions.BeginLoad();
            try { missions.RestoreToStep(step); }
            finally { missions.EndLoad(); }
            return true;
        }

        private void EnsureProfile()
        {
            if (CurrentProfile == null) CurrentProfile = NewProfile();
            if (CurrentProfile.jobState == null) CurrentProfile.jobState = new JobStateData();
        }

        private static ProfileData NewProfile()
        {
            return new ProfileData
            {
                jobState = new JobStateData(),
                highestEpisodeCompleted = 0,
                lastPlayedTicks = DateTime.UtcNow.Ticks
            };
        }
    }
}
