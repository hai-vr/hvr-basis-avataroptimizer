using System.Collections.Generic;
using System.Linq;
using Basis.Scripts.BasisSdk;
using HVR.Basis.AvatarOptimizer.d4rk;
using HVR.Basis.Comms;
using HVR.Basis.Optimizable;
using HVR.Basis.Vixxy.Runtime;
using UnityEngine;

namespace HVR.Basis.AvatarOptimizer
{
    internal class HVRBasisAvatarOptimizer
    {
        private readonly Transform assetRoot;
        private readonly GameObjectOptimizer gameObjectOptimizer;
        private readonly SkinnedMeshOptimizer skinnedMeshOptimizer;
        private readonly VixxyOptimizer vixxyOptimizer;

        internal HVRBasisAvatarOptimizer(Transform assetRoot)
        {
            this.assetRoot = assetRoot;

            gameObjectOptimizer = new GameObjectOptimizer(assetRoot);
            skinnedMeshOptimizer = new SkinnedMeshOptimizer(assetRoot);
            vixxyOptimizer = new VixxyOptimizer(assetRoot);
        }

        public List<IHVROptimizationCommand> PreviewExecutionPlan()
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

        private List<IHVROptimizationCommand> PrepareExecutionPlan(List<IHVRAffectsOptimizers> affectedBy)
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
            var allCommands = new List<IHVROptimizationCommand>();
            
            var gameObjectOptimizationReport = gameObjectOptimizer.DecideWhatToDo(optimizationGroups);
            var skinnedMeshOptimizationReport = skinnedMeshOptimizer.DecideWhatToDo(optimizationGroups, gameObjectOptimizationReport.enableable);
            var vixxyReport = vixxyOptimizer.DecideWhatToDo(optimizationGroups, gameObjectOptimizationReport.enableable);
                
            allCommands.AddRange(gameObjectOptimizationReport.emittedCommands);
            allCommands.AddRange(skinnedMeshOptimizationReport.emittedCommands);
            allCommands.AddRange(vixxyReport.emittedCommands);
            
            return allCommands;
        }

        private void ApplyDestructiveCommands(List<IHVROptimizationCommand> commands)
        {
            gameObjectOptimizer.ApplyDestructiveCommands(commands);
            skinnedMeshOptimizer.Apply(commands);
            vixxyOptimizer.Apply(commands);
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

            foreach (var control in assetRoot.GetComponentsInChildren<P12VixxyControl>(true))
            {
                results.Add(new HVRVixxyProxy(control));
            }
            
            results.Add(new PreemptiveProxy(assetRoot));

            var affectedByOptimization = assetRoot.GetComponentsInChildren<IHVRAffectsOptimizers>(true);
            results.AddRange(affectedByOptimization);

            return results;
        }
    }
}