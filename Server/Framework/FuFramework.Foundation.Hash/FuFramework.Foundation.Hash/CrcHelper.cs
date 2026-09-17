using System;
using System.Buffers;
using System.Buffers.Binary;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace FuFramework.Foundation.Hash;

/// <summary>
/// CRC校验相关的实用函数。
/// 提供CRC32和CRC64两种校验算法的实现。
/// </summary>
public static class CrcHelper
{
	/// <summary>
	/// CRC32 算法。
	/// </summary>
	internal sealed class Crc32
	{
		private const int TableLength = 256;

		private const uint DefaultPolynomial = 3988292384u;

		private const uint DefaultSeed = uint.MaxValue;

		private readonly uint m_Seed;

		private readonly uint[] m_Table;

		private uint m_Hash;

		public Crc32()
			: this(3988292384u, uint.MaxValue)
		{
		}

		public Crc32(uint polynomial, uint seed)
		{
			m_Seed = seed;
			m_Table = InitializeTable(polynomial);
			m_Hash = seed;
		}

		public void Initialize()
		{
			m_Hash = m_Seed;
		}

		public void HashCore(byte[] bytes, int offset, int length)
		{
			m_Hash = CalculateHash(m_Table, m_Hash, bytes, offset, length);
		}

		public uint HashFinal()
		{
			return ~m_Hash;
		}

		private static uint CalculateHash(uint[] table, uint value, byte[] bytes, int offset, int length)
		{
			int num = offset + length;
			for (int i = offset; i < num; i++)
			{
				value = (value >> 8) ^ table[bytes[i] ^ (value & 0xFF)];
			}
			return value;
		}

		private static uint[] InitializeTable(uint polynomial)
		{
			uint[] array = new uint[256];
			for (int i = 0; i < 256; i++)
			{
				uint num = (uint)i;
				for (int j = 0; j < 8; j++)
				{
					num = (((num & 1) != 1) ? (num >> 1) : ((num >> 1) ^ polynomial));
				}
				array[i] = num;
			}
			return array;
		}
	}

	/// <summary>
	/// Provides an implementation of the CRC-64 algorithm as described in ECMA-182, Annex B.
	/// </summary>
	/// <remarks>
	///     <para>
	///     For methods that return byte arrays or that write into spans of bytes,
	///     this implementation emits the answer in the Big Endian byte order so that
	///     the CRC residue relationship (CRC(message concat CRC(message))) is a fixed value) holds.
	///     For CRC-64 this stable output is the byte sequence
	///     <c>{ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }</c>.
	///     </para>
	///     <para>
	///     There are multiple, incompatible, definitions of a 64-bit cyclic redundancy
	///     check (CRC) algorithm. When interoperating with another system, ensure that you
	///     are using the same definition. The definition used by this implementation is not
	///     compatible with the cyclic redundancy check described in ISO 3309.
	///     </para>
	/// </remarks>
	public sealed class Crc64 : NonCryptographicHashAlgorithm
	{
		private const ulong InitialState = 0uL;

		private const int Size = 8;

		private ulong _crc;

		/// <summary>CRC-64 transition table.</summary>
		private static ReadOnlySpan<ulong> CrcLookup => new ulong[256]; // Decompilation stub - original initialization table not recoverable

		/// <summary>
		/// Initializes a new instance of the <see cref="T:FuFramework.Foundation.Hash.CrcHelper.Crc64" /> class.
		/// </summary>
		public Crc64()
			: base(8)
		{
		}

		/// <summary>
		/// Appends the contents of <paramref name="source" /> to the data already
		/// processed for the current hash computation.
		/// </summary>
		/// <param name="source">The data to process.</param>
		public override void Append(ReadOnlySpan<byte> source)
		{
			_crc = Update(_crc, source);
		}

		/// <summary>
		/// Resets the hash computation to the initial state.
		/// </summary>
		public override void Reset()
		{
			_crc = 0uL;
		}

		/// <summary>
		/// Writes the computed hash value to <paramref name="destination" />
		/// without modifying accumulated state.
		/// </summary>
		/// <param name="destination">The buffer that receives the computed hash value.</param>
		protected override void GetCurrentHashCore(Span<byte> destination)
		{
			BinaryPrimitives.WriteUInt64BigEndian(destination, _crc);
		}

