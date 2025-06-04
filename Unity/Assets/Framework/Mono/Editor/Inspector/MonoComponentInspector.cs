//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFrameX.Editor;
using GameFrameX.Mono.Runtime;
using UnityEditor;

namespace GameFrameX.Mono.Editor
{
    [CustomEditor(typeof(MonoComponent))]
    internal sealed class MonoComponentInspector : ComponentTypeComponentInspector
    {
        protected override void RefreshTypeNames()
        {
            RefreshComponentTypeNames(typeof(IMonoManager));
        }
    }
}