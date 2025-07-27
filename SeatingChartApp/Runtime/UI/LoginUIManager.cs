using SeatingChartApp.Runtime.Systems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SeatingChartApp.Runtime.UI
{
    /// <summary>
    /// A simple login manager that allows attendants to elevate to Admin
    /// privileges via a password. It has been updated to use the ServiceProvider.
    /// </summary>
    public class LoginUIManager : MonoBehaviour
    {
        [Header("Login Panel References")]
        [SerializeField] public GameObject loginPanel;
        [SerializeField] public TMP_InputField passwordInput;
        [SerializeField] public Button loginButton;
        [SerializeField] public Button logoutButton;
        [SerializeField] public TMP_Text feedbackText;
        [SerializeField] public Toggle stayLoggedInToggle;

        [Tooltip("Password required to enter Admin mode. Change this in the inspector.")]
        public string adminPassword = "admin123";

        // Manager references obtained via ServiceProvider
        private UserRoleManager _userRoleManager;

        private void Awake()
        {
            ServiceProvider.Register(this);

            if (loginButton != null) loginButton.onClick.AddListener(AttemptLogin);
            if (logoutButton != null) logoutButton.onClick.AddListener(Logout);
        }

        private void Start()
        {
            // Resolve dependencies using ServiceProvider
            _userRoleManager = ServiceProvider.Get<UserRoleManager>();

            // Auto-login if the user chose to stay logged in previously
            if (PlayerPrefs.GetInt("StayLoggedIn", 0) == 1 && PlayerPrefs.GetInt("IsAdmin", 0) == 1)
            {
                if (_userRoleManager != null)
                {
                    _userRoleManager.SetRole(UserRoleManager.Role.Admin);
                }
                if (loginPanel != null) loginPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            ServiceProvider.Unregister<LoginUIManager>();
        }

        public void ShowLoginPanel()
        {
            if (loginPanel != null)
            {
                loginPanel.SetActive(true);
            }
        }

        public void AttemptLogin()
        {
            if (passwordInput == null || _userRoleManager == null) return;

            if (passwordInput.text == adminPassword)
            {
                _userRoleManager.SetRole(UserRoleManager.Role.Admin);
                if (feedbackText != null) feedbackText.text = "Admin mode activated";
                if (loginPanel != null) loginPanel.SetActive(false);

                if (stayLoggedInToggle != null && stayLoggedInToggle.isOn)
                {
                    PlayerPrefs.SetInt("StayLoggedIn", 1);
                    PlayerPrefs.SetInt("IsAdmin", 1);
                }
                else
                {
                    PlayerPrefs.SetInt("StayLoggedIn", 0);
                    PlayerPrefs.SetInt("IsAdmin", 0);
                }
                PlayerPrefs.Save();
                passwordInput.text = string.Empty;
            }
            else
            {
                if (feedbackText != null)
                {
                    feedbackText.text = "Incorrect password";
                    feedbackText.color = Color.red;
                    CancelInvoke(nameof(ClearFeedback));
                    Invoke(nameof(ClearFeedback), 2f);
                }
            }
        }

        private void ClearFeedback()
        {
            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
                feedbackText.color = Color.white;
            }
        }

        public void Logout()
        {
            if (_userRoleManager == null) return;

            _userRoleManager.SetRole(UserRoleManager.Role.Attendant);
            if (loginPanel != null) loginPanel.SetActive(true);
            if (feedbackText != null) feedbackText.text = string.Empty;

            PlayerPrefs.SetInt("StayLoggedIn", 0);
            PlayerPrefs.SetInt("IsAdmin", 0);
            PlayerPrefs.Save();
        }
    }
}
