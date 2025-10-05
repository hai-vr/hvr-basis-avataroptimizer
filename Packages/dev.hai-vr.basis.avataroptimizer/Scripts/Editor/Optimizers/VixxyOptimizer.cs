using System.Collections.Generic;
using System.Linq;
using HVR.Basis.Optimizable;
using HVR.Basis.Vixxy.Runtime;
using UnityEngine;

namespace HVR.Basis.AvatarOptimizer
{
    internal class VixxyOptimizer
    {
        private readonly Transform assetRoot;

        public VixxyOptimizer(Transform assetRoot)
        {
            this.assetRoot = assetRoot;
        }

        public VixxyReport DecideWhatToDo(List<HVROptimizationGroup> optimizationGroups, HashSet<Transform> enableable)
        {
            var controls = enableable
                .SelectMany(transform => transform.GetComponents<P12VixxyControl>())
                .ToList();

            var commands = new List<IHVROptimizationCommand>();
            foreach (var control in controls)
            {
                commands.Add(new HVROptimizationCommandVixxyPruned
                {
                    control = control
                });
            }

            return new VixxyReport
            {
                emittedCommands = commands
            };
        }

        public void Apply(List<IHVROptimizationCommand> commands)
        {
            foreach (var command in commands)
            {
                if (command is HVROptimizationCommandVixxyPruned pruned)
                {
                    pruned.control.OptimizePruneArrays();
                }
            }
        }
    }

    internal class HVROptimizationCommandVixxyPruned : IHVROptimizationCommand
    {
        public P12VixxyControl control;
    }

    internal class VixxyReport
    {
        public List<IHVROptimizationCommand> emittedCommands;
    }
}