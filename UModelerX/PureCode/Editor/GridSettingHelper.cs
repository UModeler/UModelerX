namespace Packages.Unity_UModelerX.PureCode.Editor
{
    public class GridSettingHelper
    {
        // snapEnabled (Unity 6+), gridSnapEnabled (Unity 2022)
        private static readonly System.Reflection.PropertyInfo _snapEnabledProp;
        private static readonly System.Reflection.PropertyInfo _gridSnapEnabledProp;
        // 인크리먼트 개별 스냅 토글 (Unity 6+에 존재할 수 있음)
        private static readonly System.Reflection.PropertyInfo _incrementMoveEnabledProp;
        private static readonly System.Reflection.PropertyInfo _incrementRotateEnabledProp;
        private static readonly System.Reflection.PropertyInfo _incrementScaleEnabledProp;

        static GridSettingHelper()
        {
            var type = typeof(UnityEditor.EditorSnapSettings);
            _snapEnabledProp = type.GetProperty("snapEnabled");
            _gridSnapEnabledProp = type.GetProperty("gridSnapEnabled");

            // 인크리먼트 개별 토글 — 여러 후보 이름 시도
            _incrementMoveEnabledProp = type.GetProperty("moveSnapEnabled")
                ?? type.GetProperty("incrementalSnapMoveEnabled");
            _incrementRotateEnabledProp = type.GetProperty("angleSnapEnabled");
            _incrementScaleEnabledProp = type.GetProperty("scaleSnapEnabled");
        }

        [UnityEditor.InitializeOnLoadMethod]
        private static void InitGridSnapSetting()
        {
            Tripolygon.UModelerX.Editor.Views.GridSnapping.GridSnapSettings.SetSnapEnable += SetSnapEnable;
            Tripolygon.UModelerX.Editor.Views.GridSnapping.GridSnapSettings.SetSnapMode += OnSetSnapMode;
        }

        private static void SetSnapEnable(bool enable)
        {
            SetSnapEnabled(enable);
        }

        /// <summary>
        /// 모드별 Unity 스냅 토글 제어.
        /// 0=None: 전부 끔
        /// 1=World: 그리드 스냅 켬
        /// 2=Increment: 인크리먼트 스냅 켬
        /// </summary>
        private static void OnSetSnapMode(int mode)
        {
            switch (mode)
            {
                case 0: // None — 전부 끔
                    _gridSnapEnabledProp?.SetValue(null, false);
                    _snapEnabledProp?.SetValue(null, false);
                    _incrementMoveEnabledProp?.SetValue(null, false);
                    _incrementRotateEnabledProp?.SetValue(null, false);
                    _incrementScaleEnabledProp?.SetValue(null, false);
                    break;

                case 1: // World — 그리드 스냅 ON
                    _gridSnapEnabledProp?.SetValue(null, true);
                    _snapEnabledProp?.SetValue(null, true);
                    break;

                case 2: // Increment
                    if (_snapEnabledProp != null)
                    {
                        // Unity 6+: gridSnap OFF, snapEnabled ON → 인크리먼트 모드
                        _gridSnapEnabledProp?.SetValue(null, false);
                        _snapEnabledProp.SetValue(null, true);
                    }
                    else
                    {
                        // Unity 2022: snapEnabled 없음. gridSnap ON으로 회전/이동 스냅 활성화
                        _gridSnapEnabledProp?.SetValue(null, true);
                    }
                    // 인크리먼트 개별 스냅 토글 켬 (존재하는 경우)
                    _incrementMoveEnabledProp?.SetValue(null, true);
                    _incrementRotateEnabledProp?.SetValue(null, true);
                    _incrementScaleEnabledProp?.SetValue(null, true);
                    break;
            }
        }

        public static bool GetSnapEnabled()
        {
            if (_snapEnabledProp != null)
            {
                return (bool)_snapEnabledProp.GetValue(null);
            }
            if (_gridSnapEnabledProp != null)
            {
                return (bool)_gridSnapEnabledProp.GetValue(null);
            }
            return false;
        }

        public static void SetSnapEnabled(bool value)
        {
            _snapEnabledProp?.SetValue(null, value);
            _gridSnapEnabledProp?.SetValue(null, value);
        }
    }
}
