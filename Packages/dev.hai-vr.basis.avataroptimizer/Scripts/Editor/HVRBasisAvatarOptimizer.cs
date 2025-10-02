using System.Collections.Generic;
using System.Linq;
using Basis.Scripts.BasisSdk;
using HVR.Basis.Optimizable;
using UnityEngine;

namespace HVR.Basis.AvatarOptimizer
{
    public class HVRBasisAvatarOptimizer
    {
        private readonly Transform assetRoot;
        private readonly List<IHVRAffectsOptimizers> affectedBy;

        public static HVRBasisAvatarOptimizer ResolveFor(Transform assetRoot)
        {
            return new HVRBasisAvatarOptimizer(assetRoot, ResolveOptimizersOf(assetRoot));
        }

        private HVRBasisAvatarOptimizer(Transform assetRoot, List<IHVRAffectsOptimizers> affectedBy)
        {
            this.assetRoot = assetRoot;
            this.affectedBy = affectedBy;
        }

        public void ExecuteOptimization()
        {
            var optimizationGroups = affectedBy
                .SelectMany(optimizers => optimizers.ResolveOptimizationGroups())
                .ToList();
            
            // TODO: Process the groups to decide what to do.
            // var someInternalProcessingLeader = new InternalAvatarOptimizationLeader();
            // var someInternalProcessingStructure = new List<InternalAvatarOptimizationCommands>();
            //FigureOutWhatToDo(someInternalProcessingLeader, someInternalProcessingStructure, optimizationGroups);

            // TODO: Turn those into commands.
            var commands = new List<HVROptimizationCommand>();
            // TurnIntoOptimizationCommands(someInternalProcessingLeader, someInternalProcessingStructure);
            
            foreach (var optimizable in affectedBy)
            {
                optimizable.ProcessOptimizationCommands(commands);
            }
            
            // TODO: Actually perform the optimization operations that may result in the removal of assets.
            // SafelyCopyAndModifyAssetsWhereApplicable();
        }

        private static List<IHVRAffectsOptimizers> ResolveOptimizersOf(Transform assetRoot)
        {
            var results = new List<IHVRAffectsOptimizers>();
            
            var basisAvatarNullable = assetRoot.GetComponent<BasisAvatar>();
            if (basisAvatarNullable != null)
            {
                results.Add(new HVROptimizableAvatarProxy(basisAvatarNullable));
            }

            var affectedByOptimization = assetRoot.GetComponentsInChildren<IHVRAffectsOptimizers>(true);
            results.AddRange(affectedByOptimization);

            return results;
        }
    }
}