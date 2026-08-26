using System.Threading;
using Cysharp.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Hotfix.Framework.Core
{
    /// <summary>
    /// 可取消异步对象：实现者的所有异步操作随对象销毁而取消，且可被 await 等待清理完成。
    /// </summary>
    public interface ICancelAsync
    {
        /// <summary>
        /// 取消令牌。对象销毁（OnDispose/Dispose）时触发，在途操作观察它并中止。
        /// </summary>
        CancellationToken Token { get; }

        /// <summary>
        /// 触发取消并等待所有在途操作完成清理（释放句柄 + 卸载资源）后才返回。可重入、幂等。
        /// </summary>
        UniTask CancelAsync();
    }
}
