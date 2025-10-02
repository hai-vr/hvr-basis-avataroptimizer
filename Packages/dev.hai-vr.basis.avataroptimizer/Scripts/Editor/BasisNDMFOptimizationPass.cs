using HVR.Basis.AvatarOptimizer;
using nadena.dev.ndmf;

[assembly: ExportsPlugin(typeof(BasisNDMFOptimizationPlugin))]
namespace HVR.Basis.AvatarOptimizer
{
    [RunsOnPlatforms("org.basisvr.basis-framework")]
    public class BasisNDMFOptimizationPlugin : Plugin<BasisNDMFOptimizationPlugin>
    {
        protected override void Configure()
        {
            InPhase(BuildPhase.Optimizing).Run("Optimize", ctx =>
            {
                var optimizer = new HVRBasisAvatarOptimizer(ctx.AvatarRootTransform);
                optimizer.ExecuteOptimization();
            });
        }
    }
}