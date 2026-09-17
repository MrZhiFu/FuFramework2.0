using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace FuFramework.Core.Config;

/// <summary>
/// 字节缓冲区类,用于二进制数据的读写操作
/// </summary>
public sealed class ByteBuf : ICloneable, IEquatable<ByteBuf>
{
	private readonly Action<ByteBuf> _releaser;

	/// <summary>
	/// 最小容量
	/// </summary>
	private const int MinCapacity = 16;

	/// <summary>
	/// 当前读取位置
	/// </summary>
	public int ReaderIndex { get; set; }

	/// <summary>
	/// 当前写入位置
	/// </summary>
	public int WriterIndex { get; set; }

	/// <summary>
	/// 缓冲区容量
	/// </summary>
	public int Capacity => Bytes.Length;

	/// <summary>
	/// 当前数据大小
	/// </summary>
	public int Size => WriterIndex - ReaderIndex;

	/// <summary>
	/// 缓冲区是否为空
	/// </summary>
	public bool Empty => WriterIndex <= ReaderIndex;

	/// <summary>
	/// 缓冲区是否非空
	/// </summary>
	public bool NotEmpty => WriterIndex > ReaderIndex;

	/// <summary>
	/// 内部字节数组
	/// </summary>
	public byte[] Bytes { get; private set; }

	/// <summary>
	/// 剩余可读取的字节数
	/// </summary>
	public int Remaining => WriterIndex - ReaderIndex;

	/// <summary>
	/// 未压缩的可写入字节数
	/// </summary>
	public int NotCompactWritable => Capacity - WriterIndex;

	/// <summary>
	/// 字符串缓存查找器
	/// </summary>
	public static Func<byte[], int, int, string> StringCacheFinder { get; set; }

	/// <summary>
	/// 默认构造函数,创建一个空的字节缓冲区
	/// </summary>
	public ByteBuf()
	{
		Bytes = Array.Empty<byte>();
		ReaderIndex = (WriterIndex = 0);
	}

	/// <summary>
	/// 使用指定容量创建字节缓冲区
	/// </summary>
	/// <param name="capacity">初始容量</param>
	public ByteBuf(int capacity)
	{
		Bytes = ((capacity > 0) ? new byte[capacity] : Array.Empty<byte>());
		ReaderIndex = 0;
		WriterIndex = 0;
	}

	/// <summary>
	/// 使用指定字节数组创建字节缓冲区
	/// </summary>
	/// <param name="bytes">字节数组</param>
	public ByteBuf(byte[] bytes)
	{
		Bytes = bytes;
		ReaderIndex = 0;
		WriterIndex = Capacity;
	}

	/// <summary>
	/// 使用指定字节数组和读写位置创建字节缓冲区
	/// </summary>
	/// <param name="bytes">字节数组</param>
	/// <param name="readIndex">读取位置</param>
	/// <param name="writeIndex">写入位置</param>
	public ByteBuf(byte[] bytes, int readIndex, int writeIndex)
	{
		Bytes = bytes;
		ReaderIndex = readIndex;
		WriterIndex = writeIndex;
	}

	/// <summary>
	/// 使用指定容量和释放器创建字节缓冲区
	/// </summary>
	/// <param name="capacity">初始容量</param>
	/// <param name="releaser">释放器委托</param>
	public ByteBuf(int capacity, Action<ByteBuf> releaser)
		: this(capacity)
	{
		_releaser = releaser;
	}

	/// <summary>
	/// 包装字节数组为ByteBuf
	/// </summary>
	/// <param name="bytes">要包装的字节数组</param>
	/// <returns>包装后的ByteBuf对象</returns>
	public static ByteBuf Wrap(byte[] bytes)
	{
		return new ByteBuf(bytes, 0, bytes.Length);
	}

	/// <summary>
	/// 替换内部字节数组
	/// </summary>
	/// <param name="bytes">新的字节数组</param>
	public void Replace(byte[] bytes)
	{
		Bytes = bytes;
		ReaderIndex = 0;
		WriterIndex = Capacity;
	}

	/// <summary>
	/// 替换内部字节数组并指定读写位置
	/// </summary>
	/// <param name="bytes">新的字节数组</param>
	/// <param name="beginPos">起始位置</param>
	/// <param name="endPos">结束位置</param>
	public void Replace(byte[] bytes, int beginPos, int endPos)
	{
		Bytes = bytes;
		ReaderIndex = beginPos;
		WriterIndex = endPos;
	}

	/// <summary>
	/// 增加写入位置
	/// </summary>
	/// <param name="add">增加的值</param>
	public void AddWriteIndex(int add)
	{
		WriterIndex += add;
	}

	/// <summary>
	/// 增加读取位置
	/// </summary>
	/// <param name="add">增加的值</param>
	public void AddReadIndex(int add)
	{
		ReaderIndex += add;
	}

	/// <summary>
	/// 复制当前数据
	/// </summary>
	/// <returns>复制的字节数组</returns>
	public byte[] CopyData()
	{
		int remaining = Remaining;
		if (remaining > 0)
		{
			byte[] array = new byte[remaining];
			Buffer.BlockCopy(Bytes, ReaderIndex, array, 0, remaining);
			return array;
		}
		return Array.Empty<byte>();
	}

	/// <summary>
	/// 丢弃已读取的字节
	/// </summary>
	public void DiscardReadBytes()
	{
		WriterIndex -= ReaderIndex;
		Array.Copy(Bytes, ReaderIndex, Bytes, 0, WriterIndex);
		ReaderIndex = 0;
	}

	/// <summary>
	/// 写入字节数组(不写入大小)
	/// </summary>
	/// <param name="bs">要写入的字节数组</param>
	public void WriteBytesWithoutSize(byte[] bs)
	{
		WriteBytesWithoutSize(bs, 0, bs.Length);
	}

	/// <summary>
	/// 写入字节数组的指定部分(不写入大小)
	/// </summary>
	/// <param name="bs">要写入的字节数组</param>
	/// <param name="offset">起始偏移</param>
	/// <param name="len">长度</param>
	public void WriteBytesWithoutSize(byte[] bs, int offset, int len)
	{
		EnsureWrite(len);
		Buffer.BlockCopy(bs, offset, Bytes, WriterIndex, len);
		WriterIndex += len;
	}

	/// <summary>
	/// 清空缓冲区
	/// </summary>
	public void Clear()
	{
		int readerIndex = (WriterIndex = 0);
		ReaderIndex = readerIndex;
	}

