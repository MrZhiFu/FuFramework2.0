using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FuFramework.SuperSocket.ProtoBase;
using FuFramework.SuperSocket.Server.Abstractions;
using FuFramework.SuperSocket.Server.Abstractions.Middleware;
using FuFramework.SuperSocket.Server.Abstractions.Session;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FuFramework.SuperSocket.Command;

/// <summary>
/// Represents a middleware for handling commands in a SuperSocket application.
/// </summary>
/// <typeparam name="TKey">The type of the command key.</typeparam>
/// <typeparam name="TPackageInfo">The type of the package information.</typeparam>
public class CommandMiddleware<TKey, TPackageInfo> : CommandMiddleware<TKey, TPackageInfo, TPackageInfo> where TPackageInfo : class, IKeyedPackageInfo<TKey>
{
	private class TransparentMapper : IPackageMapper<TPackageInfo, TPackageInfo>
	{
		/// <summary>
		/// Maps a package to itself.
		/// </summary>
		/// <param name="package">The package to map.</param>
		/// <returns>The same package.</returns>
		public TPackageInfo Map(TPackageInfo package)
		{
			return package;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.Command.CommandMiddleware`2" /> class.
	/// </summary>
	/// <param name="serviceProvider">The service provider for dependency injection.</param>
	/// <param name="commandOptions">The options for configuring commands.</param>
	public CommandMiddleware(IServiceProvider serviceProvider, IOptions<CommandOptions> commandOptions)
		: base(serviceProvider, commandOptions)
	{
	}

	/// <summary>
	/// Creates a package mapper for mapping packages to commands.
	/// </summary>
	/// <param name="serviceProvider">The service provider for dependency injection.</param>
	/// <returns>An instance of <see cref="T:FuFramework.SuperSocket.Command.IPackageMapper`2" />.</returns>
	protected override IPackageMapper<TPackageInfo, TPackageInfo> CreatePackageMapper(IServiceProvider serviceProvider)
	{
		return new TransparentMapper();
	}
}
public class CommandMiddleware<TKey, TNetPackageInfo, TPackageInfo> : MiddlewareBase, IPackageHandler<TNetPackageInfo> where TNetPackageInfo : class where TPackageInfo : class, IKeyedPackageInfo<TKey>
{
	private interface ICommandSet
	{
		TKey Key { get; }

		ValueTask ExecuteAsync(IAppSession session, TPackageInfo package, CancellationToken cancellationToken);
	}

	private class CommandTypeInfo
	{
		public Type CommandType { get; private set; }

		public Type ActualCommandType { get; set; }

		public ICommand Command { get; private set; }

		public Type CommandSetFactoryType { get; private set; }

		public bool WrapRequired { get; set; }

		public Func<Type, Type> WrapFactory { get; set; }

		public CommandTypeInfo(ICommand command)
		{
			Command = command;
			CommandType = command.GetType();
		}

		public CommandTypeInfo(Type commandType, Type commandSetFactoryType)
			: this(commandType, commandSetFactoryType, wrapRequired: false)
		{
		}

		public CommandTypeInfo(Type commandType, Type commandSetFactoryType, bool wrapRequired)
		{
			CommandType = commandType;
			CommandSetFactoryType = commandSetFactoryType;
			WrapRequired = wrapRequired;
		}

		public ICommandSetFactory CreateCommandSetFactory(Type type)
		{
			CommandTypeInfo commandTypeInfo = new CommandTypeInfo(WrapRequired ? WrapFactory(type) : type, null);
			commandTypeInfo.ActualCommandType = type;
			return Activator.CreateInstance(CommandSetFactoryType, commandTypeInfo) as ICommandSetFactory;
		}
	}

	private interface ICommandSetFactory
	{
		ICommandSet Create(IServiceProvider serviceProvider, CommandOptions commandOptions);
	}

