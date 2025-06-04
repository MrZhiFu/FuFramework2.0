using GameFrameX.Runtime;
using GameFrameX.UI.FairyGUI.Runtime;

public static partial class GameApp
{
    /// <summary>
    /// 获取UI包管理组件。
    /// </summary>
    public static FairyGUIPackageComponent FUIPackage
    {
        get
        {
            if (_fUIPackage == null)
            {
                _fUIPackage = GameEntry.GetComponent<FairyGUIPackageComponent>();
            }

            return _fUIPackage;
        }
    }

    private static FairyGUIPackageComponent _fUIPackage;
}