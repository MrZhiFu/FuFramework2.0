using System;
using FairyGUI;
using GameFrameX.UI.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    [UnityEngine.Scripting.Preserve]
    public class FUI : UIForm
    {
        /// <summary>
        /// UI 对象
        /// </summary>
        public GObject GObject { get; private set; }

        public Action<bool> OnVisibleChanged { get; set; }

        /// <summary>
        /// 设置UI的显示状态，不发出事件
        /// </summary>
        /// <param name="value"></param>
        protected override void InternalSetVisible(bool value)
        {
            if (GObject.visible == value)
            {
                return;
            }

            GObject.visible = value;
            OnVisibleChanged?.Invoke(value);
            EventSubscriber.Fire(UIFormVisibleChangedEventArgs.EventId, UIFormVisibleChangedEventArgs.Create(this, value, null));
        }

        public override bool Visible
        {
            get
            {
                if (GObject == null)
                {
                    return false;
                }

                return GObject.visible;
            }
            protected set
            {
                if (GObject == null)
                {
                    return;
                }

                if (GObject.visible == value)
                {
                    return;
                }

                GObject.visible = value;
                OnVisibleChanged?.Invoke(value);
                EventSubscriber.Fire(UIFormVisibleChangedEventArgs.EventId, UIFormVisibleChangedEventArgs.Create(this, value, null));
            }
        }


        /// <summary>
        /// 设置当前UI对象为全屏
        /// </summary>
        protected override void MakeFullScreen()
        {
            GObject?.asCom?.MakeFullScreen();
        }

        public FUI(GObject gObject, bool isRoot = false)
        {
            GObject = gObject;
            InitView();
        }

        public void SetGObject(GObject gObject)
        {
            GObject = gObject;
        }
    }
}