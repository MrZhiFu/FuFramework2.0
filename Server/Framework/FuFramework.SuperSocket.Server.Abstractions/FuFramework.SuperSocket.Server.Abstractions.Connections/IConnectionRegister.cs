using System.Threading.Tasks;

namespace FuFramework.SuperSocket.Server.Abstractions.Connections;

public interface IConnectionRegister
{
	Task RegisterConnection(object connection);
}
