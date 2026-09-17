using System;

namespace FuFramework.SuperSocket.ClientEngine;

public class SearchMarkState<T> where T : IEquatable<T>
{
	public T[] Mark { get; private set; }

	public int Matched { get; set; }

	public SearchMarkState(T[] mark)
	{
		Mark = mark;
	}
}