		/// <summary>
		/// Writes the computed hash value to <paramref name="destination" />
		/// then clears the accumulated state.
		/// </summary>
		protected override void GetHashAndResetCore(Span<byte> destination)
		{
			BinaryPrimitives.WriteUInt64BigEndian(destination, _crc);
			_crc = 0uL;
		}

		/// <summary>Gets the current computed hash value without modifying accumulated state.</summary>
		/// <returns>The hash value for the data already provided.</returns>
		public ulong GetCurrentHashAsUInt64()
		{
			return _crc;
		}

		/// <summary>
		/// Computes the CRC-64 hash of the provided data.
		/// </summary>
		/// <param name="source">The data to hash.</param>
		/// <returns>The CRC-64 hash of the provided data.</returns>
		/// <exception cref="T:System.ArgumentNullException">
		/// <paramref name="source" /> is <see langword="null" />.
		/// </exception>
		public static byte[] Hash(byte[] source)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			return Hash(new ReadOnlySpan<byte>(source));
		}

		/// <summary>
		/// Computes the CRC-64 hash of the provided data.
		/// </summary>
		/// <param name="source">The data to hash.</param>
		/// <returns>The CRC-64 hash of the provided data.</returns>
		public static byte[] Hash(ReadOnlySpan<byte> source)
		{
			byte[] array = new byte[8];
			BinaryPrimitives.WriteUInt64BigEndian(value: HashToUInt64(source), destination: array);
			return array;
		}

		/// <summary>
		/// Attempts to compute the CRC-64 hash of the provided data into the provided destination.
		/// </summary>
		/// <param name="source">The data to hash.</param>
		/// <param name="destination">The buffer that receives the computed hash value.</param>
		/// <param name="bytesWritten">
		/// On success, receives the number of bytes written to <paramref name="destination" />.
		/// </param>
		/// <returns>
		/// <see langword="true" /> if <paramref name="destination" /> is long enough to receive
		/// the computed hash value (8 bytes); otherwise, <see langword="false" />.
		/// </returns>
		public static bool TryHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
		{
			if (destination.Length < 8)
			{
				bytesWritten = 0;
				return false;
			}
			ulong value = HashToUInt64(source);
			BinaryPrimitives.WriteUInt64BigEndian(destination, value);
			bytesWritten = 8;
			return true;
		}

		/// <summary>
		/// Computes the CRC-64 hash of the provided data into the provided destination.
		/// </summary>
		/// <param name="source">The data to hash.</param>
		/// <param name="destination">The buffer that receives the computed hash value.</param>
		/// <returns>
		/// The number of bytes written to <paramref name="destination" />.
		/// </returns>
		public static int Hash(ReadOnlySpan<byte> source, Span<byte> destination)
		{
			if (destination.Length < 8)
			{
				NonCryptographicHashAlgorithm.ThrowDestinationTooShort();
			}
			ulong value = HashToUInt64(source);
			BinaryPrimitives.WriteUInt64BigEndian(destination, value);
			return 8;
		}

		/// <summary>Computes the CRC-64 hash of the provided data.</summary>
		/// <param name="source">The data to hash.</param>
		/// <returns>The computed CRC-64 hash.</returns>
		public static ulong HashToUInt64(ReadOnlySpan<byte> source)
		{
			return Update(0uL, source);
		}

