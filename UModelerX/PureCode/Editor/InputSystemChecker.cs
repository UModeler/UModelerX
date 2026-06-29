using Tripolygon.UModelerX.Editor;
using System.Runtime.InteropServices;

namespace Tripolygon.UModelerX.Runtime.ProBuilderChecker
{
    public class InputSystemChecker
    {
#if INPUTSYSTEM && UNITY_EDITOR
        private static UnityEngine.InputSystem.Pen pen;

        // 0.20.6 버전에서 미작동 처리
        [UnityEditor.InitializeOnLoadMethod]
        private static void InputSystemCheckerIniitialize()
        {
            //Tripolygon.UModelerX.Editor.PenController.Enable();
            Tripolygon.UModelerX.Editor.ExternPackages.ExternLibarary.Add("InputSystemEnabled", InputSystemEnabled);
            if (!Tripolygon.UModelerX.Editor.ExternPackages.ExternLibarary.ContainsKey("ReadPenInput"))
                Tripolygon.UModelerX.Editor.ExternPackages.ExternLibarary.Add("ReadPenInput", ReadPenInput);
        }

        private static bool InputSystemEnabled()
        {
            if (UnityEngine.InputSystem.InputSystem.devices.Count > 0)
            {
                var platform = Tripolygon.UModeler.Editor.UI.UnityEditorApplication.Current.Platform;
                if (platform == OSPlatform.Windows)
                    PenController.DetectTabletDevicesOnWindow();
                else if (platform == OSPlatform.OSX)
                    PenController.DetectTabletDevicesOnOSX();
                else if (platform == OSPlatform.Linux)
                    PenController.DetectTabletDevicesOnLinux();
                return true;
            }
            return false;
        }

        private static bool ReadPenInput()
        {
            if (pen == null || PaintBrushSettings.PenConnected == false)
            {
                var penDevice = UnityEngine.InputSystem.InputSystem.GetDevice("Pen");
                pen = penDevice as UnityEngine.InputSystem.Pen;
            }

            if (pen != null)
            {
                Tripolygon.UModelerX.Editor.PenController.Update(pen.press.ReadValue(), pen.pressure.ReadValue(), pen.position.ReadValue());
            }

            return pen != null;
        }

        private static string OnFindLayoutForDevice(ref UnityEngine.InputSystem.Layouts.InputDeviceDescription description, string matchedLayout, UnityEngine.InputSystem.LowLevel.InputDeviceExecuteCommandDelegate executeDeviceCommand)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Device Connected: {description.product}");
            sb.AppendLine($"Manufacturer: {description.manufacturer}");
            sb.AppendLine($"Interface: {description.interfaceName}");
            sb.AppendLine($"Matched Layout: {matchedLayout ?? "None"}");

            // Unsupported 장치 탐지
            if (string.IsNullOrEmpty(matchedLayout))
            {
                sb.AppendLine("This is an Unsupported device.");
            }
            UnityEngine.Debug.Log(sb.ToString());
            return null; // Unity의 기본 레이아웃 매칭 진행
        }
#endif
    }
}
