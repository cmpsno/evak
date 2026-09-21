using System;
using UnityEngine;

namespace Campusano.Persistence
{
    public enum MookChoice { None, Intimidate, PrivateConversation }
    public enum FatherChoice { None, Lie, Truth, Resignation }

    [Serializable]
    public class JobStateData
    {
        public bool mookJobAssigned;
        public MookChoice mookChoice;
        public FatherChoice fatherChoice;
        public bool mookEncounterComplete;
        public int cashBalance;

        public JobStateData()
        {
            mookJobAssigned = false;
            mookChoice = MookChoice.None;
            fatherChoice = FatherChoice.None;
            mookEncounterComplete = false;
            cashBalance = 0;
        }
    }

    [Serializable]
    public class SaveData
    {
        public int episodeNumber = 1;
        public string currentStepId;
        public JobStateData jobState;
        public long timestampTicks;

        public SaveData()
        {
            jobState = new JobStateData();
        }
    }

    public interface ISaveManager
    {
        SaveData CurrentData { get; }
        bool HasSaveFile { get; }

        void SaveCheckpoint(string stepId);
        bool LoadCheckpoint();
        void ClearSave();
    }

    /// <summary>
    /// Checkpoint persistence for Episode 1. JSON in PlayerPrefs under
    /// "Campusano_Ep1_Checkpoint". Mirrors the legacy static JobState flags so
    /// v0.1 NPC code keeps working, and owns the choice/cash fields.
    /// </summary>
    public class SaveManager : MonoBehaviour, ISaveManager
    {
        public static SaveManager Instance { get; private set; }

        public const string SaveKey = "Campusano_Ep1_Checkpoint";

        public SaveData CurrentData { get; private set; } = new SaveData();
        public bool HasSaveFile => PlayerPrefs.HasKey(SaveKey);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SaveCheckpoint(string stepId)
        {
            var data = new SaveData
            {
                episodeNumber = 1,
                currentStepId = stepId,
                timestampTicks = DateTime.UtcNow.Ticks,
                jobState = new JobStateData
                {
                    // Carry forward in-memory choices/cash; sync the legacy flag.
                    mookJobAssigned = CODClone.Core.JobState.MookJobAssigned,
                    mookChoice = CurrentData.jobState.mookChoice,
                    fatherChoice = CurrentData.jobState.fatherChoice,
                    mookEncounterComplete = CurrentData.jobState.mookEncounterComplete,
                    cashBalance = CurrentData.jobState.cashBalance,
                },
            };
            CurrentData = data;
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
            Debug.Log("[Save] Checkpoint @ " + stepId);
        }

        public bool LoadCheckpoint()
        {
            if (!HasSaveFile) return false;
            try
            {
                string json = PlayerPrefs.GetString(SaveKey);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null || data.jobState == null) return false;
                CurrentData = data;
                // Mirror back onto the legacy static flags.
                CODClone.Core.JobState.MookJobAssigned = data.jobState.mookJobAssigned;
                Debug.Log("[Save] Loaded checkpoint @ " + data.currentStepId);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Corrupt checkpoint, ignoring: " + e.Message);
                return false;
            }
        }

        public void ClearSave()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            CurrentData = new SaveData();
            CODClone.Core.JobState.MookJobAssigned = false;
        }
    }
}
