using UnityEditor;
using UnityEngine;
using System;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.Toolbars;
#endif // UNITY_2021_2_OR_NEWER

namespace Tripolygon.UModelerX.Editor.Views.Toolbar
{
    public class UMXCoordinateSystemSettingToolbarDropdown
    {
#if UNITY_2021_2_OR_NEWER // For Unity 2021_2 ~ 6
        private static EditorToolbarDropdown dropdown;

        public UMXCoordinateSystemSettingToolbarDropdown(EditorToolbarDropdown toolbarDropdown)
        {
            dropdown = toolbarDropdown;
        }

        public static void ShowDropdown()
        {
            var rect = new Rect(dropdown.worldBound.position, new Vector2(0, 22));
            GenericMenu menu = new GenericMenu();
            foreach (UMXCoordinateSystem coordinateSystem in Enum.GetValues(typeof(UMXCoordinateSystem)))
            {
                menu.AddItem(new GUIContent(Enum.GetName(typeof(UMXCoordinateSystem), coordinateSystem)),
                    CoordinateSystemSetting.CoordinateSystem == coordinateSystem,
                    () => OnCoordinateSystemChanged(coordinateSystem));
            }
            menu.DropDown(rect);
        }
#else // For Unity 2020_1 ~ 2021_1
        private static ToolbarDropdownButton dropdown;

        public UMXCoordinateSystemSettingToolbarDropdown(ToolbarDropdownButton dropdownButton)
        {
            dropdown = dropdownButton;
            OnCoordinateSystemChanged(CoordinateSystemSetting.CoordinateSystem);
        }

        public static void ShowDropdown(object target)
        {
            if (target is Rect rectTarget)
            {
                var rect = rectTarget;
                GenericMenu menu = new GenericMenu();
                foreach (UMXCoordinateSystem coordinateSystem in Enum.GetValues(typeof(UMXCoordinateSystem)))
                {
                    menu.AddItem(new GUIContent(Enum.GetName(typeof(UMXCoordinateSystem), coordinateSystem)),
                        CoordinateSystemSetting.CoordinateSystem == coordinateSystem,
                        () => OnCoordinateSystemChanged(coordinateSystem));
                }
                menu.DropDown(rect);
            }
        }
#endif

        private static void OnCoordinateSystemChanged(UMXCoordinateSystem newCoordinateSystem)
        {
            if (dropdown.text != nameof(newCoordinateSystem))
            {
                CoordinateSystemSetting.CoordinateSystem = newCoordinateSystem;
                dropdown.text = Enum.GetName(typeof(UMXCoordinateSystem), newCoordinateSystem);
                dropdown.icon = TextureProvider.CreateContent(typeof(UMXCoordinateSystemSettingToolbarButton), dropdown.text);
                if (newCoordinateSystem == UMXCoordinateSystem.Local)
                {
                    Tools.pivotRotation = PivotRotation.Local;
                }
                else if (newCoordinateSystem == UMXCoordinateSystem.Global)
                {
                    Tools.pivotRotation = PivotRotation.Global;
                }
            }
        }
    }
}

