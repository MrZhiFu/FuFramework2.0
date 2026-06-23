using System;

namespace FuFramework.SuperSocket.ClientEngine;

public interface IBufferSetter
{
	void SetBuffer(ArraySegment<byte> bufferSegment);
}
