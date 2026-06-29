#if UNITY_2021_2_OR_NEWER
using UnityEditor.Toolbars;
using Tripolygon.UModelerX.Editor.Views.Toolbar;
using Tripolygon.UModelerX.Editor.MeshOpsProcessor;
using UnityEditor;

namespace Tripolygon.UModelerX.Editor.MeshOps
{
    [EditorToolbarElement("SceneView/UMX Combine")]
    public class UMXMeshOpsCombineTool : UMXMeshOpsButtonBase
    {
        public UMXMeshOpsCombineTool() : base()
        {
            EditorApplication.update += Init;
        }

        private void Init()
        {
            string path = TextureProvider.GetPath(GetType(), "Combine");
            this.icon = TextureProvider.Load(path);

            var toolInput = new UMXToolInputHint(MouseInputHint.LeftMouseButton,
                UnityEngine.KeyCode.None, new ModifierKeyInputHint[] { ModifierKeyInputHint.Ctrl, ModifierKeyInputHint.Alt },
                        "Open Tool Manual", "Link to the tool's manual.");

            string name, input, desc;
            (name, input, desc) = toolInput.InputHintString();
            string tooltipStr = "Combine\n\n" + $"{name + input + desc}";

            base.tooltip = tooltipStr;

            //ApplyVisibility(); // 초기 1회
            //EditorApplication.delayCall += ApplyVisibility; // 에디터 초기화 직후 1회
            //RegisterCallback<UnityEngine.UIElements.AttachToPanelEvent>(_ => ApplyVisibility()); // 씬뷰 열릴 때마다 자동

            RegisterCallback<UnityEngine.UIElements.ClickEvent>(OnToolbarClick);
            EditorApplication.update -= Init;
        }

        private void OnToolbarClick(UnityEngine.UIElements.ClickEvent evt)
        {
            if (evt.ctrlKey && evt.altKey)
            {
                UnityEngine.Application.OpenURL("https://docs.umodeler.com/combine");
                evt.StopImmediatePropagation();
            }
        }

        //private void ApplyVisibility()
        //{
        //    // 부트스트랩(코어)에서 버전/라이선스/환경 판정
        //    bool shouldShow = UMXOverlayBootstrap.ConfigureVisibility();

        //    // 완전 숨김(자리를 차지하지 않음)
        //    style.display = shouldShow ?
        //        UnityEngine.UIElements.DisplayStyle.Flex : UnityEngine.UIElements.DisplayStyle.None;

        //    // 혹시 남는 포커스/클릭 방지
        //    SetEnabled(shouldShow);
        //}

        protected override void ExecuteTool()
        {
            UMXMeshOpsCombineProcessor.Execute();
        }
    }
}
#endif

