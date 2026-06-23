using System.Buffers;

namespace FuFramework.SuperSocket.ProtoBase.ProxyProtocol;

/// <summary>
/// A pipeline filter for handling proxy protocol packages.
/// </summary>
/// <typeparam name="TPackageInfo">The type of the package information.</typeparam>
public class ProxyProtocolPipelineFilter<TPackageInfo> : PackagePartsPipelineFilter<TPackageInfo>, IProxyProtocolPipelineFilter, IPipelineFilter
{
	/// <summary>
	/// The application-specific pipeline filter to switch to after processing the proxy protocol.
	/// </summary>
	private readonly IPipelineFilter<TPackageInfo> _applicationPipelineFilter;

	/// <summary>
	/// The original filter context before processing the proxy protocol.
	/// </summary>
	private object _originalFilterContext;

	/// <summary>
	/// Gets the proxy information associated with the connection.
	/// </summary>
	public ProxyInfo ProxyInfo { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="T:FuFramework.SuperSocket.ProtoBase.ProxyProtocol.ProxyProtocolPipelineFilter`1" /> class.
	/// </summary>
	/// <param name="applicationPipelineFilter">The application-specific pipeline filter to switch to after processing the proxy protocol.</param>
	public ProxyProtocolPipelineFilter(IPipelineFilter<TPackageInfo> applicationPipelineFilter)
	{
		_applicationPipelineFilter = applicationPipelineFilter;
	}

	/// <summary>
	/// Creates a new package instance.
	/// </summary>
	/// <returns>The created package.</returns>
	protected override TPackageInfo CreatePackage()
	{
		return default(TPackageInfo);
	}

	/// <summary>
	/// Gets the first part reader for processing the proxy protocol package.
	/// </summary>
	/// <returns>The first part reader.</returns>
	protected override IPackagePartReader<TPackageInfo> GetFirstPartReader()
	{
		return ProxyProtocolPackagePartReader<TPackageInfo>.ProxyProtocolSwitch;
	}

	/// <summary>
	/// Resets the state of the pipeline filter.
	/// </summary>
	public override void Reset()
	{
		ProxyInfo.Prepare();
		base.NextFilter = _applicationPipelineFilter;
		base.Reset();
		base.Context = _originalFilterContext;
	}

	/// <summary>
	/// Filters the data and processes the proxy protocol package.
	/// </summary>
	/// <param name="reader">The sequence reader containing the data.</param>
	/// <returns>The processed package, or <c>null</c> if more data is needed.</returns>
	public override TPackageInfo Filter(ref SequenceReader<byte> reader)
	{
		if (ProxyInfo == null)
		{
			_originalFilterContext = base.Context;
			ProxyInfo context = (ProxyInfo = new ProxyInfo());
			base.Context = context;
		}
		return base.Filter(ref reader);
	}
}
