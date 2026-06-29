#if !UNITY_2021_2_OR_NEWER // UNITY_2020_1 ~ UNITY_2021_1
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Rect = UnityEngine.Rect;

namespace Tripolygon.UModelerX.Editor.Views.Toolbar
{
    public class ToolbarDropdownButton : Button
    {
        protected const float basePaddingX = 4;
        protected const float iconPaddingY = 3;
        protected const float dropdownPaddingY = 4.5f;
        protected Vector2 iconSize = new Vector2(16, 16);
        protected Vector2 dropdownSize = new Vector2(14, 14);

        public float Width { get; private set; }

        public Texture icon; // 임시로 public. 코드네이트 시스템에서 접근하는 이유 확인
        protected Texture dropDownIcon;
        public event Action<object> onClicked;

        public ToolbarDropdownButton(string text = null)
        {
            icon = TextureProvider.CreateContent(GetType(), text);
            dropDownIcon = EditorGUIUtility.IconContent("icon dropdown@2x").image;
        }

        public virtual void Render(Rect rect)
        {
            var content = new GUIContent(this.text, this.icon);

            // Dropdown with label
            if (string.IsNullOrEmpty(this.text) == false)
            {
                var textStyle = new GUIStyle();
                textStyle.fontSize = 12;
                var textSize = textStyle.CalcSize(new GUIContent(this.text));

                rect.width = UMXToolbarStyle.labelButtonStyle.padding.left + iconSize.x + textSize.x + basePaddingX + dropdownSize.x + UMXToolbarStyle.labelButtonStyle.padding.right;
                this.Width = rect.width;

                if (GUI.Button(rect, content, UMXToolbarStyle.labelButtonStyle))
                {
                    onClicked?.Invoke(rect);
                }
            }
            // Dropdown without label
            else
            {
                var contentSize = UMXToolbarStyle.buttonStyle.CalcSize(content);
                this.Width = contentSize.x;
                if (GUI.Button(rect, this.icon, UMXToolbarStyle.buttonStyle))
                {
                    onClicked?.Invoke(rect);
                }
            }

            // Draw ▼
            rect.position = rect.position + Vector2.right * (rect.width - basePaddingX - dropdownSize.x) + Vector2.up * dropdownPaddingY;
            GUI.DrawTexture(new Rect(rect.position, dropdownSize), this.dropDownIcon);
        }
    }
}
#endif // UNITY_2020_1 ~ UNITY_2021_1
