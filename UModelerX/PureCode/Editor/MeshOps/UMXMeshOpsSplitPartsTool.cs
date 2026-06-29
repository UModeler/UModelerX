#if UNITY_2021_2_OR_NEWER
using UnityEditor.Toolbars;
using Tripolygon.UModelerX.Editor.Views.Toolbar;
using Tripolygon.UModelerX.Editor.MeshOpsProcessor;
using UnityEditor;

namespace Tripolygon.UModelerX.Editor.MeshOps
{
    //[global::UnityEditor.Toolbars.EditorToolbarElement("UMX Mesh Ops/UMX Split Parts", typeof(global::UnityEditor.SceneView))]
    [EditorToolbarElement("SceneView/UMX Split Parts")]
    class UMXMeshOpsSplitPartsTool : UMXMeshOpsButtonBase
    {
        public UMXMeshOpsSplitPartsTool() : base()
        {
            EditorApplication.update += Init;
        }

        private void Init()
        {
            this.name = "Separate Parts";
            string path = TextureProvider.GetPath(GetType(), "SplitPartsTool");
            this.icon = TextureProvider.Load(path);

            var toolInput = new UMXToolInputHint(MouseInputHint.LeftMouseButton,
                UnityEngine.KeyCode.None, new ModifierKeyInputHint[] { ModifierKeyInputHint.Ctrl, ModifierKeyInputHint.Alt },
                        "Open Tool Manual", "Link to the tool's manual.");

            string name, input, desc;
            (name, input, desc) = toolInput.InputHintString();
            string tooltipStr = this.name + "\n\n" + $"{name + input + desc}";
            this.tooltip = tooltipStr;

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
                UnityEngine.Application.OpenURL("https://docs.umodeler.com/separate-parts");
                evt.StopImmediatePropagation();
            }
        }

        //private void ApplyVisibility()
        //{
        //    // 부트스트랩(코어)에서 버전/라이선스/환경 판정
        //    bool shouldShow = UMXOverlayBootstrap.ConfigureVisibility();

        //    //bool shouldShow = true;

        //    // 완전 숨김(자리를 차지하지 않음)
        //    style.display = shouldShow ?
        //        UnityEngine.UIElements.DisplayStyle.Flex : UnityEngine.UIElements.DisplayStyle.None;

        //    // 혹시 남는 포커스/클릭 방지
        //    SetEnabled(shouldShow);
        //}

        /// <summary>
        /// 서로 떨어져 있는 mesh를 각각의 game object로 만든 후 기존의 오브젝트 삭제
        /// </summary>
        protected override void ExecuteTool()
        {
            UMXMeshOpsSplitPartsProcessor.Excute();
        }
    }
}
#endif
