using FuFramework.Core.Editor;
using FuFramework.Network.Runtime;
using UnityEditor;
using UnityEngine;
using Utility = FuFramework.Core.Runtime.Utility;

// ReSharper disable once CheckNamespace
namespace FuFramework.Network.Editor
{
    /// <summary>
    /// 自定义网络组件的Inspector
    /// </summary>
    [CustomEditor(typeof(NetworkManager))]
    internal sealed class NetworkGameComponentInspector : FuFrameworkInspector
    {
        private SerializedProperty m_IgnoredSendNetworkIds;
        private SerializedProperty m_IgnoredReceiveNetworkIds;
        private SerializedProperty m_rpcTimeout;

        private readonly GUIContent m_IgnoredSendNetworkIdsGUIContent = new("忽略发送消息ID的日志打印");
        private readonly GUIContent m_IgnoredReceiveNetworkIdsGUIContent = new("忽略接收消息ID的日志打印");
        private readonly GUIContent m_rpcTimeoutGUIContent = new("RPC超时时间,单位:毫秒");

        private void OnEnable()
        {
            m_IgnoredSendNetworkIds = serializedObject.FindProperty("m_IgnoredSendNetworkIds");
            m_IgnoredReceiveNetworkIds = serializedObject.FindProperty("m_IgnoredReceiveNetworkIds");
            m_rpcTimeout = serializedObject.FindProperty("m_rpcTimeout");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                GUI.enabled = !EditorApplication.isPlaying;
                EditorGUILayout.IntSlider(m_rpcTimeout, 3000, 50000, m_rpcTimeoutGUIContent);
                EditorGUILayout.PropertyField(m_IgnoredSendNetworkIds, m_IgnoredSendNetworkIdsGUIContent);
                EditorGUILayout.PropertyField(m_IgnoredReceiveNetworkIds, m_IgnoredReceiveNetworkIdsGUIContent);
                GUI.enabled = false;
            }
            EditorGUI.EndDisabledGroup();
            serializedObject.ApplyModifiedProperties();
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("仅在运行时可用.", MessageType.Info);
                return;
            }

            NetworkManager t = (NetworkManager)target;

            if (IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("网络频道数", t.NetworkChannelCount.ToString());

                INetworkChannel[] networkChannels = t.GetAllNetworkChannels();
                foreach (INetworkChannel networkChannel in networkChannels)
                {
                    DrawNetworkChannel(networkChannel);
                }
            }

            Repaint();
        }

        private void DrawNetworkChannel(INetworkChannel networkChannel)
        {
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField(networkChannel.Name, networkChannel.Connected ? "Connected" : "Disconnected");
                // EditorGUILayout.LabelField("Service Type", networkChannel.ServiceType.ToString());
                EditorGUILayout.LabelField("Address Family", networkChannel.EAddressFamily.ToString());
                EditorGUILayout.LabelField("Local Address", networkChannel.Connected ? networkChannel.Socket.LocalEndPoint.ToString() : "Unavailable");
                EditorGUILayout.LabelField("Remote Address", networkChannel.Connected ? networkChannel.Socket.RemoteEndPoint.ToString() : "Unavailable");
                EditorGUILayout.LabelField("Send Packet", $"{networkChannel.SendPacketCount} / {networkChannel.SentPacketCount}");
                EditorGUILayout.LabelField("Miss Heart Beat Count", networkChannel.MissHeartBeatCount.ToString());
                EditorGUILayout.LabelField("Heart Beat", $"{networkChannel.HeartBeatElapseSeconds:F2} / {networkChannel.HeartBeatInterval:F2}");
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

            EditorGUILayout.Separator();
        }
    }
}