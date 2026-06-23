// FuFramework 组织下的以及组织衍生的项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
// 
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
// 
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

global using System;
global using System.Threading.Tasks;
global using MongoDB.Bson;
global using MongoDB.Driver;
global using MongoDB.Bson.Serialization;
global using MongoDB.Bson.Serialization.Attributes;
global using FuFramework.Core;
global using FuFramework.Core.Utility;
global using FuFramework.Core.Components;
global using FuFramework.Core.Events;
global using FuFramework.Core.Hotfix.Agent;
global using FuFramework.Core.Hotfix;
global using FuFramework.Core.Actors;
global using FuFramework.Core.Timer;
global using FuFramework.Core.Abstractions.Attribute;
global using FuFramework.Proto.Proto;
global using FuFramework.DataBase;
global using FuFramework.DataBase.Mongo;
global using FuFramework.DataBase.Abstractions;
global using FuFramework.Utility.Extensions;
global using FuFramework.Utility.Setting;
global using FuFramework.Apps.Common.Event;
global using FuFramework.Utility;
global using System.Collections;
global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.Reflection;
global using MongoDB.Bson.Serialization.Conventions;
global using MongoDB.Bson.Serialization.Options;
global using System.Text.Json.Serialization;
global using FuFramework.NetWork.Abstractions;
global using FuFramework.NetWork.Messages;