using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace FuFramework.SuperSocket.Primitives;

/// <summary>
/// Represents configuration options for loading an X.509 certificate.
/// </summary>
public class CertificateOptions
{
	/// <summary>
	/// Gets or sets the certificate file path (pfx).
	/// </summary>
	public string FilePath { get; set; }

	/// <summary>
	/// Gets or sets the password for the certificate file.
	/// </summary>
	public string Password { get; set; }

	/// <summary>
	/// Gets or sets the name of the store where the certificate is located.
	/// </summary>
	public string StoreName { get; set; } = "My";

	/// <summary>
	/// Gets or sets the thumbprint of the certificate.
	/// </summary>
	public string Thumbprint { get; set; }

	/// <summary>
	/// Gets or sets the store location of the certificate.
	/// </summary>
	public StoreLocation StoreLocation { get; set; } = StoreLocation.CurrentUser;

	/// <summary>
	/// Gets or sets the key storage flags used to instantiate the X509Certificate2 object.
	/// </summary>
	public X509KeyStorageFlags KeyStorageFlags { get; set; }

	/// <summary>
	/// Retrieves the X.509 certificate based on the specified options.
	/// </summary>
	/// <returns>The loaded <see cref="T:System.Security.Cryptography.X509Certificates.X509Certificate" />.</returns>
	/// <exception cref="T:System.Exception">Thrown if neither <see cref="P:FuFramework.SuperSocket.Primitives.CertificateOptions.FilePath" /> nor <see cref="P:FuFramework.SuperSocket.Primitives.CertificateOptions.Thumbprint" /> is provided.</exception>
	public X509Certificate GetCertificate()
	{
		if (!string.IsNullOrEmpty(FilePath))
		{
			string text = FilePath;
			if (!Path.IsPathRooted(text))
			{
				text = Path.Combine(AppContext.BaseDirectory, text);
			}
			return new X509Certificate2(text, Password, KeyStorageFlags);
		}
		if (!string.IsNullOrEmpty(Thumbprint))
		{
			using (X509Store x509Store = new X509Store((StoreName)Enum.Parse(typeof(StoreName), StoreName), StoreLocation))
			{
				x509Store.Open(OpenFlags.ReadOnly);
				return x509Store.Certificates.OfType<X509Certificate2>().FirstOrDefault((X509Certificate2 c) => c.Thumbprint.Equals(Thumbprint, StringComparison.OrdinalIgnoreCase));
			}
		}
		throw new Exception($"Either {FilePath} or {Thumbprint} is required to load the certificate.");
	}
}
