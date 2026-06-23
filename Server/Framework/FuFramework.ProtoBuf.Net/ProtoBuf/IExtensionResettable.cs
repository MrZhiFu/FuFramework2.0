namespace ProtoBuf;

/// <summary>
/// Provides the ability to remove all existing extension data
/// </summary>
public interface IExtensionResettable : IExtension
{
	/// <summary>
	/// Remove all existing extension data
	/// </summary>
	void Reset();
}
