using SeatingChartApp.Runtime.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeatingChartApp.Runtime.UI
{
    /// <summary>
    /// Displays the current user role in the UI and allows admins to quickly
    /// switch back to attendant mode.  When not in admin mode the button
    /// hides itself.  Intended to be placed on the top bar.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class RoleIndicatorButton : MonoBehaviour
    {
        private Button _button;
        private TMP_Text _label;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _label = GetComponentInChildren<TMP_Text>();
            // Subscribe to role changes
            if (UserRoleManager.Instance != null)
            {
                UserRoleManager.Instance.OnRoleChanged += OnRoleChanged;
                OnRoleChanged(UserRoleManager.Instance.CurrentRole);
            }
            // Setup click to switch back to attendant when admin
            _button.onClick.AddListener(OnButtonClick);
        }

        private void OnDestroy()
        {
            if (UserRoleManager.Instance != null)
            {
                UserRoleManager.Instance.OnRoleChanged -= OnRoleChanged;
            }
        }

        private void OnRoleChanged(UserRoleManager.Role role)
        {
            bool isAdmin = role == UserRoleManager.Role.Admin;
            if (_button != null)
            {
                _button.gameObject.SetActive(isAdmin);
            }
            if (_label != null)
            {
                _label.text = isAdmin ? "Admin Mode (Tap to exit)" : string.Empty;
                _label.color = isAdmin ? new Color(0.9f, 0.2f, 0.2f) : Color.black;
            }
        }

        private void OnButtonClick()
        {
            // Only allow switching to attendant when in admin mode
            if (UserRoleManager.Instance != null && UserRoleManager.Instance.CurrentRole == UserRoleManager.Role.Admin)
            {
                UserRoleManager.Instance.SetRole(UserRoleManager.Role.Attendant);
            }
        }
    }
}