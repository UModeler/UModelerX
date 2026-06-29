#if UNITY_2021_2_OR_NEWER
using UnityEngine;
using UnityEditor;
using UnityEditor.Toolbars;
using Tripolygon.UModelerX.Editor.Views.Toolbar;
using Tripolygon.UModelerX.Editor.MeshOpsProcessor;
using Tripolygon.UModelerX.Views.Commentary;
using Tripolygon.UModelerX.Runtime;

namespace Tripolygon.UModelerX.Editor.MeshOps
{
    
    //[global::UnityEditor.Toolbars.EditorToolbarElement("UMX Mesh Ops/UMX Pivot", typeof(global::UnityEditor.SceneView))]
    [EditorToolbarElement("SceneView/UMX Pivot")]
    public class UMXMeshOpsPivotTool : UMXMeshOpsToggleBase
    {
        private PivotGizmoData gizmoData;
        private bool suppressGizmoUndoOnce = false;

        public UMXMeshOpsPivotTool() : base()
        {
            EditorApplication.update += Init;
        }

        private void Init()
        {
            base.name = "Pivot";
            string path = TextureProvider.GetPath(GetType(), "Pivot");
            base.icon = TextureProvider.Load(path);
            base.BaseAction = new MeshOpsBaseAction();
            base.BaseAction.Title = "Pivot Tool";
            base.BaseAction.AddButton(new UMXOverlayButton("Top Center", SetTopCenter));
            base.BaseAction.AddButton(new UMXOverlayButton("Center", SetCenter));
            base.BaseAction.AddButton(new UMXOverlayButton("Bottom Center", SetBottomCenter));
            base.BaseAction.AddButton(new UMXOverlayButton("Gizmo Location", SetGizmoPosition));

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
            if (evt.ctrlKey &&  evt.altKey)
            {
                UnityEngine.Application.OpenURL("https://docs.umodeler.com/pivot");
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

        /// <summary>
        /// pivot을 renderer bound의 가장 윗면 중심으로 이동시킵니다.
        /// </summary>
        private void SetTopCenter()
        {
            MeshOpsPivotProcessor.SetTopCenter(base.gameObjects, this.gizmoData);
        }

        /// <summary>
        /// pivot을 renderer bound의 중심으로 이동시킵니다.
        /// </summary>
        private void SetCenter()
        {
            MeshOpsPivotProcessor.SetCenter(base.gameObjects, this.gizmoData);
        }

        /// <summary>
        /// pivot을 renderer bound의 가장 아랫면의 중심으로 이동시킵니다.
        /// </summary>
        private void SetBottomCenter()
        {
            MeshOpsPivotProcessor.SetBottomCenter(base.gameObjects, this.gizmoData);
        }

        /// <summary>
        /// pivot을 gizmo의 위치로 이동시킵니다.
        /// </summary>
        private void SetGizmoPosition()
        {
            MeshOpsPivotProcessor.SetGizmoPosition(base.gameObjects, this.gizmoData, ref this.suppressGizmoUndoOnce);
        }


        private void RenderSupportLine(GameObject gameObject)
        {
            var right = gameObject.transform.right;
            var forward = gameObject.transform.forward;
            var up = gameObject.transform.up;

            UnityEngine.Vector3 origin = gameObject.transform.position;
            UnityEngine.Vector3 dir = right;

            float distance = 10000f;

            // right (red)
            UnityEditor.Handles.color = new Color(1, 0, 0, 0.6f);
            UnityEditor.Handles.DrawLine(origin - dir * distance, origin + dir * distance);

            // forward (blue)
            UnityEditor.Handles.color = new Color(0, 0, 1, 0.6f);
            dir = forward;
            UnityEditor.Handles.DrawLine(origin - dir * distance, origin + dir * distance);

            // up (green)
            UnityEditor.Handles.color = new Color(0, 1, 0, 0.6f);
            dir = up;
            UnityEditor.Handles.DrawLine(origin - dir * distance, origin + dir * distance);
        }

        /// <summary>
        /// 각각의 오브젝트에 맞는 기즈모를 렌더링 하는 함수
        /// </summary>
        protected void OnRenderGUI()
        {
            for (int i = 0; i < this.gizmoData.renderGizmoPositions.Count; i++)
            {
                using (var check = new UnityEditor.EditorGUI.ChangeCheckScope())
                {
                    UnityEngine.Vector3 p = UnityEditor.Handles.PositionHandle(
                        this.gizmoData.renderGizmoPositions[i],
                        base.gameObjects[i].transform.rotation
                    );

                    this.RenderSupportLine(base.gameObjects[i]);

                    if (check.changed && this.suppressGizmoUndoOnce == false)
                    {
                        UnityEditor.Undo.RecordObject(this.gizmoData, "Move Gizmo");
                        this.gizmoData.renderGizmoPositions[i] = p;
                    }
                }
            }

            base.SetPositionFields(this.gizmoData.renderGizmoPositions[0]);
            if (this.gizmoData.renderGizmoPositions.Count == 1)
            {
                this.gizmoData.renderGizmoPositions[0] = base.GetFloatFieldVector3();
            }

            // 프레임 말(첫 Repaint)에서만 억제 해제
            if (this.suppressGizmoUndoOnce && UnityEngine.Event.current.type == UnityEngine.EventType.Repaint)
            {
                this.suppressGizmoUndoOnce = false;
            }
        }

        private bool OnTransformFlag = false;

        protected override void ShowPanel()
        {
            base.gameObjects = Selection.gameObjects;
            if (base.gameObjects == null)
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

            if (this.gizmoData == null)
                this.gizmoData = UnityEngine.ScriptableObject.CreateInstance<PivotGizmoData>();

            if (base.value == true && this.OnTransformFlag == false)
            {
                UnityEditor.Tools.hidden = true; // gizmo를 숨김 
                this.gizmoData.gizmoPositions.Clear();
                this.gizmoData.renderGizmoPositions.Clear();

                foreach (var gameObject in base.gameObjects)
                {
                    this.gizmoData.gizmoPositions.Add(gameObject.transform.position);
                    this.gizmoData.renderGizmoPositions.Add(gameObject.transform.position);
                }

                this.OnTransformFlag = true;
                SceneView.duringSceneGui -= this.OnSceneGUI; // 중복 방지
                SceneView.duringSceneGui += this.OnSceneGUI;

            }
            else if (base.value == false && this.OnTransformFlag == true)
            {
                this.OnTransformFlag = false;
            }

            base.ShowPanel();

            if (this.gameObjects.Length > 1)
                base.ShowPositionSection(false);
            else
                base.ShowPositionSection(true);
        }

        protected override void HidePanel()
        {
            if (this.OnTransformFlag == true)
            {
                this.OnTransformFlag = false;
            }
            SceneView.duringSceneGui -= this.OnSceneGUI;
            if (base.gameObjects != null)
            {
                foreach (var gameObject in base.gameObjects)
                {
                    if (gameObject == null)
                        continue;
                    if (gameObject.GetComponent<UModelerXCurveMesh>())
                    {
                        base.HidePanel();
                        return;
                    }
                }
            }
            UnityEditor.Tools.hidden = false;
            base.HidePanel();
        }

        // Scene 뷰 프레임마다 호출되는 콜백
        private void OnSceneGUI(SceneView sceneView)
        {
            var prevHidden = UnityEditor.Tools.hidden;
            UnityEditor.Tools.hidden = true;
            try
            {
                this.OnRenderGUI();
            }
            finally
            {
                UnityEditor.Tools.hidden = prevHidden;
            }

            if (Event.current.type == UnityEngine.EventType.Repaint)
                sceneView.Repaint();
        }
    }
}
#endif
