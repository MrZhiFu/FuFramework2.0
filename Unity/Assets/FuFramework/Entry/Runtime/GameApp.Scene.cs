using GameFrameX.Runtime;
using GameFrameX.Scene.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取场景组件。
    /// </summary>
    public static SceneComponent Scene
    {
        get
        {
            if (_scene == null)
            {
                _scene = GameEntry.GetComponent<SceneComponent>();
            }

            return _scene;
        }
    }

    private static SceneComponent _scene;
}