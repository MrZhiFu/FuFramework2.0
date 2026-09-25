// ReSharper disable once CheckNamespace
namespace FuFramework.Core.Editor
{
    /// <summary>
    /// FuFramework 编辑器菜单优先级集中定义。
    /// 功能：
    ///     1. 所有 [MenuItem] 的 priority 一律引用本类常量，禁止再写字面量魔数，便于统一调整菜单顺序。
    ///     2. 按菜单组分段管理：每组一个基准值（私有常量），组内成员 = 基准值 + 偏移量；整组调序只需修改基准值。
    ///     3. 同一父菜单下相邻两项 priority 差 >= 11 时，Unity 会自动插入分隔线；各段基准值之间的间隔已保证组与组之间有分隔线。
    /// 注意：
    ///     新增菜单项时必须先在本类登记常量，再在 MenuItem 中引用，不得直接写数字。
    /// </summary>
    public static class FuMenuPriority
    {
        #region 小游戏

        /// <summary>
        /// 小游戏段基准值。
        /// </summary>
        private const int MINI_GAME_BASE = 10;

        /// <summary>
        /// 微信小游戏：开启。
        /// </summary>
        public const int MINI_GAME_WECHAT_OPEN = MINI_GAME_BASE + 0;

        /// <summary>
        /// 微信小游戏：关闭。
        /// </summary>
        public const int MINI_GAME_WECHAT_CLOSE = MINI_GAME_BASE + 1;

        /// <summary>
        /// 抖音小游戏：开启。
        /// </summary>
        public const int MINI_GAME_DOUYIN_OPEN = MINI_GAME_BASE + 10;

        /// <summary>
        /// 抖音小游戏：关闭。
        /// </summary>
        public const int MINI_GAME_DOUYIN_CLOSE = MINI_GAME_BASE + 11;

        #endregion

        #region 构建

        /// <summary>
        /// 构建产物段基准值（各平台打包）。
        /// </summary>
        private const int BUILD_PRODUCT_BASE = 200;

        /// <summary>
        /// 构建 Windows X64 平台产物。
        /// </summary>
        public const int BUILD_PRODUCT_WINDOWS_X64 = BUILD_PRODUCT_BASE + 0;

        /// <summary>
        /// 构建 MacOS 平台产物。
        /// </summary>
        public const int BUILD_PRODUCT_MACOS = BUILD_PRODUCT_BASE + 1;

        /// <summary>
        /// 构建 Apk 平台产物。
        /// </summary>
        public const int BUILD_PRODUCT_APK = BUILD_PRODUCT_BASE + 2;

        /// <summary>
        /// 构建 AAB 平台产物。
        /// </summary>
        public const int BUILD_PRODUCT_AAB = BUILD_PRODUCT_BASE + 3;

        /// <summary>
        /// 构建 WebGL 平台产物。
        /// </summary>
        public const int BUILD_PRODUCT_WEBGL = BUILD_PRODUCT_BASE + 4;

        /// <summary>
        /// 构建微信小游戏 WebGL 平台产物。
        /// </summary>
        public const int BUILD_PRODUCT_WECHAT_MINI_GAME_WEBGL = BUILD_PRODUCT_BASE + 5;

        /// <summary>
        /// 构建 Xcode 工程（Debug）。
        /// </summary>
        public const int BUILD_PRODUCT_XCODE_DEBUG = BUILD_PRODUCT_BASE + 6;

        /// <summary>
        /// 构建 Xcode 工程（Release）。
        /// </summary>
        public const int BUILD_PRODUCT_XCODE_RELEASE = BUILD_PRODUCT_BASE + 7;

        /// <summary>
        /// 构建热更段基准值（热更代码 DLL 处理）。
        /// </summary>
        private const int BUILD_HOTFIX_BASE = 300;

        /// <summary>
        /// 复制热更新代码 DLL 到 Assets/Bundles/Code。
        /// </summary>
        public const int BUILD_HOTFIX_COPY_HOTFIX_CODE = BUILD_HOTFIX_BASE + 0;

        /// <summary>
        /// 复制 AOT 代码 DLL 到 Assets/Bundles/AOTCode。
        /// </summary>
        public const int BUILD_HOTFIX_COPY_AOT_CODE = BUILD_HOTFIX_BASE + 1;

        /// <summary>
        /// 构建程序集标记段基准值（HotFix.asmdef 编辑器兼容标记）。
        /// </summary>
        private const int BUILD_ASMDEF_BASE = 400;

        /// <summary>
        /// 标记 HotFix.asmdef 程序集在 Editor 环境下也可使用。
        /// </summary>
        public const int BUILD_ASMDEF_MARK_EDITOR_AVAILABLE = BUILD_ASMDEF_BASE + 0;

        /// <summary>
        /// 标记 HotFix.asmdef 程序集仅在非 Editor 环境（运行时）下使用。
        /// </summary>
        public const int BUILD_ASMDEF_MARK_RUNTIME_ONLY = BUILD_ASMDEF_BASE + 1;

        /// <summary>
        /// 构建 WebGL 工具段基准值（WebGL 构建辅助工具）。
        /// </summary>
        private const int BUILD_WEBGL_TOOLS_BASE = 450;

