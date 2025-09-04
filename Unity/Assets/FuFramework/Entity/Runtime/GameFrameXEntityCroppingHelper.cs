using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entity.Runtime
{
    public class GameFrameXEntityCroppingHelper : MonoBehaviour
    {
        private void Start()
        {
            _ = typeof(HideEntityCompleteEventArgs);
            _ = typeof(ShowEntityFailureEventArgs);
            _ = typeof(ShowEntitySuccessEventArgs);
            _ = typeof(EntityManager);
            _ = typeof(DefaultEntityHelper);
            _ = typeof(AttachEntityInfo);
            _ = typeof(Entity);
            _ = typeof(EntityLogic);
        }
    }
}