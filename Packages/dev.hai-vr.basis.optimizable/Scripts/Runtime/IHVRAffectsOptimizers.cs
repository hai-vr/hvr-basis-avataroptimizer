using System.Collections.Generic;
using UnityEngine;

namespace HVR.Basis.Optimizable
{
    public interface IHVRAffectsOptimizers
    {
        /// Returns a list of groups. Each group is updated as a single unit independently.<br/>
        /// For example, if this returns two groups: A group that changes 1 blendShape, and another group that changes 3 blendShapes,
        /// then the 3 blendShapes *could* be merged into one blendShape as long as there is no other optimization group that modifies a subset.
        public List<HVROptimizationGroup> ResolveOptimizationGroups();

        /// Requests this component to prune itself due to optimization decisions that will be made.<br/>
        /// For example, if a Renderer is going to be merged into another, then a ComponentRemoved command will be issued;
        /// as it is assumed that all elements of an optimization group are affected the same way, then removing one will not affect the visible behavior.
        public void ProcessOptimizationCommands(List<HVROptimizationCommand> commands);
    }

    public class HVROptimizationCommand
    {
        public HVROptimizationCommandKind kind;
        public object value;
    }

    public enum HVROptimizationCommandKind
    {
        GameObjectRemoved,
        ComponentRemoved,
        BlendShapeListReduced
    }

    public class HVROptimizationCommandGameObjectRemoved
    {
        public List<GameObject> gameObjects;
    }

    public class HVROptimizationCommandComponentRemoved
    {
        public List<Component> components;
    }

    public class HVROptimizationCommandBlendShapeListReduced
    {
        public Mesh subjectMesh;
        public List<string> blendShapeNamesBefore;
        public List<string> blendShapeNamesAfter;

        public int ResolveNewIndexOrMinusOne(int indexOfBefore)
        {
            if (indexOfBefore < 0 || indexOfBefore >= blendShapeNamesBefore.Count) return -1;

            var nameOfBefore = blendShapeNamesBefore[indexOfBefore];
            var indexOfAfter = blendShapeNamesAfter.IndexOf(nameOfBefore);

            return indexOfAfter;
        }
    }
}