        /// <summary>
        /// 输出 WebGL 平台的 HybridCLR il2cpp 目录设置命令行。
        /// </summary>
        public const int BUILD_WEBGL_HYBRIDCLR_COMMAND = BUILD_WEBGL_TOOLS_BASE + 0;

        #endregion

        #region 目录

        /// <summary>
        /// 打开文件夹段基准值。
        /// </summary>
        private const int OPEN_FOLDER_BASE = 500;

        /// <summary>
        /// 打开 Data Path 文件夹。
        /// </summary>
        public const int OPEN_FOLDER_DATA = OPEN_FOLDER_BASE + 0;

        /// <summary>
        /// 打开 Persistent Data Path 文件夹。
        /// </summary>
        public const int OPEN_FOLDER_PERSISTENT_DATA = OPEN_FOLDER_BASE + 1;

        /// <summary>
        /// 打开 Streaming Assets Path 文件夹。
        /// </summary>
        public const int OPEN_FOLDER_STREAMING_ASSETS = OPEN_FOLDER_BASE + 2;

        /// <summary>
        /// 打开 Temporary Cache Path 文件夹。
        /// </summary>
        public const int OPEN_FOLDER_TEMPORARY_CACHE = OPEN_FOLDER_BASE + 3;

        /// <summary>
        /// 打开 Console Log Path 文件夹。
        /// </summary>
        public const int OPEN_FOLDER_CONSOLE_LOG = OPEN_FOLDER_BASE + 4;

        #endregion

        #region 日志

        /// <summary>
        /// 日志总开关段基准值。
        /// </summary>
        private const int LOG_SWITCH_BASE = 600;

        /// <summary>
        /// 开启所有日志。
        /// </summary>
        public const int LOG_ENABLE_ALL = LOG_SWITCH_BASE + 0;

        /// <summary>
        /// 禁用所有日志。
        /// </summary>
        public const int LOG_DISABLE_ALL = LOG_SWITCH_BASE + 1;

        /// <summary>
        /// 日志级别段基准值（开启某级别及以上）。
        /// </summary>
        private const int LOG_ENABLE_LEVEL_BASE = 700;

        /// <summary>
        /// 开启信息（Info）及以上级别的日志。
        /// </summary>
        public const int LOG_ENABLE_INFO_ABOVE = LOG_ENABLE_LEVEL_BASE + 0;

        /// <summary>
        /// 开启调试（Debug）及以上级别的日志。
        /// </summary>
        public const int LOG_ENABLE_DEBUG_ABOVE = LOG_ENABLE_LEVEL_BASE + 1;

        /// <summary>
        /// 开启警告（Warning）及以上级别的日志。
        /// </summary>
        public const int LOG_ENABLE_WARNING_ABOVE = LOG_ENABLE_LEVEL_BASE + 2;

        /// <summary>
        /// 开启错误（Error）及以上级别的日志。
        /// </summary>
        public const int LOG_ENABLE_ERROR_ABOVE = LOG_ENABLE_LEVEL_BASE + 3;

        /// <summary>
        /// 开启严重错误（Fatal）及以上级别的日志。
        /// </summary>
        public const int LOG_ENABLE_FATAL_ABOVE = LOG_ENABLE_LEVEL_BASE + 4;

        /// <summary>
        /// 日志级别段基准值（仅开启某一级别）。
        /// </summary>
        private const int LOG_ONLY_LEVEL_BASE = 800;

        /// <summary>
        /// 仅开启信息（Info）级别日志。
        /// </summary>
        public const int LOG_ONLY_INFO = LOG_ONLY_LEVEL_BASE + 0;

        /// <summary>
        /// 仅开启调试（Debug）级别日志。
        /// </summary>
        public const int LOG_ONLY_DEBUG = LOG_ONLY_LEVEL_BASE + 1;

        /// <summary>
        /// 仅开启警告（Warning）级别日志。
        /// </summary>
        public const int LOG_ONLY_WARNING = LOG_ONLY_LEVEL_BASE + 2;

        /// <summary>
        /// 仅开启错误（Error）级别日志。
        /// </summary>
        public const int LOG_ONLY_ERROR = LOG_ONLY_LEVEL_BASE + 3;

        /// <summary>
        /// 仅开启严重错误（Fatal）级别日志。
        /// </summary>
        public const int LOG_ONLY_FATAL = LOG_ONLY_LEVEL_BASE + 4;

        /// <summary>
        /// 网络日志段基准值。
        /// </summary>
        private const int NETWORK_LOG_BASE = 900;

        /// <summary>
        /// 开启网络响应日志打印。
        /// </summary>
        public const int NETWORK_LOG_ENABLE_RESPONSE = NETWORK_LOG_BASE + 0;

        /// <summary>
        /// 关闭网络响应日志打印。
        /// </summary>
        public const int NETWORK_LOG_DISABLE_RESPONSE = NETWORK_LOG_BASE + 1;

        /// <summary>
        /// 开启网络请求日志打印。
        /// </summary>
        public const int NETWORK_LOG_ENABLE_REQUEST = NETWORK_LOG_BASE + 3;

