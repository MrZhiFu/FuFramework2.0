using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Config.Runtime
{
    public class GameFrameXConfigCroppingHelper : MonoBehaviour
    {
        private void Start()
        {
            _ = typeof(ConfigManager);
            _ = typeof(IConfigManager);
            _ = typeof(LoadConfigFailureEventArgs);
            _ = typeof(LoadConfigSuccessEventArgs);
            _ = typeof(LoadConfigUpdateEventArgs);
            _ = typeof(IDataTable<>);
            _ = typeof(BaseDataTable<>);
            _ = typeof(ConfigComponent);
        }
    }
}