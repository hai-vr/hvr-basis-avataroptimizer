using System.Collections.Generic;
using System.Linq;
using HVR.Basis.Optimizable;
using UnityEngine;

namespace HVR.Basis.AvatarOptimizer
{
    internal class SkinnedMeshOptimizer
    {
        private readonly Transform assetRoot;

        public SkinnedMeshOptimizer(Transform assetRoot)
        {
            this.assetRoot = assetRoot;
        }

        public SkinnedMeshOptimizationReport DecideWhatToDo(List<HVROptimizationGroup> optimizationGroups, HashSet<Transform> enableable)
        {
            var blendShapeOptimizationGroups = optimizationGroups
                .Where(group => group.kind == HVROptimizationGroupKind.BlendShapeVaries)
                .ToList();

            var enableableSmrs = enableable
                .Select(t => t.GetComponent<SkinnedMeshRenderer>())
                .Where(renderer => renderer != null)
                .Where(renderer => renderer.sharedMesh != null)
                .ToHashSet();

            return MakeReport(blendShapeOptimizationGroups, enableableSmrs);
        }

        private SkinnedMeshOptimizationReport MakeReport(List<HVROptimizationGroup> blendShapeOptimizationGroups, HashSet<SkinnedMeshRenderer> enableableSmrs)
        {
            // # Blendshape merging rules:
            // - If multiple SMRs refer to the same mesh, and a blendshape varies, then that blendshape cannot be baked into the mesh.
            // - If multiple SMRs refer to the same mesh, and a blendshape does not vary, but has a different default value for each mesh, then it cannot be baked.
            // - Otherwise, a blendshape can be baked into the mesh.
            //
            // # SMR merging rules:
            // - If multiple SMRs refer to the same mesh, but only one of them varies the blendshape, then those SMRs cannot be merged together.
            // - If two optimization groups toggle the Component or the GameObject of any of its hierarchy differently to the SMRs that refer to the same mesh, then those SMRs cannot be merged together.
            //
            // # Special rules:
            // - Blendshapes must not be given a different name than the one it was originally set to because of runtime components. (THIS MAY BE FIXABLE USING RUNTIME AWARENESS OF OPTIMIZATION PASS)
            
            var meshToUpdateGroups = new Dictionary<Mesh, List<BlendShapesThatAreUpdatedTogether>>();
            foreach (var optimizationGroup in blendShapeOptimizationGroups)
            {
                var relevantSkinnedMeshes = optimizationGroup.subjects.Cast<SkinnedMeshRenderer>().Intersect(enableableSmrs).ToHashSet();
                foreach (var relevantSkinnedMesh in relevantSkinnedMeshes)
                {
                    var sharedMesh = relevantSkinnedMesh.sharedMesh;
                    if (!meshToUpdateGroups.ContainsKey(sharedMesh))
                    {
                        meshToUpdateGroups[sharedMesh] = new List<BlendShapesThatAreUpdatedTogether>();
                    }
                
                    meshToUpdateGroups[sharedMesh].Add(new BlendShapesThatAreUpdatedTogether
                    {
                        blendShapeNames = ((HVROptimizationGroupBlendShapeVaries)optimizationGroup.value).blendShapeNames.ToList()
                    });
                }
            }

            var meshToSmrs = enableableSmrs
                .GroupBy(renderer => renderer.sharedMesh)
                .ToDictionary(group => group.Key, group => group.ToList());

            var blendShapeReports = meshToSmrs.Keys
                .Select(mesh =>
                {
                    var existingBlendShapes = new List<string>();
                    for (var i = 0; i < mesh.blendShapeCount; i++)
                    {
                        existingBlendShapes.Add(mesh.GetBlendShapeName(i));
                    }
                    
                    HashSet<string> weNeedThoseBlendShapes;
                    if (meshToUpdateGroups.TryGetValue(mesh, out var group))
                    {
                        // Add blendShapes that vary.
                        weNeedThoseBlendShapes = group
                            .SelectMany(together => together.blendShapeNames)
                            .ToHashSet();
                    }
                    else
                    {
                        // This blendShape does not vary.
                        weNeedThoseBlendShapes = new HashSet<string>();
                    }
                    
                    var smrsThatUseThisMesh = meshToSmrs[mesh];
                    for (var blendShapeIndex = 0; blendShapeIndex < mesh.blendShapeCount; blendShapeIndex++)
                    {
                        var thereIsAtLeastOneSmrThatHasADifferentValueForThisBlendshape = smrsThatUseThisMesh
                            .Select(renderer => renderer.GetBlendShapeWeight(blendShapeIndex))
                            .Distinct()
                            .Count() > 1;
                        if (thereIsAtLeastOneSmrThatHasADifferentValueForThisBlendshape)
                        {
                            // We need to preserve this blendShape as multiple SMRs have set it to different values.
                            weNeedThoseBlendShapes.Add(mesh.GetBlendShapeName(blendShapeIndex));
                        }
                    }

                    return new BlendShapeReport
                    {
                        mesh = mesh,
                        existingBlendShapes = existingBlendShapes,
                        resultingBlendShapes = existingBlendShapes.Intersect(weNeedThoseBlendShapes).ToList() // This should mostly preserve the order of the blendShapes.
                    };
                })
                .ToList();

            var emittedCommands = new List<HVROptimizationCommand>();
            
            foreach (var blendShapeReport in blendShapeReports)
            {
                var isRelevant = !blendShapeReport.existingBlendShapes.SequenceEqual(blendShapeReport.resultingBlendShapes);
                if (isRelevant)
                {
                    emittedCommands.Add(new HVROptimizationCommand
                    {
                        kind = HVROptimizationCommandKind.BlendShapeListReduced,
                        value = new HVROptimizationCommandBlendShapeListReduced
                        {
                            subjectMesh = blendShapeReport.mesh,
                            blendShapeNamesBefore = blendShapeReport.existingBlendShapes.ToList(),
                            blendShapeNamesAfter = blendShapeReport.resultingBlendShapes.ToList()
                        }
                    });
                }
            }
            
            return new SkinnedMeshOptimizationReport
            {
                emittedCommands = emittedCommands
            };
        }