	private class CommandSetFactory<TAppSession> : ICommandSetFactory where TAppSession : IAppSession
	{
		public CommandTypeInfo CommandType { get; private set; }

		public CommandSetFactory(CommandTypeInfo commandType)
		{
			CommandType = commandType;
		}

		public ICommandSet Create(IServiceProvider serviceProvider, CommandOptions commandOptions)
		{
			CommandSet<TAppSession> commandSet = new CommandSet<TAppSession>();
			commandSet.Initialize(serviceProvider, CommandType, commandOptions);
			return commandSet;
		}
	}

	private class CommandSet<TAppSession> : ICommandSet where TAppSession : IAppSession
	{
		private readonly bool _isKeyString;

		public IAsyncCommand<TAppSession, TPackageInfo> AsyncCommand { get; private set; }

		public ICommand<TAppSession, TPackageInfo> Command { get; private set; }

		public IReadOnlyList<ICommandFilter> Filters { get; private set; }

		public CommandMetadata Metadata { get; private set; }

		public TKey Key { get; private set; }

		public CommandSet()
		{
			_isKeyString = typeof(TKey) == typeof(string);
		}

		private CommandMetadata GetCommandMetadata(Type commandType)
		{
			CommandAttribute commandAttribute = commandType.GetCustomAttribute(typeof(CommandAttribute)) as CommandAttribute;
			CommandMetadata commandMetadata = null;
			if (commandAttribute == null)
			{
				if (!_isKeyString)
				{
					throw new Exception("The command " + commandType.FullName + " needs a CommandAttribute defined.");
				}
				return new CommandMetadata(commandType.Name, commandType.Name);
			}
			string name = commandAttribute.Name;
			if (string.IsNullOrEmpty(name))
			{
				name = commandType.Name;
			}
			if (commandAttribute.Key == null)
			{
				if (!_isKeyString)
				{
					throw new Exception($"The command {commandType.FullName} needs a Key in type '{typeof(TKey).Name}' defined in its CommandAttribute.");
				}
				return new CommandMetadata(name, name);
			}
			return new CommandMetadata(name, commandAttribute.Key);
		}

		protected void SetCommand(ICommand command)
		{
			Command = command as ICommand<TAppSession, TPackageInfo>;
			AsyncCommand = command as IAsyncCommand<TAppSession, TPackageInfo>;
		}

		public void Initialize(IServiceProvider serviceProvider, CommandTypeInfo commandTypeInfo, CommandOptions commandOptions)
		{
			ICommand command = commandTypeInfo.Command;
			if (command == null)
			{
				command = ((!(commandTypeInfo.CommandType != commandTypeInfo.ActualCommandType)) ? (ActivatorUtilities.CreateInstance(serviceProvider, commandTypeInfo.CommandType) as ICommand) : (ActivatorUtilities.CreateFactory(commandTypeInfo.CommandType, new Type[1] { typeof(IServiceProvider) })(serviceProvider, new object[1] { serviceProvider }) as ICommand));
			}
			SetCommand(command);
			CommandMetadata commandMetadata = GetCommandMetadata(commandTypeInfo.ActualCommandType);
			try
			{
				Key = (TKey)commandMetadata.Key;
				Metadata = commandMetadata;
			}
			catch (Exception innerException)
			{
				throw new Exception($"The command {commandMetadata.Name}'s Key {commandMetadata.Key} cannot be converted to the desired type '{typeof(TKey).Name}'.", innerException);
			}
			List<ICommandFilter> list = new List<ICommandFilter>();
			if (commandOptions.GlobalCommandFilterTypes.Any())
			{
				list.AddRange(commandOptions.GlobalCommandFilterTypes.Select((Type t) => ActivatorUtilities.CreateInstance(serviceProvider, t) as CommandFilterBaseAttribute));
			}
			list.AddRange(commandTypeInfo.ActualCommandType.GetCustomAttributes(inherit: false).OfType<CommandFilterBaseAttribute>());
			Filters = list;
		}

