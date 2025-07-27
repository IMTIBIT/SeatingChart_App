using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SeatingChartApp.Runtime.UI;
using SeatingChartApp.Runtime.Systems;
using System.Reflection;
using System.Collections.Generic;

namespace SeatingChartApp.Editor
{
    /// <summary>
    /// Editor utility that intelligently constructs or verifies the entire seating chart UI
    /// and supporting managers in the current scene, now including Phase 4 components.
    /// </summary>
    public static class FullSceneAutoSetupTool
    {
        [MenuItem("Tools/SeatingChartApp/Verify and Set Up Scene")]
        public static void GenerateFullScene()
        {
            // --- Find or Create Core Scene Objects ---
            GameObject canvasGO = FindOrCreateCanvas();
            FindOrCreateEventSystem();

            // --- Add or Get All Required Manager Components ---
            var layoutManager = FindOrCreateComponent<LayoutManager>(canvasGO);
            var userRoleManager = FindOrCreateComponent<UserRoleManager>(canvasGO);
            var analyticsManager = FindOrCreateComponent<AnalyticsManager>(canvasGO);
            var areaManager = FindOrCreateComponent<AreaManager>(canvasGO);
            var seatingUIManager = FindOrCreateComponent<SeatingUIManager>(canvasGO);
            var loginUIManager = FindOrCreateComponent<LoginUIManager>(canvasGO);
            var adminToolsManager = FindOrCreateComponent<AdminToolsManager>(canvasGO);
            var addSeatUIManager = FindOrCreateComponent<AddSeatUIManager>(canvasGO);
            var layoutEditManager = FindOrCreateComponent<LayoutEditManager>(canvasGO);
            var sessionManager = FindOrCreateComponent<SessionManager>(canvasGO); // 🆕 NEW

            // --- Find or Create UI Panels ---
            Sprite whiteSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            Transform canvasTransform = canvasGO.transform;

            GameObject assignmentPanel = FindOrCreatePanel(canvasTransform, "AssignmentPanel", new Vector2(600, 800), new Color(0.1f, 0.1f, 0.1f, 0.95f), whiteSprite);
            GameObject loginPanel = FindOrCreatePanel(canvasTransform, "LoginPanel", new Vector2(400, 300), new Color(0.1f, 0.1f, 0.1f, 0.95f), whiteSprite);
            GameObject adminPanel = FindOrCreatePanel(canvasTransform, "AdminToolsPanel", new Vector2(250, 550), new Color(0.1f, 0.1f, 0.1f, 0.95f), whiteSprite);
            GameObject topBar = FindOrCreatePanel(canvasTransform, "TopBar", new Vector2(0, 80), new Color(0.15f, 0.15f, 0.15f, 1f), whiteSprite);
            GameObject layoutEditToolbar = FindOrCreatePanel(canvasTransform, "LayoutEditToolbar", new Vector2(250, 150), new Color(0.1f, 0.1f, 0.1f, 0.95f), whiteSprite);
            GameObject overlay = FindOrCreatePanel(canvasTransform, "Overlay", Vector2.zero, new Color(0, 0, 0, 0.5f), null);
            GameObject eodConfirmPanel = FindOrCreatePanel(canvasTransform, "EOD_ConfirmationPanel", new Vector2(500, 300), new Color(0.2f, 0.1f, 0.1f, 0.98f), whiteSprite); // 🆕 NEW

            // --- Configure Panel Layouts ---
            ConfigureTopBar(topBar.GetComponent<RectTransform>());
            ConfigureOverlay(overlay.GetComponent<RectTransform>());
            layoutEditToolbar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -200);
            adminPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(835, -200); // Adjusted for iPad Pro landscape
            assignmentPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-660, 0);

            // --- Link Managers to UI Panels ---
            SetPrivateField(seatingUIManager, "assignmentPanel", assignmentPanel);
            SetPrivateField(seatingUIManager, "overlay", overlay);
            SetPrivateField(loginUIManager, "loginPanel", loginPanel);
            SetPrivateField(layoutEditManager, "layoutEditToolbar", layoutEditToolbar);
            SetPrivateField(adminToolsManager, "endOfDayConfirmationPanel", eodConfirmPanel); // 🆕 NEW