        public void Apply(List<HVROptimizationCommand> commands)
        {
            var blendShapeCommands = commands
                .Where(command => command.kind == HVROptimizationCommandKind.BlendShapeListReduced)
                .Select(command => (HVROptimizationCommandBlendShapeListReduced)command.value)
                .ToList();

            var meshToSmr = assetRoot
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(renderer => renderer.sharedMesh != null)
                .GroupBy(renderer => renderer.sharedMesh)
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var blendShapeCommand in blendShapeCommands)
            {
                var mesh = blendShapeCommand.subjectMesh;
                if (meshToSmr.TryGetValue(mesh, out var smrs))
                {
                    var newMesh = Object.Instantiate(mesh);
                    newMesh.ClearBlendShapes();

                    var vertexCount = mesh.vertexCount;
                    
                    var dVertices = new Vector3[vertexCount];
                    var dNormals = new Vector3[vertexCount];
                    var dTangents = new Vector3[vertexCount];
                    
                    foreach (var blendShapeName in blendShapeCommand.blendShapeNamesAfter)
                    {
                        var beforeIndex = blendShapeCommand.blendShapeNamesBefore.IndexOf(blendShapeName);
                        var frames = mesh.GetBlendShapeFrameCount(beforeIndex);
                        for (var frameIndex = 0; frameIndex < frames; frameIndex++)
                        {
                            var frameWeight = mesh.GetBlendShapeFrameWeight(beforeIndex, frameIndex);
                            
                            mesh.GetBlendShapeFrameVertices(beforeIndex, frameIndex, dVertices, dNormals, dTangents);
                            newMesh.AddBlendShapeFrame(blendShapeName, frameWeight, dVertices, dNormals, dTangents);
                        }
                    }

                    {
                        var removedBlendShapes = blendShapeCommand.blendShapeNamesBefore
                            .Except(blendShapeCommand.blendShapeNamesAfter)
                            .ToList();

                        if (removedBlendShapes.Count > 0)
                        {
                            // Bake blendShapes that are not used, using the current non-zero weight of the first SMR that uses it.
                            var firstSmr = smrs[0];
                            
                            Vector3[] vertices = null;
                            Vector3[] normals = null;
                            Vector4[] tangents = null;

                            var anythingChanged = false;
                        
                            foreach (var removedBlendShape in removedBlendShapes)
                            {
                                var blendShapeIndex = blendShapeCommand.blendShapeNamesBefore.IndexOf(removedBlendShape);
                                var wantedWeight = firstSmr.GetBlendShapeWeight(blendShapeIndex);
                                if (wantedWeight > 0)
                                {
                                    if (vertices == null)
                                    {
                                        vertices = mesh.vertices;
                                        normals = mesh.normals;
                                        tangents = mesh.tangents;
                                    }
                                    
                                    ResolveBlendShapeFrameVerticesForWeight(mesh, blendShapeIndex, dVertices, dNormals, dTangents, wantedWeight, vertexCount);
                                    for (var index = 0; index < vertices.Length; index++)
                                    {
                                        vertices[index] += dVertices[index];
                                        // Note: normals and tangents can be null on some meshes.
                                        if (normals != null) normals[index] += dNormals[index];
                                        if (tangents != null) tangents[index] += (Vector4)dTangents[index];
                                    }
                                
                                    anythingChanged = true;
                                }
                            }

                            if (anythingChanged)
                            {
                                newMesh.vertices = vertices;
                                if (normals != null) newMesh.normals = normals;
                                if (tangents != null) newMesh.tangents = tangents;
                            }
                        }
                    }

                    var newBlendShapeCount = newMesh.blendShapeCount;
                    foreach (var smr in smrs)
                    {
                        var newWeightsToAssign = new List<float>();
                        for (var newBlendShapeIndex = 0; newBlendShapeIndex < newBlendShapeCount; newBlendShapeIndex++)
                        {
                            var newBlendShapeName = blendShapeCommand.blendShapeNamesAfter[newBlendShapeIndex];
                            var oldIndex = blendShapeCommand.blendShapeNamesBefore.IndexOf(newBlendShapeName);
                            
                            newWeightsToAssign.Add(smr.GetBlendShapeWeight(oldIndex));
                        }

                        smr.sharedMesh = newMesh;

                        for (var newIndex = 0; newIndex < newWeightsToAssign.Count; newIndex++)
                        {
                            var newWeight = newWeightsToAssign[newIndex];
                            smr.SetBlendShapeWeight(newIndex, newWeight);
                        }
                    }
                }
            }
        }

