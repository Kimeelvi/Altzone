using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace MenuUi.Scripts.Window
{
    /// <summary>
    /// Helper OnGUI window to show breadcrumbs from <c>WindowManager</c>.
    /// </summary>
    internal class Breadcrumbs : MonoBehaviour
    {
        public bool _visible;
        public Key _controlKey = Key.F3;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private int _windowId;
        private Rect _windowRect;
        private string _windowTitle;
        private bool _hasStyles;
        private GUIStyle _guiLabelStyle;
        private StringBuilder _builder;
        private readonly List<MyWindow> _windows = new();

        private void OnEnable()
        {
            var type = GetType();
            Assert.IsTrue(FindObjectsOfType(type).Length == 1, $"FindObjectsOfType({type}).Length == 1");
            _windowId = type.GetHashCode();
            _windowRect = new Rect(0, 0, Screen.width, Screen.height / 3f);
            _windowTitle = $"({_controlKey}) Windows: 0";
            _builder = new StringBuilder();
        }

        private void Update()
        {
            if (Keyboard.current[_controlKey].wasPressedThisFrame)
            {
                ToggleWindowState();
            }
        }

        private void ToggleWindowState()
        {
            _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                return;
            }
            if (!_hasStyles)
            {
                _hasStyles = true;
                _guiLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = new GUIStyleState
                    {
                        textColor = Color.yellow,
                    },
                    fontSize = 18,
                };
            }
            _windowRect = GUILayout.Window(_windowId, _windowRect, DebugWindow, _windowTitle);
        }

        private void DebugWindow(int windowId)
        {
            var windowManager = WindowManager.Get();
            var windowCount = windowManager.WindowCount;
            _windowTitle = $"({_controlKey}) Windows: {windowCount}";
            _builder.Clear();
            if (windowCount == 0)
            {
                _builder.Append("EMPTY");
            }
            else
            {
                _windows.Clear();
                _windows.AddRange(windowManager.WindowStack);
                _windows.Reverse();
                foreach (var window in _windows)
                {
                    if (_builder.Length > 0)
                    {
                        _builder.Append(" > ");
                    }
                    _builder.Append(window._windowDef.WindowName);
                }
            }
            GUILayout.Label(_builder.ToString(), _guiLabelStyle);
        }
#endif
    }
}