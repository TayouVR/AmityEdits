using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using VF.Inspector;

namespace org.Tayou.AmityEdits {
    [CustomEditor(typeof(ItemSetup), true)]
    public class ItemSetupEditor : AmityBaseEditor {
        private ReorderableList _targets;
        private ItemSetup _itemSetup;

        public ReorderableList Targets =>
            _targets ?? (_targets = new ReorderableList(_itemSetup.targets, typeof(ItemData)) {
                drawHeaderCallback = DrawHeaderCallback,
                drawElementCallback = DrawElementCallback,
                elementHeightCallback = ElementHeightCallback,
                onAddCallback = list => _itemSetup.targets.Add(new ItemData())
            });

        private float ElementHeightCallback(int index) {
            float height = 0;
            height += EditorGUIUtility.singleLineHeight * 1.25f; // Transform Field
            ItemData itemSetupTarget = _itemSetup.targets[index];
            if (itemSetupTarget == null) return height;
            
            height += EditorGUIUtility.singleLineHeight * 1.25f; // position
            height += EditorGUIUtility.singleLineHeight * 1.25f; // rotation
            height += EditorGUIUtility.singleLineHeight * 1.25f; // button
            return height;
        }

        /*private void OnSceneGUI() {
            for (var i = 0; i < _itemSetup.targets.Count; i++) {
                var itemTarget = _itemSetup.targets[i];
                if (_itemSetup.itemPreviewIndex == i) {
                    EditorGUI.BeginChangeCheck();
                    var position1 = itemTarget.transform.position;
                    var positionOffset = Handles.DoPositionHandle(itemTarget.transform.TransformPoint(itemTarget.PositionOffset),
                        Quaternion.Euler(itemTarget.RotationOffset) * itemTarget.transform.rotation) - position1;
                    var rotationOffset =
                        Handles.DoRotationHandle(Quaternion.Euler(itemTarget.RotationOffset), itemTarget.PositionOffset + position1).eulerAngles;
                    if (EditorGUI.EndChangeCheck()) {
                        Undo.RecordObject(target, "Altered offsets");
                        itemTarget.PositionOffset = positionOffset;
                        itemTarget.RotationOffset = rotationOffset;
                    }
                } 
            }
        }*/

        [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.InSelectionHierarchy)]
        static void DrawGizmo(ItemSetup itemSetup, GizmoType gizmoType) {
            for (var i = 0; i < itemSetup.targets.Count; i++) {
                var itemTarget = itemSetup.targets[i];
                if (itemSetup.itemPreviewIndex != i) {
                    var position1 = itemTarget.transform.TransformPoint(itemTarget.position);
                    var cumAngle = Quaternion.Euler(itemTarget.rotation.eulerAngles) * itemTarget.transform.rotation;
                    var direction = new Vector3(Mathf.Cos(cumAngle.x), Mathf.Sin(cumAngle.y), Mathf.Tan(cumAngle.z));
                    VRCFuryGizmoUtils.DrawArrow(position1, cumAngle * Vector3.up * 0.25f + position1, Color.cyan);
                }

            }
        }

