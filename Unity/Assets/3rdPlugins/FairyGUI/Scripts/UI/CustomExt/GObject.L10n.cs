using System;
using System.Collections.Generic;

namespace FairyGUI
{
    /// <summary>
    /// L10n 段解析结果。
    ///     简单模式：IsCtrl=false，SimpleKey 为多语言 key；
    ///     控制器模式：IsCtrl=true，CtrlName 为父组件控制器名，PageKeys 为 页索引 → key。
    /// </summary>
    internal class L10nGearData
    {
        /// <summary>
        /// 是否为控制器模式（逐页 key）。
        /// </summary>
        public bool IsCtrl;

        /// <summary>
        /// 简单模式的多语言 key（IsCtrl=false 时有效）。
        /// </summary>
        public string SimpleKey;

        /// <summary>
        /// 控制器模式的控制器名（IsCtrl=true 时有效）。
        /// </summary>
        public string CtrlName;

        /// <summary>
        /// 控制器模式的 控制器页索引 → 多语言 key 映射（IsCtrl=true 时有效）。
        /// 控制器切页时按新页索引取 key 重应用文本。
        /// </summary>
        public Dictionary<int, string> PageKeys;
    }

    /// <summary>
    /// GObject 的 L10n（声明式多语言）扩展。
    ///     数据来源：FGUI 编辑器 CustomL10n 插件写入组件 customData 的 "L10n:" 段
    ///     （customData 已由原版 Setup_BeforeAdd 读入 data 字段）。
    ///     应用时机：Setup_AfterAdd 末尾（控制器/子级就绪后）+ 控制器切页 + RefreshAllL10n 遍历。
    /// </summary>
    public partial class GObject
    {
        /// <summary>
        /// 语言文本解析委托：由热更层启动时注入（AOT 阶段注 AOT 表版本，热更后覆盖为热更表版本）。
        /// 未注入时 ResolveL10nText 原样返回 key。
        /// </summary>
        public static Func<string, string> GetLanguageText;

        /// <summary>
        /// L10n 段解析结果（SetupL10nGear 时填充）。
        /// </summary>
        private L10nGearData _l10nGearData;

        /// <summary>
        /// 控制器模式的页变更回调引用（DisposeL10n 时退订，防泄漏）。
        /// </summary>
        private Controller _l10nController;

        private EventCallback0 _l10nCtrlCallback;

        /// <summary>
        /// 是否处理 L10n：仅 GTextField/GButton/GLabel 覆写为 true，其余组件零开销跳过。
        /// </summary>
        protected virtual bool ShouldHandleL10nGear() => false;

        /// <summary>
        /// 把解析出的 key 对应文本写入目标属性（子类覆写：text/title）。
        /// </summary>
        /// <param name="key">多语言 key（已解析为当前页/简单模式）</param>
        protected virtual void OnApplyL10nText(string key) { }

        /// <summary>
        /// Setup_AfterAdd 锚点入口：解析 data 中的 L10n 段并首次应用，控制器模式订阅页变更。
        /// </summary>
        private void SetupL10nGear()
        {
            DisposeL10n(); // 池化复用/重复 Setup 防御：先重置

            if (!ShouldHandleL10nGear()) return;

            // 解析 L10n 段数据
            var customData = data as string;
            _l10nGearData = ParseL10nGear(customData);
            if (_l10nGearData == null) return;

            // 首次应用：按解析结果应用文本（控制器模式按当前页取 key）
            ApplyL10nGearData();

            if (_l10nGearData.IsCtrl && parent != null)
            {
                var ctrl = parent.GetController(_l10nGearData.CtrlName);
                if (ctrl != null)
                {
                    _l10nController   = ctrl;
                    _l10nCtrlCallback = ApplyL10nGearData;
                    ctrl.onChanged.Add(_l10nCtrlCallback);
                }
            }
        }

