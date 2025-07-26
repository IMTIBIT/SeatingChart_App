using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SeatingChartApp.Runtime.UI;
using SeatingChartApp.Runtime.Systems;
using System.Reflection;

namespace SeatingChartApp.Editor
{
    /// <summary>
    /// Editor utility that constructs the entire seating chart UI and
    /// supporting managers in a new scene.  It creates a canvas, scales
    /// appropriately for mobile resolutions, sets up assignment, login and
    /// admin panels, adds required managers and wires all fields using
    /// reflection.  Use this tool to rapidly bootstrap a project without
    /// manually configuring UI prefabs.
    /// </summary>
    public static class FullSceneAutoSetupTool
    {
        [MenuItem("Tools/SeatingChartApp/Full Auto Setup Scene")]
        public static void GenerateFullScene()
        {
            // Root canvas and scaler
            GameObject canvasGO = new GameObject("SeatingChart_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.layer = LayerMask.NameToLayer("UI");

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Target high‑resolution iPad baseline.  Scaling mode will adjust to other devices.
            scaler.referenceResolution = new Vector2(2048, 1536);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            // Ensure EventSystem exists
            // Create an EventSystem only if one does not already exist.  Use the
            // non‑obsolete API to query for existing instances.
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            // Add core managers
            canvasGO.AddComponent<LayoutManager>();
            canvasGO.AddComponent<UserRoleManager>();

            // Load built‑in white sprite for panels and buttons
            Sprite whiteSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

            // ----- Panels -----
            // Assignment Panel
            GameObject assignmentPanel = CreatePanel("AssignmentPanel", canvasGO.transform, new Vector2(600, 750), new Color(1f, 1f, 1f, 0.9f), whiteSprite);
            RectTransform assignmentRT = assignmentPanel.GetComponent<RectTransform>();
            assignmentRT.anchoredPosition = new Vector2(-650f, 0f);

            // Login Panel
            GameObject loginPanel = CreatePanel("LoginPanel", canvasGO.transform, new Vector2(400, 300), new Color(1f, 1f, 1f, 0.9f), whiteSprite);
            RectTransform loginRT = loginPanel.GetComponent<RectTransform>();
            loginRT.anchoredPosition = new Vector2(0f, 350f);

            // Admin Panel
            GameObject adminPanel = CreatePanel("AdminPanel", canvasGO.transform, new Vector2(500, 400), new Color(1f, 1f, 1f, 0.9f), whiteSprite);
            RectTransform adminRT = adminPanel.GetComponent<RectTransform>();
            adminRT.anchoredPosition = new Vector2(650f, 0f);

            // Top Bar Panel
            GameObject topBar = CreatePanel("TopBar", canvasGO.transform, new Vector2(2048, 100), new Color(0.95f, 0.95f, 0.95f, 1f), whiteSprite);
            RectTransform topBarRT = topBar.GetComponent<RectTransform>();
            topBarRT.anchorMin = new Vector2(0f, 1f);
            topBarRT.anchorMax = new Vector2(1f, 1f);
            topBarRT.pivot = new Vector2(0.5f, 1f);
            topBarRT.anchoredPosition = new Vector2(0f, 0f);

            // ----- SeatingUIManager -----
            SeatingUIManager seatingUI = canvasGO.AddComponent<SeatingUIManager>();
            // Set private serialized fields via reflection
            SetPrivateField(seatingUI, "assignmentPanel", assignmentPanel);
            // Header text
            TMP_Text headerText = CreateText("SeatHeader", assignmentPanel.transform, "Seat", Color.black);
            headerText.alignment = TextAlignmentOptions.Center;
            RectTransform headerRT = headerText.GetComponent<RectTransform>();
            headerRT.sizeDelta = new Vector2(500, 60);
            headerRT.anchoredPosition = new Vector2(0f, 320f);
            SetPrivateField(seatingUI, "seatHeaderText", headerText);
            // Inputs
            var firstName = CreateInputField("FirstNameInput", assignmentPanel.transform, "First Name");
            firstName.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 240f);
            SetPrivateField(seatingUI, "firstNameInput", firstName);
            var lastName = CreateInputField("LastNameInput", assignmentPanel.transform, "Last Name");
            lastName.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 170f);
            SetPrivateField(seatingUI, "lastNameInput", lastName);
            var room = CreateInputField("RoomNumberInput", assignmentPanel.transform, "Room #");
            room.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 100f);
            SetPrivateField(seatingUI, "roomNumberInput", room);
            var party = CreateInputField("PartySizeInput", assignmentPanel.transform, "Party Size");
            party.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 30f);
            SetPrivateField(seatingUI, "partySizeInput", party);
            var guestId = CreateInputField("GuestIDInput", assignmentPanel.transform, "Guest ID (optional)");
            guestId.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -40f);
            SetPrivateField(seatingUI, "guestIdInput", guestId);
            var notes = CreateInputField("NotesInput", assignmentPanel.transform, "Notes (optional)");
            notes.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -110f);
            SetPrivateField(seatingUI, "notesInput", notes);
            // Buttons row
            Button assignBtn = CreateStyledButton("AssignButton", assignmentPanel.transform, "Assign", whiteSprite);
            assignBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150f, -220f);
            SetPrivateField(seatingUI, "assignButton", assignBtn);
            Button cancelBtn = CreateStyledButton("CancelButton", assignmentPanel.transform, "Cancel", whiteSprite);
            cancelBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(150f, -220f);
            SetPrivateField(seatingUI, "cancelButton", cancelBtn);
            Button clearBtn = CreateStyledButton("ClearButton", assignmentPanel.transform, "Clear", whiteSprite);
            clearBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150f, -300f);
            SetPrivateField(seatingUI, "clearButton", clearBtn);
            Button oosBtn = CreateStyledButton("ToggleOOSButton", assignmentPanel.transform, "Out of Service", whiteSprite);
            oosBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(150f, -300f);
            SetPrivateField(seatingUI, "outOfServiceButton", oosBtn);

            // Delete seat button (admin only).  Centrally aligned below other buttons
            Button deleteBtn = CreateStyledButton("DeleteSeatButton", assignmentPanel.transform, "Delete Seat", whiteSprite);
            deleteBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -380f);
            SetPrivateField(seatingUI, "deleteButton", deleteBtn);

            // Edit seat button (admin only).  Positioned next to Delete seat
            Button editSeatBtn = CreateStyledButton("EditSeatButton", assignmentPanel.transform, "Edit Seat", whiteSprite);
            editSeatBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(150f, -380f);
            SetPrivateField(seatingUI, "editSeatButton", editSeatBtn);
            // Feedback text
            TMP_Text feedback = CreateText("FeedbackText", assignmentPanel.transform, string.Empty, Color.red);
            feedback.alignment = TextAlignmentOptions.Center;
            RectTransform feedbackRT = feedback.GetComponent<RectTransform>();
            feedbackRT.sizeDelta = new Vector2(500f, 50f);
            feedbackRT.anchoredPosition = new Vector2(0f, -450f);
            SetPrivateField(seatingUI, "feedbackText", feedback);

            // Create a full‑screen overlay to dismiss the assignment panel by tapping outside
            GameObject overlay = CreatePanel("Overlay", canvasGO.transform, new Vector2(0f, 0f), new Color(0f, 0f, 0f, 0f), whiteSprite);
            RectTransform overlayRT = overlay.GetComponent<RectTransform>();
            // Stretch overlay across the entire canvas
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.pivot = new Vector2(0.5f, 0.5f);
            overlayRT.offsetMin = Vector2.zero;
            overlayRT.offsetMax = Vector2.zero;
            // Add a transparent Button component to capture clicks and close panel
            Button overlayButton = overlay.AddComponent<Button>();
            overlayButton.transition = Selectable.Transition.None;
            overlayButton.onClick.AddListener(() => seatingUI.ClosePanel());
            overlay.SetActive(false);
            // Assign overlay to the seating UI manager
            SetPrivateField(seatingUI, "overlay", overlay);

            // ----- Add Seat UI Panel -----
            // Create a panel for adding new seats.  Admins will open this via AdminToolsManager.
            GameObject addSeatPanel = CreatePanel("AddSeatPanel", canvasGO.transform, new Vector2(500f, 600f), new Color(1f, 1f, 1f, 0.95f), whiteSprite);
            RectTransform addSeatRT = addSeatPanel.GetComponent<RectTransform>();
            addSeatRT.anchoredPosition = Vector2.zero;
            // Title
            TMP_Text addSeatTitle = CreateText("AddSeatTitle", addSeatPanel.transform, "Add Seat", Color.black);
            addSeatTitle.alignment = TextAlignmentOptions.Center;
            addSeatTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 60f);
            addSeatTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 250f);
            // Seat type dropdown
            TMP_Dropdown seatTypeDropdown = CreateDropdown("SeatTypeDropdown", addSeatPanel.transform, new string[] { "Seat Type" });
            RectTransform typeRT = seatTypeDropdown.GetComponent<RectTransform>();
            typeRT.sizeDelta = new Vector2(380f, 60f);
            typeRT.anchoredPosition = new Vector2(0f, 150f);
            // Seat label input
            TMP_InputField seatLabelInput = CreateInputField("SeatLabelInput", addSeatPanel.transform, "Seat Name / ID");
            seatLabelInput.GetComponent<RectTransform>().sizeDelta = new Vector2(380f, 60f);
            seatLabelInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 70f);
            // Capacity input
            TMP_InputField capacityInput = CreateInputField("CapacityInput", addSeatPanel.transform, "Capacity");
            capacityInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            capacityInput.GetComponent<RectTransform>().sizeDelta = new Vector2(380f, 60f);
            capacityInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -10f);
            // Error text
            TMP_Text addSeatError = CreateText("AddSeatError", addSeatPanel.transform, string.Empty, Color.red);
            addSeatError.alignment = TextAlignmentOptions.Center;
            addSeatError.GetComponent<RectTransform>().sizeDelta = new Vector2(420f, 40f);
            addSeatError.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -80f);
            // Add and Cancel buttons
            Button addSeatBtn = CreateStyledButton("AddSeatConfirmButton", addSeatPanel.transform, "Add", whiteSprite);
            addSeatBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 60f);
            addSeatBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-100f, -180f);
            Button addSeatCancelBtn = CreateStyledButton("AddSeatCancelButton", addSeatPanel.transform, "Cancel", whiteSprite);
            addSeatCancelBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 60f);
            addSeatCancelBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(100f, -180f);
            // AddSeatUIManager component
            AddSeatUIManager addSeatUI = canvasGO.AddComponent<AddSeatUIManager>();
            // Use reflection to assign private fields
            SetPrivateField(addSeatUI, "addSeatPanel", addSeatPanel);
            SetPrivateField(addSeatUI, "seatTypeDropdown", seatTypeDropdown);
            SetPrivateField(addSeatUI, "seatLabelInput", seatLabelInput);
            SetPrivateField(addSeatUI, "capacityInput", capacityInput);
            SetPrivateField(addSeatUI, "addButton", addSeatBtn);
            SetPrivateField(addSeatUI, "cancelButton", addSeatCancelBtn);
            SetPrivateField(addSeatUI, "errorText", addSeatError);
            // Provide a basic seat prefab so the dropdown has an option.  Create a
            // simple rectangular seat with a SeatController and Image.  The
            // prefab is stored in memory only; users can replace it later.
            {
                GameObject seatPrefab = new GameObject("DefaultSeat", typeof(RectTransform), typeof(Image), typeof(SeatController));
                RectTransform seatRt = seatPrefab.GetComponent<RectTransform>();
                seatRt.sizeDelta = new Vector2(100f, 100f);
                seatRt.anchorMin = seatRt.anchorMax = new Vector2(0.5f, 0.5f);
                seatRt.pivot = new Vector2(0.5f, 0.5f);
                Image seatImage = seatPrefab.GetComponent<Image>();
                seatImage.sprite = whiteSprite;
                seatImage.type = Image.Type.Sliced;
                seatImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                SeatController controller = seatPrefab.GetComponent<SeatController>();
                controller.SeatImage = seatImage;
                controller.Capacity = 1;
                controller.snapToGrid = true;
                controller.gridSize = 50f;
                // Add this prefab to the seatPrefabs list
                addSeatUI.seatPrefabs = new System.Collections.Generic.List<GameObject> { seatPrefab };
            }
            // Hide the panel initially
            addSeatPanel.SetActive(false);

            // Assign AddSeatUIManager reference to SeatingUIManager for editing existing seats
            SetPrivateField(seatingUI, "addSeatUIManager", addSeatUI);

            // ----- LoginUIManager -----
            LoginUIManager loginUI = canvasGO.AddComponent<LoginUIManager>();
            loginUI.loginPanel = loginPanel;
            // Password input
            var password = CreateInputField("PasswordInput", loginPanel.transform, "Admin Password");
            password.contentType = TMP_InputField.ContentType.Password;
            password.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 50f);
            loginUI.passwordInput = password;
            // Stay logged in toggle
            Toggle stayToggle = CreateToggle("StayLoggedInToggle", loginPanel.transform, "Stay Logged In");
            stayToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(-60f, -10f);
            loginUI.stayLoggedInToggle = stayToggle;
            // Login button
            Button loginBtn = CreateStyledButton("LoginButton", loginPanel.transform, "Login", whiteSprite);
            loginBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-70f, -80f);
            loginUI.loginButton = loginBtn;
            // Logout button
            Button logoutBtn = CreateStyledButton("LogoutButton", loginPanel.transform, "Logout", whiteSprite);
            logoutBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(70f, -80f);
            loginUI.logoutButton = logoutBtn;
            // Feedback text (use login panel's own TextMeshPro if desired)
            TMP_Text loginFeedback = CreateText("LoginFeedback", loginPanel.transform, string.Empty, Color.red);
            loginFeedback.alignment = TextAlignmentOptions.Center;
            loginFeedback.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -150f);
            loginUI.feedbackText = loginFeedback;

            // ----- AdminToolsManager -----
            AdminToolsManager adminTools = canvasGO.AddComponent<AdminToolsManager>();
            // Create buttons and assign via reflection because fields are private
            Button resetBtn = CreateStyledButton("ResetLayoutButton", adminPanel.transform, "Reset Layout", whiteSprite);
            resetBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 80f);
            Button switchBtn = CreateStyledButton("RoleSwitchButton", adminPanel.transform, "Switch Role", whiteSprite);
            switchBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);
            SetPrivateField(adminTools, "resetLayoutButton", resetBtn);
            SetPrivateField(adminTools, "roleSwitchButton", switchBtn);

            // Add seat button for admins (below other buttons)
            Button addSeatAdminBtn = CreateStyledButton("AddSeatButton", adminPanel.transform, "Add Seat", whiteSprite);
            addSeatAdminBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -80f);
            SetPrivateField(adminTools, "addSeatButton", addSeatAdminBtn);
            // Link AddSeatUIManager to AdminToolsManager for opening the Add Seat panel
            SetPrivateField(adminTools, "addSeatUIManager", addSeatUI);

            // ----- Top bar login button -----
            Button openLogin = CreateStyledButton("OpenLoginButton", topBar.transform, "Admin Login", whiteSprite);
            openLogin.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 70f);
            openLogin.GetComponent<RectTransform>().anchoredPosition = new Vector2(-900f, -35f);
            openLogin.onClick.AddListener(() => loginUI.ShowLoginPanel());

            // Role indicator button (hidden when not in admin mode).  Adds RoleIndicatorButton script to manage label and toggling.
            Button roleIndicatorBtn = CreateStyledButton("RoleIndicator", topBar.transform, "", whiteSprite);
            roleIndicatorBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 70f);
            roleIndicatorBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(900f, -35f);
            roleIndicatorBtn.gameObject.AddComponent<RoleIndicatorButton>();

            // ----- Area dropdown and AreaManager -----
            // Dropdown to choose between different pool areas.  Place near the top bar centre.
            TMP_Dropdown areaDropdown = CreateDropdown("AreaDropdown", topBar.transform, new string[] { "Garden Pool", "Waterpark", "Adult Spa" });
            RectTransform areaDDRT = areaDropdown.GetComponent<RectTransform>();
            areaDDRT.sizeDelta = new Vector2(250f, 70f);
            areaDDRT.anchorMin = areaDDRT.anchorMax = new Vector2(0.5f, 0.5f);
            areaDDRT.pivot = new Vector2(0.5f, 0.5f);
            areaDDRT.anchoredPosition = new Vector2(0f, -35f);
            // Create area containers where seats for each area will reside.
            // These containers must be UI objects with RectTransform so that SeatController
            // has a valid parent RectTransform for drag calculations.
            GameObject gardenObj = new GameObject("GardenPoolContainer", typeof(RectTransform));
            RectTransform gardenPoolContainer = gardenObj.GetComponent<RectTransform>();
            gardenPoolContainer.SetParent(canvasGO.transform);
            gardenPoolContainer.localPosition = Vector3.zero;
            gardenPoolContainer.localScale = Vector3.one;
            gardenPoolContainer.anchorMin = Vector2.zero;
            gardenPoolContainer.anchorMax = Vector2.one;
            gardenPoolContainer.pivot = new Vector2(0.5f, 0.5f);
            gardenPoolContainer.offsetMin = Vector2.zero;
            gardenPoolContainer.offsetMax = Vector2.zero;

            GameObject waterparkObj = new GameObject("WaterparkContainer", typeof(RectTransform));
            RectTransform waterparkContainer = waterparkObj.GetComponent<RectTransform>();
            waterparkContainer.SetParent(canvasGO.transform);
            waterparkContainer.localPosition = Vector3.zero;
            waterparkContainer.localScale = Vector3.one;
            waterparkContainer.anchorMin = Vector2.zero;
            waterparkContainer.anchorMax = Vector2.one;
            waterparkContainer.pivot = new Vector2(0.5f, 0.5f);
            waterparkContainer.offsetMin = Vector2.zero;
            waterparkContainer.offsetMax = Vector2.zero;

            GameObject adultObj = new GameObject("AdultSpaContainer", typeof(RectTransform));
            RectTransform adultSpaContainer = adultObj.GetComponent<RectTransform>();
            adultSpaContainer.SetParent(canvasGO.transform);
            adultSpaContainer.localPosition = Vector3.zero;
            adultSpaContainer.localScale = Vector3.one;
            adultSpaContainer.anchorMin = Vector2.zero;
            adultSpaContainer.anchorMax = Vector2.one;
            adultSpaContainer.pivot = new Vector2(0.5f, 0.5f);
            adultSpaContainer.offsetMin = Vector2.zero;
            adultSpaContainer.offsetMax = Vector2.zero;
            // Add AreaManager and assign properties
            AreaManager areaManager = canvasGO.AddComponent<AreaManager>();
            areaManager.areaDropdown = areaDropdown;
            areaManager.areaNames = new System.Collections.Generic.List<string> { "Garden Pool", "Waterpark", "Adult Spa" };
            areaManager.areaContainers = new System.Collections.Generic.List<Transform> { gardenPoolContainer, waterparkContainer, adultSpaContainer };

            // Hide panels initially
            assignmentPanel.SetActive(false);
            loginPanel.SetActive(false);
            adminPanel.SetActive(false);

            // Select the canvas so the user can see the hierarchy in editor
            Selection.activeGameObject = canvasGO;
        }

        #region Helper Methods
        private static GameObject CreatePanel(string name, Transform parent, Vector2 size, Color bgColor, Sprite bgSprite)
        {
            GameObject go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            rt.localPosition = Vector3.zero;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            Image img = go.GetComponent<Image>();
            img.color = bgColor;
            img.sprite = bgSprite;
            img.type = Image.Type.Sliced;
            return go;
        }

        private static TMP_InputField CreateInputField(string name, Transform parent, string placeholder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(480f, 60f);
            rt.localScale = Vector3.one;
            // Center anchor and pivot so positioning is intuitive
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            TMP_InputField input = go.AddComponent<TMP_InputField>();
            // Background image
            Image bg = go.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.8f);
            bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");
            bg.type = Image.Type.Sliced;
            // Text component
            GameObject textGO = new GameObject("Text", typeof(TextMeshProUGUI));
            textGO.transform.SetParent(go.transform);
            var text = textGO.GetComponent<TextMeshProUGUI>();
            text.text = string.Empty;
            text.fontSize = 32;
            text.color = Color.black;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            input.textComponent = text;
            RectTransform textRT = text.GetComponent<RectTransform>();
            textRT.anchorMin = textRT.anchorMax = new Vector2(0f, 0.5f);
            textRT.pivot = new Vector2(0f, 0.5f);
            textRT.sizeDelta = new Vector2(460f, 40f);
            textRT.anchoredPosition = new Vector2(10f, 0f);
            // Placeholder
            GameObject placeholderGO = new GameObject("Placeholder", typeof(TextMeshProUGUI));
            placeholderGO.transform.SetParent(go.transform);
            var placeholderText = placeholderGO.GetComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.fontSize = 32;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f);
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
            input.placeholder = placeholderText;
            RectTransform phRT = placeholderText.GetComponent<RectTransform>();
            phRT.anchorMin = phRT.anchorMax = new Vector2(0f, 0.5f);
            phRT.pivot = new Vector2(0f, 0.5f);
            phRT.sizeDelta = new Vector2(460f, 40f);
            phRT.anchoredPosition = new Vector2(10f, 0f);
            return input;
        }

        private static Button CreateStyledButton(string name, Transform parent, string label, Sprite sprite)
        {
            GameObject go = new GameObject(name, typeof(Button), typeof(Image));
            go.transform.SetParent(parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 70f);
            rt.localScale = Vector3.one;
            // Center anchors/pivot by default
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            Button btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f);
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            GameObject textGO = new GameObject("Text", typeof(TextMeshProUGUI));
            textGO.transform.SetParent(go.transform);
            var txt = textGO.GetComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 32;
            txt.color = Color.black;
            txt.alignment = TextAlignmentOptions.Center;
            RectTransform txtRT = txt.GetComponent<RectTransform>();
            // Stretch text to fill the button area
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;
            txtRT.pivot = new Vector2(0.5f, 0.5f);
            return btn;
        }

        private static Toggle CreateToggle(string name, Transform parent, string label)
        {
            GameObject go = new GameObject(name, typeof(Toggle));
            go.transform.SetParent(parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 40f);
            rt.localScale = Vector3.one;
            // Center anchor/pivot on the toggle root
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            Toggle toggle = go.GetComponent<Toggle>();
            // Background
            GameObject bgGO = new GameObject("Background", typeof(Image));
            bgGO.transform.SetParent(go.transform);
            var bg = bgGO.GetComponent<Image>();
            bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            bg.color = Color.white;
            RectTransform bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0f, 0.5f);
            bgRT.anchorMax = new Vector2(0f, 0.5f);
            bgRT.sizeDelta = new Vector2(20f, 20f);
            bgRT.anchoredPosition = new Vector2(10f, 0f);
            // Checkmark
            GameObject ckGO = new GameObject("Checkmark", typeof(Image));
            ckGO.transform.SetParent(bgGO.transform);
            var ckImg = ckGO.GetComponent<Image>();
            ckImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
            ckImg.color = Color.black;
            RectTransform ckRT = ckImg.GetComponent<RectTransform>();
            ckRT.anchorMin = ckRT.anchorMax = new Vector2(0.5f, 0.5f);
            ckRT.sizeDelta = new Vector2(20f, 20f);
            ckRT.anchoredPosition = Vector2.zero;
            toggle.graphic = ckImg;
            toggle.targetGraphic = bg;
            // Label
            GameObject labelGO = new GameObject("Label", typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(go.transform);
            var lbl = labelGO.GetComponent<TextMeshProUGUI>();
            lbl.text = label;
            lbl.fontSize = 30;
            lbl.color = Color.black;
            RectTransform lblRT = lbl.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0f, 0.5f);
            lblRT.anchorMax = new Vector2(0f, 0.5f);
            lblRT.pivot = new Vector2(0f, 0.5f);
            lblRT.sizeDelta = new Vector2(160f, 40f);
            lblRT.anchoredPosition = new Vector2(40f, 0f);
            return toggle;
        }

        private static TMP_Text CreateText(string name, Transform parent, string content, Color color)
        {
            GameObject go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent);
            TMP_Text text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = 32;
            text.color = color;
            RectTransform rt = text.GetComponent<RectTransform>();
            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(500f, 60f);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return text;
        }

        private static TMP_Dropdown CreateDropdown(string name, Transform parent, string[] options)
        {
            // Creates a barebones TMP_Dropdown.  For a fully styled dropdown you
            // should assign a template in the editor.  This function will
            // compile and assign basic options but the visual appearance
            // requires additional setup.
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TMP_Dropdown));
            go.transform.SetParent(parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 40f);
            rt.localScale = Vector3.one;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            TMP_Dropdown dropdown = go.GetComponent<TMP_Dropdown>();
            dropdown.options.Clear();
            if (options != null)
            {
                foreach (var option in options)
                {
                    dropdown.options.Add(new TMP_Dropdown.OptionData(option));
                }
            }
            return dropdown;
        }

        /// <summary>
        /// Uses reflection to set a private serialized field on a component.
        /// </summary>
        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                Debug.LogWarning($"Field {fieldName} not found on {obj.GetType().Name}");
            }
        }
        #endregion
    }
}