using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using org.Tayou.AmityEdits;

namespace org.Tayou.AmityEdits.ShaderPatcher {
    public static class SpsConfigurer {
        private const string SpsEnabled = "_SPS_Enabled";
        private const string SpsLength = "_SPS_Length";
        private const string SpsOverrun = "_SPS_Overrun";
        private const string SpsBakedLength = "_SPS_BakedLength";
        private const string SpsBake = "_SPS_Bake";

        public static void ConfigureSpsMaterial(
            BuildContext ctx,
            Renderer skin,
            Material original,
            float worldLength,
            Texture2D spsBaked,
            SPSPlug plug,
            GameObject bakeRoot,
            IList<string> spsBlendshapes
        ) {

            var m = new Material(original);
            AssetDatabase.AddObjectToAsset(m, ctx.AssetContainer);
            SpsPatcher.Patch(m, ctx, plug.spsKeepImports, plug.shaderToPatch);
            {
                // Prevent poi from stripping our parameters
                var count = ShaderUtil.GetPropertyCount(m.shader);
                for (var i = 0; i < count; i++) {
                    var propertyName = ShaderUtil.GetPropertyName(m.shader, i);
                    if (propertyName.StartsWith("_SPS_")) {
                       m.SetOverrideTag(propertyName + "Animated", "1");
                    }
                }
            }
            m.SetFloat(SpsEnabled, plug.deformationEnabled);
            if (plug.deformationEnabled == 0) bakeRoot.active = false;
            m.SetFloat(SpsLength, worldLength);
            m.SetFloat(SpsBakedLength, worldLength);
            m.SetFloat(SpsOverrun, plug.spsOverrun ? 1 : 0);
            m.SetTexture(SpsBake, spsBaked);
            m.SetFloat("_SPS_BlendshapeCount", spsBlendshapes.Count);
            m.SetFloat("_SPS_BlendshapeVertCount", skin.GetVertexCount());
            for (var i = 0; i < spsBlendshapes.Count; i++) {
                var name = spsBlendshapes[i];
                if (skin.HasBlendshape(name)) {
                    m.SetFloat("_SPS_Blendshape" + i, ((SkinnedMeshRenderer)skin).GetBlendShapeWeight(name));
                }
            }
        }

        public static bool IsSps(Material mat) {
            return mat && mat.HasProperty(SpsBake);
        }
    }
}
