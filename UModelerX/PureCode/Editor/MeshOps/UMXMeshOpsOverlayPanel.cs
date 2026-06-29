#if UNITY_2021_2_OR_NEWER
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements; // 유니티 6 버전 밑에서 사용

namespace Tripolygon.UModelerX.Editor.MeshOps
{
    #region UMXOverlayButton 버튼 한 개 단위

    public sealed class UMXOverlayButton
    {
        public string Label { get; }
        public string Tooltip { get; }
        public System.Action Action { get; }

        public System.Func<bool> CanInvoke { get; }

        public UMXOverlayButton(string label, System.Action action, string tooltip = null, System.Func<bool> canInvoke = null)
        {
            Label = label;
            Action = action;
            Tooltip = tooltip;
            CanInvoke = canInvoke;
        }
    }
    #endregion

    #region IUMXMeshOpsActions 여러 버튼 묶음 단위
    public interface IUMXMeshOpsActions
    {
        string Title { get; set; }
        IReadOnlyList<UMXOverlayButton> Buttons { get; }
    }
    #endregion

    // Scene에 등장할 오버레이 패널
    public class UMXMeshOpsOverlayPanel : Overlay, ITransientOverlay
    {
        public bool visible { get; set; }

        private Label title;
        private VisualElement buttonHost;
        // 현재 사용중 버튼
        private IUMXMeshOpsActions currentAction;
        // 버튼 등록 이벤트 처리용 스케쥴러
        private IVisualElementScheduledItem scheduledItem;

        private FloatField thisPosX;
        private FloatField thisPosY;
        private FloatField thisPosZ;

        private UnityEngine.UIElements.VisualElement positionRoot;