        /// <summary>
        /// 关闭网络请求日志打印。
        /// </summary>
        public const int NETWORK_LOG_DISABLE_REQUEST = NETWORK_LOG_BASE + 4;

        #endregion

        #region SRDebugger 工具

        /// <summary>
        /// SRDebugger 工具段基准值。
        /// </summary>
        private const int SR_DEBUGGER_BASE = 950;

        /// <summary>
        /// 开启 SRDebugger 工具。
        /// </summary>
        public const int SR_DEBUGGER_ENABLE = SR_DEBUGGER_BASE + 0;

        /// <summary>
        /// 关闭 SRDebugger 工具。
        /// </summary>
        public const int SR_DEBUGGER_DISABLE = SR_DEBUGGER_BASE + 1;

        #endregion

        #region 配置表与多语言

        /// <summary>
        /// 配置表段基准值。
        /// </summary>
        private const int CONFIG_BASE = 1000;

        /// <summary>
        /// 导出配置表—Json。
        /// </summary>
        public const int CONFIG_EXPORT_JSON = CONFIG_BASE + 0;

        /// <summary>
        /// 导出配置表—Bin。
        /// </summary>
        public const int CONFIG_EXPORT_BIN = CONFIG_BASE + 1;

        /// <summary>
        /// 多语言检查段基准值。
        /// </summary>
        private const int L10N_BASE = 1010;

        /// <summary>
        /// 生成现存问题报告。
        /// </summary>
        public const int L10N_GENERATE_ISSUE_REPORT = L10N_BASE + 0;

        /// <summary>
        /// 打开现存问题报告。
        /// </summary>
        public const int L10N_OPEN_ISSUE_REPORT = L10N_BASE + 1;

        /// <summary>
        /// 清理多语言配置表。
        /// </summary>
        public const int L10N_CLEAN_TABLE = L10N_BASE + 2;

        /// <summary>
        /// Proto 段基准值。
        /// </summary>
        private const int PROTO_BASE = 1002;

        /// <summary>
        /// 导出 Proto—客户端。
        /// </summary>
        public const int PROTO_EXPORT_CLIENT = PROTO_BASE + 0;

        /// <summary>
        /// 导出 Proto—服务端。
        /// </summary>
        public const int PROTO_EXPORT_SERVER = PROTO_BASE + 1;

        /// <summary>
        /// 导出 Proto—全部。
        /// </summary>
        public const int PROTO_EXPORT_ALL = PROTO_BASE + 2;

        #endregion

        #region 网络类型设置

        /// <summary>
        /// 网络类型设置段基准值。
        /// </summary>
        private const int NETWORK_TYPE_BASE = 1100;

        /// <summary>
        /// 不强制使用 WebSocket 网络。
        /// </summary>
        public const int NETWORK_TYPE_NO_FORCE_WEBSOCKET = NETWORK_TYPE_BASE + 0;

        /// <summary>
        /// 强制使用 WebSocket 网络。
        /// </summary>
        public const int NETWORK_TYPE_FORCE_WEBSOCKET = NETWORK_TYPE_BASE + 1;

        #endregion

        #region 通用工具

        /// <summary>
        /// 通用工具段基准值。
        /// </summary>
        private const int TOOL_BASE = 1200;

        /// <summary>
        /// 代码防裁剪工具。
        /// </summary>
        public const int TOOL_CROPPING = TOOL_BASE + 0;

        /// <summary>
        /// 删除本地游戏数据。
        /// </summary>
        public const int TOOL_DELETE_GAME_DATA = TOOL_BASE + 10;

        /// <summary>
        /// HttpCDN 服务器段基准值。
        /// </summary>
        private const int HTTP_CDN_BASE = 1300;

        /// <summary>
        /// 启动 HttpCDN 服务器（用于模拟资源更新）。
        /// </summary>
        public const int TOOL_START_HTTP_CDN_SERVER = HTTP_CDN_BASE + 0;

        #endregion

        #region 调试面板

        /// <summary>
        /// 调试面板段基准值（各模块调试窗口，置于菜单最尾）。
        /// </summary>
        private const int DEBUG_PANEL_BASE = 1400;

        /// <summary>
        /// 配置调试面板。
        /// </summary>
        public const int DEBUG_PANEL_CONFIG = DEBUG_PANEL_BASE + 0;

        /// <summary>
        /// 对象池调试面板。
        /// </summary>
        public const int DEBUG_PANEL_OBJECT_POOL = DEBUG_PANEL_BASE + 1;

        /// <summary>
        /// 红点调试面板。
        /// </summary>
        public const int DEBUG_PANEL_RED_DOT = DEBUG_PANEL_BASE + 2;

        /// <summary>
        /// 引用池调试面板。
        /// </summary>
        public const int DEBUG_PANEL_REFERENCE_POOL = DEBUG_PANEL_BASE + 3;

        /// <summary>
        /// Web 模块调试面板。
        /// </summary>
        public const int DEBUG_PANEL_WEB = DEBUG_PANEL_BASE + 4;

        #endregion
    }
}