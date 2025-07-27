using SeatingChartApp.Runtime.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SeatingChartApp.Runtime.UI
{
    /// <summary>
    /// Displays the current user role and allows admins to switch back to
    /// attendant mode. It has been updated to use the ServiceProvider.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class RoleIndicatorButton : MonoBehaviour
    {
        private Button _button;
        private TMP_Text _label;

        // Manager reference obtained via ServiceProvider
        private UserRoleManager _userRoleManager;

        private void Start()
        {
            _button = GetComponent<Button>();
            _label = GetComponentInChildren<TMP_Text>();

            // Resolve dependency using ServiceProvider
            _userRoleManager = ServiceProvider.Get<UserRoleManager>();

            if (_userRoleManager != null)
            {
                _userRoleManager.OnRoleChanged += OnRoleChanged;
                OnRoleChanged(_userRoleManager.CurrentRole);
            }

            _button.onClick.AddListener(OnButtonClick);
        }

        private void OnDestroy()
        {
            if (_userRoleManager != null)
            {
                _userRoleManager.OnRoleChanged -= OnRoleChanged;
            }
        }

        private void OnRoleChanged(UserRoleManager.Role role)
        {
            if (_button != null)
            {
                _button.gameObject.SetActive(true);
                _button.interactable = role == UserRoleManager.Role.Admin;
            }
            if (_label != null)
            {
                _label.text = role == UserRoleManager.Role.Admin ? "ADM" : "ATD";
                _label.color = role == UserRoleManager.Role.Admin ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.2f, 0.7f, 0.2f);
            }
        }

        private void OnButtonClick()
        {
            if (_userRoleManager != null && _userRoleManager.CurrentRole == UserRoleManager.Role.Admin)
            {
                _userRoleManager.SetRole(UserRoleManager.Role.Attendant);
            }
        }
    }
}
