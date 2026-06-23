using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FuFramework.Foundation.Logger;
using FuFramework.Utility.Extensions;

namespace FuFramework.Utility;

/// <summary>
/// 此算法思想来源于“http://www.cnblogs.com/sumtec/archive/2008/02/01/1061742.html”,经测试，检测"屄defg东正教dsa SofU  ckd臺灣青年獨"这个字符串并替换掉敏感词平均花费2.7ms
/// </summary>
public sealed class IllegalWordDetection
{
	/// <summary>
	/// 存了所有的长度大于1的敏感词汇
	/// </summary>
	private static readonly HashSet<string> WordsSet = new HashSet<string>();

	/// <summary>
	/// 存了某一个词在所有敏感词中的位置，（超出8个的截断为第8个位置）
	/// </summary>
	private static readonly byte[] FastCheck = new byte[65535];

	/// <summary>
	/// 存了所有敏感词的长度信息，“Key”值为所有敏感词的第一个词，敏感词的长度会截断为8
	/// </summary>
	private static readonly byte[] FastLength = new byte[65535];

	/// <summary>
	/// 保有所有敏感词汇的第一个词的记录，可用来判断是否一个词是一个或者多个敏感词汇的“第一个词”，且可判断以某一个词作为第一个词的一系列的敏感词的最大的长度
	/// </summary>
	private static readonly byte[] StartCache = new byte[65535];

	private static char[] _dectectedBuffer;

	/// <summary>
	/// 忽略的敏感词
	/// </summary>
	private static readonly string SkipList = " \t\r\n~!@#$%^&*()_+-=【】、{}|;':\"，。、《》？αβγδεζηθικλμνξοπρστυφχψωΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩ。，、；：？！…—·ˉ\u00a8‘’“”々～‖∶＂＇\uff40｜〃〔〕〈〉《》「」『』．〖〗【】（）［］｛｝ⅠⅡⅢⅣⅤⅥⅦⅧⅨⅩⅪⅫ⒈⒉⒊⒋⒌⒍⒎⒏⒐⒑⒒⒓⒔⒕⒖⒗⒘⒙⒚⒛㈠㈡㈢㈣㈤㈥㈦㈧㈨㈩①②③④⑤⑥⑦⑧⑨⑩⑴⑵⑶⑷⑸⑹⑺⑻⑼⑽⑾⑿⒀⒁⒂⒃⒄⒅⒆⒇≈≡≠＝≤≥＜＞≮≯∷±＋－×÷／∫∮∝∞∧∨∑∏∪∩∈∵∴⊥∥∠⌒⊙≌∽√§№☆★○●◎◇◆□℃‰€■△▲※→←↑↓〓¤°＃＆＠＼︿\uff3f\uffe3―♂♀┌┍┎┐┑┒┓─┄┈├┝┞┟┠┡┢┣│┆┊┬┭┮┯┰┱┲┳┼┽┾┿╀╁╂╃└┕┖┗┘┙┚┛━┅┉┤┥┦┧┨┩┪┫┃┇┋┴┵┶┷┸┹┺┻╋╊╉╈╇╆╅╄";

	private static readonly BitArray SkipBitArray = new BitArray(65535);

	/// <summary>
	/// 保有所有敏感词汇的最后一个词的记录，仅用来判断是否一个词是一个或者多个敏感词汇的“最后一个词”
	/// </summary>
	private static readonly BitArray EndCache = new BitArray(65535);

	/// <summary>
	/// 通过配置表初始化
	/// </summary>
	/// <param name="badData">配置表数据</param>
	/// <param name="badIdx">字段idx，字段类型必须是string（从0开始）</param>
	/// <param name="backThread">是否新开线程执行</param>
	public static void Init(byte[] badData, int badIdx = 1, bool backThread = true)
	{
		if (backThread)
		{
			Task.Run(delegate
			{
				try
				{
					InnerInitBytes(badData, badIdx);
				}
				catch (Exception ex)
				{
					LogHelper.Error(ex.ToString());
				}
			});
			return;
		}
		try
		{
			InnerInitBytes(badData, badIdx);
		}
		catch (Exception ex2)
		{
			LogHelper.Error(ex2.ToString());
		}
	}

	/// <summary>
	/// 初始化敏感词
	/// </summary>
	/// <param name="badWords">敏感词列表</param>
	/// <param name="backThread">是否新开线程执行</param>
	public static void Init(string[] badWords, bool backThread = true)
	{
		if (backThread)
		{
			Task.Run(delegate
			{
				try
				{
					InnerInit(badWords);
				}
				catch (Exception ex)
				{
					LogHelper.Error(ex.ToString());
				}
			});
			return;
		}
		try
		{
			InnerInit(badWords);
		}
		catch (Exception ex2)
		{
			LogHelper.Error(ex2.ToString());
		}
	}

