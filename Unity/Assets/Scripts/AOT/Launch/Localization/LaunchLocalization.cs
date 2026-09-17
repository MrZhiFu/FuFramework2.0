using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using AOT.Framework.Core.Log;
using AOT.Framework.Localization;
using Luban;
using SimpleJSON;

// ReSharper disable once CheckNamespace
namespace AOT.Launch.Localization
{
    /// <summary>
    /// AOT 本地化表行数据。
    ///     对应 TbLocalizationAOT（Config/Excels/Local/L-LocalizationAOT-aot-热更前.xlsx）一行；
    ///     <see cref="LaunchLocalization"/> 的 bin 解析按 Excel 列顺序硬约定读取，调整表列时须同步本类与解析逻辑。
    /// </summary>
    public sealed class LocalizationAOTRow
    {
        /// <summary> 多语言 key </summary>
        public string Key { get; set; }

        /// <summary> 是否导出到代码中 </summary>
        public bool IsCode { get; set; }

        /// <summary> 简体中文 </summary>
        public string ChineseSimplified { get; set; }

        /// <summary> 繁体中文 </summary>
        public string ChineseTraditional { get; set; }

        /// <summary> 英语 </summary>
        public string English { get; set; }

        /// <summary> 日语 </summary>
        public string Japanese { get; set; }

        /// <summary> 韩语 </summary>
        public string Korean { get; set; }

        /// <summary> 泰语 </summary>
        public string Thai { get; set; }

        /// <summary> 印尼语 </summary>
        public string Indonesian { get; set; }

        /// <summary> 法语 </summary>
        public string French { get; set; }

        /// <summary> 德语 </summary>
        public string German { get; set; }

        /// <summary> 俄语 </summary>
        public string Russian { get; set; }

        /// <summary> 意大利语 </summary>
        public string Italian { get; set; }

        /// <summary> 葡萄牙语（葡萄牙） </summary>
        public string PortuguesePortugal { get; set; }

        /// <summary> 西班牙语 </summary>
        public string Spanish { get; set; }

        /// <summary> 越南语 </summary>
        public string Vietnamese { get; set; }
    }

    /// <summary>
    /// AOT 阶段本地化多语言门面。
    ///     从 Resources 加载 AOT 本地化表数据（YooAsset 未就绪阶段的既有加载通道），
    ///     脱离 UIModule/EventModule 自包含运行（与 LaunchView 同一设计原则）；
    ///     热更后仍可被 Hotfix 侧调用（Hotfix 程序集引用 AOT 程序集）。
    ///     数据解析自包含（json 用 SimpleJSON / bin 用 Luban.ByteBuf），不依赖热更侧配置框架。
    /// </summary>
    public static class LaunchLocalization
    {
        /// <summary>
        /// 语言偏好在 PlayerPrefs 中的存储键（值为 (int)ELanguage）。
        /// </summary>
        private const string LanguagePrefKey = "AOT_Localization_Language";

        /// <summary>
        /// Resources 下 AOT 本地化表数据的加载路径（不含扩展名）。
        /// </summary>
        private const string DataPath = "Config/tblocalizationaot";

        /// <summary>
        /// 多语言行数据字典。key 为多语言 key，value 为行数据。
        /// </summary>
        private static readonly Dictionary<string, LocalizationAOTRow> s_Rows = new();

        /// <summary>
        /// 当前使用的语言。
        /// </summary>
        public static ELanguage Language { get; private set; } = ELanguage.Unspecified;

