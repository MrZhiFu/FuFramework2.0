//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFrameX.Runtime;
using UnityEngine;

namespace GameFrameX.Web.Runtime
{
    /// <summary>
    /// Web 请求组件。
    /// 提供HTTP GET和POST请求功能的Unity组件。
    /// 支持字符串和字节数组格式的请求结果。
    /// 可以设置请求超时时间。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Game Framework/Web")]
    
    public sealed class WebComponent : GameFrameworkComponent
    {
        /// <summary>
        /// Web请求管理器实例
        /// </summary>
        private IWebManager m_WebManager;

        /// <summary>
        /// 请求超时时间配置
        /// </summary>
        [SerializeField] [Tooltip("超时时间.单位：秒")]
        private float m_Timeout = 5f;

        /// <summary>
        /// 获取或设置下载超时时长，以秒为单位。
        /// 当请求超过此时间未完成时会自动终止。
        /// </summary>
        public float Timeout
        {
            get { return m_WebManager.Timeout; }
            set { m_WebManager.Timeout = m_Timeout = value; }
        }

        /// <summary>
        /// 游戏框架组件初始化。
        /// 在此方法中初始化Web管理器并设置超时时间。
        /// </summary>
        protected override void Awake()
        {
            ImplementationComponentType = Utility.Assembly.GetType(componentType);
            InterfaceComponentType      = typeof(IWebManager);
            base.Awake();
            m_WebManager = GameFrameworkEntry.GetModule<IWebManager>();
            if (m_WebManager == null)
            {
                Log.Fatal("Web manager is invalid.");
                return;
            }

            m_WebManager.Timeout = m_Timeout;
        }

        /// <summary>
        /// 发送Get请求，返回字符串结果。
        /// 这是最基础的GET请求方法，不包含任何查询参数和请求头。
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="userData">用户自定义数据，会在结果中原样返回</param>
        /// <returns>返回包含字符串结果的WebStringResult异步任务</returns>
        public Task<WebStringResult> GetToString(string url, object userData = null)
        {
            return m_WebManager.GetToString(url, userData);
        }

        /// <summary>
        /// 发送带查询参数的Get请求，返回字符串结果。
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="queryString">URL查询参数字典，会被附加到URL后面</param>
        /// <param name="userData">用户自定义数据，会在结果中原样返回</param>
        /// <returns>返回包含字符串结果的WebStringResult异步任务</returns>
        public Task<WebStringResult> GetToString(string url, Dictionary<string, string> queryString, object userData = null)
        {
            return m_WebManager.GetToString(url, queryString, userData);
        }

        /// <summary>
        /// 发送带查询参数和请求头的Get请求，返回字符串结果。
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="queryString">URL查询参数字典，会被附加到URL后面</param>
        /// <param name="header">HTTP请求头字典</param>
        /// <param name="userData">用户自定义数据，会在结果中原样返回</param>
        /// <returns>返回包含字符串结果的WebStringResult异步任务</returns>
        public Task<WebStringResult> GetToString(string url, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)
        {
            return m_WebManager.GetToString(url, queryString, header, userData);
        }


        /// <summary>
        /// 发送Get请求，返回字节数组结果。
        /// 适用于下载二进制数据如图片、音频等。
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="userData">用户自定义数据，会在结果中原样返回</param>
        /// <returns>返回包含字节数组的WebBufferResult异步任务</returns>
        public Task<WebBufferResult> GetToBytes(string url, object userData = null)
        {
            return m_WebManager.GetToBytes(url, userData);
        }

        /// <summary>
        /// 发送带查询参数的Get请求，返回字节数组结果。
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="queryString">URL查询参数字典，会被附加到URL后面</param>
        /// <param name="userData">用户自定义数据，会在结果中原样返回</param>
        /// <returns>返回包含字节数组的WebBufferResult异步任务</returns>
        public Task<WebBufferResult> GetToBytes(string url, Dictionary<string, string> queryString, object userData = null)
        {
            return m_WebManager.GetToBytes(url, queryString, userData);
        }

        /// <summary>
        /// 发送带查询参数和请求头的Get请求，返回字节数组结果。
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="queryString">URL查询参数字典，会被附加到URL后面</param>
        /// <param name="header">HTTP请求头字典</param>
        /// <param name="userData">用户自定义数据，会在结果中原样返回</param>
        /// <returns>返回包含字节数组的WebBufferResult异步任务</returns>
        public Task<WebBufferResult> GetToBytes(string url, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)
        {
            return m_WebManager.GetToBytes(url, queryString, header, userData);
        }


