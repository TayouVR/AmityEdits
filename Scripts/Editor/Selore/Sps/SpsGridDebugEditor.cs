// SPDX-License-Identifier: GPL-3.0-only
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace org.Tayou.AmityEdits {
    [CustomEditor(typeof(SpsGridDebug))]
    public class SpsGridDebugEditor : AmityBaseEditor<SpsGridDebug> {
        public override VisualElement CreateInspector() {
            var root = new VisualElement();

            var toggleField = new PropertyField(
                serializedObject.FindProperty("showOverlay"), "Show Overlay");
            root.Add(toggleField);

            root.Add(new Label("Visual Elements") {
                style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 8 }
            });
            root.Add(new PropertyField(serializedObject.FindProperty("showRing"), "Ring"));
            root.Add(new PropertyField(serializedObject.FindProperty("showArrow"), "Direction Arrow"));
            root.Add(new PropertyField(serializedObject.FindProperty("showTags"), "Tag Dots"));
            root.Add(new PropertyField(serializedObject.FindProperty("showChainLinks"), "Chain Links"));

            root.Add(new Label("Colors") {
                style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 8 }
            });
            root.Add(new PropertyField(serializedObject.FindProperty("holeColor"), "Hole"));
            root.Add(new PropertyField(serializedObject.FindProperty("ringColor"), "Ring"));
            root.Add(new PropertyField(serializedObject.FindProperty("reversibleColor"), "Reversible"));
            root.Add(new PropertyField(serializedObject.FindProperty("plugColor"), "Plug"));
            root.Add(new PropertyField(serializedObject.FindProperty("chainColor"), "Chain"));

            // Rebuild button
            var rebuildBtn = new Button(() => {
                var debug = (SpsGridDebug)target;
                if (debug.isActiveAndEnabled) {
                    debug.enabled = false;
                    debug.enabled = true;
                }
            }) { text = "Recreate Overlay" };
            root.Add(rebuildBtn);

            root.Bind(serializedObject);
            return root;
        }
    }
}