		private static ulong Update(ulong crc, ReadOnlySpan<byte> source)
		{
			ReadOnlySpan<ulong> crcLookup = CrcLookup;
			for (int i = 0; i < source.Length; i++)
			{
				ulong num = crc >> 56;
				num ^= source[i];
				crc = crcLookup[(int)num] ^ (crc << 8);
			}
			return crc;
		}
	}

	/// <summary>
	/// Represents a non-cryptographic hash algorithm.
	/// </summary>
	public abstract class NonCryptographicHashAlgorithm
	{
		/// <summary>
		/// Gets the number of bytes produced from this hash algorithm.
		/// </summary>
		/// <value>The number of bytes produced from this hash algorithm.</value>
		public int HashLengthInBytes { get; }

		/// <summary>
		/// Called from constructors in derived classes to initialize the
		/// <see cref="T:FuFramework.Foundation.Hash.CrcHelper.NonCryptographicHashAlgorithm" /> class.
		/// </summary>
		/// <param name="hashLengthInBytes">
		/// The number of bytes produced from this hash algorithm.
		/// </param>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		/// <paramref name="hashLengthInBytes" /> is less than 1.
		/// </exception>
		protected NonCryptographicHashAlgorithm(int hashLengthInBytes)
		{
			if (hashLengthInBytes < 1)
			{
				throw new ArgumentOutOfRangeException("hashLengthInBytes");
			}
			HashLengthInBytes = hashLengthInBytes;
		}

		/// <summary>
		/// When overridden in a derived class,
		/// appends the contents of <paramref name="source" /> to the data already
		/// processed for the current hash computation.
		/// </summary>
		/// <param name="source">The data to process.</param>
		public abstract void Append(ReadOnlySpan<byte> source);

		/// <summary>
		/// When overridden in a derived class,
		/// resets the hash computation to the initial state.
		/// </summary>
		public abstract void Reset();

		/// <summary>
		/// When overridden in a derived class,
		/// writes the computed hash value to <paramref name="destination" />
		/// without modifying accumulated state.
		/// </summary>
		/// <param name="destination">The buffer that receives the computed hash value.</param>
		/// <remarks>
		///     <para>
		///     Implementations of this method must write exactly
		///     <see cref="P:FuFramework.Foundation.Hash.CrcHelper.NonCryptographicHashAlgorithm.HashLengthInBytes" /> bytes to <paramref name="destination" />.
		///     Do not assume that the buffer was zero-initialized.
		///     </para>
		///     <para>
		///     The <see cref="T:FuFramework.Foundation.Hash.CrcHelper.NonCryptographicHashAlgorithm" /> class validates the
		///     size of the buffer before calling this method, and slices the span
		///     down to be exactly <see cref="P:FuFramework.Foundation.Hash.CrcHelper.NonCryptographicHashAlgorithm.HashLengthInBytes" /> in length.
		///     </para>
		/// </remarks>
		protected abstract void GetCurrentHashCore(Span<byte> destination);

		/// <summary>
		/// Appends the contents of <paramref name="source" /> to the data already
		/// processed for the current hash computation.
		/// </summary>
		/// <param name="source">The data to process.</param>
		/// <exception cref="T:System.ArgumentNullException">
		/// <paramref name="source" /> is <see langword="null" />.
		/// </exception>
		public void Append(byte[] source)
		{
			if (source == null)
			{
				throw new ArgumentNullException("source");
			}
			Append(new ReadOnlySpan<byte>(source));
		}

		/// <summary>
		/// Appends the contents of <paramref name="stream" /> to the data already
		/// processed for the current hash computation.
		/// </summary>
		/// <param name="stream">The data to process.</param>
		/// <exception cref="T:System.ArgumentNullException">
		/// <paramref name="stream" /> is <see langword="null" />.
		/// </exception>
		/// <seealso cref="T:System.IO.Stream" />
		public void Append(Stream stream)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}
			byte[] array = ArrayPool<byte>.Shared.Rent(4096);
			while (true)
			{
				int num = stream.Read(array, 0, array.Length);
				if (num == 0)
				{
					break;
				}
				Append(new ReadOnlySpan<byte>(array, 0, num));
			}
			ArrayPool<byte>.Shared.Return(array);
		}

		/// <summary>
		/// Gets the current computed hash value without modifying accumulated state.
		/// </summary>
		/// <returns>
		/// The hash value for the data already provided.
		/// </returns>
		public byte[] GetCurrentHash()
		{
			byte[] array = new byte[HashLengthInBytes];
			GetCurrentHashCore(array);
			return array;
		}

		/// <summary>
		/// Attempts to write the computed hash value to <paramref name="destination" />
		/// without modifying accumulated state.
		/// </summary>
		/// <param name="destination">The buffer that receives the computed hash value.</param>
		/// <param name="bytesWritten">
		/// On success, receives the number of bytes written to <paramref name="destination" />.
		/// </param>
		/// <returns>
		/// <see langword="true" /> if <paramref name="destination" /> is long enough to receive
		/// the computed hash value; otherwise, <see langword="false" />.
		/// </returns>
		public bool TryGetCurrentHash(Span<byte> destination, out int bytesWritten)
		{
			if (destination.Length < HashLengthInBytes)
			{
				bytesWritten = 0;
				return false;
			}
			GetCurrentHashCore(destination.Slice(0, HashLengthInBytes));
			bytesWritten = HashLengthInBytes;
			return true;
		}

		/// <summary>
		/// Writes the computed hash value to <paramref name="destination" />
		/// without modifying accumulated state.
		/// </summary>
		/// <param name="destination">The buffer that receives the computed hash value.</param>
		/// <returns>
		/// The number of bytes written to <paramref name="destination" />,
		/// which is always <see cref="P:FuFramework.Foundation.Hash.CrcHelper.NonCryptographicHashAlgorithm.HashLengthInBytes" />.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">
		/// <paramref name="destination" /> is shorter than <see cref="P:FuFramework.Foundation.Hash.CrcHelper.NonCryptographicHashAlgorithm.HashLengthInBytes" />.
		/// </exception>
		public int GetCurrentHash(Span<byte> destination)
		{
			if (destination.Length < HashLengthInBytes)
			{
				ThrowDestinationTooShort();
			}
			GetCurrentHashCore(destination.Slice(0, HashLengthInBytes));
			return HashLengthInBytes;
		}

		/// <summary>
		/// Gets the current computed hash value and clears the accumulated state.
		/// </summary>
		/// <returns>
		/// The hash value for the data already provided.
		/// </returns>
		public byte[] GetHashAndReset()
		{
			byte[] array = new byte[HashLengthInBytes];
			GetHashAndResetCore(array);
			return array;
		}

		/// <summary>
		/// Attempts to write the computed hash value to <paramref name="destination" />.
		/// If successful, clears the accumulated state.
		/// </summary>
		/// <param name="destination">The buffer that receives the computed hash value.</param>
		/// <param name="bytesWritten">
		/// On success, receives the number of bytes written to <paramref name="destination" />.
		/// </param>
		/// <returns>
		/// <see langword="true" /> and clears the accumulated state
		/// if <paramref name="destination" /> is long enough to receive
		/// the computed hash value; otherwise, <see langword="false" />.
		/// </returns>
		public bool TryGetHashAndReset(Span<byte> destination, out int bytesWritten)
		{
			if (destination.Length < HashLengthInBytes)
			{
				bytesWritten = 0;
				return false;
			}
			GetHashAndResetCore(destination.Slice(0, HashLengthInBytes));
			bytesWritten = HashLengthInBytes;
			return true;
		}

		/// <summary>
		/// Writes the computed hash value to <paramref name="destination" />
		/// then clears the accumulated state.
		/// </summary>
		/// <param name="destination">The buffer that receives the computed hash value.</param>
		/// <returns>
		/// The number of bytes written to <paramref name="destination" />,
		/// which is always <see cref="P:FuFramework.Foundation.Hash.CrcHelper.NonCryptographicHashAlgorithm.HashLengthInBytes" />.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">
		/// <paramref name="destination" /> is shorter than <see cref="P:FuFramework.Foundation.Hash.CrcHelper.NonCryptographicHashAlgorithm.HashLengthInBytes" />.
		/// </exception>
		public int GetHashAndReset(Span<byte> destination)
		{
			if (destination.Length < HashLengthInBytes)
			{
				ThrowDestinationTooShort();
			}
			GetHashAndResetCore(destination.Slice(0, HashLengthInBytes));
			return HashLengthInBytes;
		}

		/// <summary>
		/// Writes the computed hash value to <paramref name="destination" />
		/// then clears the accumulated state.
		/// </summary>
		/// <param name="destination">The buffer that receives the computed hash value.</param>
		/// <remarks>
		///     <para>
		///     Implementations of this method must write exactly
		///     <see cref="P:FuFramework.Foundation.Hash.CrcHelper.NonCryptographicHashAlgorithm.HashLengthInBytes" /> bytes to <paramref name="destination" />.
		///     Do not assume that the buffer was zero-initialized.
		///     </para>
		///     <para>
		///     The <see cref="T:FuFramework.Foundation.Hash.CrcHelper.NonCryptographicHashAlgorithm" /> class validates the
		///     size of the buffer before calling this method, and slices the span
		///     down to be exactly <see cref="P:FuFramework.Foundation.Hash.CrcHelper.NonCryptographicHashAlgorithm.HashLengthInBytes" /> in length.
		///     </para>
		///     <para>
		///     The default implementation of this method calls
		///     <see cref="M:FuFramework.Foundation.Hash.CrcHelper.NonCryptographicHashAlgorithm.GetCurrentHashCore(System.Span{System.Byte})" /> followed by <see cref="M:FuFramework.Foundation.Hash.CrcHelper.NonCryptographicHashAlgorithm.Reset" />.
		///     Overrides of this method do not need to call either of those methods,
		///     but must ensure that the caller cannot observe a difference in behavior.
		///     </para>
		/// </remarks>
		protected virtual void GetHashAndResetCore(Span<byte> destination)
		{
			GetCurrentHashCore(destination);
			Reset();
		}

		/// <summary>
		/// This method is not supported and should not be called.
		/// Call <see cref="M:FuFramework.Foundation.Hash.CrcHelper.NonCryptographicHashAlgorithm.GetCurrentHash" /> or <see cref="M:FuFramework.Foundation.Hash.CrcHelper.NonCryptographicHashAlgorithm.GetHashAndReset" />
		/// instead.
		/// </summary>
		/// <returns>This method will always throw a <see cref="T:System.NotSupportedException" />.</returns>
		/// <exception cref="T:System.NotSupportedException">In all cases.</exception>
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Use GetCurrentHash() to retrieve the computed hash code.", true)]
		public override int GetHashCode()
		{
			throw new NotSupportedException();
		}

		private protected static void ThrowDestinationTooShort()
		{
			throw new ArgumentException("destination");
		}
	}

	/// <summary>
	/// 缓存字节数组的长度,用于分块读取大文件
	/// </summary>
	private const int CachedBytesLength = 4096;

	/// <summary>
	/// 用于缓存读取数据的字节数组
	/// </summary>
	private static readonly byte[] SCachedBytes = new byte[4096];

	/// <summary>
	/// CRC32算法的实例
	/// </summary>
	private static readonly Crc32 SAlgorithm = new Crc32();

	/// <summary>
	/// CRC64算法的实例
	/// </summary>
	private static readonly Crc64 SAlgorithm64 = new Crc64();

	/// <summary>
	/// 计算二进制流的CRC64值
	/// </summary>
	/// <param name="bytes">要计算的二进制字节数组</param>
	/// <returns>计算得到的CRC64校验值</returns>
	public static ulong GetCrc64(byte[] bytes)
	{
		SAlgorithm64.Reset();
		SAlgorithm64.Append(bytes);
		return SAlgorithm64.GetCurrentHashAsUInt64();
	}

	/// <summary>
	/// 计算流的CRC64值
	/// </summary>
	/// <param name="stream">要计算的数据流</param>
	/// <returns>计算得到的CRC64校验值</returns>
	public static ulong GetCrc64(Stream stream)
	{
		SAlgorithm64.Reset();
		SAlgorithm64.Append(stream);
		return SAlgorithm64.GetCurrentHashAsUInt64();
	}

	/// <summary>
	/// 计算二进制流的CRC32值
	/// </summary>
	/// <param name="bytes">要计算的二进制字节数组</param>
	/// <returns>计算得到的CRC32校验值</returns>
	/// <exception cref="T:System.ArgumentNullException">当bytes参数为null时抛出</exception>
	public static int GetCrc32(byte[] bytes)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", "Bytes is invalid.");
		}
		return GetCrc32(bytes, 0, bytes.Length);
	}

	/// <summary>
	/// 计算二进制流指定范围的CRC32值
	/// </summary>
	/// <param name="bytes">要计算的二进制字节数组</param>
	/// <param name="offset">起始偏移量</param>
	/// <param name="length">要计算的长度</param>
	/// <returns>计算得到的CRC32校验值</returns>
	/// <exception cref="T:System.ArgumentNullException">当bytes参数为null时抛出</exception>
	/// <exception cref="T:System.ArgumentException">当offset或length参数无效时抛出</exception>
	public static int GetCrc32(byte[] bytes, int offset, int length)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", "Bytes is invalid.");
		}
		if (offset < 0 || length < 0 || offset + length > bytes.Length)
		{
			throw new ArgumentException("Offset or length is invalid.", "offset");
		}
		SAlgorithm.HashCore(bytes, offset, length);
		uint result = SAlgorithm.HashFinal();
		SAlgorithm.Initialize();
		return (int)result;
	}

	/// <summary>
	/// 计算流的CRC32值
	/// </summary>
	/// <param name="stream">要计算的数据流</param>
	/// <returns>计算得到的CRC32校验值</returns>
	/// <exception cref="T:System.ArgumentNullException">当stream参数为null时抛出</exception>
	public static int GetCrc32(Stream stream)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream", "Stream is invalid.");
		}
		while (true)
		{
			int num = stream.Read(SCachedBytes, 0, 4096);
			if (num <= 0)
			{
				break;
			}
			SAlgorithm.HashCore(SCachedBytes, 0, num);
		}
		uint result = SAlgorithm.HashFinal();
		SAlgorithm.Initialize();
		Array.Clear(SCachedBytes, 0, 4096);
		return (int)result;
	}

	/// <summary>
	/// 将CRC32值转换为字节数组
	/// </summary>
	/// <param name="crc32">要转换的CRC32值</param>
	/// <returns>转换后的4字节数组，按大端序排列</returns>
	public static byte[] GetCrc32Bytes(int crc32)
	{
		return new byte[4]
		{
			(byte)((crc32 >> 24) & 0xFF),
			(byte)((crc32 >> 16) & 0xFF),
			(byte)((crc32 >> 8) & 0xFF),
			(byte)(crc32 & 0xFF)
		};
	}

	/// <summary>
	/// 将CRC32值转换为字节数组并存入指定数组
	/// </summary>
	/// <param name="crc32">要转换的CRC32值</param>
	/// <param name="bytes">存放结果的目标数组</param>
	public static void GetCrc32Bytes(int crc32, byte[] bytes)
	{
		GetCrc32Bytes(crc32, bytes, 0);
	}

	/// <summary>
	/// 将CRC32值转换为字节数组并存入指定数组的指定位置
	/// </summary>
	/// <param name="crc32">要转换的CRC32值</param>
	/// <param name="bytes">存放结果的目标数组</param>
	/// <param name="offset">在目标数组中的起始位置</param>
	/// <exception cref="T:System.ArgumentNullException">当bytes参数为null时抛出</exception>
	/// <exception cref="T:System.ArgumentException">当offset参数无效或目标数组剩余空间不足4字节时抛出</exception>
	public static void GetCrc32Bytes(int crc32, byte[] bytes, int offset)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes", "Result is invalid.");
		}
		if (offset < 0 || offset + 4 > bytes.Length)
		{
			throw new ArgumentException("Offset or length is invalid.", "offset");
		}
		bytes[offset] = (byte)((crc32 >> 24) & 0xFF);
		bytes[offset + 1] = (byte)((crc32 >> 16) & 0xFF);
		bytes[offset + 2] = (byte)((crc32 >> 8) & 0xFF);
		bytes[offset + 3] = (byte)(crc32 & 0xFF);
	}

	/// <summary>
	/// 使用指定编码计算流的CRC32值
	/// </summary>
	/// <param name="stream">要计算的数据流</param>
	/// <param name="code">用于编码的字节数组，将与数据进行XOR运算</param>
	/// <param name="length">要计算的字节数，如果为负数或超过流长度则使用整个流</param>
	/// <returns>计算得到的CRC32校验值</returns>
	/// <exception cref="T:System.ArgumentNullException">当stream或code参数为null时抛出</exception>
	/// <exception cref="T:System.ArgumentException">当code长度小于等于0时抛出</exception>
	internal static int GetCrc32(Stream stream, byte[] code, int length)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream", "Stream is invalid.");
		}
		if (code == null)
		{
			throw new ArgumentNullException("code", "Code is invalid.");
		}
		int num = code.Length;
		if (num <= 0)
		{
			throw new ArgumentException("Code length is invalid.", "codeLength");
		}
		int num2 = (int)stream.Length;
		if (length < 0 || length > num2)
		{
			length = num2;
		}
		int num3 = 0;
		while (true)
		{
			int num4 = stream.Read(SCachedBytes, 0, 4096);
			if (num4 <= 0)
			{
				break;
			}
			if (length > 0)
			{
				for (int i = 0; i < num4 && i < length; i++)
				{
					SCachedBytes[i] ^= code[num3++];
					num3 %= num;
				}
				length -= num4;
			}
			SAlgorithm.HashCore(SCachedBytes, 0, num4);
		}
		uint result = SAlgorithm.HashFinal();
		SAlgorithm.Initialize();
		Array.Clear(SCachedBytes, 0, 4096);
		return (int)result;
	}
}
