using System.Collections.Generic;

namespace FuFramework.SuperSocket.ClientEngine;

public interface IBatchQueue<T>
{
	bool IsEmpty { get; }

	int Count { get; }

	bool Enqueue(T item);

	bool Enqueue(IList<T> items);

	bool TryDequeue(IList<T> outputItems);
}
