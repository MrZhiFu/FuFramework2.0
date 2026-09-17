using System.Threading.Tasks;

namespace FuFramework.SuperSocket.Connection;

internal interface ISupplyController
{
	ValueTask SupplyRequired();

	void SupplyEnd();
}
