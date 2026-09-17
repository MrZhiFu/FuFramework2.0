using System;

namespace FuFramework.SuperSocket.ClientEngine;

public class DataEventArgs : EventArgs
{
	public byte[] Data { get; set; }

	public int Offset { get; set; }

	public int Length { get; set; }
}
