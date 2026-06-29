#if UNITY_2021_2_OR_NEWER
using UnityEditor;
using UnityEditor.Toolbars;
using Tripolygon.UModelerX.Editor.Views.Toolbar;
using Tripolygon.UModelerX.Editor.CurveMesh;
using Tripolygon.UModelerX.Editor.MeshOpsProcessor;
using Tripolygon.UModelerX.Runtime;
using Tripolygon.UModelerX.Views.Commentary;

namespace Tripolygon.UModelerX.Editor.MeshOps
{
    [EditorToolbarElement("SceneView/UMX Reset X Form")]
    public class UMXMeshOpsResetXFormTool : UMXMeshOpsToggleBase
    {
        private ResetXFormTool umodelResetXFormTool = new ResetXFormTool();
        public UMXMeshOpsResetXFormTool() : base()
        {
            EditorApplication.update += Init;
        }

        private void Init()
        {
            base.name = "Reset X From";
            string path = TextureProvider.GetPath(GetType(), "Reset X Form");
            base.icon = TextureProvider.Load(path);
            base.BaseAction = new MeshOpsBaseAction();
            base.BaseAction.Title = "Reset X Form";
            base.BaseAction.AddButton(new UMXOverlayButton("Reset All", ResetAll));
            base.BaseAction.AddButton(new UMXOverlayButton("Reset Position", ResetPosition));
            base.BaseAction.AddButton(new UMXOverlayButton("Reset Rotation", ResetRotation));
            base.BaseAction.AddButton(new UMXOverlayButton("Reset Scale", ResetScale));

            var toolInput = new UMXToolInputHint(MouseInputHint.LeftMouseButton,
                UnityEngine.KeyCode.None, new ModifierKeyInputHint[] { ModifierKeyInputHint.Ctrl, ModifierKeyInputHint.Alt },
                        "Open Tool Manual", "Link to the tool's manual.");

            string name, input, desc;
            (name, input, desc) = toolInput.InputHintString();
            string tooltipStr = base.name + "\n\n" + $"{name + input + desc}";
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
                UnityEngine.Application.OpenURL("https://docs.umodeler.com/reset-x-form");
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
        /// local scale, local transform, local rotation 초기화
        /// </summary>
        private void ResetAll()
        {
            UMXMeshOpsResetXFormProcessor.ResetAll(base.gameObjects);
        }

        /// <summary>
        /// local scale 초기화
        /// </summary>
        private void ResetPosition()
        {
            UMXMeshOpsResetXFormProcessor.ResetPosition(base.gameObjects);
        }

        /// <summary>
        /// local scale 초기화
        /// </summary>
        private void ResetScale()
        {
            UMXMeshOpsResetXFormProcessor.ResetScale(base.gameObjects);
        }

        /// <summary>
        /// local rotation 초기화
        /// </summary>
        private void ResetRotation()
        {
            UMXMeshOpsResetXFormProcessor.ResetRotation(base.gameObjects);
        }

        /// <summary>
        /// 패널을 보여줍니다.
        /// </summary>
        protected override void ShowPanel()
        {
            base.gameObjects = Selection.gameObjects;
            if (base.gameObjects == null || base.gameObjects.Length == 0)
            {
                return;
            }

            foreach (var gameObject in base.gameObjects)
            {
                if (gameObject.GetComponent<UModelerXCurveMesh>())
                {
                    UnityEngine.Debug.Log("Please do not use CurveMesh.");
                    base.value = false;
                    return;
                }
            }

            base.ShowPanel();
            base.ShowPositionSection(false);
        }
    }
}
#endif