        /// <summary>
        /// 初始化：加载 AOT 本地化表数据并确定当前语言。
        /// 须在启动流程首次展示文本前 await 完成；数据缺失仅记录错误，不阻断启动。
        /// </summary>
        /// <returns>初始化异步流程</returns>
        public static async UniTask InitializeAsync()
        {
            Language = LoadLanguagePreference();

            var textAsset = Resources.Load<TextAsset>(DataPath);
            if (textAsset == null)
            {
                FuLogger.LogError($"[LaunchLocalization] Resources 下未找到 {DataPath}，AOT 本地化不可用!");
                return;
            }

            s_Rows.Clear();

            try
            {
#if ENABLE_BINARY_CONFIG
                ParseBin(textAsset.bytes);
#else
                ParseJson(textAsset.text);
#endif
            }
            catch (Exception e)
            {
                // 数据解析失败（变体错配/数据损坏）仅记录错误并清空半解析数据，不阻断启动流程
                FuLogger.LogError($"[LaunchLocalization] AOT 本地化数据解析失败，AOT 本地化不可用：{e.Message}");
                s_Rows.Clear();
            }

            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 获取 AOT 本地化多语言文本。
        /// </summary>
        /// <param name="key">多语言 key（使用 AOT 版 LanguageKey 静态类字段）</param>
        /// <param name="args">格式化参数</param>
        /// <returns>本地化文本；key 未找到时记录错误并返回空串</returns>
        public static string GetLanguage(string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            if (!s_Rows.TryGetValue(key, out var row))
            {
                FuLogger.LogError($"[LaunchLocalization] 多语言key '{key}' 没找到，请检查AOT多语言配置表!");
                return string.Empty;
            }

            var text = Language switch
            {
                // @formatter:off
                ELanguage.ChineseSimplified  => row.ChineseSimplified,
                ELanguage.ChineseTraditional => row.ChineseTraditional,
                ELanguage.English            => row.English,
                ELanguage.Japanese           => row.Japanese,
                ELanguage.Korean             => row.Korean,
                ELanguage.Thai               => row.Thai,
                ELanguage.Indonesian         => row.Indonesian,
                ELanguage.French             => row.French,
                ELanguage.German             => row.German,
                ELanguage.Italian            => row.Italian,
                ELanguage.PortuguesePortugal => row.PortuguesePortugal,
                ELanguage.Spanish            => row.Spanish,
                ELanguage.Vietnamese         => row.Vietnamese,
                ELanguage.PortugueseBrazil   => row.PortuguesePortugal,
                ELanguage.Russian            => row.Russian,
                ELanguage.Belarusian         => row.Russian,
                ELanguage.Ukrainian          => row.Russian,
                _                            => row.English, // 语言类型未支持时，统一使用英语
                // @formatter:on
            };

            // 目标语言字段为空时回退英语（与 Hotfix 版同构）
            if (string.IsNullOrEmpty(text))
            {
                text = row.English;
            }

            text ??= string.Empty;

            return args is { Length: > 0 } ? string.Format(text, args) : text;
        }

        /// <summary>
        /// 解析 json 变体数据：顶层数组，每个元素为一条行数据的对象。
        /// </summary>
        /// <param name="json">表数据 json 文本</param>
        private static void ParseJson(string json)
        {
            foreach (var node in JSON.Parse(json).Children)
            {
                if (!node.IsObject) continue;

                var row = new LocalizationAOTRow
                {
                    Key                = node["key"],
                    IsCode             = node["is_code"].AsBool,
                    ChineseSimplified  = node["ChineseSimplified"],
                    ChineseTraditional = node["ChineseTraditional"],
                    English            = node["English"],
                    Japanese           = node["Japanese"],
                    Korean             = node["Korean"],
                    Thai               = node["Thai"],
                    Indonesian         = node["Indonesian"],
                    French             = node["French"],
                    German             = node["German"],
                    Russian            = node["Russian"],
                    Italian            = node["Italian"],
                    PortuguesePortugal = node["PortuguesePortugal"],
                    Spanish            = node["Spanish"],
                    Vietnamese         = node["Vietnamese"],
                };

                if (string.IsNullOrEmpty(row.Key)) continue;

                s_Rows.TryAdd(row.Key, row);
            }
        }

        /// <summary>
        /// 解析 bin 变体数据：记录数前置，之后按 Excel 列顺序逐条读取（字符串=ReadString，布尔=ReadBool）。
        /// </summary>
        /// <param name="bytes">表数据二进制</param>
        private static void ParseBin(byte[] bytes)
        {
            var buf = new ByteBuf(bytes);

            for (var n = buf.ReadSize(); n > 0; --n)
            {
                var row = new LocalizationAOTRow
                {
                    Key                = buf.ReadString(),
                    IsCode             = buf.ReadBool(),
                    ChineseSimplified  = buf.ReadString(),
                    ChineseTraditional = buf.ReadString(),
                    English            = buf.ReadString(),
                    Japanese           = buf.ReadString(),
                    Korean             = buf.ReadString(),
                    Thai               = buf.ReadString(),
                    Indonesian         = buf.ReadString(),
                    French             = buf.ReadString(),
                    German             = buf.ReadString(),
                    Russian            = buf.ReadString(),
                    Italian            = buf.ReadString(),
                    PortuguesePortugal = buf.ReadString(),
                    Spanish            = buf.ReadString(),
                    Vietnamese         = buf.ReadString(),
                };

                if (string.IsNullOrEmpty(row.Key)) continue;

                s_Rows.TryAdd(row.Key, row);
            }
        }

        /// <summary>
        /// 读取语言偏好：用户持久化选择优先，无记录时回退系统语言。
        /// </summary>
        /// <returns>当前语言</returns>
        private static ELanguage LoadLanguagePreference()
        {
            var value = PlayerPrefs.GetInt(LanguagePrefKey, (int)ELanguage.Unspecified);
            if (value > (int)ELanguage.Unspecified && value <= (int)ELanguage.Vietnamese)
            {
                return (ELanguage)value;
            }

            return ELanguageHelper.FromSystemLanguage(Application.systemLanguage);
        }
    }
}
