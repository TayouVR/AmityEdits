// SPDX-License-Identifier: GPL-3.0-only
/*
 *  Copyright (C) 2023 Tayou <git@tayou.org>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using nadena.dev.ndmf;
using org.Tayou.AmityEdits;
using UnityEditor;
using UnityEngine;

namespace org.Tayou.AmityEdits.ShaderPatcher {
    /// Auto-computed Selore parameters derived from the renderer + component
    /// transforms. Each value is overridable on the SeloreShaderPatcher component.
    public struct SeloreAutoParams {
        /// Penetrator origin in the renderer's object space.
        public Vector3 startPositionOS;
        /// Penetrator orientation as euler angles (degrees) in the renderer's
        /// object space.
        public Vector3 startRotationEuler;
        /// Penetrator length in object-space meters.
        public float length;
    }

    public static class SeloreConfigurer {
        private const string SelorePatched = "_Selore_Patched";

        private const string PropEnabled = "Selore_PenetratorEnabled";
        private const string PropDeformStrength = "Selore_DeformStrength";
        private const string PropStartPosition = "Selore_StartPosition";
        private const string PropStartRotation = "Selore_StartRotation";
        private const string PropLength = "Selore_PenetratorLength";
        private const string PropChannel = "Selore_Channel";
        private const string PropAllTheWayThrough = "Selore_AllTheWayThrough";

        /// Patch the shader of `original`, then write all Selore_* properties on a
        /// fresh copy of the material. Returns the new patched material. The new
        /// material is added to ctx.AssetContainer so it gets serialized into the
        /// built avatar.
        public static Material ConfigureSeloreMaterial(
            BuildContext ctx,
            Renderer renderer,
            Material original,
            SeloreShaderPatcher plug,
            SeloreAutoParams auto
        ) {
            var m = new Material(original);
            AssetDatabase.AddObjectToAsset(m, ctx.AssetContainer);

            SelorePatcher.Patch(m, ctx, plug.keepImports, plug.shaderToPatch);

            // Prevent material-property strippers (Poiyomi, lilToon) from
            // dropping our properties because nothing in the shader reads them
            // statically.
            if (m.shader != null) {
                var count = ShaderUtil.GetPropertyCount(m.shader);
                for (var i = 0; i < count; i++) {
                    var propertyName = ShaderUtil.GetPropertyName(m.shader, i);
                    if (propertyName.StartsWith("Selore_") || propertyName == SelorePatched) {
                        m.SetOverrideTag(propertyName + "Animated", "1");
                    }
                }
            }

            // Resolve final values: component override wins over auto-computed.
            var startPos = plug.overrideStartPosition ? plug.startPosition : auto.startPositionOS;
            var startRot = plug.overrideStartRotation ? plug.startRotation : auto.startRotationEuler;
            var length = plug.overrideLength ? plug.length : auto.length;

            SetFloatIfHas(m, PropEnabled, plug.featureDeformationEnabled ? plug.deformationEnabled : 0f);
            SetFloatIfHas(m, PropDeformStrength, Mathf.Clamp01(plug.deformStrength));
            SetVectorIfHas(m, PropStartPosition, new Vector4(startPos.x, startPos.y, startPos.z, 0f));
            SetVectorIfHas(m, PropStartRotation, new Vector4(startRot.x, startRot.y, startRot.z, 0f));
            SetFloatIfHas(m, PropLength, length);
            SetFloatIfHas(m, PropChannel, (float)(int)plug.channel);
            SetFloatIfHas(m, PropAllTheWayThrough, plug.allTheWayThrough ? 1f : 0f);

            return m;
        }

        public static bool IsSelorePatched(Material mat) {
            return mat && mat.HasProperty(SelorePatched);
        }

        private static void SetFloatIfHas(Material m, string name, float value) {
            if (m.HasProperty(name)) m.SetFloat(name, value);
        }

        private static void SetVectorIfHas(Material m, string name, Vector4 value) {
            if (m.HasProperty(name)) m.SetVector(name, value);
        }
    }
}
