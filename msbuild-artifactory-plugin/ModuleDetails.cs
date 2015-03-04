using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JFrog.Artifactory.Model;
using JFrog.Artifactory.Utils;
using JFrog.Artifactory.Utils.regexCapturing;

namespace JFrog.Artifactory
{
	public class ModuleDetails
	{
		public Module Module { get; private set; }

		private readonly List<DeployDetails> _deployDetails = new List<DeployDetails>();
		public IEnumerable<DeployDetails> DeployDetails { get { return _deployDetails; } }

		public ModuleDetails(Build build, ProjectModel project, string nugetPackagesCacheFolder, string targetRepository, string scope)
		{
			var module = new Module(project.AssemblyName);

			// why recursively?
			var packageConfigPath = Directory.GetFiles(project.projectDirectory, "packages.config", SearchOption.AllDirectories).FirstOrDefault();

			if (project.artifactoryDeploy != null)
			{
				foreach (var deployAttribute in project.artifactoryDeploy)
				{
					var details = BuildArtifacts.resolve(deployAttribute, project.projectDirectory, targetRepository);
					deployAttribute.properties.AddRange(build.getDefaultProperties());
					foreach (var artifactDetail in details)
					{
						//Add default artifact properties
						artifactDetail.properties = Build.buildMatrixParamsString(deployAttribute.properties);

						var artifactName = artifactDetail.file.Name;
						module.Artifacts.Add(new Artifact
						{
							type = artifactDetail.file.Extension.Replace(".", String.Empty),
							md5 = artifactDetail.md5,
							sha1 = artifactDetail.sha1,
							name = artifactName
						});
					}
					_deployDetails.AddRange(details);
				}
			}
			module.AddNuGetDependencies(nugetPackagesCacheFolder, packageConfigPath, scope);
			build.modules.Add(module);
			Module = module;
		}

		public ModuleDetails(Build build, string name, IEnumerable<DeployDetails> deployDetails)
		{
			var module = new Module(name);
			AddDeployDetails(deployDetails);
			build.modules.Add(module);
			Module = module;
		}

		public ModuleDetails(string name)
		{
			Module = new Module(name);
		}

		public void AddDeployDetails(IEnumerable<DeployDetails> deployDetails)
		{
			var dd = deployDetails.ToList();
			foreach (var artifactDetail in dd)
			{
				var artifactName = artifactDetail.file.Name;
				Module.Artifacts.Add(new Artifact
				{
					type = artifactDetail.file.Extension.Replace(".", String.Empty),
					md5 = artifactDetail.md5,
					sha1 = artifactDetail.sha1,
					name = artifactName
				});
			}
			_deployDetails.AddRange(dd);
		}
	}
}
