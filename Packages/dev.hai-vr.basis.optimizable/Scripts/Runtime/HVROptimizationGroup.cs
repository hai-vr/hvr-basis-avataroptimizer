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
        
        /// Tags that Component must be able to execute itself even if it is disabled or the GameObject hierarchy it belongs in is disabled.
        ComponentRunsEvenWhenOff,
        
        /// (HVROptimizationGroupBlendShape) Sets the value of those BlendShapes to all the subjects at once.
        BlendShapeVaries,
        
        // (HVROptimizationGroupMaterialPropertyBlock) Sets the material shader property inside all the subjects at once.
        MaterialPropertyBlockVaries,
        
        // Can change a specific material slot of all the subjects at once.
        ProvidesSupplementalMaterials
    }

    public class HVROptimizationGroupBlendShapeVaries
    {
        public string[] blendShapeNames;
    }

    public class HVROptimizationMaterialPropertyBlockVaries
    {
        public string[] shaderPropertyNames;
    }

    public class HVROptimizationProvidesSupplementalMaterials
    {
        public int slot;
        public Material[] materials;
    }
}