        /// <summary>
        /// 发送Post请求，返回字符串结果。
        /// 这是最基础的POST请求方法。
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单数据字典，作为请求体发送</param>
        /// <param name="userData">用户自定义数据，会在结果中原样返回</param>
        /// <returns>返回包含字符串结果的WebStringResult异步任务</returns>
        public Task<WebStringResult> PostToString(string url, Dictionary<string, object> from = null, object userData = null)
        {
            return m_WebManager.PostToString(url, from, userData);
        }

        /// <summary>
        /// 发送带查询参数的Post请求，返回字符串结果。
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单数据字典，作为请求体发送</param>
        /// <param name="queryString">URL查询参数字典，会被附加到URL后面</param>
        /// <param name="userData">用户自定义数据，会在结果中原样返回</param>
        /// <returns>返回包含字符串结果的WebStringResult异步任务</returns>
        public Task<WebStringResult> PostToString(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, object userData = null)
        {
            return m_WebManager.PostToString(url, from, queryString, userData);
        }

        /// <summary>
        /// 发送带查询参数和请求头的Post请求，返回字符串结果。
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单数据字典，作为请求体发送</param>
        /// <param name="queryString">URL查询参数字典，会被附加到URL后面</param>
        /// <param name="header">HTTP请求头字典</param>
        /// <param name="userData">用户自定义数据，会在结果中原样返回</param>
        /// <returns>返回包含字符串结果的WebStringResult异步任务</returns>
        public Task<WebStringResult> PostToString(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)
        {
            return m_WebManager.PostToString(url, from, queryString, header, userData);
        }


        /// <summary>
        /// 发送Post请求，返回字节数组结果。
        /// 适用于上传和下载二进制数据。
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单数据字典，作为请求体发送</param>
        /// <param name="userData">用户自定义数据，会在结果中原样返回</param>
        /// <returns>返回包含字节数组的WebBufferResult异步任务</returns>
        public Task<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, object userData = null)
        {
            return m_WebManager.PostToBytes(url, from, userData);
        }

        /// <summary>
        /// 发送带查询参数的Post请求，返回字节数组结果。
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单数据字典，作为请求体发送</param>
        /// <param name="queryString">URL查询参数字典，会被附加到URL后面</param>
        /// <param name="userData">用户自定义数据，会在结果中原样返回</param>
        /// <returns>返回包含字节数组的WebBufferResult异步任务</returns>
        public Task<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, object userData = null)
        {
            return m_WebManager.PostToBytes(url, from, queryString, userData);
        }

        /// <summary>
        /// 发送带查询参数和请求头的Post请求，返回字节数组结果。
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单数据字典，作为请求体发送</param>
        /// <param name="queryString">URL查询参数字典，会被附加到URL后面</param>
        /// <param name="header">HTTP请求头字典</param>
        /// <param name="userData">用户自定义数据，会在结果中原样返回</param>
        /// <returns>返回包含字节数组的WebBufferResult异步任务</returns>
        public Task<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, Dictionary<string, string> queryString, Dictionary<string, string> header, object userData = null)
        {
            return m_WebManager.PostToBytes(url, from, queryString, header, userData);
        }

        /// <summary>
        /// 发送Post请求，用于发送和接收Protocol Buffer消息。
        /// 此方法仅在启用ENABLE_GAME_FRAME_X_WEB_PROTOBUF_NETWORK宏定义时可用。
        /// </summary>
        /// <param name="url">目标服务器的URL地址</param>
        /// <param name="message">要发送的Protocol Buffer消息对象，必须继承自MessageObject</param>
        /// <typeparam name="T">返回的数据类型，必须继承自MessageObject并且实现IResponseMessage接口</typeparam>
        /// <returns>返回一个任务对象，该任务完成时将包含从服务器接收到的Protocol Buffer响应数据，数据类型为T</returns>
        /// <remarks>
        /// 此方法专门用于处理Protocol Buffer格式的请求和响应。
        /// 发送的消息和接收的响应都必须是Protocol Buffer消息类型。
        /// </remarks>
        public Task<T> Post<T>(string url, GameFrameX.Network.Runtime.MessageObject message) where T : GameFrameX.Network.Runtime.MessageObject, GameFrameX.Network.Runtime.IResponseMessage
        {
            return m_WebManager.Post<T>(url, message);
        }
    }
}