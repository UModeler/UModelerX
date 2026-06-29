using UnityEditor;

namespace Tripolygon.UModelerX.Editor.Views.Toolbar
{
#if UNITY_2021_2_OR_NEWER && !UNITY_2022_1_OR_NEWER // UNITY 2021_2 ~ 2021_3
    public static class UMXToolbarSetter
{
    static EditorWindow sceneViewWindow;

    public static void Init(EditorWindow window)
    {
        sceneViewWindow = window;
        EditorApplication.update -= OnUpdate;
        EditorApplication.update += OnUpdate;
    }

    // SceneView 로딩시 UMX Setting이 꺼져있다면 켜주기.
    static void OnUpdate()
    {
        sceneViewWindow.TryGetOverlay("UMX Settings", out var overlay);

        if (overlay.displayed == false)
        {
            overlay.displayed = true;
            EditorApplication.update -= OnUpdate;
        }
    }
}

#elif UNITY_2020_1_OR_NEWER // UNITY 2020_1 ~ 2021_1
    using System;
    using UnityEngine;
    using UnityEngine.UIElements;
    using System.Reflection;

    public static class UMXToolbarSetter
    {
        private static float uiToolWidth = 30.0f;
        private static float uiGizmoWidth = 80.0f;

        public static float padding
        {
            get
            {
#if UNITY_2021_1_OR_NEWER
                return 12.0f;
#else
                return 5.0f;
#endif
            }
        }

        public static Vector2 BasePosition
        {
            get
            {
                return new Vector2(uiToolWidth * 8 + 2 * uiGizmoWidth + padding * 3, 4);
            }
        }

        // Unity 툴바 버튼들 크기의 합
        public static int leftToolbarButtonSize;
        // Unity 툴바 공간 크기의 합
        public static int leftToolbarPadding;

        static Type toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        static Type guiViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GUIView");
        static Type IWindowBackendType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.IWindowBackend");
        static PropertyInfo windowBackendPropertyInfo = guiViewType.GetProperty("windowBackend", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        static PropertyInfo visualTreePropertyInfo = IWindowBackendType.GetProperty("visualTree", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo OnGUIHandlerField = typeof(IMGUIContainer).GetField("m_OnGUIHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        static ScriptableObject m_currentToolbar;
        public static Action handler;

        static UMXToolbarSetter()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        static void OnUpdate()
        {
            if (m_currentToolbar == null)
            {
                var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
                m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;

                if (m_currentToolbar != null)
                {
                    var windowBackend = windowBackendPropertyInfo.GetValue(m_currentToolbar);
                    var visualTree = (VisualElement)visualTreePropertyInfo.GetValue(windowBackend, null);
                    var container = (IMGUIContainer)visualTree[0];

                    var handler = (Action)OnGUIHandlerField.GetValue(container);
                    handler -= OnGUI;
                    handler += OnGUI;
                    OnGUIHandlerField.SetValue(container, handler);
                }
            }
        }

        static void OnGUI()
        {
            if (handler != null)
            {
                handler();
            }
        }
    }
#endif // UNITY 2020_1 ~ 2021_1
}
