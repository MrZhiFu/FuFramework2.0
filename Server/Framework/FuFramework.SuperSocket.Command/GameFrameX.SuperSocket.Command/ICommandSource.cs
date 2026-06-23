using System;
using System.Collections.Generic;

namespace FuFramework.SuperSocket.Command;

public interface ICommandSource
{
	IEnumerable<Type> GetCommandTypes(Predicate<Type> criteria);
}
