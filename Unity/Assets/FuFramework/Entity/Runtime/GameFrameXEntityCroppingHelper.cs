using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Runtime
{
    public class GameFrameXEntityCroppingHelper : MonoBehaviour
    {
        private void Start()
        {
            _ = typeof(EntityManager);
            _ = typeof(HideEntityCompleteEventArgs);
            _ = typeof(IEntityGroupHelper);
            _ = typeof(ShowEntityFailureEventArgs);
            _ = typeof(ShowEntitySuccessEventArgs);
            _ = typeof(DefaultEntityGroupHelper);
            _ = typeof(DefaultEntityHelper);
            _ = typeof(AttachEntityInfo);
            _ = typeof(Entity);
            _ = typeof(EntityComponent);
            _ = typeof(EntityGroupHelperBase);
            _ = typeof(EntityLogic);
            _ = typeof(ShowEntityInfo);
        }
    }
}