        public void Apply(IUMXMeshOpsActions cmds)
        {
            this.currentAction = cmds;
            if (this.title != null)
            {
                this.title.text = string.IsNullOrEmpty(cmds?.Title) ? "Mesh Ops" : cmds.Title;
            }

            if (this.buttonHost != null)
            {
                RebuildButtons();
            }
        }

        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    paddingLeft = 8,
                    paddingRight = 0,
                    paddingTop = 8,
                    paddingBottom = 8,
                    minWidth = 300,
                    width = 300,          // 고정 폭
                }
            };

            root.Add(this.buttonHost);

            // === Title 행(핸들 + 텍스트) ===
            var titleRow = new UnityEngine.UIElements.VisualElement
            {
                style =
                {
                    flexDirection = UnityEngine.UIElements.FlexDirection.Row,
                    alignItems = UnityEngine.UIElements.Align.Center,
                    //unityTextAlign = TextAnchor.MiddleCenter,
                    marginBottom = 20
                }
            };

            // 핸들(작대기 2개) 컨테이너
            var handle = new UnityEngine.UIElements.VisualElement
            {
                style =
                {
                    width = 18,
                    height = 10,
                    marginRight = 8,
                    flexShrink = 0,
                    flexGrow = 0
                }
            };

            // 테마별 라인 색
            UnityEngine.Color barCol = UnityEditor.EditorGUIUtility.isProSkin
                ? new UnityEngine.Color(0.82f, 0.82f, 0.82f, 0.95f)  // 다크: 밝은 회색
                : new UnityEngine.Color(0.45f, 0.45f, 0.45f, 0.95f); // 라이트: 좀 더 진한 회색

            UnityEngine.UIElements.VisualElement MakeBar()
            {
                var bar = new UnityEngine.UIElements.VisualElement
                {
                    style =
                    {
                        height = 2,
                        width = 18,
                        backgroundColor = barCol,
                        borderTopLeftRadius = 1,
                        borderTopRightRadius = 1,
                        borderBottomLeftRadius = 1,
                        borderBottomRightRadius = 1
                    }
                };
                return bar;
            }

            // 위/아래 두 줄
            var barTop = MakeBar();
            barTop.style.marginBottom = 3;
            handle.Add(barTop);

            var barBottom = MakeBar();
            handle.Add(barBottom);

            this.title = new Label("Pivot Tool")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 1,
                }
            };

            titleRow.Add(handle);
            titleRow.Add(this.title);
            root.Add(titleRow);

            this.buttonHost = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                }
            };
            root.Add(this.buttonHost);

            root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                this.scheduledItem ??= root.schedule.Execute(UpdateEnabledStates).Every(120);
                this.scheduledItem.Resume();
            });
            root.RegisterCallback<DetachFromPanelEvent>(_ => this.scheduledItem?.Pause());

            RebuildButtons();

            // === Position 표시 섹션 ===
            this.positionRoot = new UnityEngine.UIElements.VisualElement();
            root.Add(this.positionRoot);

            // --- Title ---
            var positionTitle = new UnityEngine.UIElements.Label("Position")
            {
                style =
                {
                    marginTop = 6,
                    marginBottom = 4,
                    unityFontStyleAndWeight = UnityEngine.FontStyle.Normal
                }
            };
            this.positionRoot.Add(positionTitle);

            // --- Row container (X Y Z) ---
            var posRow = new UnityEngine.UIElements.VisualElement
            {
                style =
                {
                    flexDirection = UnityEngine.UIElements.FlexDirection.Row,
                    alignItems = UnityEngine.UIElements.Align.Center
                }
            };
            this.positionRoot.Add(posRow);

            // 공통 빌더
            FloatField MakePosField(string labelText, float initial)
            {
                var wrap = new UnityEngine.UIElements.VisualElement
                {
                    style =
                    {
                        flexDirection = UnityEngine.UIElements.FlexDirection.Row,
                        alignItems = UnityEngine.UIElements.Align.Center,
                        marginRight = 6
                    }
                };

                var lab = new UnityEngine.UIElements.Label(labelText)
                {
                    style =
                    {
                        width = 12,
                        unityTextAlign = UnityEngine.TextAnchor.MiddleLeft
                    }
                };

                var fld = new FloatField
                {
                    value = initial,
                    isReadOnly = false,
                    style =
                    {
                        width = 80,
                        height = 20
                    },
                    formatString = "0.0000"
                };

                wrap.Add(lab);
                wrap.Add(fld);
                posRow.Add(wrap);
                return fld;
            }

            // 필드 생성 및 멤버 변수에 저장
            this.thisPosX = MakePosField("X", 100f);
            this.thisPosY = MakePosField("Y", 100f);
            this.thisPosZ = MakePosField("Z", 100f);

            return root;
        }

        private void RebuildButtons()
        {
            this.buttonHost.Clear();

            if (this.currentAction?.Buttons == null || this.currentAction.Buttons.Count == 0)
            {
                var help = new HelpBox("No actions.", HelpBoxMessageType.Info);
                this.buttonHost.Add(help);
                return;
            }

            for (int i = 0; i < this.currentAction.Buttons.Count; i += 2)
            {
                var row = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Stretch,
                        marginBottom = 6,
                        width = new Length(100, LengthUnit.Percent),
                        paddingRight = 10
                    }
                };

                // 셀 공통 스타일
                VisualElement MakeCell()
                {
                    return new VisualElement
                    {
                        style =
                        {
                            flexGrow = 1,
                            flexShrink = 1,
                            flexBasis = 0,
                            minWidth = 0,
                            paddingLeft = 0,
                            paddingRight = 0,
                            marginLeft = 0,
                            marginRight = 0
                        }
                    };
                }

                // 버튼 공통 빌더
                Button MakeBtn(UMXOverlayButton src)
                {
                    var b = new Button(() => src.Action?.Invoke())
                    {
                        text = src.Label,
                        tooltip = src.Tooltip,
                        style =
                        {
                            width = new Length(100, LengthUnit.Percent), // ← 셀을 꽉 채움
                            height = 28,

                            // 여백/패딩/마진 제거
                            marginLeft = 0,
                            marginRight = 0,
                            paddingLeft = 0,
                            paddingRight = 0,

                            borderBottomLeftRadius = 6,
                            borderBottomRightRadius = 6,
                            borderTopLeftRadius = 6,
                            borderTopRightRadius = 6,
                            unityTextAlign = TextAnchor.MiddleCenter
                        }
                    };
                    b.userData = src;
                    b.SetEnabled(src.CanInvoke?.Invoke() ?? true);
                    return b;
                }

                // 왼쪽 셀 + 버튼
                var leftCell = MakeCell();
                var b1 = MakeBtn(this.currentAction.Buttons[i]);
                leftCell.Add(b1);
                row.Add(leftCell);

                if (i + 1 < this.currentAction.Buttons.Count)
                {
                    // 가운데 고정 간격
                    var gap = new VisualElement
                    {
                        style =
                        {
                            width = 6,
                            flexGrow = 0,
                            flexShrink = 0
                        }
                    };
                    row.Add(gap);

                    // 오른쪽 셀 + 버튼
                    var rightCell = MakeCell();
                    var b2 = MakeBtn(this.currentAction.Buttons[i + 1]);
                    rightCell.Add(b2);
                    row.Add(rightCell);
                }

                this.buttonHost.Add(row);
            }
        }

        public void ShowPositionSection(bool visible)
        {
            if (this.positionRoot == null)
                return;

            this.positionRoot.style.display = visible
                ? UnityEngine.UIElements.DisplayStyle.Flex
                : UnityEngine.UIElements.DisplayStyle.None;
        }

        private bool IsFieldFocused(FloatField field)
        {
            if (field == null)
                return false;

            var panel = field.panel;
            if (panel == null)
                return false;

            var focused = panel.focusController?.focusedElement;
            if (focused == null)
                return false;

            // FloatField 자체 or 내부 텍스트 입력이면 "편집 중"으로 취급
            var textInput = field.Q("unity-text-input");
            return focused == field || focused == textInput;
        }

        public Vector3 GetFloatFieldVector3()
        {
            return new Vector3(this.thisPosX.value, this.thisPosY.value, this.thisPosZ.value);
        }

        public void SetPositionFields(UnityEngine.Vector3 pos)
        {
            if (this.thisPosX != null && !IsFieldFocused(this.thisPosX))
            {
                this.thisPosX.SetValueWithoutNotify(pos.x);
            }

            if (this.thisPosY != null && !IsFieldFocused(this.thisPosY))
            {
                this.thisPosY.SetValueWithoutNotify(pos.y);
            }

            if (this.thisPosZ != null && !IsFieldFocused(this.thisPosZ))
            {
                this.thisPosZ.SetValueWithoutNotify(pos.z);
            }
        }

        private void UpdateEnabledStates()
        {
            if (this.buttonHost == null)
            {
                return;
            }

            foreach (var element in this.buttonHost.Children())
            {
                if (element is Button buttonUI && buttonUI.userData is UMXOverlayButton button && button.CanInvoke != null)
                {
                    buttonUI.SetEnabled(button.CanInvoke());
                }
            }
        }

        // ① [추가] 인라인(Overlay 없이) 표시용 루트 참조
        private VisualElement inlineRoot;
        private const string InlineRootName = "UMX_MeshOps_InlinePanel";

        // ====== 인라인 표시/숨김(Overlay 슬롯 미사용) ======

        public void ShowInline(Tripolygon.UModelerX.Editor.MeshOps.IUMXMeshOpsActions actions)
        {
            this.Apply(actions);
            this.EnsureMountedToSceneView();
            if (this.inlineRoot != null)
            {
                this.inlineRoot.style.display = UnityEngine.UIElements.DisplayStyle.Flex;
                // 중요: 계층 상 맨 앞으로 보내기 (zIndex 대체)
                this.inlineRoot.BringToFront(); // 형제 중 최상단으로. :contentReference[oaicite:1]{index=1}
            }
            this.Apply(actions);
        }

        public void HideInline()
        {
            if (this.inlineRoot != null)
            {
                this.inlineRoot.style.display = UnityEngine.UIElements.DisplayStyle.None;
            }
        }

        public void UnmountInline()
        {
            if (this.inlineRoot == null) return;
            UnityEditor.SceneView sv = UnityEditor.SceneView.lastActiveSceneView;
            if (sv != null)
            {
                sv.rootVisualElement.Remove(this.inlineRoot);
            }
            this.inlineRoot = null;
        }

        private void EnsureMountedToSceneView()
        {
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv == null && SceneView.sceneViews != null && SceneView.sceneViews.Count > 0)
                sv = (SceneView)SceneView.sceneViews[0];
            if (sv == null) return;

            // 이미 있으면 재사용
            inlineRoot = sv.rootVisualElement.Q<VisualElement>(InlineRootName);
            if (inlineRoot != null) return;

            // ---- 내부 내용(기존 패널 내용)
            VisualElement content = CreatePanelContent();
            // 내부는 자연 높이/너비, 늘리지 않음
            content.style.flexGrow = 0;
            content.style.flexShrink = 0;
            content.style.height = StyleKeyword.Auto;
            content.style.width = StyleKeyword.Auto;
            content.style.alignSelf = Align.FlexStart;

            // ---- 외부 프레임(절대 위치 + 크롬)
            var frame = new VisualElement { name = InlineRootName };

            bool pro = EditorGUIUtility.isProSkin;
            frame.style.backgroundColor = new Color(0.427f * 0.03f, 0.427f * 0.03f, 0.427f * 0.03f, 0.8f);


            var borderCol = pro ? new Color(1, 1, 1, 0.06f) : new Color(0, 0, 0, 0.10f);
            frame.style.borderTopWidth = frame.style.borderRightWidth =
            frame.style.borderBottomWidth = frame.style.borderLeftWidth = 1;
            frame.style.borderTopColor = frame.style.borderRightColor =
            frame.style.borderBottomColor = frame.style.borderLeftColor = borderCol;

            frame.style.borderTopLeftRadius = 8;
            frame.style.borderTopRightRadius = 8;
            frame.style.borderBottomLeftRadius = 8;
            frame.style.borderBottomRightRadius = 8;

            frame.style.paddingLeft = 8;
            frame.style.paddingRight = 8;
            frame.style.paddingTop = 8;
            frame.style.paddingBottom = 8;
            frame.style.minWidth = 160;

            // 절대 배치(좌표는 left/top만 사용)
            frame.style.position = Position.Absolute;
            //frame.style.left = 0;                  // 임시
            //frame.style.top = 0;                  // 임시
            frame.style.left = StyleKeyword.Auto;
            frame.style.top = StyleKeyword.Auto;
            frame.style.right = StyleKeyword.Auto;  // 항상 해제
            frame.style.bottom = StyleKeyword.Auto;  // 항상 해제

            // 크기 자동 + 스트레치 금지
            frame.style.flexGrow = 0;
            frame.style.flexShrink = 0;
            frame.style.height = StyleKeyword.Auto;
            frame.style.width = StyleKeyword.Auto;
            frame.style.alignSelf = Align.FlexStart;

            frame.pickingMode = PickingMode.Position;

            // 내용 부착
            frame.Add(content);

            // 씬뷰에 부착
            sv.rootVisualElement.Add(frame);
            inlineRoot = frame;

            // 첫 레이아웃 후 우하단으로 한 번만 이동
            inlineRoot.RegisterCallback<GeometryChangedEvent>(OnFirstLayout);
            inlineRoot.BringToFront();

            // 드래그는 frame(껍데기)에만 적용
            inlineRoot.AddManipulator(new DraggablePanelManipulator(inlineRoot));
        }

        private void OnFirstLayout(GeometryChangedEvent evt)
        {
            inlineRoot.UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);

            var parentRect = inlineRoot.parent.contentRect;
            float w = inlineRoot.resolvedStyle.width;
            float h = inlineRoot.resolvedStyle.height;

            const float margin = 12f;
            inlineRoot.style.left = StyleKeyword.Auto;
            inlineRoot.style.top = StyleKeyword.Auto;

            inlineRoot.style.right = margin;
            inlineRoot.style.bottom = margin;
        }

        public sealed class DraggablePanelManipulator : PointerManipulator
        {
            private Vector2 _startMouse;
            private Vector2 _startPos; // left/top
            private bool _active;

            public DraggablePanelManipulator(VisualElement target)
            {
                base.target = target;
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            }

            // 필수 ①: 콜백 등록
            protected override void RegisterCallbacksOnTarget()
            {
                base.target.RegisterCallback<PointerDownEvent>(OnPointerDown);
                base.target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                base.target.RegisterCallback<PointerUpEvent>(OnPointerUp);
                base.target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

                // (구버전/환경 대비: 마우스 이벤트도 함께 등록해두면 안전)
                base.target.RegisterCallback<MouseDownEvent>(OnMouseDown);
                base.target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                base.target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            }

            // 필수 ②: 콜백 해제
            protected override void UnregisterCallbacksFromTarget()
            {
                base.target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                base.target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                base.target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                base.target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

                base.target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
                base.target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            }
            private void BeginDrag(Vector2 mousePos, int pointerId = -1, bool capture = true)
            {
                target.style.position = Position.Absolute;

                // left/top을 숫자값으로 고정
                if (target.style.left == StyleKeyword.Null || target.style.left == StyleKeyword.Auto)
                    target.style.left = target.resolvedStyle.left;
                if (target.style.top == StyleKeyword.Null || target.style.top == StyleKeyword.Auto)
                    target.style.top = target.resolvedStyle.top;

                // 반대 앵커 완전 차단
                target.style.right = StyleKeyword.Auto;
                target.style.bottom = StyleKeyword.Auto;


                // 드래그 중 높이 고정
                var h = target.resolvedStyle.height;
                if (float.IsNaN(h) || h <= 0) h = target.contentRect.height; // 안전장치
                target.style.height = h;

                _startMouse = mousePos;
                _startPos = new Vector2(target.layout.x, target.layout.y);
                _active = true;
                target.style.left = _startPos.x;
                target.style.top = _startPos.y;

                if (capture && pointerId != -1) target.CapturePointer(pointerId);
                target.BringToFront();
            }

            private void DragTo(Vector2 mousePos)
            {
                // 프레임마다 앵커 재차단(테마/USS가 개입해도 무력화)
                target.style.right = StyleKeyword.Auto;
                target.style.bottom = StyleKeyword.Auto;

                Vector2 delta = mousePos - _startMouse;
                float newLeft = _startPos.x + delta.x;
                float newTop = _startPos.y + delta.y;

                // 필요 시 클램프
                var parent = target.parent;
                if (parent != null)
                {
                    Rect pr = parent.contentRect;
                    float w = target.resolvedStyle.width;
                    float h = target.resolvedStyle.height;
                    newLeft = Mathf.Clamp(newLeft, 0, Mathf.Max(0, pr.width - w));
                    newTop = Mathf.Clamp(newTop, 0, Mathf.Max(0, pr.height - h));
                }

                target.style.left = newLeft;
                target.style.top = newTop;
            }

            private void EndDrag(int pointerId = -1, bool release = true)
            {
                if (release && pointerId != -1 && target.HasPointerCapture(pointerId))
                    target.ReleasePointer(pointerId);

                // 드래그 종료 후 다시 자연 높이
                target.style.height = StyleKeyword.Auto;

                // 한 번 더 안전하게 정리
                target.style.right = StyleKeyword.Auto;
                target.style.bottom = StyleKeyword.Auto;
            }

            // ----- Pointer 이벤트 -----
            private void OnPointerDown(PointerDownEvent evt)
            {
                if (!CanStartManipulation(evt)) return;
                BeginDrag(evt.position, evt.pointerId, capture: true);
                evt.StopImmediatePropagation();
            }

            private void OnPointerMove(PointerMoveEvent evt)
            {
                if (!_active || !target.HasPointerCapture(evt.pointerId)) return;
                DragTo(evt.position);
                evt.StopPropagation();
            }

            private void OnPointerUp(PointerUpEvent evt)
            {
                if (!CanStopManipulation(evt)) return;
                EndDrag(evt.pointerId, release: true);
                evt.StopPropagation();
            }

            private void OnPointerCaptureOut(PointerCaptureOutEvent evt) => _active = false;

            // ----- Mouse 이벤트(대비용) -----
            private void OnMouseDown(MouseDownEvent evt)
            {
                if (evt.button != 0) return;
                BeginDrag(evt.mousePosition, -1, capture: false);
            }

            private void OnMouseMove(MouseMoveEvent evt)
            {
                if (!_active) return;
                DragTo(evt.mousePosition);
            }

            private void OnMouseUp(MouseUpEvent evt) => EndDrag(-1, release: false);
        }
    }
}
#endif
