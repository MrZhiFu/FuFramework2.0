namespace FuFramework.ProtoExport
{
    /// <summary>
    /// 协议生成帮助基类
    /// </summary>
    internal interface IProtoGenerateHelper
    {
        /// <summary>
        /// 开始生成
        /// </summary>
        /// <param name="inputPath">输入目录</param>
        /// <param name="outputPath">输出目录</param>
        /// <param name="namespaceName">命名空间</param>
        void Run(MessageInfoList inputPath, string outputPath, string namespaceName = "FuFramework.Proto");

        /// <summary>
        /// 生成后处理（汇总阶段：全部 proto 解析完成后调用一次）
        /// </summary>
        /// <param name="operationCodeInfo">全部 proto 的解析结果</param>
        /// <param name="launcherOptionsOutputPath">launcher options 输出路径</param>
        /// <param name="protoInputPath">proto 输入目录</param>
        /// <param name="outputPath">生成物输出目录</param>
        /// <param name="namespaceName">生成代码命名空间</param>
        void Post(List<MessageInfoList> operationCodeInfo, string launcherOptionsOutputPath, string protoInputPath, string outputPath, string namespaceName);
    }
}