	private unsafe static void InnerInitBytes(byte[] data, int badIdx = 1)
	{
		if (data == null || data.Length < 4)
		{
			return;
		}
		DateTime now = DateTime.Now;
		int offset = 0;
		int num = data.ReadInt(ref offset);
		List<byte> list = new List<byte>();
		for (int i = 0; i < num; i++)
		{
			list.Add(data.ReadByte(ref offset));
		}
		List<string> list2 = new List<string>();
		for (int j = 0; j < num; j++)
		{
			list2.Add(data.ReadString(ref offset));
		}
		int num2 = 4;
		int num3 = 8;
		int num4 = 4;
		int num5 = 0;
		int num6 = int.MinValue;
		while (data.Length > offset)
		{
			string text = "";
			for (int k = 0; k < num; k++)
			{
				switch (list[k])
				{
				case 0:
					offset += num2;
					break;
				case 1:
					offset += num3;
					break;
				case 2:
				{
					if (badIdx == k)
					{
						text = data.ReadString(ref offset);
						break;
					}
					short num7 = data.ReadShort(ref offset);
					offset += num7;
					break;
				}
				case 3:
					offset += num4;
					break;
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			string text2 = OriginalToLower(text);
			int length = text2.Length;
			num6 = System.Math.Max(length, num6);
			fixed (char* ptr = text2)
			{
				for (int l = 0; l < length; l++)
				{
					if (l < 7)
					{
						FastCheck[(uint)ptr[l]] |= (byte)(1 << l);
					}
					else
					{
						FastCheck[(uint)ptr[l]] |= 128;
					}
				}
				int num8 = System.Math.Min(8, length);
				char c = *ptr;
				FastLength[(uint)c] |= (byte)(1 << num8 - 1);
				if (StartCache[(uint)c] < num8)
				{
					StartCache[(uint)c] = (byte)num8;
				}
				EndCache[*(ptr + length - 1)] = true;
				if (WordsSet.Add(text2))
				{
					num5++;
				}
			}
		}
		_dectectedBuffer = new char[num6];
		fixed (char* skipList = SkipList)
		{
			char* ptr2;
			char* ptr3 = (ptr2 = skipList) + SkipList.Length;
			while (ptr2 < ptr3)
			{
				SkipBitArray[*(ptr2++)] = true;
			}
		}
		LogHelper.Info($"敏感词初始化耗时:{(DateTime.Now - now).TotalMilliseconds}ms, 有效数量:{num5}");
	}

	private unsafe static void InnerInit(string[] badwords)
	{
		if (badwords == null || badwords.Length == 0)
		{
			return;
		}
		DateTime now = DateTime.Now;
		int num = 0;
		int num2 = 0;
		int num3 = int.MinValue;
		int i = 0;
		for (int num4 = badwords.Length; i < num4; i++)
		{
			if (string.IsNullOrEmpty(badwords[i]))
			{
				continue;
			}
			string text = OriginalToLower(badwords[i]);
			num2 = text.Length;
			num3 = System.Math.Max(num2, num3);
			fixed (char* ptr = text)
			{
				for (int j = 0; j < num2; j++)
				{
					if (j < 7)
					{
						FastCheck[(uint)ptr[j]] |= (byte)(1 << j);
					}
					else
					{
						FastCheck[(uint)ptr[j]] |= 128;
					}
				}
				int num5 = System.Math.Min(8, num2);
				char c = *ptr;
				FastLength[(uint)c] |= (byte)(1 << num5 - 1);
				if (StartCache[(uint)c] < num5)
				{
					StartCache[(uint)c] = (byte)num5;
				}
				EndCache[*(ptr + num2 - 1)] = true;
				if (!WordsSet.Contains(text))
				{
					WordsSet.Add(text);
					num++;
				}
			}
		}
		_dectectedBuffer = new char[num3];
		fixed (char* skipList = SkipList)
		{
			char* ptr2;
			char* ptr3 = (ptr2 = skipList) + SkipList.Length;
			while (ptr2 < ptr3)
			{
				SkipBitArray[*(ptr2++)] = true;
			}
		}
		LogHelper.Info($"敏感词初始化耗时:{(DateTime.Now - now).TotalMilliseconds}ms, 有效数量:{num}");
	}

	private unsafe static string OriginalToLower(string text)
	{
		fixed (char* ptr = text)
		{
			char* ptr2;
			for (char* ptr3 = (ptr2 = ptr) + text.Length; ptr2 < ptr3; ptr2++)
			{
				char c = *ptr2;
				if (c >= 'A' && c <= 'Z')
				{
					*ptr2 = (char)(c | 0x20);
				}
			}
		}
		return text;
	}

	private unsafe static bool EnsuranceLower(string text)
	{
		fixed (char* ptr = text)
		{
			char* ptr2;
			for (char* ptr3 = (ptr2 = ptr) + text.Length; ptr2 < ptr3; ptr2++)
			{
				char c = *ptr2;
				if (c >= 'A' && c <= 'Z')
				{
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>
	/// 过滤字符串,默认遇到敏感词汇就以'*'代替
	/// </summary>
	/// <param name="text">要查询的明感词文本</param>
	/// <param name="mask">替换目标字符</param>
	/// <returns>返回过滤后的文本</returns>
	public static string Filter(string text, char mask = '*')
	{
		DetectIllegalWords(text, returnWhenFindFirst: false, out var findResult);
		if (findResult.Count == 0)
		{
			return text;
		}
		StringBuilder stringBuilder = new StringBuilder(text);
		foreach (KeyValuePair<int, int> item in findResult)
		{
			int num = item.Value + item.Key;
			for (int i = item.Key; i < num; i++)
			{
				stringBuilder[i] = mask;
			}
		}
		return stringBuilder.ToString();
	}

	/// <summary>
	/// 判断text是否有敏感词汇
	/// </summary>
	/// <param name="text"></param>
	/// <returns></returns>
	public static bool HasBlockWords(string text)
	{
		Dictionary<int, int> findResult;
		return DetectIllegalWords(text, returnWhenFindFirst: true, out findResult);
	}

	/// <summary>
	/// 判断text是否有敏感词汇,如果有返回敏感的词汇的位置,利用指针操作来加快运算速度
	/// </summary>
	/// <param name="text">敏感词查询文本</param>
	/// <param name="returnWhenFindFirst">是否返回找到的第一个</param>
	/// <param name="findResult">查找到的敏感词结果</param>
	/// <returns>是否有敏感词汇</returns>
	public unsafe static bool DetectIllegalWords(string text, bool returnWhenFindFirst, out Dictionary<int, int> findResult)
	{
		findResult = new Dictionary<int, int>();
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (EnsuranceLower(text))
		{
			text = text.ToLower();
		}
		int num = _dectectedBuffer.Length;
		if (text.Length > num)
		{
			_dectectedBuffer = new char[num << 1];
		}
		fixed (char* ptr2 = text)
		{
			char[] dectectedBuffer = _dectectedBuffer;
			fixed (char* arrayPtr = dectectedBuffer)
			{
				char* ptr = (char*)((dectectedBuffer != null && dectectedBuffer.Length != 0) ? Unsafe.AsPointer(ref dectectedBuffer[0]) : null);
				char* ptr3 = (((FastCheck[(uint)(*ptr2)] & 1) == 0) ? (ptr2 + 1) : ptr2);
				for (char* ptr4 = ptr2 + text.Length; ptr3 < ptr4; ptr3++)
				{
					if ((FastCheck[(uint)(*ptr3)] & 1) == 0)
					{
						while (ptr3 < ptr4 - 1 && (FastCheck[(uint)(*(++ptr3))] & 1) == 0)
						{
						}
					}
					if (StartCache[(uint)(*ptr3)] != 0 && (FastLength[(uint)(*ptr3)] & 1) > 0)
					{
						findResult.Add((int)(ptr3 - ptr2), 1);
						if (returnWhenFindFirst)
						{
							return true;
						}
					}
					char* ptr5 = ptr;
					*(ptr5++) = *ptr3;
					int num2 = (int)(ptr4 - ptr3 - 1);
					int num3 = 0;
					for (int i = 1; i <= num2; i++)
					{
						char* ptr6 = ptr3 + i;
						if (SkipBitArray[*ptr6])
						{
							num3++;
							continue;
						}
						if (FastCheck[(uint)(*ptr6)] >> System.Math.Min(i - num3, 7) == 0)
						{
							break;
						}
						*(ptr5++) = *ptr6;
						if (FastLength[(uint)(*ptr3)] >> System.Math.Min(i - 1 - num3, 7) > 0 && EndCache[*ptr6])
						{
							if (WordsSet.Contains(new string(_dectectedBuffer, 0, (int)(ptr5 - ptr))))
							{
								int key = (int)(ptr3 - ptr2);
								findResult[key] = i + 1;
								ptr3 = ptr6;
								if (!returnWhenFindFirst)
								{
									break;
								}
								return true;
							}
						}
						else if (i - num3 > StartCache[(uint)(*ptr3)] && StartCache[(uint)(*ptr3)] < 128)
						{
							break;
						}
					}
				}
			}
		}
		return false;
	}
}
