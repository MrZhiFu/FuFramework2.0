using GameFrameX.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取UI包管理组件。
    /// </summary>
    public static FuiPackageComponent FUIPackage
    {
        get
        {
            if (_fUIPackage == null)
            {
                _fUIPackage = GameEntry.GetComponent<FuiPackageComponent>();
            }

            return _fUIPackage;
        }
    }

    private static FuiPackageComponent _fUIPackage;
}