		public async ValueTask ExecuteAsync(IAppSession session, TPackageInfo package, CancellationToken cancellationToken)
		{
			if (Filters.Count > 0)
			{
				await ExecuteAsyncWithFilter(session, package, cancellationToken);
				return;
			}
			TAppSession session2 = (TAppSession)session;
			IAsyncCommand<TAppSession, TPackageInfo> asyncCommand = AsyncCommand;
			if (asyncCommand != null)
			{
				await asyncCommand.ExecuteAsync(session2, package, cancellationToken);
			}
			else
			{
				Command.Execute(session2, package);
			}
		}

		private async ValueTask ExecuteAsyncWithFilter(IAppSession session, TPackageInfo package, CancellationToken cancellationToken)
		{
			CommandExecutingContext context = default(CommandExecutingContext);
			context.Package = package;
			context.Session = session;
			context.CancellationToken = cancellationToken;
			ICommand command2;
			if (AsyncCommand == null)
			{
				ICommand command = Command;
				command2 = command;
			}
			else
			{
				ICommand command = AsyncCommand;
				command2 = command;
			}
			ICommand command3 = command2;
			if (command3 is ICommandWrap commandWrap)
			{
				command3 = commandWrap.InnerCommand;
			}
			context.CurrentCommand = command3;
			IReadOnlyList<ICommandFilter> filters = Filters;
			bool flag = true;
			for (int i = 0; i < filters.Count; i++)
			{
				ICommandFilter commandFilter = filters[i];
				if (commandFilter is AsyncCommandFilterAttribute asyncCommandFilterAttribute)
				{
					flag = await asyncCommandFilterAttribute.OnCommandExecutingAsync(context);
				}
				else if (commandFilter is CommandFilterAttribute commandFilterAttribute)
				{
					flag = commandFilterAttribute.OnCommandExecuting(context);
				}
				if (!flag)
				{
					break;
				}
			}
			if (!flag)
			{
				return;
			}
			try
			{
				_ = 1;
				try
				{
					TAppSession session2 = (TAppSession)session;
					IAsyncCommand<TAppSession, TPackageInfo> asyncCommand = AsyncCommand;
					if (asyncCommand != null)
					{
						await asyncCommand.ExecuteAsync(session2, package, cancellationToken);
					}
					else
					{
						Command.Execute(session2, package);
					}
				}
				catch (Exception exception)
				{
					context.Exception = exception;
				}
			}
			finally
			{
				for (int j = 0; j < filters.Count; j++)
				{
					ICommandFilter commandFilter2 = filters[j];
					if (commandFilter2 is AsyncCommandFilterAttribute asyncCommandFilterAttribute2)
					{
						await asyncCommandFilterAttribute2.OnCommandExecutedAsync(context);
					}
					else if (commandFilter2 is CommandFilterAttribute commandFilterAttribute2)
					{
						commandFilterAttribute2.OnCommandExecuted(context);
					}
				}
			}
		}

		public override string ToString()
		{
			ICommand command = Command;
			if (command == null)
			{
				command = AsyncCommand;
			}
			return command?.GetType().ToString();
		}
	}

	private Dictionary<TKey, ICommandSet> _commands;

	private Func<IAppSession, TPackageInfo, CancellationToken, ValueTask> _unknownPackageHandler;

	private ILogger _logger;

	protected IPackageMapper<TNetPackageInfo, TPackageInfo> PackageMapper { get; private set; }

	public CommandMiddleware(IServiceProvider serviceProvider, IOptions<CommandOptions> commandOptions)
		: this(serviceProvider, commandOptions, (IPackageMapper<TNetPackageInfo, TPackageInfo>)null)
	{
	}

