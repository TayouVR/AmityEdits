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
        
        /// <summary>
        /// GUID for package.json
        /// </summary>
        private const string PackageJsonGuid = "d8946230fdb0492db31311fd7566afd0";
        private static string version;
        
        public override string QualifiedName => "org.tayou.non_destructive_plugins";
        
        public override string DisplayName => "Tayous Non Destructive Plugins";
        

        /// <summary>
        /// Current version of VRCFury
        /// </summary>
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
        
        /// <summary>
        /// Partial Implementation of the package manifest for deserialization
        /// </summary>
        public class PackageManifestData {
            public string version;
        }
        
        protected override void Configure() {
            Sequence sequence = InPhase(BuildPhase.Resolving);
            // Do Resolving Operations here
            
            sequence = InPhase(BuildPhase.Generating);
            sequence.Run("Run Item Setups", ctx => new ItemSetupPass(ctx).Process());
            sequence.Run("Run Clothing Manager", ctx => new ClothingManagerPass(ctx).Process());
            
            sequence = InPhase(BuildPhase.Transforming);
            // Do Transforming Operations here
            
            sequence = InPhase(BuildPhase.Optimizing);
            // Do Optimizations here
        }
    }
}