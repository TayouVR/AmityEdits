using System;
using org.Tayou.AmityEdits;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace org.Tayou.AmityEdits {
    internal class DepthActionSliderWidth : BaseField<Vector2> {
        private readonly MinMaxSlider slider;
        private readonly FloatField xField;
        private readonly FloatField yField;
        
        public DepthActionSliderWidth(SerializedProperty prop, DepthActionUnits units) : base(null,null) {
            bindingPath = prop.propertyPath;
            Clear();

            var output = new VisualElement();
            output.Row();

            xField = new FloatField().FlexBasis(50);
            xField.isDelayed = true;
            xField.RegisterValueChangedCallback(e => {
                var v = e.newValue;
                Changed(new Vector2(
                    v,
                    Mathf.Max(v, value.y)
                ));
            });
            output.Add(xField);

            var c = new VisualElement();

            var test = new Label(units == DepthActionUnits.Plugs ? "\u2193 0" : units == DepthActionUnits.Local ? "\u2193 0 local-unit wide" : "\u2193 0m wide");
            test.style.position = Position.Absolute;
            test.style.bottom = 15;
            test.style.fontSize = 9;
            c.Add(test);
        
            var test3 = new Label(units == DepthActionUnits.Plugs ? "2 plug-widths \u2193" : units == DepthActionUnits.Local ? "2 local-units \u2193" : "2m \u2193");
            test3.style.position = Position.Absolute;
            test3.style.bottom = 15;
            test3.style.right = 0;
            test3.style.fontSize = 9;
            test3.style.unityTextAlign = TextAnchor.UpperRight;
            c.Add(test3);

            slider = new MinMaxSlider {
                highLimit = 2,
                lowLimit = 0
            };
            slider.RegisterValueChangedCallback(e => {
                // When using slider, store in steps of 0.2
                Changed(new Vector2(
                    (float)Math.Round(e.newValue.x, 2),
                    (float)Math.Round(e.newValue.y, 2)
                ));
            });
            c.Add(slider);

            output.style.marginTop = 20;

            output.Add(c.FlexGrow(1).FlexBasis(0));

            yField = new FloatField().FlexBasis(50);
            yField.isDelayed = true;
            yField.RegisterValueChangedCallback(e => {
                var v = e.newValue;
                Changed(new Vector2(
                    Mathf.Min(v, value.x),
                    v
                ));
            });
            output.Add(yField);

            output.FlexGrow(1);
            Add(output);
        }

        private void Changed(Vector2 newValue) {
            newValue.x = Mathf.Min(newValue.x, newValue.y);
            newValue.y = Mathf.Max(newValue.x, newValue.y);
            newValue.x = Mathf.Clamp(newValue.x, -1, 3);
            newValue.y = Mathf.Clamp(newValue.y, -1, 3);
            value = newValue;
            // This usually happens automatically, but might not if the user sets the float
            // to something invalid
            SetValueWithoutNotify(value);
        }

        public override void SetValueWithoutNotify(Vector2 newValue) {
            base.SetValueWithoutNotify(newValue);
            slider.SetValueWithoutNotify(newValue);
            xField.SetValueWithoutNotify(newValue.x);
            yField.SetValueWithoutNotify(newValue.y);
        }
    }
}
