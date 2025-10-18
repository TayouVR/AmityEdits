// SPDX-License-Identifier: GPL-3.0-only
/*
 *  Copyright (C) 2025 Tayou <git@tayou.org>
 *
 *  Utility helpers to discover VRCFury features via reflection, without
 *  taking a hard assembly dependency on VRCFury.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace org.Tayou.AmityEdits.EditorUtils {
    /// <summary>
    /// Utilities for finding VRCFury features (models/builders) on an avatar or GameObject using reflection.
    /// Designed to work even if VRCFury internals are 'internal' and without compile-time references.
    /// </summary>
    public static class VRCFuryFeatureUtils {
        private const string VrcfuryComponentTypeName = "VRCFury"; // actual full name is VF.Model.VRCFury, but we match by Name for resilience
        private const string VrcfuryFullTypeName = "VF.Model.VRCFury";
        private const string FeatureFinderFullTypeName = "VF.Feature.Base.FeatureFinder";

        /// <summary>
        /// Returns all VRCFury components (VF.Model.VRCFury) found under the given root.
        /// </summary>
        public static Component[] GetVrcfuryComponents(GameObject root, bool includeInactive = true) {
            if (root == null) return Array.Empty<Component>();
            return root.GetComponentsInChildren<Component>(includeInactive)
                .Where(c => c != null && string.Equals(c.GetType().FullName, VrcfuryFullTypeName, StringComparison.Ordinal))
                .ToArray();
        }

        /// <summary>
        /// Enumerate all feature model objects from all VRCFury components under root.
        /// Returns the raw model instances (internal VRCFury FeatureModel).
        /// </summary>
        public static IEnumerable<object> EnumerateAllFeatureModels(GameObject root, bool includeInactive = true) {
            foreach (var c in GetVrcfuryComponents(root, includeInactive)) {
                foreach (var f in GetFeaturesFromComponent(c)) {
                    if (f != null) yield return f;
                }
            }
        }

        /// <summary>
        /// Returns the feature model instances from a single VRCFury component by invoking GetAllFeatures() via reflection.
        /// </summary>
        public static IEnumerable<object> GetFeaturesFromComponent(Component vrcfComponent) {
            var results = new List<object>();
            if (vrcfComponent == null) return results;
            var type = vrcfComponent.GetType();
            if (!string.Equals(type.FullName, VrcfuryFullTypeName, StringComparison.Ordinal)) return results;

            try {
                var method = type.GetMethod("GetAllFeatures", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null) return results;
                var listObj = method.Invoke(vrcfComponent, null);
                if (listObj is IEnumerable enumerable) {
                    foreach (var item in enumerable) {
                        if (item != null) results.Add(item);
                    }
                }
            } catch (Exception e) {
                Debug.LogWarning($"[Amity] Failed to enumerate VRCFury features: {e.Message}");
            }
            return results;
        }

        /// <summary>
        /// Find features by the model's full type name (e.g., "VF.Model.Feature.OverrideMenuSettings").
        /// </summary>
        public static IEnumerable<object> FindFeaturesByModelFullName(GameObject root, string modelFullTypeName, bool includeInactive = true) {
            if (string.IsNullOrEmpty(modelFullTypeName)) return Enumerable.Empty<object>();
            return EnumerateAllFeatureModels(root, includeInactive)
                .Where(m => m != null && string.Equals(m.GetType().FullName, modelFullTypeName, StringComparison.Ordinal));
        }

        /// <summary>
        /// Attempts to resolve a builder Type for a given feature model instance using VRCFury FeatureFinder.
        /// Returns null if FeatureFinder is unavailable or mapping is unknown.
        /// </summary>
        public static Type TryGetBuilderTypeForModel(object featureModel) {
            if (featureModel == null) return null;
            var modelType = featureModel.GetType();
            var featureFinder = Type.GetType(FeatureFinderFullTypeName);
            if (featureFinder == null) return null;
            try {
                var getBuilderType = featureFinder.GetMethod("GetBuilderType", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (getBuilderType == null) return null;
                var builderType = getBuilderType.Invoke(null, new object[] { modelType }) as Type;
                return builderType;
            } catch {
                return null;
            }
        }

        /// <summary>
        /// Attempts to get the OverrideMenuSettings model instance if present on the avatar root.
        /// Returns the raw model object (internal to VRCFury) or null if not found.
        /// </summary>
        public static object GetOverrideMenuSettingsModel(GameObject root, bool includeInactive = true) {
            const string overrideMenuSettingsType = "VF.Model.Feature.OverrideMenuSettings";
            return FindFeaturesByModelFullName(root, overrideMenuSettingsType, includeInactive).FirstOrDefault();
        }

        /// <summary>
        /// Tries to extract commonly-used fields from OverrideMenuSettings via reflection.
        /// Returns true if values were extracted.
        /// </summary>
        public static bool TryReadOverrideMenuSettings(object overrideMenuSettingsModel, out string nextText, out Texture2D nextIconTexture) {
            nextText = null;
            nextIconTexture = null;
            if (overrideMenuSettingsModel == null) return false;
            try {
                var type = overrideMenuSettingsModel.GetType();
                if (!string.Equals(type.FullName, "VF.Model.Feature.OverrideMenuSettings", StringComparison.Ordinal)) {
                    return false;
                }

                // nextText: string
                var nextTextField = type.GetField("nextText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (nextTextField != null) {
                    nextText = nextTextField.GetValue(overrideMenuSettingsModel) as string;
                }

                // nextIcon: GuidTexture2d -> has objRef of Texture2D
                var nextIconField = type.GetField("nextIcon", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var guidWrapper = nextIconField?.GetValue(overrideMenuSettingsModel);
                if (guidWrapper != null) {
                    var wrapperType = guidWrapper.GetType();
                    var objRefField = wrapperType.GetField("objRef", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var obj = objRefField?.GetValue(guidWrapper) as Object;
                    nextIconTexture = obj as Texture2D;
                }
                return true;
            } catch (Exception e) {
                Debug.LogWarning($"[Amity] Failed to read OverrideMenuSettings values: {e.Message}");
                return false;
            }
        }
    }
}
