using System;
using GameFrameX.Runtime;
using GameFrameX.UI.Runtime;

namespace GameFrameX.UI.FairyGUI.Runtime
{
    internal sealed class OpenUIFormInfoData : IReference
    {
        public int  SerialId { get; private set; } = 0;
        public Type FormType { get; private set; }

        public string PackageName { get; private set; }

        public string UIName { get; private set; }

        public bool PauseCoveredUIForm { get; private set; } = false;

        public object UserData { get; private set; } = null;

        public static OpenUIFormInfoData Create(int serialId, string packageName, string uiName, Type uiFormType, bool pauseCoveredUIForm, object userData)
        {
            var openUIFormInfo = ReferencePool.Acquire<OpenUIFormInfoData>();
            openUIFormInfo.SerialId           = serialId;
            openUIFormInfo.PauseCoveredUIForm = pauseCoveredUIForm;
            openUIFormInfo.UserData           = userData;
            openUIFormInfo.PackageName        = packageName;
            openUIFormInfo.UIName             = uiName;
            openUIFormInfo.FormType           = uiFormType;
            return openUIFormInfo;
        }

        public void Clear()
        {
            SerialId           = 0;
            PauseCoveredUIForm = false;
            UserData           = null;
            PackageName        = null;
            UIName             = null;
        }
    }
}