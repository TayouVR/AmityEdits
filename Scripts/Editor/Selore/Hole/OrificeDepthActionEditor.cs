using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace org.Tayou.AmityEdits {
    [CustomEditor(typeof(OrificeDepthAction))]
    public class OrificeDepthActionEditor : AmityBaseEditor<OrificeDepthAction> {
        private VisualElement graphContainer;
        private VisualElement pointsContainer;
        
        private Vector2 graphSize = new Vector2(400, 400);
        private float zoom = 1f;
        private Vector2 panOffset = Vector2.zero;
        
        private void OnEnable() {
            //EditorApplication.update += Update; // handle any continuous updates
        }
        
        public override VisualElement CreateInspector() {
            VisualElement root = new VisualElement();
            
            // Load USS stylesheet (optional)
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/OrificeDepthActionGraph.uss");
            if (styleSheet != null) {
                root.styleSheets.Add(styleSheet);
            }

            var depthUnitsProp = serializedObject.FindProperty("depthActionUnits");
            root.Add(new PropertyField(depthUnitsProp));
            root.Add(new Label("Depth: "));
            var depthProp = serializedObject.FindProperty("depth");
            var depthSlider = new DepthActionSlider(depthProp, Target.depthActionUnits);
            root.Add(depthSlider);
            root.Add(new Label("Width: "));
            var widthProp = serializedObject.FindProperty("penetrationWidth");
            var widthSlider = new DepthActionSliderWidth(widthProp, Target.depthActionUnits);
            root.Add(widthSlider);
            
            // Create graph container
            graphContainer = new VisualElement {
                name = "graph-container"
            };
            graphContainer.style.width = graphSize.x;
            graphContainer.style.height = graphSize.y;
            UpdateGraphSize();
            graphContainer.style.flexGrow = 1;
            graphContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            graphContainer.style.overflow = Overflow.Hidden;
            
            // Create points container (for draggable points)
            pointsContainer = new VisualElement {
                name = "points-container"
            };
            pointsContainer.style.position = Position.Absolute;
            pointsContainer.style.width = Length.Percent(100);
            pointsContainer.style.height = Length.Percent(100);
            
            graphContainer.Add(pointsContainer);
            
            // Register drawing callback
            graphContainer.generateVisualContent += OnGenerateVisualContent;
            
            // Register mouse events for panning
            graphContainer.RegisterCallback<WheelEvent>(OnWheel);
            graphContainer.RegisterCallback<MouseDownEvent>(OnMouseDown);
            
            root.Add(graphContainer);

            // Visibility controller
            void UpdateDepthUnits() {
                // Read current enum value from the property
                var units = (DepthActionUnits)depthUnitsProp.enumValueIndex;
                
                // TODO: need to update the UI here

            }

            void UpdateGraphSize() {
                // var depth = depthProp.vector2Value;
                // var width = widthProp.vector2Value;
                // var units = (DepthActionUnits)depthUnitsProp.enumValueIndex;
                //
                // graphContainer.style.width = depth.x - depth.y;
                // graphContainer.style.height =  width.x - width.y;
            }
            
            root.TrackPropertyValue(depthUnitsProp, _ => UpdateDepthUnits());
            root.TrackPropertyValue(depthProp, _ => UpdateGraphSize());
            root.TrackPropertyValue(widthProp, _ => UpdateGraphSize());
            
            root.Add(new PropertyField(serializedObject.FindProperty("actions")));
            
            RefreshGraph();
            return root;
        }
        
        private void OnGenerateVisualContent(MeshGenerationContext ctx) {
            DrawGrid(ctx);
            DrawAxes(ctx);
        }
        
        private void DrawGrid(MeshGenerationContext ctx) {
            var painter = ctx.painter2D;
            painter.strokeColor = new Color(0.3f, 0.3f, 0.3f);
            painter.lineWidth = 1f;
            var gridHeight = Target.depth.y - Target.depth.x;
            var gridWidth = Target.penetrationWidth.y - Target.penetrationWidth.x;
            
            var rect = graphContainer.contentRect;
            float gridSpacingX = gridHeight * 10f * zoom;
            float gridSpacingY = gridWidth * 10f * zoom;
            
            // Vertical lines
            for (float x = 0; x < rect.width; x += gridSpacingX) {
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, rect.height));
                painter.Stroke();
            }
            
            // Horizontal lines
            for (float y = 0; y < rect.height; y += gridSpacingY) {
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, y));
                painter.LineTo(new Vector2(rect.width, y));
                painter.Stroke();
            }
        }
        
        private void DrawAxes(MeshGenerationContext ctx) {
            if (Target == null) return;
            
            var painter = ctx.painter2D;
            var rect = graphContainer.contentRect;
            
            // Calculate center position (0,0 point)
            Vector2 center = GetGraphPosition(0, 0);
            
            // Draw X axis (depth)
            painter.strokeColor = Color.red;
            painter.lineWidth = 2f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, center.y));
            painter.LineTo(new Vector2(rect.width, center.y));
            painter.Stroke();
            
            // Draw Y axis (width)
            painter.strokeColor = Color.green;
            painter.BeginPath();
            painter.MoveTo(new Vector2(center.x, 0));
            painter.LineTo(new Vector2(center.x, rect.height));
            painter.Stroke();
        }
        
        private void RefreshGraph() {
            pointsContainer.Clear();
            
            if (Target == null || Target.actions == null) return;
            
            foreach (var actionState in Target.actions) {
                CreatePointElement(actionState);
            }
        }
        
        private void CreatePointElement(OrificeDepthActionState actionState) {
            var point = new VisualElement {
                name = "action-point"
            };
            point.style.position = Position.Absolute;
            point.style.width = 16;
            point.style.height = 16;
            point.style.borderTopLeftRadius = 8;
            point.style.borderTopRightRadius = 8;
            point.style.borderBottomLeftRadius = 8;
            point.style.borderBottomRightRadius = 8;
            point.style.backgroundColor = new Color(0.3f, 0.6f, 1f);
            point.style.borderLeftWidth = 2;
            point.style.borderRightWidth = 2;
            point.style.borderTopWidth = 2;
            point.style.borderBottomWidth = 2;
            point.style.borderLeftColor = Color.white;
            point.style.borderRightColor = Color.white;
            point.style.borderTopColor = Color.white;
            point.style.borderBottomColor = Color.white;
            
            UpdatePointPosition(point, actionState);
            
            // Make draggable
            var manipulator = new PointDragManipulator(this, actionState);
            point.AddManipulator(manipulator);
            
            // Add tooltip
            point.tooltip = $"Depth: {actionState.depth:F3}, Width: {actionState.width:F3}";
            
            pointsContainer.Add(point);
        }
        
        private void UpdatePointPosition(VisualElement point, OrificeDepthActionState actionState) {
            Vector2 pos = GetGraphPosition(actionState.depth, actionState.width);
            point.style.left = pos.x - 8; // Center the point
            point.style.top = pos.y - 8;
        }
        
        private Vector2 GetGraphPosition(float depth, float width) {
            if (Target == null) return Vector2.zero;
            
            var rect = graphContainer.contentRect;
            
            // Normalize depth and width to 0-1 range
            float depthRange = Target.depth.x - Target.depth.y;
            float widthRange = Target.penetrationWidth.x - Target.penetrationWidth.y;
            
            float normalizedDepth = (depth - Target.depth.y) / depthRange;
            float normalizedWidth = (width - Target.penetrationWidth.y) / widthRange;
            
            // Convert to screen coordinates (flip Y axis)
            float x = normalizedDepth * rect.width;
            float y = (1f - normalizedWidth) * rect.height;
            
            return new Vector2(x, y) + panOffset;
        }
        
        private Vector2 GetWorldPosition(Vector2 screenPos) {
            if (Target == null) return Vector2.zero;
            
            var rect = graphContainer.contentRect;
            
            // Remove pan offset
            Vector2 localPos = screenPos - panOffset;
            
            // Normalize to 0-1 range
            float normalizedDepth = localPos.x / rect.width;
            float normalizedWidth = 1f - (localPos.y / rect.height);
            
            // Convert to world coordinates
            float depthRange = Target.depth.x - Target.depth.y;
            float widthRange = Target.penetrationWidth.x - Target.penetrationWidth.y;
            
            float depth = normalizedDepth * depthRange + Target.depth.y;
            float width = normalizedWidth * widthRange + Target.penetrationWidth.y;
            
            return new Vector2(depth, width);
        }
        
        private void AddNewPoint() {
            if (Target == null) return;
            
            Undo.RecordObject(Target, "Add Depth Action Point");
            
            var newAction = new OrificeDepthActionState {
                depth = (Target.depth.y + Target.depth.x) / 2f,
                width = (Target.penetrationWidth.y + Target.penetrationWidth.x) / 2f,
                action = null
            };
            
            var actionList = new System.Collections.Generic.List<OrificeDepthActionState>(Target.actions ?? new OrificeDepthActionState[0]);
            actionList.Add(newAction);
            Target.actions = actionList.ToArray();
            
            EditorUtility.SetDirty(Target);
            RefreshGraph();
        }
        
        private void OnWheel(WheelEvent evt) {
            zoom = Mathf.Clamp(zoom - evt.delta.y * 0.01f, 0.5f, 2f);
            graphContainer.MarkDirtyRepaint();
        }
        
        private void OnMouseDown(MouseDownEvent evt) {
            if (evt.button == 2) { // Middle mouse button
                // Handle panning
                evt.StopPropagation();
            }
        }
        
        // Drag manipulator for points
        private class PointDragManipulator : PointerManipulator {
            private OrificeDepthActionEditor editor;
            private OrificeDepthActionState actionState;
            private bool isDragging;
            
            public PointDragManipulator(OrificeDepthActionEditor editor, OrificeDepthActionState actionState) {
                this.editor = editor;
                this.actionState = actionState;
            }
            
            protected override void RegisterCallbacksOnTarget() {
                target.RegisterCallback<PointerDownEvent>(OnPointerDown);
                target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            }
            
            protected override void UnregisterCallbacksFromTarget() {
                target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            }
            
            private void OnPointerDown(PointerDownEvent evt) {
                if (evt.button == 0) {
                    isDragging = true;
                    target.CapturePointer(evt.pointerId);
                    evt.StopPropagation();
                }
            }
            
            private void OnPointerMove(PointerMoveEvent evt) {
                if (!isDragging) return;
                
                Vector2 worldPos = editor.GetWorldPosition(evt.localPosition);
                
                Undo.RecordObject(editor.target, "Move Depth Action Point");
                actionState.depth = Mathf.Clamp(worldPos.x, editor.Target.depth.y, editor.Target.depth.x);
                actionState.width = Mathf.Clamp(worldPos.y, editor.Target.penetrationWidth.y, editor.Target.penetrationWidth.x);
                
                editor.UpdatePointPosition(target, actionState);
                target.tooltip = $"Depth: {actionState.depth:F3}, Width: {actionState.width:F3}";
                
                EditorUtility.SetDirty(editor.target);
                evt.StopPropagation();
            }
            
            private void OnPointerUp(PointerUpEvent evt) {
                if (isDragging && evt.button == 0) {
                    isDragging = false;
                    target.ReleasePointer(evt.pointerId);
                    evt.StopPropagation();
                }
            }
        }
    }
}
