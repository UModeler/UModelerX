#if UNITY_2021_2_OR_NEWER
using UnityEngine;
using UnityEditor;
using UnityEditor.Toolbars;
using Tripolygon.UModelerX.Runtime;
using Tripolygon.UModelerX.Editor.Views.Toolbar;
using Tripolygon.UModelerX.Editor.MeshOpsProcessor;
using System.Collections.Generic;

namespace Tripolygon.UModelerX.Editor.MeshOps
{
// TODO: 추후에 프리팹 관련 오브젝트를 위한 비활성화를 추가하자.
    //[global::UnityEditor.Toolbars.EditorToolbarElement("UMX Mesh Ops/UMX UModelerize", typeof(global::UnityEditor.SceneView))]
    [EditorToolbarElement("SceneView/UMX UModelerize")]
    public partial class UMXMeshOpsUmodelerizeToolAndDelete : UMXMeshOpsButtonBase
    {
        private Texture2D umodelerizeIcon = null;
        private Texture2D deleteComponentIcon = null;
        private string umodelerizeName = "UModelerize";
        private string deleteComponentName = "Delete UModelerize Component";
        private bool umodelerizeFlag = true;
        private GameObject[] gameObjects = null;

        public UMXMeshOpsUmodelerizeToolAndDelete() : base()
        {
            EditorApplication.update += Init;
        }

        private void Init()
        {
            base.name = this.umodelerizeName;
            string path = TextureProvider.GetPath(GetType(), "Umodelerize");
            this.umodelerizeIcon = TextureProvider.Load(path);
            base.icon = this.umodelerizeIcon;

            var toolInput = new UMXToolInputHint(MouseInputHint.LeftMouseButton,
                UnityEngine.KeyCode.None, new ModifierKeyInputHint[] { ModifierKeyInputHint.Ctrl, ModifierKeyInputHint.Alt },
                        "Open Tool Manual", "Link to the tool's manual.");
            string name, input, desc;
            (name, input, desc) = toolInput.InputHintString();
            this.umodelerizeName += "\n\n" + $"{name + input + desc}";
            base.tooltip = this.umodelerizeName;

            this.deleteComponentName += "\n\n" + $"{name + input + desc}";

            path = TextureProvider.GetPath(GetType(), "DeleteComponents");
            this.deleteComponentIcon = TextureProvider.Load(path);
            Selection.selectionChanged += OnSelectionChanged;

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
                if (this.umodelerizeFlag == true)
                    UnityEngine.Application.OpenURL("https://docs.umodeler.com/umodelerize");
                else
                    UnityEngine.Application.OpenURL("https://docs.umodeler.com/delete-umodeler-component");
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

        ~UMXMeshOpsUmodelerizeToolAndDelete()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }
// TODO: 유모델러라이즈와 삭제를 분리하는것이 좋겠음.
        /// <summary>
        /// 유모델러라이즈가 가능한 오브젝트가 선택되었는지 체크
        /// </summary>
        /// <returns></returns>
        private void OnSelectionChanged()
        {
            if (UMXMeshOpsUmodelerizeProcessor.CheckUmodelerize(Selection.gameObjects) == true)
            {
                this.umodelerizeFlag = true;
                base.name = this.umodelerizeName;
                base.tooltip = this.umodelerizeName;
                base.icon = this.umodelerizeIcon;
            }
            else
            {
                this.umodelerizeFlag = false;
                base.name = this.deleteComponentName;
                base.tooltip = this.deleteComponentName;
                base.icon = this.deleteComponentIcon;
            }
        }

        protected override void ExecuteTool()
        {
            var selectedArray = UnityEditor.Selection.gameObjects;
            if (selectedArray == null || selectedArray.Length == 0)
                return;

            this.gameObjects = selectedArray;

            if (this.umodelerizeFlag == true)
            {
                if (UMXMeshOpsUmodelerizeProcessor.Execute(this.gameObjects) == true)
                {
                    OnSelectionChanged();
                }
            }
            else
            {
                if (UMXMeshOpsDeleteProcessor.DeleteSelectedObjects(this.gameObjects) == true)
                {
                    OnSelectionChanged();
                }
            }
            // #warning 추후에 에디터에서 UModelerXEditableMeshEvents.Sync() 를 사용하자.
            //UModelerXEditableMeshEvents.Sync();
        }
    }
}
#endif
