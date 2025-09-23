using FuFramework.Core.Editor;
using FuFramework.Network.Runtime;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Editor
{
    /// <summary>
    /// 自定义网络管理器的Inspector
    /// </summary>
    [CustomEditor(typeof(NetworkManager))]
    internal sealed class NetworkManagerInspector : FuFrameworkInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var networkManager = target as NetworkManager;
            if (networkManager == null) return;

            EditorGUILayout.LabelField("网络频道数：", networkManager.NetworkChannelCount.ToString());

            INetworkChannel[] networkChannels = networkManager.GetAllNetworkChannels();
            foreach (INetworkChannel networkChannel in networkChannels)
            {
                DrawNetworkChannel(networkChannel);
            }
        }

        /// <summary>
        /// 绘制网络频道信息
        /// </summary>
        /// <param name="networkChannel"></param>
        private void DrawNetworkChannel(INetworkChannel networkChannel)
        {
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField(networkChannel.Name, networkChannel.Connected ? "Connected" : "Disconnected");
                EditorGUILayout.LabelField("网络协议类型：", networkChannel.EAddressFamily.ToString());
                EditorGUILayout.LabelField("本地地址：", networkChannel.Connected ? networkChannel.Socket.LocalEndPoint.ToString() : "Unavailable");
                EditorGUILayout.LabelField("远程地址：", networkChannel.Connected ? networkChannel.Socket.RemoteEndPoint.ToString() : "Unavailable");
                EditorGUILayout.LabelField("发送包数量：", $"{networkChannel.SendPacketCount} / {networkChannel.SentPacketCount}");
                EditorGUILayout.LabelField("丢失的心跳包数量：", networkChannel.MissHeartBeatCount.ToString());
                EditorGUILayout.LabelField("心跳包间隔：", $"{networkChannel.HeartBeatElapseSeconds:F2} / {networkChannel.HeartBeatInterval:F2}");
                EditorGUI.BeginDisabledGroup(!networkChannel.Connected);
                {
                    if (GUILayout.Button("Disconnect"))
                    {
                        networkChannel.Close();
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndVertical();
        }
    }
}