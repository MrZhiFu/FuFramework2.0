using GameFrameX.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取对象池组件。
    /// </summary>
    public static ObjectPoolComponent ObjectPool
    {
        get
        {
            if (_objectPool == null)
            {
                _objectPool = GameEntry.GetComponent<ObjectPoolComponent>();
            }

            return _objectPool;
        }
    }

    private static ObjectPoolComponent _objectPool;
}