using System;
using UnityEngine;

namespace SeatingChartApp.Runtime.Systems
{
    /// <summary>
    /// Central authority for tracking the current user role.  Other
    /// components such as the UI and SeatController query this manager
    /// to determine whether admin‑only functionality should be enabled.
    /// Singleton pattern is used to ensure a single source of truth.
    /// </summary>
    public class UserRoleManager : MonoBehaviour
    {
        public static UserRoleManager Instance { get; private set; }

        /// <summary>
        /// Defines the available roles in the system.  Additional roles
        /// (e.g. Supervisor) can be added in the future.
        /// </summary>
        public enum Role
        {
            Attendant,
            Admin
        }

        /// <summary>
        /// Current active role.  Defaults to Attendant.
        /// </summary>
        public Role CurrentRole { get; private set; } = Role.Attendant;

        /// <summary>
        /// Raised whenever the role changes.  UI managers can subscribe
        /// to update their visuals accordingly.
        /// </summary>
        public event Action<Role> OnRoleChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Assigns a new role and notifies any listeners.  Only call this
        /// from a trusted source such as the login system or editor tools.
        /// </summary>
        public void SetRole(Role newRole)
        {
            if (CurrentRole != newRole)
            {
                CurrentRole = newRole;
                OnRoleChanged?.Invoke(newRole);
            }
        }
    }
}