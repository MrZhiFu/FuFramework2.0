using System;

namespace FuFramework.Core.Hotfix;

internal class MultiAgentImpException : Exception
{
	public MultiAgentImpException(string message)
		: base(message)
	{
	}
}