        /// <summary>
        /// 从 customData 字符串解析 L10n: 段（customData 按 | 分段，L10n: 段与其他插件段共存）。
        /// </summary>
        /// <param name="customData">组件 customData 字符串</param>
        /// <returns>解析结果；无 L10n 段时返回 null</returns>
        private static L10nGearData ParseL10nGear(string customData)
        {
            if (string.IsNullOrEmpty(customData)) return null;

            // 预检定位：未绑定 L10n 的组件（绝大多数）在此零分配返回
            var flagIndex = customData.IndexOf("L10n:", StringComparison.Ordinal);
            if (flagIndex < 0) return null;

            var bodyStart = flagIndex + 5;
            var bodyEnd   = customData.IndexOf('|', bodyStart);

            if (bodyEnd < 0)
                bodyEnd = customData.Length;

            var body = customData.Substring(bodyStart, bodyEnd - bodyStart);
            if (body.Length == 0) return null;

            // 控制器模式判定：段体同时含 "," 与 "="（格式 {ctrl},{page}={key},...），
            // 简单模式的 key 不含这两个字符（key 命名为 [a-z0-9_] 格式）
            if (body.IndexOf(',') >= 0 && body.IndexOf('=') >= 0)
            {
                // 控制器模式：{ctrlName},{pageId}={key},{pageId}={key}...
                var data = new L10nGearData
                {
                    IsCtrl   = true,
                    PageKeys = new Dictionary<int, string>()
                };

                var sepIndex = body.IndexOf(',');
                data.CtrlName = body.Substring(0, sepIndex);
                foreach (var pair in body.Substring(sepIndex + 1).Split(','))
                {
                    var kv = pair.Split('=');
                    if (kv.Length == 2 && int.TryParse(kv[0], out var pageId))
                    {
                        data.PageKeys[pageId] = kv[1];
                    }
                }

                return data.PageKeys.Count > 0 ? data : null;
            }

            // 简单模式：{key}
            return new L10nGearData { SimpleKey = body };
        }

        /// <summary>
        /// 按解析结果应用文本：简单模式直取 key；控制器模式按当前页取 key。
        /// </summary>
        private void ApplyL10nGearData()
        {
            if (_l10nGearData == null) return;

            string key;
            if (_l10nGearData.IsCtrl)
            {
                var pageIndex = parent?.GetController(_l10nGearData.CtrlName)?.selectedIndex ?? -1;
                if (!_l10nGearData.PageKeys.TryGetValue(pageIndex, out key)) return;
            }
            else
            {
                key = _l10nGearData.SimpleKey;
            }

            if (string.IsNullOrEmpty(key)) return;
            // 只传原始 key，解析统一收敛在 OnApplyL10nText（text/title）内，避免二次解析把已译文当 key 再查
            OnApplyL10nText(key);
        }

        /// <summary>
        /// key → 文本：委托未注入时原样返回 key。
        /// </summary>
        /// <param name="key">多语言 key</param>
        /// <returns>对应语言的文本</returns>
        internal string ResolveL10nText(string key)
        {
            var getter = GetLanguageText;
            return getter == null ? key : getter(key) ?? key;
        }

        /// <summary>
        /// 语言切换遍历入口：对 root 子树内已绑定 L10n 的组件重应用文本。
        /// 由 Hotfix 侧 WinBase 在语言切换事件中调用（仅可见界面）。
        /// </summary>
        /// <param name="root">界面根组件</param>
        public static void RefreshAllL10n(GComponent root)
        {
            if (root == null) return;

            if (root._l10nGearData != null)
                root.ApplyL10nGearData();

            var cnt = root.numChildren;
            for (var i = 0; i < cnt; i++)
            {
                var child = root.GetChildAt(i);

                // 先应用自身（非 GComponent 的绑定组件在此处理），再递归子树
                if (child._l10nGearData != null)
                    child.ApplyL10nGearData();

                if (child is GComponent childComp)
                    RefreshAllL10n(childComp);
            }
        }

        /// <summary>
        /// 释放 L10n 状态并退订控制器页变更（GObject.Dispose 锚点调用）。
        /// </summary>
        private void DisposeL10n()
        {
            if (_l10nController != null && _l10nCtrlCallback != null)
            {
                _l10nController.onChanged.Remove(_l10nCtrlCallback);
            }

            _l10nController   = null;
            _l10nCtrlCallback = null;
            _l10nGearData     = null;
        }
    }
}