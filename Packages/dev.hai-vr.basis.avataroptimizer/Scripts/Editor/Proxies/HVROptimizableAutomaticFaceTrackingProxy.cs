using System.Collections.Generic;
using System.Linq;
using HVR.Basis.Comms;
using HVR.Basis.Optimizable;
using UnityEngine;

namespace HVR.Basis.AvatarOptimizer
{
    internal class HVROptimizableAutomaticFaceTrackingProxy : IHVRAffectsOptimizers
    {
        private readonly AutomaticFaceTracking automaticFaceTracking;

        public HVROptimizableAutomaticFaceTrackingProxy(AutomaticFaceTracking automaticFaceTracking)
        {
            this.automaticFaceTracking = automaticFaceTracking;
        }
        
        public List<HVROptimizationGroup> ResolveOptimizationGroups()
        {
            var results = new List<HVROptimizationGroup>();

            var smrs = HVRCommsUtil.GetAvatar(automaticFaceTracking).GetComponentsInChildren<SkinnedMeshRenderer>(true);

            var files = automaticFaceTracking.ResolveFilesOrNull(smrs, out _);
            if (files == null) return results;

            var foundSmrs = automaticFaceTracking.FindSkinnedMeshes(files, smrs);
            var smrToBlendshapeNames = BlendshapeActuation.ResolveSmrToBlendshapeNames(foundSmrs.ToArray());
            foreach (var file in files)
            {
                foreach (var definition in file.definitions)
                {
                    var targets = BlendshapeActuation.ComputeTargets(smrToBlendshapeNames, definition.blendshapes, definition.onlyFirstMatch);
                    foreach (var target in targets)
                    {
                        var sharedMesh = target.Renderer.sharedMesh;
                        var blendShapes = target.BlendshapeIndices
                            .Select(i => sharedMesh.GetBlendShapeName(i))
                            .ToArray();

                        results.Add(new HVROptimizationGroup
                        {
                            kind = HVROptimizationGroupKind.BlendShapeVaries,
                            // TODO: We need to extend ComputeTargets so that it may return multiple SMRs per ComputedActuator to update as a single unit.
                            subjects = new Component[]{ target.Renderer },
                            value = new HVROptimizationGroupBlendShapeVaries
                            {
                                blendShapeNames = blendShapes
                            }
                        });
                    }
                }
            }

            return results;
        }

        public void ProcessOptimizationCommands(List<IHVROptimizationCommand> commands)
        {
            // Nothing to do. Everything is already evaluated at runtime.
        }
    }
}