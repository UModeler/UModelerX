namespace Tripolygon.UModelerX.Runtime.ProBuilderChecker
{
    public class ProBuilderChecker
    {
#if PROBUILDER_ENABLED && UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void ProbuiderEnabled()
        {
            Tripolygon.UModelerX.Editor.ExternPackages.ExternLibarary.Add("ProBuilder", ProbuilderEditorInstance);
            UnityEditor.ProBuilder.ProBuilderEditor.selectionUpdated += ProBuilderEditor_selectionUpdated;
        }

        private static void ProBuilderEditor_selectionUpdated(System.Collections.Generic.IEnumerable<UnityEngine.ProBuilder.ProBuilderMesh> obj)
        {
            Tripolygon.UModelerX.Editor.ExternPackages.ExternLibraryChange();
        }

        private static bool ProbuilderEditorInstance()
        {
            return UnityEditor.ProBuilder.ProBuilderEditor.instance != null && UnityEditor.ProBuilder.ProBuilderEditor.instance.hasFocus;
        }
#elif PROBUILDER_6_0_0
        [UnityEditor.InitializeOnLoadMethod]
        static void ProbuiderEnabled()
        {
            Tripolygon.UModelerX.Editor.ExternPackages.ExternLibarary.Add("ProBuilder", ProbuilderEditorInstance);
            UnityEditor.ProBuilder.ProBuilderEditor.selectionUpdated += ProBuilderEditor_selectionUpdated;
        }

        private static void ProBuilderEditor_selectionUpdated(System.Collections.Generic.IEnumerable<UnityEngine.ProBuilder.ProBuilderMesh> obj)
        {
            Tripolygon.UModelerX.Editor.ExternPackages.ExternLibraryChange();
        }

        private static bool ProbuilderEditorInstance()
        {
            return UnityEditor.ProBuilder.ProBuilderEditor.instance != null;// && UnityEditor.ProBuilder.ProBuilderEditor.instance.hasFocus;
        }
#else
#endif
    }
}

