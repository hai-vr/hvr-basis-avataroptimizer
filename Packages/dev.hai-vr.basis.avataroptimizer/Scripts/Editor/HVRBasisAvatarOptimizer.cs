using System.Collections.Generic;
using System.Linq;
using Basis.Scripts.BasisSdk;
using HVR.Basis.AvatarOptimizer.d4rk;
using HVR.Basis.Comms;
using HVR.Basis.Optimizable;
using UnityEngine;

namespace HVR.Basis.AvatarOptimizer
{
    internal class HVRBasisAvatarOptimizer
    {
        private readonly Transform assetRoot;
        private readonly GameObjectOptimizer gameObjectOptimizer;
        private readonly SkinnedMeshOptimizer skinnedMeshOptimizer;

        internal HVRBasisAvatarOptimizer(Transform assetRoot)
        {
            this.assetRoot = assetRoot;

            gameObjectOptimizer = new GameObjectOptimizer(assetRoot);
            skinnedMeshOptimizer = new SkinnedMeshOptimizer(assetRoot);
        }

        public List<HVROptimizationCommand> PreviewExecutionPlan()
        {
            var affectedBy = ResolveOptimizersOf(assetRoot);
            
            return PrepareExecutionPlan(affectedBy);
        }

        public void ExecuteOptimization()
        {
            var affectedBy = ResolveOptimizersOf(assetRoot);
            
            var commands = PrepareExecutionPlan(affectedBy);

            foreach (var optimizable in affectedBy)
            {
                optimizable.ProcessOptimizationCommands(commands);
            }
            
            // TODO: Actually perform the optimization operations that may result in the removal of assets.
            // SafelyCopyAndModifyAssetsWhereApplicable();
            
            ApplyDestructiveCommands(commands);
        }

        private List<HVROptimizationCommand> PrepareExecutionPlan(List<IHVRAffectsOptimizers> affectedBy)
        {
            var optimizationGroups = affectedBy
                .SelectMany(optimizers => optimizers.ResolveOptimizationGroups())
                .ToList();

            {
                // TODO: Process the groups to decide what to do.
                // var someInternalProcessingLeader = new InternalAvatarOptimizationLeader();
                // var someInternalProcessingStructure = new List<InternalAvatarOptimizationCommands>();
                //FigureOutWhatToDo(someInternalProcessingLeader, someInternalProcessingStructure, optimizationGroups);

                // TODO: Turn those into commands.
                // TurnIntoOptimizationCommands(someInternalProcessingLeader, someInternalProcessingStructure);
            }

            // STUB: We start simple for now.
            var allCommands = new List<HVROptimizationCommand>();
            
            var gameObjectOptimizationReport = gameObjectOptimizer.DecideWhatToDo(optimizationGroups);
            var skinnedMeshOptimizationReport = skinnedMeshOptimizer.DecideWhatToDo(optimizationGroups, gameObjectOptimizationReport.enableable);
                
            allCommands.AddRange(gameObjectOptimizationReport.emittedCommands);
            allCommands.AddRange(skinnedMeshOptimizationReport.emittedCommands);
            
            return allCommands;
        }

        private void ApplyDestructiveCommands(List<HVROptimizationCommand> commands)
        {
            gameObjectOptimizer.ApplyDestructiveCommands(commands);
            skinnedMeshOptimizer.Apply(commands);
        }

        private static List<IHVRAffectsOptimizers> ResolveOptimizersOf(Transform assetRoot)
        {
            var results = new List<IHVRAffectsOptimizers>();
            
            var basisAvatarNullable = assetRoot.GetComponent<BasisAvatar>();
            if (basisAvatarNullable != null)
            {
                results.Add(new HVROptimizableAvatarProxy(basisAvatarNullable));
            }

            foreach (var automaticFaceTracking in assetRoot.GetComponentsInChildren<AutomaticFaceTracking>(true))
            {
                results.Add(new HVROptimizableAutomaticFaceTrackingProxy(automaticFaceTracking));
            }
            
            results.Add(new PreemptiveProxy(assetRoot));

            var affectedByOptimization = assetRoot.GetComponentsInChildren<IHVRAffectsOptimizers>(true);
            results.AddRange(affectedByOptimization);

            return results;
        }
    }
}