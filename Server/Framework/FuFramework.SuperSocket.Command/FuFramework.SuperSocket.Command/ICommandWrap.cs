namespace FuFramework.SuperSocket.Command;

internal interface ICommandWrap
{
	ICommand InnerCommand { get; }
}