        private Vector3[] dVerticesRightmost;
        private Vector3[] dNormalsRightmost;
        private Vector3[] dTangentsRightmost;
        private void ResolveBlendShapeFrameVerticesForWeight(Mesh mesh, int blendShapeIndex, Vector3[] dVertices, Vector3[] dNormals, Vector3[] dTangents, float wantedWeight, int vertexCount)
        {
            var frameCount = mesh.GetBlendShapeFrameCount(blendShapeIndex);
            if (frameCount == 1)
            {
                var frameWeight = mesh.GetBlendShapeFrameWeight(blendShapeIndex, 0);
                mesh.GetBlendShapeFrameVertices(blendShapeIndex, 0, dVertices, dNormals, dTangents);

                if (frameWeight <= wantedWeight) return; // We're good, dVertices, dNormals, and dTangents contain the correct result.
                
                var lerped = Mathf.InverseLerp(0, frameWeight, wantedWeight);
                for (var i = 0; i < vertexCount; i++)
                {
                    dVertices[i] *= lerped;
                    dNormals[i] *= lerped;
                    dTangents[i] *= lerped;
                }
            }
            else
            {
                if (dVerticesRightmost == null || dVerticesRightmost.Length != vertexCount)
                {
                    dVerticesRightmost = new Vector3[vertexCount];
                    dNormalsRightmost = new Vector3[vertexCount];
                    dTangentsRightmost = new Vector3[vertexCount];
                }
                
                var frameWeights = new List<float>();
                for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    frameWeights.Add(mesh.GetBlendShapeFrameWeight(blendShapeIndex, frameIndex));
                }

                for (var frameIndex = frameCount - 1; frameIndex >= 0; frameIndex--)
                {
                    var frameWeight = frameWeights[frameIndex];
                    if (frameWeight <= wantedWeight)
                    {
                        mesh.GetBlendShapeFrameVertices(blendShapeIndex, frameIndex, dVertices, dNormals, dTangents);
                        var nextFrameIndex = frameIndex + 1;
                        if (nextFrameIndex < frameWeights.Count)
                        {
                            mesh.GetBlendShapeFrameVertices(blendShapeIndex, nextFrameIndex, dVerticesRightmost, dNormalsRightmost, dTangentsRightmost);
                            
                            var nextFrameWeight = frameWeights[nextFrameIndex];
                            
                            var lerped = Mathf.InverseLerp(frameWeight, nextFrameWeight, wantedWeight);
                            for (var i = 0; i < vertexCount; i++)
                            {
                                dVertices[i] = Vector3.Lerp(dVertices[i], dVerticesRightmost[i], lerped);
                                dNormals[i] = Vector3.Lerp(dNormals[i], dVerticesRightmost[i], lerped);
                                dTangents[i] = Vector3.Lerp(dTangents[i], dVerticesRightmost[i], lerped);
                            }
                        }
                        else
                        {
                            // We're good, dVertices, dNormals, and dTangents contain the correct result.
                        }
                    }
                    else if (frameIndex == 0)
                    {
                        mesh.GetBlendShapeFrameVertices(blendShapeIndex, frameIndex, dVertices, dNormals, dTangents);
                        
                        var lerped = Mathf.InverseLerp(0, frameWeight, wantedWeight);
                        for (var i = 0; i < vertexCount; i++)
                        {
                            dVertices[i] *= lerped;
                            dNormals[i] *= lerped;
                            dTangents[i] *= lerped;
                        }
                    }
                }
            }
        }
    }

    internal class BlendShapeReport
    {
        public Mesh mesh;
        public List<string> existingBlendShapes;
        public List<string> resultingBlendShapes;
    }

    internal class BlendShapesThatAreUpdatedTogether
    {
        public List<string> blendShapeNames;
    }

    internal class SkinnedMeshOptimizationReport
    {
        public List<HVROptimizationCommand> emittedCommands;
    }
}