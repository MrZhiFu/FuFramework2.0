// ReSharper disable once CheckNamespace

namespace FuFramework.Core.Runtime
{
    public static partial class Utility
    {
        public static partial class Verifier
        {
            /// <summary>
            /// CRC32 算法相关的实用函数。
            /// 功能：
            ///     1. 提供 CRC32 算法的默认多项式和种子值。
            ///     2. 提供 CRC32 算法的查找表初始化。
            ///     3. 提供 CRC32 算法的哈希值计算。
            ///     4. 提供 CRC32 算法的重置哈希值方法。
            /// CRC32（循环冗余校验32位）是一种广泛应用于数据完整性校验的技术，通过生成一个32位的哈希值来检测数据传输或存储过程中的错误
            /// </summary>
            private sealed class Crc32
            {
                /// <summary>
                /// CRC32 算法的查找表长度(256)。
                /// </summary>
                private const int TableLength = 256;

                /// <summary>
                /// CRC32 算法的默认多项式(0xedb88320 十进制为3,990,539,042)。
                /// </summary>
                private const uint DefaultPolynomial = 0xedb88320;

                /// <summary>
                /// CRC32 算法的默认种子值(0xffffffff 十进制为4,294,967,295)。
                /// </summary>
                private const uint DefaultSeed = 0xffffffff;

                /// <summary>
                /// CRC32 算法的种子值。
                /// </summary>
                private readonly uint m_Seed;

                /// <summary>
                /// 存储预计算的CRC32查找表。
                /// </summary>
                private readonly uint[] m_Table;

                /// <summary>
                /// 当前的哈希值。
                /// </summary>
                private uint m_Hash;

                /// <summary>
                /// 使用默认多项式和种子初始化 CRC32。
                /// </summary>
                public Crc32() : this(DefaultPolynomial, DefaultSeed) { }

                /// <summary>
                /// 使用指定的多项式和种子初始化 CRC32。
                /// </summary>
                /// <param name="polynomial">CRC32 多项式。</param>
                /// <param name="seed">CRC32 种子值。</param>
                public Crc32(uint polynomial, uint seed)
                {
                    m_Seed  = seed;
                    m_Table = InitializeTable(polynomial);
                    m_Hash  = seed;
                }

                /// <summary>
                /// 重置 CRC32 哈希值。
                /// </summary>
                public void Initialize() => m_Hash = m_Seed;

                /// <summary>
                /// 计算指定字节数组的 CRC32 哈希值。
                /// </summary>
                /// <param name="bytes">字节数组。</param>
                /// <param name="offset">字节数组中开始计算的偏移量。</param>
                /// <param name="length">要计算的字节数量。</param>
                public void HashCore(byte[] bytes, int offset, int length)
                    => m_Hash = CalculateHash(m_Table, m_Hash, bytes, offset, length);

                /// <summary>
                /// 完成 CRC32 哈希计算并返回最终结果。
                /// </summary>
                public uint HashFinal() => ~m_Hash;

                /// <summary>
                /// 计算 CRC32 哈希值。
                /// </summary>
                /// <param name="table">CRC32 查找表。</param>
                /// <param name="value">当前哈希值。</param>
                /// <param name="bytes">字节数组。</param>
                /// <param name="offset">字节数组中开始计算的偏移量。</param>
                /// <param name="length">要计算的字节数量。</param>
                /// <returns>计算后的哈希值。</returns>
                private static uint CalculateHash(uint[] table, uint value, byte[] bytes, int offset, int length)
                {
                    var last = offset + length;
                    for (var i = offset; i < last; i++)
                    {
                        unchecked
                        {
                            value = (value >> 8) ^ table[bytes[i] ^ value & 0xff];
                        }
                    }

                    return value;
                }

                /// <summary>
                /// 初始化 CRC32 查找表。
                /// </summary>
                /// <param name="polynomial">CRC32 多项式。</param>
                /// <returns>CRC32 查找表。</returns>
                private static uint[] InitializeTable(uint polynomial)
                {
                    var table = new uint[TableLength];
                    for (var i = 0; i < TableLength; i++)
                    {
                        var entry = (uint)i;
                        for (var j = 0; j < 8; j++)
                        {
                            if ((entry & 1) == 1)
                                entry = (entry >> 1) ^ polynomial;
                            else
                                entry >>= 1;
                        }

                        table[i] = entry;
                    }

                    return table;
                }
            }
        }
    }
}