	public CommandMiddleware(IServiceProvider serviceProvider, IOptions<CommandOptions> commandOptions, IPackageMapper<TNetPackageInfo, TPackageInfo> packageMapper)
	{
		_logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger("CommandMiddleware");
		Type sessionType = serviceProvider.GetService<ISessionFactory>().SessionType;
		List<CommandTypeInfo> commandInterfaces = new List<CommandTypeInfo>();
		List<ICommandSetFactory> list = new List<ICommandSetFactory>();
		Type[] ignorePackageInterfaces = new Type[1] { typeof(IKeyedPackageInfo<TKey>) };
		List<Type> list2 = (from f in typeof(TPackageInfo).GetTypeInfo().GetInterfaces()
			where !ignorePackageInterfaces.Contains(f)
			select f).ToList();
		list2.Add(typeof(TPackageInfo));
		List<Type> list3 = new List<Type>
		{
			typeof(IAppSession),
			sessionType
		};
		Type type = sessionType;
		while (true)
		{
			Type baseType = type.BaseType;
			if (baseType == null || baseType == typeof(object))
			{
				break;
			}
			list3.Add(baseType);
			type = baseType;
		}
		(new Type[1])[0] = typeof(IKeyedPackageInfo<TKey>);
		foreach (Type item in list2)
		{
			foreach (Type item2 in list3)
			{
				RegisterCommandInterfaces(commandInterfaces, list, serviceProvider, item2, item, wrapRequired: true);
			}
		}
		list.AddRange(from t in commandOptions.Value.GetCommandTypes((Type t) => true).Select(delegate(Type t)
			{
				if (t.IsAbstract)
				{
					return (ICommandSetFactory)null;
				}
				for (int i = 0; i < commandInterfaces.Count; i++)
				{
					CommandTypeInfo commandTypeInfo = commandInterfaces[i];
					if (commandTypeInfo.CommandType.IsAssignableFrom(t))
					{
						return commandTypeInfo.CreateCommandSetFactory(t);
					}
				}
				return (ICommandSetFactory)null;
			})
			where t != null
			select t);
		IEnumerable<ICommandSet> enumerable = list.Select((ICommandSetFactory t) => t.Create(serviceProvider, commandOptions.Value));
		IEqualityComparer<TKey> service = serviceProvider.GetService<IEqualityComparer<TKey>>();
		Dictionary<TKey, ICommandSet> dictionary = ((service == null) ? new Dictionary<TKey, ICommandSet>() : new Dictionary<TKey, ICommandSet>(service));
		foreach (ICommandSet item3 in enumerable)
		{
			if (dictionary.ContainsKey(item3.Key))
			{
				string message = $"Duplicated command with Key {item3.Key} is found: {item3.ToString()}";
				_logger.LogError(message);
				throw new Exception(message);
			}
			dictionary.Add(item3.Key, item3);
			_logger.LogDebug($"The command with key {item3.Key} is registered: {item3.ToString()}");
		}
		_commands = dictionary;
		PackageMapper = ((packageMapper != null) ? packageMapper : CreatePackageMapper(serviceProvider));
		object unknownPackageHandler = commandOptions.Value.UnknownPackageHandler;
		if (unknownPackageHandler != null)
		{
			_unknownPackageHandler = unknownPackageHandler as Func<IAppSession, TPackageInfo, CancellationToken, ValueTask>;
			if (_unknownPackageHandler == null)
			{
				_logger.LogError("UnknownPackageHandler was registered with incorrectly. The expected typew is " + typeof(Func<IAppSession, TPackageInfo, ValueTask>).Name + ".");
			}
		}
	}

