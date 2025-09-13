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
using System;
using System.IO;
using nadena.dev.ndmf;
using nadena.dev.ndmf.fluent;
using UnityEditor;
using UnityEngine;

[assembly: ExportsPlugin(
    typeof(org.Tayou.AmityEdits.AmityEditsPlugin)
)]

namespace org.Tayou.AmityEdits {
    public class AmityEditsPlugin : Plugin<AmityEditsPlugin> {
        
        /// GUID for package.json
        private const string PackageJsonGuid = "d8946230fdb0492db31311fd7566afd0";
        private static string version;
        
        public override string QualifiedName => "org.tayou.amity-edits";

        public const string Name = "Amity Edits";
        public override string DisplayName => Name;
        

        /// Current version of Amity Edits
        public static string Version {
            get {
                if (string.IsNullOrEmpty(version)) {
                    string assetPath = AssetDatabase.GUIDToAssetPath(PackageJsonGuid);
                    if(String.IsNullOrEmpty(assetPath))
                        version = "Development";
                    else
                        version = JsonUtility.FromJson<PackageManifestData>(File.ReadAllText(Path.GetFullPath(assetPath))).version;
                }
                
                return version;
            }
        }
        
        /// Partial Implementation of the package manifest for deserialization
        public class PackageManifestData {
            public string version;
        }
        
        protected override void Configure() {
            Sequence sequence = InPhase(BuildPhase.Resolving);
            // Do Resolving Operations here
            
            sequence = InPhase(BuildPhase.Generating);
            sequence.Run("Run Item Setups", ctx => new ItemSetupPass(ctx).Process());
            sequence.Run("Run Clothing Manager", ctx => new ClothingManagerPass(ctx).Process());
            sequence.Run("Run SPS Plug Patcher", ctx => new SPSPlugPass(ctx).Process());
            
            sequence = InPhase(BuildPhase.Transforming);
            // Do Transforming Operations here
            sequence.Run("Run Move Object", ctx => new MoveObjectPass(ctx).Process());
            
            sequence = InPhase(BuildPhase.Optimizing);
            // Do Optimizations here
        }
    }
}