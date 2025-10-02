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

        private HVRBasisAvatarOptimizer(Transform assetRoot)
        {
            this.assetRoot = assetRoot;
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

        private List<HVROptimizationCommand> PreviewExecutionPlan()
        {
            return PrepareExecutionPlan(ResolveOptimizersOf(assetRoot));
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
            var commands = DecideWhatToDo(optimizationGroups);
            return commands;
        }

        private List<HVROptimizationCommand> DecideWhatToDo(List<HVROptimizationGroup> optimizationGroups)
        {
            var commands = new List<HVROptimizationCommand>();
            
            var allTransforms = assetRoot.GetComponentsInChildren<Transform>(true).ToHashSet();
            
            HashSet<Transform> resolvedEnableable = new();
            HashSet<Transform> resolvedAlwaysOff;
            {
                var isEffectivelyOn = optimizationGroups
                    .Where(group => group.kind == HVROptimizationGroupKind.GuaranteesGameObjectTogglable)
                    .SelectMany(group => group.subjects)
                    .Select(component => component.transform)
                    .ToHashSet();
                var isEffectivelyOff = optimizationGroups
                    .Where(group => group.kind == HVROptimizationGroupKind.GameObjectEffectivelyOff)
                    .SelectMany(group => group.subjects)
                    .Select(component => component.transform)
                    .Where(transform => !isEffectivelyOn.Contains(transform))
                    .ToHashSet();
                var activability = GameObjectActivability(assetRoot, isEffectivelyOn, isEffectivelyOff);
            
                activability.Traverse(resolvedEnableable);
                resolvedAlwaysOff = allTransforms.Except(resolvedEnableable).ToHashSet();
            }
            
            // At this point, we know that all Transforms and the Components inside those transforms in resolvedAlwaysOff are worthless.
            {
                commands.Add(new HVROptimizationCommand
                {
                    kind = HVROptimizationCommandKind.GameObjectRemoved,
                    value = new HVROptimizationCommandGameObjectRemoved
                    {
                        gameObjects = resolvedAlwaysOff.Select(transform => transform.gameObject).ToList()
                    }
                });
                commands.Add(new HVROptimizationCommand
                {
                    kind = HVROptimizationCommandKind.ComponentRemoved,
                    value = new HVROptimizationCommandComponentRemoved
                    {
                        components = resolvedAlwaysOff
                            .SelectMany(transform => transform.GetComponents<Component>())
                            // null is for missing scripts.
                            .Where(component => component != null)
                            .Where(component => component is not Transform)
                            .ToList()
                    }
                });
            }

            // First of all, prune all GameObjects that are guaranteed to be OFF.
            // Then, prune all Components that are guaranteed to be OFF.

            return commands;
        }

        private InternalTreeStructure GameObjectActivability(Transform ourTransform, HashSet<Transform> isEffectivelyOn, HashSet<Transform> isEffectivelyOff)
        {
            var children = new List<InternalTreeStructure>();
            foreach (Transform childTransform in ourTransform)
            {
                children.Add(GameObjectActivability(childTransform, isEffectivelyOn, isEffectivelyOff));
            }

            var isActiveSelf = isEffectivelyOn.Contains(ourTransform) || ourTransform.gameObject.activeSelf && !isEffectivelyOff.Contains(ourTransform);
            return new InternalTreeStructure
            {
                activeSelf = isActiveSelf,
                t = ourTransform,
                children = children
            };
        }

        private static void ApplyDestructiveCommands(List<HVROptimizationCommand> commands)
        {
            foreach (var command in commands)
            {
                if (command.kind == HVROptimizationCommandKind.GameObjectRemoved)
                {
                    var order = (HVROptimizationCommandGameObjectRemoved)command.value;
                    foreach (var go in order.gameObjects)
                    {
                        // May be destroyed by a previous iteration or another command.
                        if (null != go)
                        {
                            Object.DestroyImmediate(go);
                        }
                    }
                }
                else if (command.kind == HVROptimizationCommandKind.ComponentRemoved)
                {
                    var order = (HVROptimizationCommandComponentRemoved)command.value;
                    foreach (var component in order.components)
                    {
                        // May be destroyed by a previous iteration or another command.
                        if (null != component)
                        {
                            Object.DestroyImmediate(component);
                        }
                    }
                }
            }
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

    internal class InternalTreeStructure
    {
        public bool activeSelf;
        public Transform t;
        public List<InternalTreeStructure> children;

        public void Traverse(HashSet<Transform> mutatedActiveInHierarchy)
        {
            if (activeSelf)
            {
                mutatedActiveInHierarchy.Add(t);
                foreach (var child in children)
                {
                    child.Traverse(mutatedActiveInHierarchy);
                }
            }
        }
    }
}