namespace FuFramework.SuperSocket.Command;

public interface IPackageMapper<PackageFrom, PackageTo>
{
	PackageTo Map(PackageFrom package);
}
