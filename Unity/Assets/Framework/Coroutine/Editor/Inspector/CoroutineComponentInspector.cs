//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFrameX.Coroutine.Runtime;
using GameFrameX.Editor;
using UnityEditor;

namespace GameFrameX.Coroutine.Editor
{
    [CustomEditor(typeof(CoroutineComponent))]
    internal sealed class CoroutineComponentInspector : GameFrameworkInspector
    {
    }
}