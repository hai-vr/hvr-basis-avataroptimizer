using System.Collections.Generic;
using HVR.Basis.Optimizable;
using HVR.Basis.Vixxy.Runtime;

namespace HVR.Basis.AvatarOptimizer
{
    internal class HVRVixxyProxy : IHVRAffectsOptimizers
    {
        private readonly P12VixxyControl control;

        public HVRVixxyProxy(P12VixxyControl control)
        {
            this.control = control;
        }

        public List<HVROptimizationGroup> ResolveOptimizationGroups()
        {
            return new List<HVROptimizationGroup>(); // TODO: Toggles, etc.
        }

        public void ProcessOptimizationCommands(List<IHVROptimizationCommand> commands)
        {
            foreach (var hvrOptimizationCommand in commands)
            {
                
            }
        }
    }
}