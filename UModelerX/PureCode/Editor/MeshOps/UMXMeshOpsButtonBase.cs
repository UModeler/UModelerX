#if UNITY_2021_2_OR_NEWER
using UnityEditor;
using UnityEditor.Toolbars;

namespace Tripolygon.UModelerX.Editor.MeshOps
{
    public class UMXMeshOpsButtonBase : EditorToolbarButton
    {
        public UMXMeshOpsButtonBase()
        {
            EditorApplication.update += Init;
        }

        // 아이콘 및 툴팁을 초기화
        private void Init()
        {
            this.clicked += ExecuteTool;
            EditorApplication.update -= Init;
        }

        // 즉시 실행 한다면 어떤 기능을 실행하는 지
        protected virtual void ExecuteTool()
        {

        }
    }
}
#endif

