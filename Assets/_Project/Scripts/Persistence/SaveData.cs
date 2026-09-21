using System;
using UnityEngine;

namespace Campusano.Persistence
{
    /// <summary>TICKET-EP1-01 v2. Locked save shapes — do not add/rename fields.</summary>
    public enum MookChoice { None, Intimidate, PrivateConversation }
    public enum FatherChoice { None, Lie, Truth, Resignation }
    public enum CReadChoice { None, Impressed, Suspicious, Noncommittal }        // Ep 2
    public enum SitDownPosition { None, Shoulder, BackWall, Sideboard }           // Ep 2

    [Serializable]
    public class JobStateData
    {
        // Ep 1
        public bool mookJobAssigned;
        public MookChoice mookChoice;
        public FatherChoice fatherChoice;
        public bool mookEncounterComplete;

        // Ep 2 (typed now so future reads/writes stay typed)
        public CReadChoice cReadChoice;
        public bool ep2SitDownCompleted;
        public SitDownPosition ep2SitDownPosition;
        public bool ep2SacksEnvelopeAccepted;

        public int cashBalance;
    }

    [Serializable]
    public class EpisodeSaveData
    {
        public int episodeNumber;
        public string currentStepId;             // e.g. "Ep1_M2_S1_BackRoomSetup"
        public JobStateData jobStateSnapshot;    // full snapshot at save time
        public long timestampTicks;
    }

    [Serializable]
    public class ProfileData
    {
        public JobStateData jobState;            // cumulative across episodes
        public int highestEpisodeCompleted;
        public long lastPlayedTicks;
    }

    public interface ISaveManager
    {
        EpisodeSaveData CurrentEpisodeSave { get; }
        ProfileData CurrentProfile { get; }
        bool HasEpisodeSave(int episodeNumber);

        void SaveEpisodeCheckpoint(int episodeNumber, string stepId, JobStateData snapshot);
        void SaveProfile(ProfileData profile);

        bool LoadEpisodeSave(int episodeNumber);   // populates CurrentEpisodeSave
        bool LoadProfile();                        // populates CurrentProfile

        void ClearEpisodeSave(int episodeNumber);
        void ClearAll();
    }
}
