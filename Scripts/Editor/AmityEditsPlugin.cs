﻿using nadena.dev.ndmf;
using nadena.dev.ndmf.fluent;
using UnityEngine;

[assembly: ExportsPlugin(
    typeof(org.Tayou.AmityEdits.AmityEditsPlugin)
)]

namespace org.Tayou.AmityEdits {
    public class AmityEditsPlugin : Plugin<AmityEditsPlugin> {
        
        public override string QualifiedName => "org.tayou.non_destructive_plugins";
        
        public override string DisplayName => "Tayous Non Destructive Plugins";
        
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