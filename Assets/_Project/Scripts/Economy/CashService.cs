using System;
using UnityEngine;
using Campusano.Persistence;

namespace Campusano.Economy
{
    /// <summary>TICKET-EP1-01 v2. Locked shape.</summary>
    public interface ICashService
    {
        event Action<int, string> OnCashAwarded;   // (amount, reason)
        int Balance { get; }
        void Award(int amount, string reason);
    }

    public class CashService : MonoBehaviour, ICashService
    {
        [SerializeField] private SaveManager saveManager;

        public event Action<int, string> OnCashAwarded;

        public int Balance => saveManager != null && saveManager.CurrentProfile != null
            ? saveManager.CurrentProfile.jobState.cashBalance
            : 0;

        public void Configure(SaveManager saves) { saveManager = saves; }

        public void Award(int amount, string reason)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[CashService] Non-positive award: {amount}. No-op.");
                return;
            }
            if (saveManager == null || saveManager.CurrentProfile == null)
            {
                Debug.LogError("[CashService] No SaveManager/profile; cannot award.");
                return;
            }
            saveManager.CurrentProfile.jobState.cashBalance += amount;
            saveManager.SaveProfile(saveManager.CurrentProfile);
            Debug.Log($"[Cash] +${amount} ({reason}). Balance: ${Balance}");
            OnCashAwarded?.Invoke(amount, reason);
        }
    }
}
