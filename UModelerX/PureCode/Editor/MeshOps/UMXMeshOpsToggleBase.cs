#if UNITY_2021_2_OR_NEWER
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;

namespace Tripolygon.UModelerX.Editor.MeshOps
{
    public class UMXMeshOpsToggleBase : EditorToolbarToggle
    {
        private UMXMeshOpsOverlayPanel togglePanel = null;
        private MeshOpsBaseAction baseAction;
        private static UMXMeshOpsToggleBase toggleBase = null; // 토글 종류 변경 확인용
        protected GameObject[] gameObjects = null;

        public MeshOpsBaseAction BaseAction 
        {
            get { return baseAction; }
            set { baseAction = value; }
        }

        /// <summary>
        /// 물체 선택이 변경된 경우 토글과 패널 끄기
        /// </summary>
        private void OnSelectionChanged()
        {
            var newGameObjects = Selection.gameObjects;
            if (this.gameObjects != null && newGameObjects != this.gameObjects)
            {
                base.value = false;
                HidePanel();
            }
        }

        public UMXMeshOpsToggleBase()
        {
            EditorApplication.update += Init;
        }

        /// <summary>
        /// 아이콘 및 툴팁을 초기화
        /// </summary>
        private void Init()
        {
            this.RegisterValueChangedCallback((evt) =>
                {
                    if (Selection.activeGameObject != null)
                    {
                        if (this.value == true)
                            this.ShowPanel();
                        else
                            this.HidePanel();
                    }
                    else
                    {
                        base.value = false;
                    }
                }
             );
            EditorApplication.update -= Init;
        }

        /// <summary>
        /// 패널을 보이게 합니다.
        /// </summary>
        protected virtual void ShowPanel()
        {
            Selection.selectionChanged -= this.OnSelectionChanged; // 중복 방지
            Selection.selectionChanged += this.OnSelectionChanged;

            UMXMeshOpsToggleBase nowBase = this;
            if (nowBase != toggleBase && toggleBase != null)
            {
                toggleBase.value = false;
                toggleBase.HidePanel();
            }
            toggleBase = nowBase;


            if (this.togglePanel == null)
                this.togglePanel = new UMXMeshOpsOverlayPanel();
            this.togglePanel.ShowInline(this.baseAction);
        }

        /// <summary>
        /// 패널을 삭제하고 안보이게 합니다.
        /// </summary>
        protected virtual void HidePanel()
        {
            if (this.togglePanel != null)
            {
                Selection.selectionChanged -= this.OnSelectionChanged;
                this.togglePanel.HideInline();
                this.togglePanel.UnmountInline();
                this.togglePanel = null;
            }
        }

        protected void SetPositionFields(Vector3 pos)
        {
            this.togglePanel.SetPositionFields(pos);
        }

        protected Vector3 GetFloatFieldVector3()
        {
            return this.togglePanel.GetFloatFieldVector3();
        }

        protected void ShowPositionSection(bool visible)
        {
            this.togglePanel?.ShowPositionSection(visible);
        }
    }

    // 빈 버튼 생성
    public class MeshOpsBaseAction : IUMXMeshOpsActions
    {
        public string Title { set; get; }  
        public IReadOnlyList<UMXOverlayButton> Buttons => buttons;

        private readonly List<UMXOverlayButton> buttons = new List<UMXOverlayButton>();

        public void AddButton(UMXOverlayButton button) { this.buttons.Add(button); }
    }
}
#endif