	private void RegisterCommandInterfaces(List<CommandTypeInfo> commandInterfaces, List<ICommandSetFactory> commandSetFactories, IServiceProvider serviceProvider, Type sessionType, Type packageType, bool wrapRequired = false)
	{
		Type[] typeArguments = new Type[2] { sessionType, packageType };
		typeof(ICommand<, >).GetTypeInfo().MakeGenericType(typeArguments);
		typeof(IAsyncCommand<, >).GetTypeInfo().MakeGenericType(typeArguments);
		Type commandSetFactoryType = typeof(CommandSetFactory<>).MakeGenericType(typeof(TKey), typeof(TNetPackageInfo), typeof(TPackageInfo), sessionType);
		CommandTypeInfo commandTypeInfo = new CommandTypeInfo(typeof(ICommand<, >).GetTypeInfo().MakeGenericType(typeArguments), commandSetFactoryType);
		CommandTypeInfo commandTypeInfo2 = new CommandTypeInfo(typeof(IAsyncCommand<, >).GetTypeInfo().MakeGenericType(typeArguments), commandSetFactoryType);
		commandInterfaces.Add(commandTypeInfo);
		commandInterfaces.Add(commandTypeInfo2);
		if (wrapRequired)
		{
			commandTypeInfo.WrapRequired = true;
			commandTypeInfo.WrapFactory = (Type t) => typeof(CommandWrap<, , , >).GetTypeInfo().MakeGenericType(sessionType, typeof(TPackageInfo), packageType, t);
			commandTypeInfo2.WrapRequired = true;
			commandTypeInfo2.WrapFactory = (Type t) => typeof(AsyncCommandWrap<, , , >).GetTypeInfo().MakeGenericType(sessionType, typeof(TPackageInfo), packageType, t);
		}
		RegisterCommandSetFactoriesFromServices(commandSetFactories, serviceProvider, commandTypeInfo.CommandType, commandSetFactoryType, commandTypeInfo.WrapFactory);
		RegisterCommandSetFactoriesFromServices(commandSetFactories, serviceProvider, commandTypeInfo2.CommandType, commandSetFactoryType, commandTypeInfo2.WrapFactory);
	}

	private void RegisterCommandSetFactoriesFromServices(List<ICommandSetFactory> commandSetFactories, IServiceProvider serviceProvider, Type commandType, Type commandSetFactoryType, Func<Type, Type> commandWrapFactory)
	{
		foreach (ICommand item in serviceProvider.GetServices(commandType).OfType<ICommand>())
		{
			ICommand command = item;
			Type type = command.GetType();
			if (commandWrapFactory != null)
			{
				command = Activator.CreateInstance(commandWrapFactory(item.GetType()), item) as ICommand;
			}
			CommandTypeInfo commandTypeInfo = new CommandTypeInfo(command);
			commandTypeInfo.ActualCommandType = type;
			commandSetFactories.Add(Activator.CreateInstance(commandSetFactoryType, commandTypeInfo) as ICommandSetFactory);
		}
	}

	protected virtual IPackageMapper<TNetPackageInfo, TPackageInfo> CreatePackageMapper(IServiceProvider serviceProvider)
	{
		return serviceProvider.GetService<IPackageMapper<TNetPackageInfo, TPackageInfo>>();
	}

	protected virtual async ValueTask HandlePackage(IAppSession session, TPackageInfo package, CancellationToken cancellationToken)
	{
		if (!_commands.TryGetValue(package.Key, out var value))
		{
			Func<IAppSession, TPackageInfo, CancellationToken, ValueTask> unknownPackageHandler = _unknownPackageHandler;
			if (unknownPackageHandler != null)
			{
				await unknownPackageHandler(session, package, cancellationToken);
			}
		}
		else
		{
			await value.ExecuteAsync(session, package, cancellationToken);
		}
	}

	protected virtual async Task OnPackageReceived(IAppSession session, TPackageInfo package, CancellationToken cancellationToken)
	{
		await HandlePackage(session, package, cancellationToken);
	}

	ValueTask IPackageHandler<TNetPackageInfo>.Handle(IAppSession session, TNetPackageInfo package, CancellationToken cancellationToken)
	{
		return HandlePackage(session, PackageMapper.Map(package), cancellationToken);
	}
}
