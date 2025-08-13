using FuFramework.Core.Runtime;
using FuFramework.Entity.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取实体组件。
    /// </summary>
    public static EntityComponent Entity
    {
        get
        {
            if (_entity == null)
            {
                _entity = GameEntry.GetComponent<EntityComponent>();
            }

            return _entity;
        }
    }

    private static EntityComponent _entity;
}