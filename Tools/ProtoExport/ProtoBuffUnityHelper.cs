using System.Text;

namespace FuFramework.ProtoExport
{
    /// <summary>
    /// 生成ProtoBuf 协议文件
    /// </summary>
    [Mode(ModeType.Unity)]
    internal class ProtoBuffUnityHelper : IProtoGenerateHelper
    {
        public void Run(MessageInfoList messageInfoList, string outputPath, string namespaceName = "Hotfix")
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using ProtoBuf;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using Hotfix.Framework.Network;");

            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");

            foreach (var operationCodeInfo in messageInfoList.Infos)
            {
                if (operationCodeInfo.IsEnum)
                {
                    sb.AppendLine($"\t/// <summary>");
                    sb.AppendLine($"\t/// {operationCodeInfo.Description}");
                    sb.AppendLine($"\t/// </summary>");
                    sb.AppendLine($"\tpublic enum {operationCodeInfo.Name}");
                    sb.AppendLine("\t{");
                    foreach (var operationField in operationCodeInfo.Fields)
                    {
                        sb.AppendLine($"\t\t/// <summary>");
                        sb.AppendLine($"\t\t/// {operationField.Description}");
                        sb.AppendLine($"\t\t/// </summary>");
                        sb.AppendLine($"\t\t{operationField.Type} = {operationField.Members}, ");
                    }

                    sb.AppendLine("\t}");
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendLine($"\t/// <summary>");
                    sb.AppendLine($"\t/// {operationCodeInfo.Description}");
                    sb.AppendLine($"\t/// </summary>");
                    sb.AppendLine($"\t[ProtoContract]");
                    if (string.IsNullOrEmpty(operationCodeInfo.ParentClass))
                    {
                        sb.AppendLine($"\tpublic sealed class {operationCodeInfo.Name}");
                    }
                    else
                    {
                        // 有配对响应的请求携带 Req→Resp 映射接口，供客户端 Call 泛型编译期校验；其余沿用 ParentClass
                        var parentClass = operationCodeInfo.IsRequest && !string.IsNullOrEmpty(operationCodeInfo.PairResponseName)
                            ? $"{operationCodeInfo.ParentClass}<{operationCodeInfo.PairResponseName}>"
                            : operationCodeInfo.ParentClass;
                        sb.AppendLine($"\t[MessageId(MessageIds.{MessageIdsGenerator.GetModuleName(messageInfoList.FileName)}.{operationCodeInfo.Name})]");
                        sb.AppendLine($"\tpublic sealed class {operationCodeInfo.Name} : MessageObject, {parentClass}");
                    }

                    sb.AppendLine("\t{");
                    foreach (var operationField in operationCodeInfo.Fields)
                    {
                        if (!operationField.IsValid)
                        {
                            continue;
                        }

                        sb.AppendLine($"\t\t/// <summary>");
                        sb.AppendLine($"\t\t/// {operationField.Description}");
                        sb.AppendLine($"\t\t/// </summary>");
                        sb.AppendLine($"\t\t[ProtoMember({operationField.Members})]");
                        if (operationField.IsRepeated)
                        {
                            sb.AppendLine($"\t\tpublic List<{operationField.Type}> {operationField.Name} {{ get; set; }} = new List<{operationField.Type}>();");
                        }
                        else
                        {
                            string defaultValue = string.Empty;
                            if (operationField.IsKv)
                            {
                                defaultValue = $" = new {operationField.Type}();";
                                sb.AppendLine($"\t\t[ProtoMap(DisableMap = true)]");
                            }

                            sb.AppendLine($"\t\tpublic {operationField.Type} {operationField.Name} {{ get; set; }}{defaultValue}");
                        }

                        sb.AppendLine();
                    }

                    sb.AppendLine("\t}");
                    sb.AppendLine();
                }
            }

            sb.Append("}");
            sb.AppendLine();

            File.WriteAllText(messageInfoList.OutputPath + ".cs", sb.ToString(), Encoding.UTF8);
        }

        public void Post(List<MessageInfoList> operationCodeInfo, string launcherOptionsOutputPath, string protoInputPath, string outputPath, string namespaceName)
        {
            MessageIdsGenerator.Generate(operationCodeInfo, outputPath, namespaceName);
        }
    }
}