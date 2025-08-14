using FuFramework.Core.Runtime;
using GameFrameX.Scene.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static SceneComponent _scene;

        /// <summary>
        /// 获取场景组件。
        /// </summary>
        public static SceneComponent Scene
        {
            get
            {
                if (!_scene) _scene = GameEntry.GetComponent<SceneComponent>();
                return _scene;
            }
        }
    }
}