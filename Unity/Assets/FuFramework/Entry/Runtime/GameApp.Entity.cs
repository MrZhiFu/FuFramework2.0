using FuFramework.Core.Runtime;
using FuFramework.Entity.Runtime;

// ReSharper disable once CheckNamespace
namespace FuFramework.Entry.Runtime
{
    public static partial class GameApp
    {
        private static EntityComponent _entity;

        /// <summary>
        /// 获取实体组件。
        /// </summary>
        public static EntityComponent Entity
        {
            get
            {
                if (!_entity) _entity = GameEntry.GetComponent<EntityComponent>();
                return _entity;
            }
        }
    }
}