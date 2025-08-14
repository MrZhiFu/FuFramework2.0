using FuFramework.Core.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static ObjectPoolComponent _objectPool;

        /// <summary>
        /// 获取对象池组件。
        /// </summary>
        public static ObjectPoolComponent ObjectPool
        {
            get
            {
                if (!_objectPool) _objectPool = GameEntry.GetComponent<ObjectPoolComponent>();
                return _objectPool;
            }
        }
    }
}