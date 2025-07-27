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
            // Show the role indicator at all times.  Display "ADM" for Admin, "ATD" for Attendant.
            if (_button != null)
            {
                _button.gameObject.SetActive(true);
                // Only allow clicking when admin (to switch back to attendant).  Attendant indicator is read‑only.
                _button.interactable = role == UserRoleManager.Role.Admin;
            }
            if (_label != null)
            {
                // Abbreviations for roles
                _label.text = role == UserRoleManager.Role.Admin ? "ADM" : "ATD";
                // Colour hint: red for admin, green for attendant
                _label.color = role == UserRoleManager.Role.Admin ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.2f, 0.7f, 0.2f);
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