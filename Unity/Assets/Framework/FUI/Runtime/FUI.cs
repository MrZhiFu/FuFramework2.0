using System;
using FairyGUI;
using GameFrameX.UI.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    /// <summary>
    /// FUI界面基类
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class FUI : UIForm
    {
        /// <summary>
        /// UI 对象
        /// </summary>
        public GObject GObject { get; private set; }

        /// <summary>
        /// UI显示状态变化回调
        /// </summary>
        public Action<bool> OnVisibleChanged { get; set; }

        /// <summary>
        /// UI的显示状态
        /// </summary>
        public override bool Visible
        {
            get => GObject is { visible: true };
            
            protected set
            {
                base.Visible = value;
                if (GObject == null) return;
                if (GObject.visible == value) return;
                
                GObject.visible = value;
                OnVisibleChanged?.Invoke(value);
                
                // 触发UI显示状态变化事件
                EventRegister.Fire(UIFormVisibleChangedEventArgs.EventId, UIFormVisibleChangedEventArgs.Create(this, value, null));
            }
        }

        /// <summary>
        /// 初始化UI
        /// </summary>
        /// <param name="gObject"></param>
        /// <param name="isRoot"></param>
        public FUI(GObject gObject, bool isRoot = false)
        {
            GObject = gObject;
            InitView();
        }

        /// <summary>
        /// 设置UI对象
        /// </summary>
        /// <param name="gObject"></param>
        public void SetGObject(GObject gObject) => GObject = gObject;

        /// <summary>
        /// 设置UI的显示状态
        /// </summary>
        /// <param name="value"></param>
        protected override void InternalSetVisible(bool value)
        {
            if (GObject.visible == value) return;
            GObject.visible = value;
            OnVisibleChanged?.Invoke(value);
            
            // 派发UI显示状态变化事件
            EventRegister.Fire(UIFormVisibleChangedEventArgs.EventId, UIFormVisibleChangedEventArgs.Create(this, value, null));
        }

        /// <summary>
        /// 设置当前UI对象为全屏
        /// </summary>
        protected override void MakeFullScreen() => GObject?.asCom?.MakeFullScreen();
    }
}