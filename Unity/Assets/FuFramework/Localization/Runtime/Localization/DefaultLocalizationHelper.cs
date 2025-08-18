using System;
using System.IO;
using System.Text;
using FuFramework.Core.Runtime;
using FuFramework.Asset.Runtime;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace FuFramework.Localization.Runtime
{
    /// <summary>
    /// 默认本地化辅助器。
    /// </summary>
    public class DefaultLocalizationHelper : LocalizationHelperBase
    {
        private static readonly string[] ColumnSplitSeparator = { "\t" };
        private static readonly string BytesAssetExtension = ".bytes";
        private const int ColumnCount = 4;

        private AssetComponent m_ResourceComponent;// 资源组件

        /// <summary>
        /// 获取系统语言。
        /// </summary>
        public override Language SystemLanguage
        {
            get
            {
                return Application.systemLanguage switch
                {
                    UnityEngine.SystemLanguage.Afrikaans => Language.Afrikaans,
                    UnityEngine.SystemLanguage.Arabic => Language.Arabic,
                    UnityEngine.SystemLanguage.Basque => Language.Basque,
                    UnityEngine.SystemLanguage.Belarusian => Language.Belarusian,
                    UnityEngine.SystemLanguage.Bulgarian => Language.Bulgarian,
                    UnityEngine.SystemLanguage.Catalan => Language.Catalan,
                    UnityEngine.SystemLanguage.Chinese => Language.ChineseSimplified,
                    UnityEngine.SystemLanguage.ChineseSimplified => Language.ChineseSimplified,
                    UnityEngine.SystemLanguage.ChineseTraditional => Language.ChineseTraditional,
                    UnityEngine.SystemLanguage.Czech => Language.Czech,
                    UnityEngine.SystemLanguage.Danish => Language.Danish,
                    UnityEngine.SystemLanguage.Dutch => Language.Dutch,
                    UnityEngine.SystemLanguage.English => Language.English,
                    UnityEngine.SystemLanguage.Estonian => Language.Estonian,
                    UnityEngine.SystemLanguage.Faroese => Language.Faroese,
                    UnityEngine.SystemLanguage.Finnish => Language.Finnish,
                    UnityEngine.SystemLanguage.French => Language.French,
                    UnityEngine.SystemLanguage.German => Language.German,
                    UnityEngine.SystemLanguage.Greek => Language.Greek,
                    UnityEngine.SystemLanguage.Hebrew => Language.Hebrew,
                    UnityEngine.SystemLanguage.Hungarian => Language.Hungarian,
                    UnityEngine.SystemLanguage.Icelandic => Language.Icelandic,
                    UnityEngine.SystemLanguage.Indonesian => Language.Indonesian,
                    UnityEngine.SystemLanguage.Italian => Language.Italian,
                    UnityEngine.SystemLanguage.Japanese => Language.Japanese,
                    UnityEngine.SystemLanguage.Korean => Language.Korean,
                    UnityEngine.SystemLanguage.Latvian => Language.Latvian,
                    UnityEngine.SystemLanguage.Lithuanian => Language.Lithuanian,
                    UnityEngine.SystemLanguage.Norwegian => Language.Norwegian,
                    UnityEngine.SystemLanguage.Polish => Language.Polish,
                    UnityEngine.SystemLanguage.Portuguese => Language.PortuguesePortugal,
                    UnityEngine.SystemLanguage.Romanian => Language.Romanian,
                    UnityEngine.SystemLanguage.Russian => Language.Russian,
                    UnityEngine.SystemLanguage.SerboCroatian => Language.SerboCroatian,
                    UnityEngine.SystemLanguage.Slovak => Language.Slovak,
                    UnityEngine.SystemLanguage.Slovenian => Language.Slovenian,
                    UnityEngine.SystemLanguage.Spanish => Language.Spanish,
                    UnityEngine.SystemLanguage.Swedish => Language.Swedish,
                    UnityEngine.SystemLanguage.Thai => Language.Thai,
                    UnityEngine.SystemLanguage.Turkish => Language.Turkish,
                    UnityEngine.SystemLanguage.Ukrainian => Language.Ukrainian,
                    UnityEngine.SystemLanguage.Unknown => Language.Unspecified,
                    UnityEngine.SystemLanguage.Vietnamese => Language.Vietnamese,
                    _ => Language.Unspecified
                };
            }
        }

        private void Start()
        {
            m_ResourceComponent = GameEntry.GetComponent<AssetComponent>();
            if (!m_ResourceComponent) Log.Fatal("Resource component is invalid.");
        }

        /// <summary>
        /// 读取字典。
        /// </summary>
        /// <param name="localizationManager">本地化管理器。</param>
        /// <param name="dictionaryAssetName">字典资源名称。</param>
        /// <param name="dictionaryAsset">字典资源。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>是否读取字典成功。</returns>
        public override bool ReadData(ILocalizationManager localizationManager, string dictionaryAssetName, object dictionaryAsset, object userData)
        {
            // TextAsset dictionaryTextAsset = dictionaryAsset as TextAsset;
            // if (dictionaryTextAsset != null)
            // {
            //     if (dictionaryAssetName.EndsWith(BytesAssetExtension, StringComparison.Ordinal))
            //     {
            //         return localizationManager.ParseData(dictionaryTextAsset.bytes, userData);
            //     }
            //     else
            //     {
            //         return localizationManager.ParseData(dictionaryTextAsset.text, userData);
            //     }
            // }
            //
            // Log.Warning("Dictionary asset '{0}' is invalid.", dictionaryAssetName);
            return false;
        }

        /// <summary>
        /// 读取字典。
        /// </summary>
        /// <param name="localizationManager">本地化管理器。</param>
        /// <param name="dictionaryAssetName">字典资源名称。</param>
        /// <param name="dictionaryBytes">字典二进制流。</param>
        /// <param name="startIndex">字典二进制流的起始位置。</param>
        /// <param name="length">字典二进制流的长度。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>是否读取字典成功。</returns>
        public override bool ReadData(ILocalizationManager localizationManager, string dictionaryAssetName, byte[] dictionaryBytes, int startIndex, int length, object userData)
        {
            return true;
            // if (dictionaryAssetName.EndsWith(BytesAssetExtension, StringComparison.Ordinal))
            // {
            //     return localizationManager.ParseData(dictionaryBytes, startIndex, length, userData);
            // }
            // else
            // {
            //     return localizationManager.ParseData(Utility.Converter.GetString(dictionaryBytes, startIndex, length), userData);
            // }
        }

        /// <summary>
        /// 解析字典。
        /// </summary>
        /// <param name="localizationManager">本地化管理器。</param>
        /// <param name="dictionaryString">要解析的字典字符串。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>是否解析字典成功。</returns>
        public override bool ParseData(ILocalizationManager localizationManager, string dictionaryString, object userData)
        {
            try
            {
                int position = 0;
                string dictionaryLineString = null;
                while ((dictionaryLineString = dictionaryString.ReadLine(ref position)) != null)
                {
                    if (dictionaryLineString[0] == '#')
                    {
                        continue;
                    }

                    string[] splitedLine = dictionaryLineString.Split(ColumnSplitSeparator, StringSplitOptions.None);
                    if (splitedLine.Length != ColumnCount)
                    {
                        Log.Warning("Can not parse dictionary line string '{0}' which column count is invalid.", dictionaryLineString);
                        return false;
                    }

                    string dictionaryKey = splitedLine[1];
                    string dictionaryValue = splitedLine[3];
                    if (!localizationManager.AddRawString(dictionaryKey, dictionaryValue))
                    {
                        Log.Warning("Can not add raw string with dictionary key '{0}' which may be invalid or duplicate.", dictionaryKey);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                Log.Warning("Can not parse dictionary string with exception '{0}'.", exception);
                return false;
            }
        }

        /// <summary>
        /// 解析字典。
        /// </summary>
        /// <param name="localizationManager">本地化管理器。</param>
        /// <param name="dictionaryBytes">要解析的字典二进制流。</param>
        /// <param name="startIndex">字典二进制流的起始位置。</param>
        /// <param name="length">字典二进制流的长度。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>是否解析字典成功。</returns>
        public override bool ParseData(ILocalizationManager localizationManager, byte[] dictionaryBytes, int startIndex, int length, object userData)
        {
            try
            {
                using (MemoryStream memoryStream = new MemoryStream(dictionaryBytes, startIndex, length, false))
                {
                    using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                    {
                        while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
                        {
                            string dictionaryKey = binaryReader.ReadString();
                            string dictionaryValue = binaryReader.ReadString();
                            if (!localizationManager.AddRawString(dictionaryKey, dictionaryValue))
                            {
                                Log.Warning("Can not add raw string with dictionary key '{0}' which may be invalid or duplicate.", dictionaryKey);
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                Log.Warning("Can not parse dictionary bytes with exception '{0}'.", exception);
                return false;
            }
        }

        /// <summary>
        /// 释放字典资源。
        /// </summary>
        /// <param name="localizationManager">本地化管理器。</param>
        /// <param name="dictionaryAsset">要释放的字典资源。</param>
        public override void ReleaseDataAsset(ILocalizationManager localizationManager, object dictionaryAsset)
        {
            // m_ResourceComponent.UnloadAsset(dictionaryAsset);
        }
    }
}