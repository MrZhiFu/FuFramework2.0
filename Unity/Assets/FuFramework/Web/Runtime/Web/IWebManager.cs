using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameFrameX.Web.Runtime
{
    /// <summary>
    /// Web请求管理器接口，提供HTTP GET和POST请求的功能
    /// </summary>
    public interface IWebManager
    {
        /// <summary>
        /// 发送Get请求，返回字符串结果
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns>返回WebStringResult类型的异步任务</returns>
        Task<WebStringResult> GetToString(string url, object userData = null);

        /// <summary>
        /// 发送Get请求，返回字节数组结果
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns>返回WebBufferResult类型的异步任务</returns>
        Task<WebBufferResult> GetToBytes(string url, object userData = null);

        /// <summary>
        /// 发送带查询参数的Get请求，返回字符串结果
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="queryString">URL查询参数字典</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns>返回WebStringResult类型的异步任务</returns>
        Task<WebStringResult> GetToString(string url, Dictionary<string, string> queryString, object userData = null);


        /// <summary>
        /// 发送带查询参数的Get请求，返回字节数组结果
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="queryString">URL查询参数字典</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns>返回WebBufferResult类型的异步任务</returns>
        Task<WebBufferResult> GetToBytes(string url, Dictionary<string, string> queryString, object userData = null);


        /// <summary>
        /// 发送带查询参数和请求头的Get请求，返回字符串结果
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="queryString">URL查询参数字典</param>
        /// <param name="header">HTTP请求头字典</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns>返回WebStringResult类型的异步任务</returns>
        Task<WebStringResult> GetToString(string url, Dictionary<string, string> queryString, Dictionary<string, string> header,
            object userData = null);


        /// <summary>
        /// 发送带查询参数和请求头的Get请求，返回字节数组结果
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="queryString">URL查询参数字典</param>
        /// <param name="header">HTTP请求头字典</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns>返回WebBufferResult类型的异步任务</returns>
        Task<WebBufferResult> GetToBytes(string url, Dictionary<string, string> queryString, Dictionary<string, string> header,
            object userData = null);


        /// <summary>
        /// 发送简单Post请求，返回字符串结果
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单数据字典</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns>返回WebStringResult类型的异步任务</returns>
        Task<WebStringResult> PostToString(string url, Dictionary<string, object> from, object userData = null);

        /// <summary>
        /// 发送带查询参数的Post请求，返回字符串结果
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单数据字典</param>
        /// <param name="queryString">URL查询参数字典</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns>返回WebStringResult类型的异步任务</returns>
        Task<WebStringResult> PostToString(string url, Dictionary<string, object> from, Dictionary<string, string> queryString,
            object userData = null);

        /// <summary>
        /// 发送带查询参数和请求头的Post请求，返回字符串结果
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单数据字典</param>
        /// <param name="queryString">URL查询参数字典</param>
        /// <param name="header">HTTP请求头字典</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns>返回WebStringResult类型的异步任务</returns>
        Task<WebStringResult> PostToString(string url, Dictionary<string, object> from, Dictionary<string, string> queryString,
            Dictionary<string, string> header, object userData = null);

        /// <summary>
        /// 发送简单Post请求，返回字节数组结果
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单数据字典</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns>返回WebBufferResult类型的异步任务</returns>
        Task<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, object userData = null);

        /// <summary>
        /// 发送带查询参数的Post请求，返回字节数组结果
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单数据字典</param>
        /// <param name="queryString">URL查询参数字典</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns>返回WebBufferResult类型的异步任务</returns>
        Task<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, Dictionary<string, string> queryString,
            object userData = null);

        /// <summary>
        /// 发送带查询参数和请求头的Post请求，返回字节数组结果
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="from">表单数据字典</param>
        /// <param name="queryString">URL查询参数字典</param>
        /// <param name="header">HTTP请求头字典</param>
        /// <param name="userData">用户自定义数据</param>
        /// <returns>返回WebBufferResult类型的异步任务</returns>
        Task<WebBufferResult> PostToBytes(string url, Dictionary<string, object> from, Dictionary<string, string> queryString,
            Dictionary<string, string> header, object userData = null);

        /// <summary>
        /// 发送Protobuf消息的Post请求，并接收指定类型的响应
        /// </summary>
        /// <param name="url">目标服务器的URL地址</param>
        /// <param name="message">要发送的Protobuf消息对象，必须继承自MessageObject</param>
        /// <typeparam name="T">返回的数据类型，必须继承自MessageObject并且实现IResponseMessage接口</typeparam>
        /// <returns>返回指定类型T的异步任务，该任务完成时将包含从服务器接收到的响应数据</returns>
        /// <remarks>
        /// 此方法用于向指定的URL发送POST请求，并接收响应。请求的消息体由参数message提供，而响应则会被解析为指定的泛型类型T。
        /// 仅在启用ENABLE_GAME_FRAME_X_WEB_PROTOBUF_NETWORK宏定义时可用。
        /// </remarks>
        Task<T> Post<T>(string url, GameFrameX.Network.Runtime.MessageObject message)
            where T : GameFrameX.Network.Runtime.MessageObject, GameFrameX.Network.Runtime.IResponseMessage;

        /// <summary>
        /// 超时时间
        /// </summary>
        float Timeout { get; set; }
    }
}