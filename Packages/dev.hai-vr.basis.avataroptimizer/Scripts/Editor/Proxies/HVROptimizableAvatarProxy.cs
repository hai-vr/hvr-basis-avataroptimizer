using System.Collections.Generic;
using Basis.Scripts.BasisSdk;
using HVR.Basis.Optimizable;
using UnityEngine;

namespace HVR.Basis.AvatarOptimizer
{
    internal class HVROptimizableAvatarProxy : IHVRAffectsOptimizers
    {
        private readonly BasisAvatar avatar;

        public HVROptimizableAvatarProxy(BasisAvatar avatar)
        {
            this.avatar = avatar;
        }

        public List<HVROptimizationGroup> ResolveOptimizationGroups()
        {
            var results = new List<HVROptimizationGroup>();
            
            if (avatar.FaceVisemeMesh != null && avatar.FaceVisemeMesh.sharedMesh != null)
            {
                ResolveOptimizationGroupsFor(results, avatar.FaceVisemeMesh, avatar.FaceVisemeMovement);
            }
            if (avatar.FaceBlinkMesh != null && avatar.FaceBlinkMesh.sharedMesh != null)
            {
                ResolveOptimizationGroupsFor(results, avatar.FaceBlinkMesh, avatar.BlinkViseme);
            }

            return results;
        }

        private void ResolveOptimizationGroupsFor(List<HVROptimizationGroup> resultsMutated, SkinnedMeshRenderer smr, int[] blendShapeIndices)
        {
            var sharedMesh = smr.sharedMesh;
            var blendShapeCount = sharedMesh.blendShapeCount;
            foreach (var movement in blendShapeIndices)
            {
                if (movement >= 0 && movement < blendShapeCount)
                {
                    resultsMutated.Add(new HVROptimizationGroup
                    {
                        kind = HVROptimizationGroupKind.BlendShapeVaries,
                        subjects = new Component[] { smr },
                        value = new HVROptimizationGroupBlendShapeVaries
                        {
                            blendShapeNames = new[]{ sharedMesh.GetBlendShapeName(movement) }
                        }
                    });
                }
            }
        }

        public void ProcessOptimizationCommands(List<IHVROptimizationCommand> commands)
        {
            var isVisemeMeshValid = avatar.FaceVisemeMesh != null && avatar.FaceVisemeMesh.sharedMesh != null;
            var isBlinkMeshValid = avatar.FaceBlinkMesh != null && avatar.FaceBlinkMesh.sharedMesh != null;
            if (!isVisemeMeshValid && !isBlinkMeshValid) return;
            
            foreach (var command in commands)
            {
                if (command is HVROptimizationCommandBlendShapeListReduced)
                {
                    var changed = (HVROptimizationCommandBlendShapeListReduced)command;
                    if (isVisemeMeshValid)
                    {
                        RebuildIndexIfApplicable(avatar.FaceVisemeMovement, avatar.FaceVisemeMesh, changed);
                    }
                    if (isBlinkMeshValid)
                    {
                        RebuildIndexIfApplicable(avatar.BlinkViseme, avatar.FaceBlinkMesh, changed);
                    }
                }
            }
        }

        private void RebuildIndexIfApplicable(int[] arrayMutated, SkinnedMeshRenderer smr, HVROptimizationCommandBlendShapeListReduced reduced)
        {
            if (reduced.subjectMesh != smr.sharedMesh) return;
            
            for (var index = 0; index < arrayMutated.Length; index++)
            {
                var previousIndex = arrayMutated[index];
                arrayMutated[index] = reduced.ResolveNewIndexOrMinusOne(previousIndex);
            }
        }
    }
}