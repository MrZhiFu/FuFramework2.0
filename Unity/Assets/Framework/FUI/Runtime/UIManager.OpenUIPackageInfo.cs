using GameFrameX.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// 打开界面时，界面的包信息，包括界面名称，资源包名
    /// 用来在界面打开时传递给界面实例，在界面关闭时传递给界面实例。并提供对象回收的功能。
    /// </summary>
    internal sealed class OpenUIPackageInfo : IReference
    {
        /// <summary>
        /// 界面名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 界面资源包名
        /// </summary>
        public string PackageName { get; private set; }


        /// <summary>
        /// 创建OpenUIPackageInfo实例
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="uiName"></param>
        /// <returns></returns>
        public static OpenUIPackageInfo Create(string packageName, string uiName)
        {
            var uiPackageInfo = ReferencePool.Acquire<OpenUIPackageInfo>();
            uiPackageInfo.PackageName = packageName;
            uiPackageInfo.Name        = uiName;
            return uiPackageInfo;
        }

        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            PackageName = null;
            Name        = null;
        }
    }
}