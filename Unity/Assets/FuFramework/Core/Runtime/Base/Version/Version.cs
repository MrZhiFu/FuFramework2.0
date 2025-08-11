// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Runtime
{
    /// <summary>
    /// 版本号类。
    /// </summary>
    public static partial class Version
    {
        /// 框架版本号
        private const string GameFrameworkVersionString = "0.1.0";

      
        /// 版本号辅助器
        private static IVersionHelper _versionHelper;

        /// <summary>
        /// 获取游戏框架版本号。
        /// </summary>
        public static string GameFrameworkVersion => GameFrameworkVersionString;

        /// <summary>
        /// 获取游戏版本号。
        /// </summary>
        public static string GameVersion => _versionHelper != null ? _versionHelper.GameVersion : string.Empty;

        /// <summary>
        /// 设置版本号辅助器。
        /// </summary>
        /// <param name="versionHelper">要设置的版本号辅助器。</param>
        public static void SetVersionHelper(IVersionHelper versionHelper) => _versionHelper = versionHelper;
    }
}