using System.Collections.Generic;
using HVR.Basis.Optimizable;
using UnityEngine;

namespace HVR.Basis.AvatarOptimizer.d4rk
{
    internal class PreemptiveProxy : IHVRAffectsOptimizers
    {
        private readonly Transform assetRoot;

        public PreemptiveProxy(Transform assetRoot)
        {
            this.assetRoot = assetRoot;
        }

        public List<HVROptimizationGroup> ResolveOptimizationGroups()
        {
            var results = new List<HVROptimizationGroup>();
            AppendMMDBlendShapes(results);
            return results;
        }

        private void AppendMMDBlendShapes(List<HVROptimizationGroup> results)
        {
            var body = assetRoot.Find("Body");
            if (body == null) return;

            var smr = body.GetComponent<SkinnedMeshRenderer>();
            if (smr == null) return;

            var sharedMesh = smr.sharedMesh;
            if (sharedMesh == null) return;
            
            var subjects = new Component[] { smr };

            var blendShapeCount = sharedMesh.blendShapeCount;
            for (var i = 0; i < blendShapeCount; i++)
            {
                var blendShapeName = sharedMesh.GetBlendShapeName(i);
                if (d4rkOptmizerExtractions.MMDBlendShapes.Contains(blendShapeName))
                {
                    results.Add(new HVROptimizationGroup
                    {
                        subjects = subjects,
                        kind = HVROptimizationGroupKind.BlendShapeVaries,
                        value = new HVROptimizationGroupBlendShapeVaries
                        {
                            blendShapeNames = new[] { blendShapeName }
                        }
                    });
                }
            }
        }

        public void ProcessOptimizationCommands(List<HVROptimizationCommand> commands)
        {
        }
    }
}