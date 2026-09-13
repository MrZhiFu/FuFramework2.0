using Luban.CodeTarget;
using Luban.CSharp.TemplateExtensions;
using Scriban;

namespace Luban.CSharp.CodeTarget;

/// <summary>
/// 自定义导出目标为cs-dotnet-bin(服务器用)
/// </summary>
[CodeTarget("cs-dotnet-bin")]
public class CsharpDotNetBinCodeTarget : CsharpCodeTargetBase
{
    protected override void OnCreateTemplateContext(TemplateContext ctx)
    {
        base.OnCreateTemplateContext(ctx);
        ctx.PushGlobal(new CsharpBinTemplateExtension());
    }
}