            // --- Create and Link UI Elements for AdminToolsManager ---
            var resetBtn = FindOrCreateButton(adminPanel.transform, "ResetLayoutButton", "Reset Layout");
            var roleSwitchBtn = FindOrCreateButton(adminPanel.transform, "RoleSwitchButton", "Switch Role");
            var layoutEditBtn = FindOrCreateButton(adminPanel.transform, "LayoutEditButton", "Edit Layout");
            var saveDefaultBtn = FindOrCreateButton(adminPanel.transform, "SaveDefaultButton", "Save as Default");
            var exportBtn = FindOrCreateButton(adminPanel.transform, "ExportDataButton", "Export Data");
            var endOfDayBtn = FindOrCreateButton(adminPanel.transform, "EndOfDayButton", "END OF DAY");
            // Link fields...
            SetPrivateField(adminToolsManager, "resetLayoutButton", resetBtn);
            SetPrivateField(adminToolsManager, "roleSwitchButton", roleSwitchBtn);
            SetPrivateField(adminToolsManager, "layoutEditButton", layoutEditBtn);
            SetPrivateField(adminToolsManager, "saveDefaultLayoutButton", saveDefaultBtn);
            SetPrivateField(adminToolsManager, "exportDataButton", exportBtn);
            SetPrivateField(adminToolsManager, "endOfDayButton", endOfDayBtn);

            // --- Create and Link UI Elements for End of Day Confirmation Panel ---
            var eodText = FindOrCreateText(eodConfirmPanel.transform, "ConfirmationText", "Are you sure you want to end the day?\n\nThis will clear all guests and finalize today's analytics. This action cannot be undone.", 24);
            var eodConfirmBtn = FindOrCreateButton(eodConfirmPanel.transform, "ConfirmEOD_Button", "Confirm");
            var eodCancelBtn = FindOrCreateButton(eodConfirmPanel.transform, "CancelEOD_Button", "Cancel");
            eodConfirmBtn.onClick.AddListener(() => adminToolsManager.ConfirmEndOfDay());
            eodCancelBtn.onClick.AddListener(() => adminToolsManager.CancelEndOfDay());


            // --- Final State ---
            assignmentPanel.SetActive(false);
            loginPanel.SetActive(false);
            layoutEditToolbar.SetActive(false);
            overlay.SetActive(false);
            eodConfirmPanel.SetActive(false); // 🆕 NEW

            Debug.Log("Scene setup verification complete. All components, including Phase 4, have been created or verified.");
            Selection.activeGameObject = canvasGO;
        }

        #region Helper Methods
        // (Helper methods remain the same as previous versions)
        private static T FindOrCreateComponent<T>(GameObject target) where T : Component { T c = target.GetComponent<T>(); if (c == null) c = target.AddComponent<T>(); return c; }
        private static GameObject FindOrCreateCanvas() { Canvas c = Object.FindFirstObjectByType<Canvas>(); if (c != null) return c.gameObject; GameObject go = new GameObject("SeatingChart_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; CanvasScaler s = go.GetComponent<CanvasScaler>(); s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; s.referenceResolution = new Vector2(2732, 2048); s.matchWidthOrHeight = 0.5f; return go; }
        private static void FindOrCreateEventSystem() { if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null) new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule)); }
        private static GameObject FindOrCreateChild(Transform parent, string name) { Transform t = parent.Find(name); if (t != null) return t.gameObject; GameObject go = new GameObject(name); go.AddComponent<RectTransform>(); go.transform.SetParent(parent, false); return go; }
        private static GameObject FindOrCreatePanel(Transform parent, string name, Vector2 size, Color color, Sprite sprite) { GameObject go = FindOrCreateChild(parent, name); Image i = FindOrCreateComponent<Image>(go); i.color = color; i.sprite = sprite; i.type = Image.Type.Sliced; go.GetComponent<RectTransform>().sizeDelta = size; return go; }
        private static Button FindOrCreateButton(Transform parent, string name, string label) { GameObject go = FindOrCreateChild(parent, name); FindOrCreateComponent<Image>(go); Button b = FindOrCreateComponent<Button>(go); TMP_Text t = FindOrCreateText(go.transform, "Text", label, 24); t.alignment = TextAlignmentOptions.Center; return b; }
        private static TMP_Text FindOrCreateText(Transform parent, string name, string content, int fontSize, Color? color = null) { GameObject go = FindOrCreateChild(parent, name); TMP_Text t = FindOrCreateComponent<TextMeshProUGUI>(go); t.text = content; t.fontSize = fontSize; t.color = color ?? Color.white; return t; }
        private static void ConfigureTopBar(RectTransform rt) { rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1); rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(0, 80); }
        private static void ConfigureOverlay(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f); rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; FindOrCreateComponent<Button>(rt.gameObject); }
        private static void SetPrivateField(object obj, string fieldName, object value) { var f = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance); if (f != null) f.SetValue(obj, value); else Debug.LogWarning($"Field '{fieldName}' not found on {obj.GetType().Name}."); }
        #endregion
    }
}