        private void DrawElementCallback(Rect rect, int index, bool isactive, bool isfocused) {
            rect.height = EditorGUIUtility.singleLineHeight;
            ItemData itemSetupTarget = _itemSetup.targets[index];
            
            _itemSetup.targets[index].transform = (Transform)EditorGUI.ObjectField(rect, new GUIContent($"Target {index}"), _itemSetup.targets[index].transform, typeof(Transform), true);
            //HierarchyTransform.OnInspectorGUI(serializedObject.FindProperty("targets").GetArrayElementAtIndex(index).FindPropertyRelative("path").serializedObject);
            
            if (itemSetupTarget != null) {
                EditorGUI.BeginChangeCheck();
                rect.y += EditorGUIUtility.singleLineHeight * 1.25f;
                var positionOffset = EditorGUI.Vector3Field(rect, "Position Offset", itemSetupTarget.position);
                rect.y += EditorGUIUtility.singleLineHeight * 1.25f;
                var rotationOffset = EditorGUI.Vector3Field(rect, "Rotation Offset", itemSetupTarget.rotation.eulerAngles);
                rect.y += EditorGUIUtility.singleLineHeight * 1.25f;
                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(target, $"Changed Item Target Offsets");
                    _itemSetup.transform.position = itemSetupTarget.transform.position + positionOffset;
                    
                    itemSetupTarget.position = positionOffset;
                    itemSetupTarget.EulerAngles = rotationOffset;
                    // don't actually assign rotation, quaternions and euler angles are a pain to deal with....
                    //itemSetupTarget.rotationOffset = rotationOffset;
                }
                
                if (_itemSetup.itemPreviewIndex == index) {
                    SaveCurrentTransformToOffsets();
                    if (GUI.Button(rect, "Stop Preview")) {
                        EditorUtility.SetDirty(_itemSetup);
                        _itemSetup.itemPreviewIndex = -1;
                        Undo.RecordObject(target, $"Took Item Target #{index} out of Preview mode");
                    }
                } else {
                    if (GUI.Button(rect, "Preview")) {
                        EditorUtility.SetDirty(_itemSetup);
                        _itemSetup.itemPreviewIndex = index;
                        Undo.RecordObject(target, $"Set Item Target #{index} in Preview mode");
                        _itemSetup.transform.position = itemSetupTarget.transform.position + positionOffset;
                        
                        // this causes deep decimal inaccuracies
                        //_itemSetup.transform.rotation = Quaternion.Euler(itemSetupTarget.transform.rotation.eulerAngles + rotationOffset);
                        //_itemSetup.gameObject.SetActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// Saves the current Transform (position & rotation) to the corresponding fields based on the preview index (-1 for rest state)
        /// </summary>
        public void SaveCurrentTransformToOffsets() {
            Transform itemSetupTrans = _itemSetup.transform;
            if (_itemSetup.itemPreviewIndex == -1) {
                _itemSetup.restPosition = itemSetupTrans.position;
                _itemSetup.restRotation = itemSetupTrans.rotation;
            } else {
                _itemSetup.targets[_itemSetup.itemPreviewIndex].position = itemSetupTrans.position;
                _itemSetup.targets[_itemSetup.itemPreviewIndex].rotation = itemSetupTrans.rotation;
            }
        }
        
        private void DrawHeaderCallback(Rect rect) {
            EditorGUI.LabelField(rect, "Targets");
        }
        
        private void OnEnable() {
            _itemSetup = (ItemSetup) target;
            //EditorApplication.update += Update; // handle any continuous updates
        }

        public override void DrawInspector() {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("itemDefaultActiveState"), new GUIContent("Enabled at rest"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("itemStaysActive"), new GUIContent("Keep Item Always Active"), true);
            
            EditorGUILayout.LabelField("Reset Tranform");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("restPosition"), new GUIContent("Position"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("restRotation"), new GUIContent("Rotation"), true);

            Targets.DoLayoutList();
            
            //EditorUtility.SetDirty(_itemSetup);
        }

        /*public override VisualElement CreateInspectorGUI() {
            // Each editor window contains a root VisualElement object
            VisualElement root = new VisualElement();

            // VisualElements objects can contain other VisualElement following a tree hierarchy.
            VisualElement label = new Label("Hello World! From C#");
            root.Add(label);

            // Import UXML
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath("7cf2c731f759d7d4390271437e0b08b7"));
            if (visualTree) {
                VisualElement labelFromUXML = visualTree.CloneTree();
                root.Add(labelFromUXML);
            }

            // A stylesheet can be added to a VisualElement.
            // The style will be applied to the VisualElement and all of its children.
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath("4630ed2041dfd814ea1b5b2397ad1cf1"));
            if (styleSheet) {
                VisualElement labelWithStyle = new Label("Hello World! With Style");
                labelWithStyle.styleSheets.Add(styleSheet);
                root.Add(labelWithStyle);
            }
            
            root.Add(new IMGUIContainer(OnInspectorGUI));
            
            return root;
        }*/
    }
}