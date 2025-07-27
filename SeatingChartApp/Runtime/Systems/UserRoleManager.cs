using System;
using UnityEngine;

namespace SeatingChartApp.Runtime.Systems
{
    /// <summary>
    /// Central authority for tracking the current user role. It no longer uses
    /// a singleton and relies on the ServiceProvider.
    /// </summary>
    public class UserRoleManager : MonoBehaviour
    {
        public enum Role
        {
            Attendant,
            Admin
        }

        public Role CurrentRole { get; private set; } = Role.Attendant;
        public event Action<Role> OnRoleChanged;

        private void Awake()
        {
            ServiceProvider.Register(this);
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            ServiceProvider.Unregister<UserRoleManager>();
        }

        public void SetRole(Role newRole)
        {
            if (CurrentRole != newRole)
            {
                CurrentRole = newRole;
                OnRoleChanged?.Invoke(newRole);
                DebugManager.Log(LogCategory.Authentication, $"User role changed to {newRole}.");
            }
        }

        public void ToggleRole()
        {
            SetRole(CurrentRole == Role.Admin ? Role.Attendant : Role.Admin);
        }
    }
}
