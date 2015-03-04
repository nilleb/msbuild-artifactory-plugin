using System.Collections.Generic;
using System.Linq;
using JFrog.Artifactory.Model;
using NuGet;

namespace JFrog.Artifactory.Utils
{
	public static class ModuleExtensions
	{
		///<summary>
		/// Using Nuget.Core API, we gather all nuget's packages that specific project depend on them.
		/// </summary>
		public static void AddNuGetDependencies(this Module module, string nugetPackagesCacheFolder, string packagesConfigFullPath, string scope)
		{
			if (!string.IsNullOrEmpty(packagesConfigFullPath))
			{
				var sharedPackages = new LocalPackageRepository(nugetPackagesCacheFolder);
				var packageReferenceFile = new PackageReferenceFile(packagesConfigFullPath);
				var projectPackages = packageReferenceFile.GetPackageReferences();

				foreach (var pack in projectPackages.Select(package => sharedPackages.FindPackage(package.Id, package.Version)))
				{
					using (var packageStream = ((OptimizedZipPackage)pack).GetStream())
					{
						var buf = new byte[packageStream.Length];
						packageStream.Read(buf, 0, buf.Length);

						module.Dependencies.Add(new Dependency
						{
							type = "nupkg",
							md5 = MD5CheckSum.GenerateMD5(buf),
							sha1 = Sha1Reference.GenerateSHA1(buf),
							scopes = new List<string> { scope },
							id = pack.Id + ":" + pack.Version
						});
					}
				}
			}
		}
	}
}
