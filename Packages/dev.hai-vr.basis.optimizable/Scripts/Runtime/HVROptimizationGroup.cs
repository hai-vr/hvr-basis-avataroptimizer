using UnityEngine;

namespace HVR.Basis.Optimizable
{
    public class HVROptimizationGroup
    {
        public Component[] subjects;
        public HVROptimizationGroupKind kind;
        public object value;
    }

    public enum HVROptimizationGroupKind
    {
        /// Toggles the GameObject of all the subjects at once, guaranteeing that this can happen.
        GuaranteesGameObjectTogglable,
        
        /// Toggles the Component of all the subjects at once, guaranteeing that this can happen.
        GuaranteesComponentTogglable,
        
        /// Tags that GameObject is effectively OFF, despite it being potentially ON by default in the scene. A togglable may overrule that.
        GameObjectEffectivelyOff,
        
        /// Tags that Component is effectively OFF, despite it being potentially ON by default in the scene. A togglable may overrule that.
        ComponentEffectivelyOff,
        
        /// (HVROptimizationGroupBlendShape) Sets the value of those BlendShapes to all the subjects at once.
        BlendShape,
        
        // (HVROptimizationGroupMaterialPropertyBlock) Sets the material shader property inside all the subjects at once.
        MaterialPropertyBlock,
        
        // Can change a specific material slot of all the subjects at once.
        ProvidesSupplementalMaterials
    }

    public class HVROptimizationGroupBlendShape
    {
        public string[] blendShapeNames;
    }

    public class HVROptimizationMaterialPropertyBlock
    {
        public string[] shaderPropertyNames;
    }

    public class HVROptimizationProvidesSupplementalMaterials
    {
        public int slot;
        public Material[] materials;
    }
}