	private static int PropSize(int initSize, int needSize)
	{
		int num;
		for (num = Math.Max(initSize, 16); num < needSize; num <<= 1)
		{
		}
		return num;
	}

	private void EnsureWrite0(int size)
	{
		int num = WriterIndex + size - ReaderIndex;
		if (num < Capacity)
		{
			WriterIndex -= ReaderIndex;
			Array.Copy(Bytes, ReaderIndex, Bytes, 0, WriterIndex);
			ReaderIndex = 0;
		}
		else
		{
			byte[] array = new byte[PropSize(Capacity, num)];
			WriterIndex -= ReaderIndex;
			Buffer.BlockCopy(Bytes, ReaderIndex, array, 0, WriterIndex);
			ReaderIndex = 0;
			Bytes = array;
		}
	}

	/// <summary>
	/// 确保有足够的写入空间
	/// </summary>
	/// <param name="size">需要的空间大小</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void EnsureWrite(int size)
	{
		if (WriterIndex + size > Capacity)
		{
			EnsureWrite0(size);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void EnsureRead(int size)
	{
		if (ReaderIndex + size > WriterIndex)
		{
			throw new SerializationException();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool CanRead(int size)
	{
		return ReaderIndex + size <= WriterIndex;
	}

	/// <summary>
	/// 追加一个字节
	/// </summary>
	/// <param name="x">要追加的字节</param>
	public void Append(byte x)
	{
		EnsureWrite(1);
		Bytes[WriterIndex++] = x;
	}

	/// <summary>
	/// 写入布尔值
	/// </summary>
	/// <param name="b">要写入的布尔值</param>
	public void WriteBool(bool b)
	{
		EnsureWrite(1);
		Bytes[WriterIndex++] = (b ? ((byte)1) : ((byte)0));
	}

	/// <summary>
	/// 读取布尔值
	/// </summary>
	/// <returns>读取的布尔值</returns>
	public bool ReadBool()
	{
		EnsureRead(1);
		return Bytes[ReaderIndex++] != 0;
	}

	/// <summary>
	/// 写入字节
	/// </summary>
	/// <param name="x">要写入的字节</param>
	public void WriteByte(byte x)
	{
		EnsureWrite(1);
		Bytes[WriterIndex++] = x;
	}

	/// <summary>
	/// 读取字节
	/// </summary>
	/// <returns>读取的字节</returns>
	public byte ReadByte()
	{
		EnsureRead(1);
		return Bytes[ReaderIndex++];
	}

	/// <summary>
	/// 写入短整型
	/// </summary>
	/// <param name="x">要写入的短整型值</param>
	public void WriteShort(short x)
	{
		if (x >= 0)
		{
			if (x < 128)
			{
				EnsureWrite(1);
				Bytes[WriterIndex++] = (byte)x;
				return;
			}
			if (x < 16384)
			{
				EnsureWrite(2);
				Bytes[WriterIndex + 1] = (byte)x;
				Bytes[WriterIndex] = (byte)((x >> 8) | 0x80);
				WriterIndex += 2;
				return;
			}
		}
		EnsureWrite(3);
		Bytes[WriterIndex] = byte.MaxValue;
		Bytes[WriterIndex + 2] = (byte)x;
		Bytes[WriterIndex + 1] = (byte)(x >> 8);
		WriterIndex += 3;
	}

	/// <summary>
	/// 读取短整型
	/// </summary>
	/// <returns>读取的短整型值</returns>
	public short ReadShort()
	{
		EnsureRead(1);
		int num = Bytes[ReaderIndex];
		if (num < 128)
		{
			ReaderIndex++;
			return (short)num;
		}
		if (num < 192)
		{
			EnsureRead(2);
			int num2 = ((num & 0x3F) << 8) | Bytes[ReaderIndex + 1];
			ReaderIndex += 2;
			return (short)num2;
		}
		if (num == 255)
		{
			EnsureRead(3);
			int num3 = (Bytes[ReaderIndex + 1] << 8) | Bytes[ReaderIndex + 2];
			ReaderIndex += 3;
			return (short)num3;
		}
		throw new SerializationException();
	}

	/// <summary>
	/// 读取固定长度的短整型
	/// </summary>
	/// <returns>读取的短整型值</returns>
	public short ReadFshort()
	{
		EnsureRead(2);
		short result = (short)((Bytes[ReaderIndex + 1] << 8) | Bytes[ReaderIndex]);
		ReaderIndex += 2;
		return result;
	}

	/// <summary>
	/// 写入固定长度的短整型
	/// </summary>
	/// <param name="x">要写入的短整型值</param>
	public void WriteFshort(short x)
	{
		EnsureWrite(2);
		Bytes[WriterIndex] = (byte)x;
		Bytes[WriterIndex + 1] = (byte)(x >> 8);
		WriterIndex += 2;
	}

	/// <summary>
	/// 写入整型
	/// </summary>
	/// <param name="x">要写入的整型值</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WriteInt(int x)
	{
		WriteUint((uint)x);
	}

	/// <summary>
	/// 读取整型
	/// </summary>
	/// <returns>读取的整型值</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int ReadInt()
	{
		return (int)ReadUint();
	}

	/// <summary>
	/// 写入无符号整型
	/// </summary>
	/// <param name="x">要写入的无符号整型值</param>
	public void WriteUint(uint x)
	{
		if (x < 128)
		{
			EnsureWrite(1);
			Bytes[WriterIndex++] = (byte)x;
		}
		else if (x < 16384)
		{
			EnsureWrite(2);
			Bytes[WriterIndex + 1] = (byte)x;
			Bytes[WriterIndex] = (byte)((x >> 8) | 0x80);
			WriterIndex += 2;
		}
		else if (x < 2097152)
		{
			EnsureWrite(3);
			Bytes[WriterIndex + 2] = (byte)x;
			Bytes[WriterIndex + 1] = (byte)(x >> 8);
			Bytes[WriterIndex] = (byte)((x >> 16) | 0xC0);
			WriterIndex += 3;
		}
		else if (x < 268435456)
		{
			EnsureWrite(4);
			Bytes[WriterIndex + 3] = (byte)x;
			Bytes[WriterIndex + 2] = (byte)(x >> 8);
			Bytes[WriterIndex + 1] = (byte)(x >> 16);
			Bytes[WriterIndex] = (byte)((x >> 24) | 0xE0);
			WriterIndex += 4;
		}
		else
		{
			EnsureWrite(5);
			Bytes[WriterIndex] = 240;
			Bytes[WriterIndex + 4] = (byte)x;
			Bytes[WriterIndex + 3] = (byte)(x >> 8);
			Bytes[WriterIndex + 2] = (byte)(x >> 16);
			Bytes[WriterIndex + 1] = (byte)(x >> 24);
			WriterIndex += 5;
		}
	}

	/// <summary>
	/// 读取无符号整型
	/// </summary>
	/// <returns>读取的无符号整型值</returns>
	public uint ReadUint()
	{
		EnsureRead(1);
		uint num = Bytes[ReaderIndex];
		if (num < 128)
		{
			ReaderIndex++;
			return num;
		}
		if (num < 192)
		{
			EnsureRead(2);
			uint result = ((num & 0x3F) << 8) | Bytes[ReaderIndex + 1];
			ReaderIndex += 2;
			return result;
		}
		if (num < 224)
		{
			EnsureRead(3);
			uint result2 = ((num & 0x1F) << 16) | (uint)(Bytes[ReaderIndex + 1] << 8) | Bytes[ReaderIndex + 2];
			ReaderIndex += 3;
			return result2;
		}
		if (num < 240)
		{
			EnsureRead(4);
			uint result3 = ((num & 0xF) << 24) | (uint)(Bytes[ReaderIndex + 1] << 16) | (uint)(Bytes[ReaderIndex + 2] << 8) | Bytes[ReaderIndex + 3];
			ReaderIndex += 4;
			return result3;
		}
		EnsureRead(5);
		int result4 = (Bytes[ReaderIndex + 1] << 24) | (Bytes[ReaderIndex + 2] << 16) | (Bytes[ReaderIndex + 3] << 8) | Bytes[ReaderIndex + 4];
		ReaderIndex += 5;
		return (uint)result4;
	}

	/// <summary>
	/// 使用不安全代码写入无符号整型
	/// </summary>
	/// <param name="x">要写入的无符号整型值</param>
	public unsafe void WriteUint_Unsafe(uint x)
	{
		if (x < 128)
		{
			EnsureWrite(1);
			Bytes[WriterIndex++] = (byte)(x << 1);
		}
		else if (x < 16384)
		{
			EnsureWrite(2);
			fixed (byte* ptr = &Bytes[WriterIndex])
			{
				*(uint*)ptr = (x << 2) | 1;
			}
			WriterIndex += 2;
		}
		else if (x < 2097152)
		{
			EnsureWrite(3);
			fixed (byte* ptr = &Bytes[WriterIndex])
			{
				*(uint*)ptr = (x << 3) | 3;
			}
			WriterIndex += 3;
		}
		else if (x < 268435456)
		{
			EnsureWrite(4);
			fixed (byte* ptr = &Bytes[WriterIndex])
			{
				*(uint*)ptr = (x << 4) | 7;
			}
			WriterIndex += 4;
		}
		else
		{
			EnsureWrite(5);
			fixed (byte* ptr = &Bytes[WriterIndex])
			{
				*(uint*)ptr = (x << 5) | 0xF;
			}
			WriterIndex += 5;
		}
	}

	/// <summary>
	/// 使用不安全代码读取无符号整型
	/// </summary>
	/// <returns>读取的无符号整型值</returns>
	public unsafe uint ReadUint_Unsafe()
	{
		EnsureRead(1);
		uint num = Bytes[ReaderIndex];
		if ((num & 1) == 0)
		{
			ReaderIndex++;
			return num >> 1;
		}
		if ((num & 3) == 1)
		{
			EnsureRead(2);
			fixed (byte* ptr = &Bytes[ReaderIndex])
			{
				byte* intPtr = ptr;
				ReaderIndex += 2;
				return *(uint*)intPtr >> 2;
			}
		}
		if ((num & 7) == 3)
		{
			EnsureRead(3);
			fixed (byte* ptr = &Bytes[ReaderIndex])
			{
				byte* intPtr2 = ptr;
				ReaderIndex += 3;
				return *(uint*)intPtr2 >> 3;
			}
		}
		if ((num & 0xF) == 7)
		{
			EnsureRead(4);
			fixed (byte* ptr = &Bytes[ReaderIndex])
			{
				byte* intPtr3 = ptr;
				ReaderIndex += 4;
				return *(uint*)intPtr3 >> 4;
			}
		}
		EnsureRead(5);
		fixed (byte* ptr = &Bytes[ReaderIndex])
		{
			byte* intPtr4 = ptr;
			ReaderIndex += 5;
			return *(uint*)intPtr4 >> 5;
		}
	}

	/// <summary>
	/// 读取固定长度的整型
	/// </summary>
	/// <returns>读取的整型值</returns>
	public int ReadFint()
	{
		EnsureRead(4);
		int result = (Bytes[ReaderIndex + 3] << 24) | (Bytes[ReaderIndex + 2] << 16) | (Bytes[ReaderIndex + 1] << 8) | Bytes[ReaderIndex];
		ReaderIndex += 4;
		return result;
	}

	/// <summary>
	/// 写入固定长度的整型
	/// </summary>
	/// <param name="x">要写入的整型值</param>
	public void WriteFint(int x)
	{
		EnsureWrite(4);
		Bytes[WriterIndex] = (byte)x;
		Bytes[WriterIndex + 1] = (byte)(x >> 8);
		Bytes[WriterIndex + 2] = (byte)(x >> 16);
		Bytes[WriterIndex + 3] = (byte)(x >> 24);
		WriterIndex += 4;
	}

	/// <summary>
	/// 安全地读取固定长度的整型
	/// </summary>
	/// <returns>读取的整型值</returns>
	public int ReadFint_Safe()
	{
		EnsureRead(4);
		int result = (Bytes[ReaderIndex + 3] << 24) | (Bytes[ReaderIndex + 2] << 16) | (Bytes[ReaderIndex + 1] << 8) | Bytes[ReaderIndex];
		ReaderIndex += 4;
		return result;
	}

	/// <summary>
	/// 安全地写入固定长度的整型
	/// </summary>
	/// <param name="x">要写入的整型值</param>
	public void WriteFint_Safe(int x)
	{
		EnsureWrite(4);
		Bytes[WriterIndex] = (byte)x;
		Bytes[WriterIndex + 1] = (byte)(x >> 8);
		Bytes[WriterIndex + 2] = (byte)(x >> 16);
		Bytes[WriterIndex + 3] = (byte)(x >> 24);
		WriterIndex += 4;
	}

	/// <summary>
	/// 写入长整型
	/// </summary>
	/// <param name="x">要写入的长整型值</param>
	public void WriteLong(long x)
	{
		WriteUlong((ulong)x);
	}

	/// <summary>
	/// 读取长整型
	/// </summary>
	/// <returns>读取的长整型值</returns>
	public long ReadLong()
	{
		return (long)ReadUlong();
	}

	/// <summary>
	/// 将浮点数作为长整型写入
	/// </summary>
	/// <param name="x">要写入的浮点数</param>
	public void WriteNumberAsLong(double x)
	{
		WriteLong((long)x);
	}

	/// <summary>
	/// 读取长整型并转换为浮点数
	/// </summary>
	/// <returns>读取的浮点数</returns>
	public double ReadLongAsNumber()
	{
		return ReadLong();
	}

	private void WriteUlong(ulong x)
	{
		if (x < 128)
		{
			EnsureWrite(1);
			Bytes[WriterIndex++] = (byte)x;
		}
		else if (x < 16384)
		{
			EnsureWrite(2);
			Bytes[WriterIndex + 1] = (byte)x;
			Bytes[WriterIndex] = (byte)((x >> 8) | 0x80);
			WriterIndex += 2;
		}
		else if (x < 2097152)
		{
			EnsureWrite(3);
			Bytes[WriterIndex + 2] = (byte)x;
			Bytes[WriterIndex + 1] = (byte)(x >> 8);
			Bytes[WriterIndex] = (byte)((x >> 16) | 0xC0);
			WriterIndex += 3;
		}
		else if (x < 268435456)
		{
			EnsureWrite(4);
			Bytes[WriterIndex + 3] = (byte)x;
			Bytes[WriterIndex + 2] = (byte)(x >> 8);
			Bytes[WriterIndex + 1] = (byte)(x >> 16);
			Bytes[WriterIndex] = (byte)((x >> 24) | 0xE0);
			WriterIndex += 4;
		}
		else if (x < 34359738368L)
		{
			EnsureWrite(5);
			Bytes[WriterIndex + 4] = (byte)x;
			Bytes[WriterIndex + 3] = (byte)(x >> 8);
			Bytes[WriterIndex + 2] = (byte)(x >> 16);
			Bytes[WriterIndex + 1] = (byte)(x >> 24);
			Bytes[WriterIndex] = (byte)((x >> 32) | 0xF0);
			WriterIndex += 5;
		}
		else if (x < 4398046511104L)
		{
			EnsureWrite(6);
			Bytes[WriterIndex + 5] = (byte)x;
			Bytes[WriterIndex + 4] = (byte)(x >> 8);
			Bytes[WriterIndex + 3] = (byte)(x >> 16);
			Bytes[WriterIndex + 2] = (byte)(x >> 24);
			Bytes[WriterIndex + 1] = (byte)(x >> 32);
			Bytes[WriterIndex] = (byte)((x >> 40) | 0xF8);
			WriterIndex += 6;
		}
		else if (x < 35184372088832L)
		{
			EnsureWrite(7);
			Bytes[WriterIndex + 6] = (byte)x;
			Bytes[WriterIndex + 5] = (byte)(x >> 8);
			Bytes[WriterIndex + 4] = (byte)(x >> 16);
			Bytes[WriterIndex + 3] = (byte)(x >> 24);
			Bytes[WriterIndex + 2] = (byte)(x >> 32);
			Bytes[WriterIndex + 1] = (byte)(x >> 40);
			Bytes[WriterIndex] = (byte)((x >> 48) | 0xFC);
			WriterIndex += 7;
		}
		else if (x < 72057594037927936L)
		{
			EnsureWrite(8);
			Bytes[WriterIndex + 7] = (byte)x;
			Bytes[WriterIndex + 6] = (byte)(x >> 8);
			Bytes[WriterIndex + 5] = (byte)(x >> 16);
			Bytes[WriterIndex + 4] = (byte)(x >> 24);
			Bytes[WriterIndex + 3] = (byte)(x >> 32);
			Bytes[WriterIndex + 2] = (byte)(x >> 40);
			Bytes[WriterIndex + 1] = (byte)(x >> 48);
			Bytes[WriterIndex] = 254;
			WriterIndex += 8;
		}
		else
		{
			EnsureWrite(9);
			Bytes[WriterIndex] = byte.MaxValue;
			Bytes[WriterIndex + 8] = (byte)x;
			Bytes[WriterIndex + 7] = (byte)(x >> 8);
			Bytes[WriterIndex + 6] = (byte)(x >> 16);
			Bytes[WriterIndex + 5] = (byte)(x >> 24);
			Bytes[WriterIndex + 4] = (byte)(x >> 32);
			Bytes[WriterIndex + 3] = (byte)(x >> 40);
			Bytes[WriterIndex + 2] = (byte)(x >> 48);
			Bytes[WriterIndex + 1] = (byte)(x >> 56);
			WriterIndex += 9;
		}
	}

	/// <summary>
	/// 读取无符号长整型
	/// </summary>
	/// <returns>读取的无符号长整型值</returns>
	public ulong ReadUlong()
	{
		EnsureRead(1);
		uint num = Bytes[ReaderIndex];
		if (num < 128)
		{
			ReaderIndex++;
			return num;
		}
		if (num < 192)
		{
			EnsureRead(2);
			uint num2 = ((num & 0x3F) << 8) | Bytes[ReaderIndex + 1];
			ReaderIndex += 2;
			return num2;
		}
		if (num < 224)
		{
			EnsureRead(3);
			uint num3 = ((num & 0x1F) << 16) | (uint)(Bytes[ReaderIndex + 1] << 8) | Bytes[ReaderIndex + 2];
			ReaderIndex += 3;
			return num3;
		}
		if (num < 240)
		{
			EnsureRead(4);
			uint num4 = ((num & 0xF) << 24) | (uint)(Bytes[ReaderIndex + 1] << 16) | (uint)(Bytes[ReaderIndex + 2] << 8) | Bytes[ReaderIndex + 3];
			ReaderIndex += 4;
			return num4;
		}
		if (num < 248)
		{
			EnsureRead(5);
			uint num5 = (uint)((Bytes[ReaderIndex + 1] << 24) | (Bytes[ReaderIndex + 2] << 16) | (Bytes[ReaderIndex + 3] << 8) | Bytes[ReaderIndex + 4]);
			uint num6 = num & 7;
			ReaderIndex += 5;
			return ((ulong)num6 << 32) | num5;
		}
		if (num < 252)
		{
			EnsureRead(6);
			uint num7 = (uint)((Bytes[ReaderIndex + 2] << 24) | (Bytes[ReaderIndex + 3] << 16) | (Bytes[ReaderIndex + 4] << 8) | Bytes[ReaderIndex + 5]);
			uint num8 = ((num & 3) << 8) | Bytes[ReaderIndex + 1];
			ReaderIndex += 6;
			return ((ulong)num8 << 32) | num7;
		}
		if (num < 254)
		{
			EnsureRead(7);
			uint num9 = (uint)((Bytes[ReaderIndex + 3] << 24) | (Bytes[ReaderIndex + 4] << 16) | (Bytes[ReaderIndex + 5] << 8) | Bytes[ReaderIndex + 6]);
			uint num10 = ((num & 1) << 16) | (uint)(Bytes[ReaderIndex + 1] << 8) | Bytes[ReaderIndex + 2];
			ReaderIndex += 7;
			return ((ulong)num10 << 32) | num9;
		}
		if (num < 255)
		{
			EnsureRead(8);
			uint num11 = (uint)((Bytes[ReaderIndex + 4] << 24) | (Bytes[ReaderIndex + 5] << 16) | (Bytes[ReaderIndex + 6] << 8) | Bytes[ReaderIndex + 7]);
			int num12 = (Bytes[ReaderIndex + 1] << 16) | (Bytes[ReaderIndex + 2] << 8) | Bytes[ReaderIndex + 3];
			ReaderIndex += 8;
			return ((ulong)(uint)num12 << 32) | num11;
		}
		EnsureRead(9);
		uint num13 = (uint)((Bytes[ReaderIndex + 5] << 24) | (Bytes[ReaderIndex + 6] << 16) | (Bytes[ReaderIndex + 7] << 8) | Bytes[ReaderIndex + 8]);
		int num14 = (Bytes[ReaderIndex + 1] << 24) | (Bytes[ReaderIndex + 2] << 16) | (Bytes[ReaderIndex + 3] << 8) | Bytes[ReaderIndex + 4];
		ReaderIndex += 9;
		return ((ulong)(uint)num14 << 32) | num13;
	}

	/// <summary>
	/// 写入长整型
	/// </summary>
	/// <param name="x">要写入的长整型值</param>
	public void WriteFlong(long x)
	{
		EnsureWrite(8);
		Bytes[WriterIndex] = (byte)x;
		Bytes[WriterIndex + 1] = (byte)(x >> 8);
		Bytes[WriterIndex + 2] = (byte)(x >> 16);
		Bytes[WriterIndex + 3] = (byte)(x >> 24);
		Bytes[WriterIndex + 4] = (byte)(x >> 32);
		Bytes[WriterIndex + 5] = (byte)(x >> 40);
		Bytes[WriterIndex + 6] = (byte)(x >> 48);
		Bytes[WriterIndex + 7] = (byte)(x >> 56);
		WriterIndex += 8;
	}

	/// <summary>
	/// 读取长整型
	/// </summary>
	/// <returns>读取的长整型值</returns>   
	public long ReadFlong()
	{
		EnsureRead(8);
		int num = (Bytes[ReaderIndex + 3] << 24) | (Bytes[ReaderIndex + 2] << 16) | (Bytes[ReaderIndex + 1] << 8) | Bytes[ReaderIndex];
		long result = ((long)((Bytes[ReaderIndex + 7] << 24) | (Bytes[ReaderIndex + 6] << 16) | (Bytes[ReaderIndex + 5] << 8) | Bytes[ReaderIndex + 4]) << 32) | num;
		ReaderIndex += 8;
		return result;
	}

	private unsafe static void Copy8(byte* dst, byte* src)
	{
		*dst = *src;
		dst[1] = src[1];
		dst[2] = src[2];
		dst[3] = src[3];
		dst[4] = src[4];
		dst[5] = src[5];
		dst[6] = src[6];
		dst[7] = src[7];
	}

	private unsafe static void Copy4(byte* dst, byte* src)
	{
		*dst = *src;
		dst[1] = src[1];
		dst[2] = src[2];
		dst[3] = src[3];
	}

	/// <summary>
	/// 写入浮点数
	/// </summary>
	/// <param name="x">要写入的浮点数</param>
	public unsafe void WriteFloat(float x)
	{
		EnsureWrite(4);
		fixed (byte* ptr = &Bytes[WriterIndex])
		{
			if ((long)ptr % 4L == 0L)
			{
				*(float*)ptr = x;
			}
			else
			{
				Copy4(ptr, (byte*)(&x));
			}
		}
		WriterIndex += 4;
	}

	/// <summary>
	/// 读取浮点数
	/// </summary>
	/// <returns>读取的浮点数</returns>
	public unsafe float ReadFloat()
	{
		EnsureRead(4);
		float result = default(float);
		fixed (byte* ptr = &Bytes[ReaderIndex])
		{
			if ((long)ptr % 4L == 0L)
			{
				result = *(float*)ptr;
			}
			else
			{
				*(int*)(&result) = *ptr | (ptr[1] << 8) | (ptr[2] << 16) | (ptr[3] << 24);
			}
		}
		ReaderIndex += 4;
		return result;
	}

	/// <summary>
	/// 写入双精度浮点数
	/// </summary>
	/// <param name="x">要写入的双精度浮点数</param>
	public unsafe void WriteDouble(double x)
	{
		EnsureWrite(8);
		fixed (byte* ptr = &Bytes[WriterIndex])
		{
			if ((long)ptr % 8L == 0L)
			{
				*(double*)ptr = x;
			}
			else
			{
				Copy8(ptr, (byte*)(&x));
			}
		}
		WriterIndex += 8;
	}

	/// <summary>
	/// 读取双精度浮点数
	/// </summary>
	/// <returns>读取的双精度浮点数</returns>
	public unsafe double ReadDouble()
	{
		EnsureRead(8);
		double result = default(double);
		fixed (byte* ptr = &Bytes[ReaderIndex])
		{
			if ((long)ptr % 8L == 0L)
			{
				result = *(double*)ptr;
			}
			else
			{
				int num = *ptr | (ptr[1] << 8) | (ptr[2] << 16) | (ptr[3] << 24);
				int num2 = ptr[4] | (ptr[5] << 8) | (ptr[6] << 16) | (ptr[7] << 24);
				*(long*)(&result) = ((long)num2 << 32) | (uint)num;
			}
		}
		ReaderIndex += 8;
		return result;
	}

	/// <summary>
	/// 写入大小
	/// </summary>
	/// <param name="n">要写入的大小</param>    
	public void WriteSize(int n)
	{
		WriteUint((uint)n);
	}

	/// <summary>
	/// 读取大小
	/// </summary>
	/// <returns>读取的大小</returns>
	public int ReadSize()
	{
		return (int)ReadUint();
	}

	/// <summary>
	/// 写入有符号整型
	/// </summary>
	/// <param name="x">要写入的有符号整型值</param>
	public void WriteSint(int x)
	{
		WriteUint((uint)((x << 1) ^ (x >>> 31)));
	}

	/// <summary>
	/// 读取有符号整型
	/// </summary>
	/// <returns>读取的有符号整型值</returns>
	public int ReadSint()
	{
		uint num = ReadUint();
		return (int)((num >> 1) ^ ((num & 1) << 31));
	}

	/// <summary>
	/// 写入长整型
	/// </summary>
	/// <param name="x">要写入的长整型值</param>
	public void WriteSlong(long x)
	{
		WriteUlong((ulong)((x << 1) ^ (x >>> 63)));
	}

	/// <summary>
	/// 读取长整型
	/// </summary>
	/// <returns>读取的长整型值</returns>
	public long ReadSlong()
	{
		long num = ReadLong();
		return (num >>> 1) ^ ((num & 1) << 63);
	}

	/// <summary>
	/// 写入字符串
	/// </summary>
	/// <param name="x">要写入的字符串</param>
	public void WriteString(string x)
	{
		int num = ((x != null) ? Encoding.UTF8.GetByteCount(x) : 0);
		WriteSize(num);
		if (num > 0)
		{
			EnsureWrite(num);
			Encoding.UTF8.GetBytes(x, 0, x.Length, Bytes, WriterIndex);
			WriterIndex += num;
		}
	}

	/// <summary>
	/// 读取字符串
	/// </summary>
	/// <returns>读取的字符串</returns> 
	public string ReadString()
	{
		int num = ReadSize();
		if (num > 0)
		{
			EnsureRead(num);
			string result = ((StringCacheFinder != null) ? StringCacheFinder(Bytes, ReaderIndex, num) : Encoding.UTF8.GetString(Bytes, ReaderIndex, num));
			ReaderIndex += num;
			return result;
		}
		return string.Empty;
	}

	/// <summary>
	/// 写入字节数组
	/// </summary>
	/// <param name="x">要写入的字节数组</param>
	public void WriteBytes(byte[] x)
	{
		int num = ((x != null) ? x.Length : 0);
		WriteSize(num);
		if (num > 0)
		{
			EnsureWrite(num);
			x.CopyTo(Bytes, WriterIndex);
			WriterIndex += num;
		}
	}

	/// <summary>
	/// 读取字节数组
	/// </summary>
	/// <returns>读取的字节数组</returns>
	public byte[] ReadBytes()
	{
		int num = ReadSize();
		if (num > 0)
		{
			EnsureRead(num);
			byte[] array = new byte[num];
			Buffer.BlockCopy(Bytes, ReaderIndex, array, 0, num);
			ReaderIndex += num;
			return array;
		}
		return Array.Empty<byte>();
	}

	/// <summary>
	/// 写入复数
	/// </summary>
	/// <param name="x">要写入的复数</param>
	public void WriteComplex(Complex x)
	{
		WriteDouble(x.Real);
		WriteDouble(x.Imaginary);
	}

	/// <summary>
	/// 读取复数
	/// </summary>
	/// <returns>读取的复数</returns>
	public Complex ReadComplex()
	{
		double real = ReadDouble();
		double imaginary = ReadDouble();
		return new Complex(real, imaginary);
	}

	/// <summary>
	/// 写入二维向量
	/// </summary>
	/// <param name="x">要写入的二维向量</param>
	public void WriteVector2(Vector2 x)
	{
		WriteFloat(x.X);
		WriteFloat(x.Y);
	}

	/// <summary>
	/// 读取二维向量
	/// </summary>
	/// <returns>读取的二维向量</returns>
	public Vector2 ReadVector2()
	{
		float x = ReadFloat();
		float y = ReadFloat();
		return new Vector2(x, y);
	}

	/// <summary>
	/// 写入三维向量
	/// </summary>
	/// <param name="x">要写入的三维向量</param>
	public void WriteVector3(Vector3 x)
	{
		WriteFloat(x.X);
		WriteFloat(x.Y);
		WriteFloat(x.Z);
	}

	/// <summary>
	/// 读取三维向量
	/// </summary>
	/// <returns>读取的三维向量</returns>
	public Vector3 ReadVector3()
	{
		float x = ReadFloat();
		float y = ReadFloat();
		float z = ReadFloat();
		return new Vector3(x, y, z);
	}

	/// <summary>
	/// 写入四维向量
	/// </summary>
	/// <param name="x">要写入的四维向量</param>
	public void WriteVector4(Vector4 x)
	{
		WriteFloat(x.X);
		WriteFloat(x.Y);
		WriteFloat(x.Z);
		WriteFloat(x.W);
	}

	/// <summary>
	/// 读取四维向量
	/// </summary>
	/// <returns>读取的四维向量</returns>
	public Vector4 ReadVector4()
	{
		float x = ReadFloat();
		float y = ReadFloat();
		float z = ReadFloat();
		float w = ReadFloat();
		return new Vector4(x, y, z, w);
	}

	/// <summary>
	/// 写入四元数
	/// </summary>
	/// <param name="x">要写入的四元数</param>
	public void WriteQuaternion(Quaternion x)
	{
		WriteFloat(x.X);
		WriteFloat(x.Y);
		WriteFloat(x.Z);
		WriteFloat(x.W);
	}

	/// <summary>
	/// 读取四元数
	/// </summary>
	/// <returns>读取的四元数</returns>
	public Quaternion ReadQuaternion()
	{
		float x = ReadFloat();
		float y = ReadFloat();
		float z = ReadFloat();
		float w = ReadFloat();
		return new Quaternion(x, y, z, w);
	}

	/// <summary>
	/// 写入4x4矩阵
	/// </summary>
	/// <param name="x">要写入的4x4矩阵</param>
	public void WriteMatrix4x4(Matrix4x4 x)
	{
		WriteFloat(x.M11);
		WriteFloat(x.M12);
		WriteFloat(x.M13);
		WriteFloat(x.M14);
		WriteFloat(x.M21);
		WriteFloat(x.M22);
		WriteFloat(x.M23);
		WriteFloat(x.M24);
		WriteFloat(x.M31);
		WriteFloat(x.M32);
		WriteFloat(x.M33);
		WriteFloat(x.M34);
		WriteFloat(x.M41);
		WriteFloat(x.M42);
		WriteFloat(x.M43);
		WriteFloat(x.M44);
	}

	/// <summary>
	/// 读取4x4矩阵
	/// </summary>
	/// <returns>读取的4x4矩阵</returns>
	public Matrix4x4 ReadMatrix4x4()
	{
		float m = ReadFloat();
		float m2 = ReadFloat();
		float m3 = ReadFloat();
		float m4 = ReadFloat();
		float m5 = ReadFloat();
		float m6 = ReadFloat();
		float m7 = ReadFloat();
		float m8 = ReadFloat();
		float m9 = ReadFloat();
		float m10 = ReadFloat();
		float m11 = ReadFloat();
		float m12 = ReadFloat();
		float m13 = ReadFloat();
		float m14 = ReadFloat();
		float m15 = ReadFloat();
		float m16 = ReadFloat();
		return new Matrix4x4(m, m2, m3, m4, m5, m6, m7, m8, m9, m10, m11, m12, m13, m14, m15, m16);
	}

	/// <summary>
	/// 跳过字节
	/// </summary>
	internal void SkipBytes()
	{
		int num = ReadSize();
		EnsureRead(num);
		ReaderIndex += num;
	}

	/// <summary>
	/// 写入字节缓冲区
	/// </summary>
	/// <param name="o">要写入的字节缓冲区</param>
	public void WriteByteBufWithSize(ByteBuf o)
	{
		int size = o.Size;
		if (size > 0)
		{
			WriteSize(size);
			WriteBytesWithoutSize(o.Bytes, o.ReaderIndex, size);
		}
		else
		{
			WriteByte(0);
		}
	}

	/// <summary>
	/// 写入字节缓冲区
	/// </summary>
	/// <param name="o">要写入的字节缓冲区</param>
	public void WriteByteBufWithoutSize(ByteBuf o)
	{
		int size = o.Size;
		if (size > 0)
		{
			WriteBytesWithoutSize(o.Bytes, o.ReaderIndex, size);
		}
	}

	/// <summary>
	/// 尝试读取字节
	/// </summary>
	/// <param name="x">要读取的字节</param>
	/// <returns>是否成功读取</returns>
	public bool TryReadByte(out byte x)
	{
		if (CanRead(1))
		{
			x = Bytes[ReaderIndex++];
			return true;
		}
		x = 0;
		return false;
	}

	/// <summary>
	/// 尝试反序列化字节缓冲区
	/// </summary>
	/// <param name="maxSize">最大大小</param>
	/// <param name="inplaceTempBody">临时字节缓冲区</param>
	/// <returns>反序列化错误</returns>
	public EDeserializeError TryDeserializeInplaceByteBuf(int maxSize, ByteBuf inplaceTempBody)
	{
		int readerIndex = ReaderIndex;
		bool flag = false;
		try
		{
			int num = Bytes[ReaderIndex];
			int num2;
			if (num < 128)
			{
				ReaderIndex++;
				num2 = num;
			}
			else if (num < 192)
			{
				if (!CanRead(2))
				{
					return EDeserializeError.NOT_ENOUGH;
				}
				num2 = ((num & 0x3F) << 8) | Bytes[ReaderIndex + 1];
				ReaderIndex += 2;
			}
			else if (num < 224)
			{
				if (!CanRead(3))
				{
					return EDeserializeError.NOT_ENOUGH;
				}
				num2 = ((num & 0x1F) << 16) | (Bytes[ReaderIndex + 1] << 8) | Bytes[ReaderIndex + 2];
				ReaderIndex += 3;
			}
			else
			{
				if (num >= 240)
				{
					return EDeserializeError.EXCEED_SIZE;
				}
				if (!CanRead(4))
				{
					return EDeserializeError.NOT_ENOUGH;
				}
				num2 = ((num & 0xF) << 24) | (Bytes[ReaderIndex + 1] << 16) | (Bytes[ReaderIndex + 2] << 8) | Bytes[ReaderIndex + 3];
				ReaderIndex += 4;
			}
			if (num2 > maxSize)
			{
				return EDeserializeError.EXCEED_SIZE;
			}
			if (Remaining < num2)
			{
				return EDeserializeError.NOT_ENOUGH;
			}
			int readerIndex2 = ReaderIndex;
			ReaderIndex += num2;
			inplaceTempBody.Replace(Bytes, readerIndex2, ReaderIndex);
			flag = true;
		}
		finally
		{
			if (!flag)
			{
				ReaderIndex = readerIndex;
			}
		}
		return EDeserializeError.OK;
	}

	/// <summary>
	/// 写入原始标签
	/// </summary>
	/// <param name="b1">要写入的标签</param>
	public void WriteRawTag(byte b1)
	{
		EnsureWrite(1);
		Bytes[WriterIndex++] = b1;
	}

	/// <summary>
	/// 写入原始标签
	/// </summary>
	/// <param name="b1">要写入的标签</param>
	/// <param name="b2">要写入的标签</param>
	public void WriteRawTag(byte b1, byte b2)
	{
		EnsureWrite(2);
		Bytes[WriterIndex] = b1;
		Bytes[WriterIndex + 1] = b2;
		WriterIndex += 2;
	}

	/// <summary>
	/// 写入原始标签
	/// </summary>
	/// <param name="b1">要写入的标签</param>
	/// <param name="b2">要写入的标签</param>
	/// <param name="b3">要写入的标签</param>
	public void WriteRawTag(byte b1, byte b2, byte b3)
	{
		EnsureWrite(3);
		Bytes[WriterIndex] = b1;
		Bytes[WriterIndex + 1] = b2;
		Bytes[WriterIndex + 2] = b3;
		WriterIndex += 3;
	}

	/// <summary>
	/// 开始写入段
	/// </summary>
	/// <param name="oldSize">旧大小</param>
	public void BeginWriteSegment(out int oldSize)
	{
		oldSize = Size;
		EnsureWrite(1);
		WriterIndex++;
	}

	/// <summary>
	/// 结束写入段
	/// </summary>
	/// <param name="oldSize">旧大小</param>
	public void EndWriteSegment(int oldSize)
	{
		int num = ReaderIndex + oldSize;
		int num2 = WriterIndex - num - 1;
		if (num2 < 128)
		{
			Bytes[num] = (byte)num2;
			return;
		}
		if (num2 < 16384)
		{
			EnsureWrite(1);
			Bytes[WriterIndex] = Bytes[num + 1];
			Bytes[num + 1] = (byte)num2;
			Bytes[num] = (byte)((num2 >> 8) | 0x80);
			WriterIndex++;
			return;
		}
		if (num2 < 2097152)
		{
			EnsureWrite(2);
			Bytes[WriterIndex + 1] = Bytes[num + 2];
			Bytes[num + 2] = (byte)num2;
			Bytes[WriterIndex] = Bytes[num + 1];
			Bytes[num + 1] = (byte)(num2 >> 8);
			Bytes[num] = (byte)((num2 >> 16) | 0xC0);
			WriterIndex += 2;
			return;
		}
		if (num2 < 268435456)
		{
			EnsureWrite(3);
			Bytes[WriterIndex + 2] = Bytes[num + 3];
			Bytes[num + 3] = (byte)num2;
			Bytes[WriterIndex + 1] = Bytes[num + 2];
			Bytes[num + 2] = (byte)(num2 >> 8);
			Bytes[WriterIndex] = Bytes[num + 1];
			Bytes[num + 1] = (byte)(num2 >> 16);
			Bytes[num] = (byte)((num2 >> 24) | 0xE0);
			WriterIndex += 3;
			return;
		}
		throw new SerializationException("exceed max segment size");
	}

	/// <summary>
	/// 读取段
	/// </summary>
	/// <param name="startIndex">开始索引</param>
	/// <param name="segmentSize">段大小</param>
	public void ReadSegment(out int startIndex, out int segmentSize)
	{
		EnsureRead(1);
		int num = Bytes[ReaderIndex++];
		startIndex = ReaderIndex;
		if (num < 128)
		{
			segmentSize = num;
			ReaderIndex += segmentSize;
		}
		else if (num < 192)
		{
			EnsureRead(1);
			segmentSize = ((num & 0x3F) << 8) | Bytes[ReaderIndex];
			int num2 = ReaderIndex + segmentSize;
			Bytes[ReaderIndex] = Bytes[num2];
			ReaderIndex += segmentSize + 1;
		}
		else if (num < 224)
		{
			EnsureRead(2);
			segmentSize = ((num & 0x1F) << 16) | (Bytes[ReaderIndex] << 8) | Bytes[ReaderIndex + 1];
			int num3 = ReaderIndex + segmentSize;
			Bytes[ReaderIndex] = Bytes[num3];
			Bytes[ReaderIndex + 1] = Bytes[num3 + 1];
			ReaderIndex += segmentSize + 2;
		}
		else
		{
			if (num >= 240)
			{
				throw new SerializationException("exceed max size");
			}
			EnsureRead(3);
			segmentSize = ((num & 0xF) << 24) | (Bytes[ReaderIndex] << 16) | (Bytes[ReaderIndex + 1] << 8) | Bytes[ReaderIndex + 2];
			int num4 = ReaderIndex + segmentSize;
			Bytes[ReaderIndex] = Bytes[num4];
			Bytes[ReaderIndex + 1] = Bytes[num4 + 1];
			Bytes[ReaderIndex + 2] = Bytes[num4 + 2];
			ReaderIndex += segmentSize + 3;
		}
		if (ReaderIndex > WriterIndex)
		{
			throw new SerializationException("segment data not enough");
		}
	}

	/// <summary>
	/// 读取段
	/// </summary>
	/// <param name="buf">要读取的段</param>
	public void ReadSegment(ByteBuf buf)
	{
		ReadSegment(out var startIndex, out var segmentSize);
		buf.Bytes = Bytes;
		buf.ReaderIndex = startIndex;
		buf.WriterIndex = startIndex + segmentSize;
	}

	/// <summary>
	/// 进入段
	/// </summary>
	/// <param name="saveState">段保存状态</param>
	public void EnterSegment(out SegmentSaveState saveState)
	{
		ReadSegment(out var startIndex, out var segmentSize);
		saveState = new SegmentSaveState(ReaderIndex, WriterIndex);
		ReaderIndex = startIndex;
		WriterIndex = startIndex + segmentSize;
	}

	/// <summary>
	/// 离开段
	/// </summary>
	/// <param name="saveState">段保存状态</param>
	public void LeaveSegment(SegmentSaveState saveState)
	{
		ReaderIndex = saveState.ReaderIndex;
		WriterIndex = saveState.WriterIndex;
	}

	/// <summary>
	/// 转换为字符串
	/// </summary>
	/// <returns>字符串</returns>
	public override string ToString()
	{
		string[] array = new string[WriterIndex - ReaderIndex];
		for (int i = ReaderIndex; i < WriterIndex; i++)
		{
			array[i - ReaderIndex] = Bytes[i].ToString("X2");
		}
		return string.Join(".", array);
	}

	/// <summary>
	/// 比较是否相等
	/// </summary>
	/// <param name="obj">要比较的对象</param>
	/// <returns>是否相等</returns>
	public override bool Equals(object obj)
	{
		if (obj is ByteBuf other)
		{
			return Equals(other);
		}
		return false;
	}

	/// <summary>
	/// 比较是否相等
	/// </summary>
	/// <param name="other">要比较的对象</param>
	/// <returns>是否相等</returns> 
	public bool Equals(ByteBuf other)
	{
		if (other == null)
		{
			return false;
		}
		if (Size != other.Size)
		{
			return false;
		}
		int i = 0;
		for (int size = Size; i < size; i++)
		{
			if (Bytes[ReaderIndex + i] != other.Bytes[other.ReaderIndex + i])
			{
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// 克隆
	/// </summary>
	/// <returns>克隆的对象</returns>
	public object Clone()
	{
		return new ByteBuf(CopyData());
	}

	/// <summary>
	/// 从字符串创建字节缓冲区
	/// </summary>
	/// <param name="value">要创建的字符串</param>
	/// <returns>字节缓冲区</returns>
	public static ByteBuf FromString(string value)
	{
		string[] array = value.Split(',');
		byte[] array2 = new byte[array.Length];
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i] = byte.Parse(array[i]);
		}
		return new ByteBuf(array2);
	}

	/// <summary>
	/// 获取哈希码
	/// </summary>
	/// <returns>哈希码</returns>
	public override int GetHashCode()
	{
		int num = 17;
		for (int i = ReaderIndex; i < WriterIndex; i++)
		{
			num = num * 23 + Bytes[i];
		}
		return num;
	}

	/// <summary>
	/// 释放
	/// </summary>
	public void Release()
	{
		_releaser?.Invoke(this);
	}
}
