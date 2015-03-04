using JFrog.Artifactory.Model;
using JFrog.Artifactory.Utils.regexCapturing;
using NuGet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JFrog.Artifactory.Utils
{
	public class BuildInfoExtractor
	{
		private const string validEmailPattern = "^[_A-Za-z0-9-]+(\\.[_A-Za-z0-9-]+)*@[A-Za-z0-9-]+(\\.[A-Za-z0-9-]+)*((\\.[A-Za-z]{2,}){1}$)";

		public static Build extractBuild(ArtifactoryBuild task, ArtifactoryConfig artifactoryConfig, MsBuildInfoLog log)
		{
			var build = new Build
			{
				modules = new List<Module>(),
			};

			build.started = string.Format(Build.STARTED_FORMAT, task.StartTime);
			build.artifactoryPrincipal = task.User;
			build.buildAgent = new BuildAgent { name = "MSBuild", version = task.ToolVersion };
			build.type = "MSBuild";

			build.agent = Agent.BuildAgentFactory(task);

			//get the current use from the windows OS
			System.Security.Principal.WindowsIdentity user;
			user = System.Security.Principal.WindowsIdentity.GetCurrent();
			if (user != null) build.principal = string.Format(@"{0}", user.Name);

			//Calculate how long it took to do the build
			var start = DateTime.ParseExact(task.StartTime, Build.ARTIFACTORY_DATE_FORMAT, null);
			build.startedDateMillis = GetTimeStamp();
			build.durationMillis = Convert.ToInt64((DateTime.Now - start).TotalMilliseconds);

			build.number = string.IsNullOrWhiteSpace(task.BuildNumber) ? build.startedDateMillis : task.BuildNumber;
			build.name = task.BuildName ?? task.ProjectName;
			build.url = build.agent.BuildAgentUrl();
			build.vcsRevision = task.VcsRevision;

			//Add build server properties, if exists.
			build.properties = AddSystemVariables(artifactoryConfig, build);
			build.licenseControl = AddLicenseControl(artifactoryConfig, log);

			ConfigHttpClient(artifactoryConfig, build);

			return build;
		}

		/// <summary>
		/// Read all referenced nuget's in the .csproj calculate their md5, sha1 and id.
		/// </summary>
		public static void ProcessModule(Build build, ProjectModel project, ArtifactoryBuild task)
		{
			var module = new Module(project.AssemblyName);

			var localSource = Path.Combine(task.SolutionRoot, "packages");
			// why recursively?
			var packageConfigPath = Directory.GetFiles(project.projectDirectory, "packages.config", SearchOption.AllDirectories).FirstOrDefault();

			if (project.artifactoryDeploy != null)
			{
				foreach (var deployAttribute in project.artifactoryDeploy)
				{
					var details = BuildArtifacts.resolve(deployAttribute, project.projectDirectory, task.DeploymentRepository);
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

						var artifactId = module.id + ":" + artifactName;
						task.deployableArtifactBuilderMap.AddOrSet(artifactId, artifactDetail);
					}
				}
			}
			module.AddNuGetDependencies(localSource, packageConfigPath, task.Configuration);
			build.modules.Add(module);
		}

		/// <summary>
		/// Gather all windows system variables and their values
		/// </summary>
		/// <returns></returns>
		private static Dictionary<string, string> AddSystemVariables(ArtifactoryConfig artifactoryConfig, Build build)
		{
			var enable = artifactoryConfig.PropertyGroup.EnvironmentVariables.EnabledEnvVariable;
			if (string.IsNullOrWhiteSpace(enable) || !enable.ToLower().Equals("true"))
				return new Dictionary<string, string>();

			// includePatterns = new List<Pattern>();
			//List<Pattern> excludePatterns = new List<Pattern>();
			var includePatterns = artifactoryConfig.PropertyGroup.EnvironmentVariables.IncludePatterns.Pattern;
			var excludePatterns = artifactoryConfig.PropertyGroup.EnvironmentVariables.ExcludePatterns.Pattern;

			var includeRegexUnion = new StringBuilder();
			var excludeRegexUnion = new StringBuilder();

			if (includePatterns != null && includePatterns.Count > 0)
			{
				includePatterns.ForEach(pattern => includeRegexUnion.Append(WildcardToRegex(pattern.key)).Append("|"));
				includeRegexUnion.Remove(includeRegexUnion.Length - 1, 1);
			}

			if (excludePatterns != null && excludePatterns.Count > 0)
			{
				excludePatterns.ForEach(pattern => excludeRegexUnion.Append(WildcardToRegex(pattern.key)).Append("|"));
				excludeRegexUnion.Remove(excludeRegexUnion.Length - 1, 1);
			}

			var includeRegex = new Regex(includeRegexUnion.ToString(), RegexOptions.IgnoreCase);
			var excludeRegex = new Regex(excludeRegexUnion.ToString(), RegexOptions.IgnoreCase);

			//System.Environment.GetEnvironmentVariables()
			//EnvironmentVariableTarget
			var sysVariables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);
			var dicVariables = new Dictionary<string, string>();

			foreach (string key in sysVariables.Keys)
			{
				if (!PathConflicts(includePatterns, excludePatterns, includeRegex, excludeRegex, key))
				{
					dicVariables.Add(key, (string)sysVariables[key]);
				}
			}

			dicVariables.AddRange(build.agent.BuildAgentEnvironment());

			return dicVariables;
		}

		private static LicenseControl AddLicenseControl(ArtifactoryConfig artifactoryConfig, MsBuildInfoLog log)
		{
			var licenseControl = new LicenseControl();

			licenseControl.runChecks = artifactoryConfig.PropertyGroup.LicenseControlCheck.EnabledLicenseControl;
			licenseControl.autoDiscover = artifactoryConfig.PropertyGroup.LicenseControlCheck.AutomaticLicenseDiscovery;
			licenseControl.includePublishedArtifacts = artifactoryConfig.PropertyGroup.LicenseControlCheck.IncludePublishedArtifacts;
			licenseControl.licenseViolationsRecipients = new List<string>();
			licenseControl.scopes = new List<string>();

			foreach (var recip in artifactoryConfig.PropertyGroup.LicenseControlCheck.LicenseViolationRecipients.Recipient)
			{
				if (validateEmail(recip))
				{
					licenseControl.licenseViolationsRecipients.Add(recip.email);
				}
				else
				{
					log.Warning("Invalid email address, under License Control violation recipients.");
				}
			}

			foreach (var scope in artifactoryConfig.PropertyGroup.LicenseControlCheck.ScopesForLicenseAnalysis.Scope)
			{
				licenseControl.scopes.Add(scope.value);
			}

			return licenseControl;
		}

		/// <summary>
		/// Get Timestamp sine 1/1/1970
		/// </summary>
		/// <returns>string double value</returns>
		private static string GetTimeStamp()
		{
			return DateTime.UtcNow.ToSecondsFromTheEpoch().ToString();
		}

		private static string WildcardToRegex(string pattern)
		{
			if (string.IsNullOrWhiteSpace(pattern))
				return pattern;

			return "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
		}

		private static bool PathConflicts(List<Pattern> includePatterns, List<Pattern> excludePatterns, Regex includeRegex, Regex excludeRegex, string key)
		{
			if ((includePatterns.Count > 0) && !includeRegex.Match(key).Success)
			{
				return true;
			}

			if ((excludePatterns.Count > 0) && excludeRegex.Match(key).Success)
			{
				return true;
			}

			return false;
		}

		private static bool validateEmail(Recipient recipient)
		{
			if (recipient == null && string.IsNullOrWhiteSpace(recipient.email))
				return false;

			var emailRegex = new Regex(validEmailPattern, RegexOptions.IgnoreCase);

			return emailRegex.Match(recipient.email).Success;
		}

		private static void ConfigHttpClient(ArtifactoryConfig artifactoryConfig, Build build)
		{
			build.deployClient = new DeployClient();
			if (!string.IsNullOrWhiteSpace(artifactoryConfig.PropertyGroup.ConnectionTimeout))
				build.deployClient.timeout = int.Parse(artifactoryConfig.PropertyGroup.ConnectionTimeout);

			var proxySettings = artifactoryConfig.PropertyGroup.ProxySettings;

			//Check if the user Bypass proxy settings
			if (!string.IsNullOrWhiteSpace(proxySettings.Bypass) && proxySettings.Bypass.ToLower().Equals("true"))
			{
				build.deployClient.proxy = new Proxy();
				build.deployClient.proxy.IsBypass = true;
				return;
			}

			/*
			* Incase that the proxy settings, is not set in the plugin level, we need
			* to check for proxy settings in the environment variables.
			*/
			if (string.IsNullOrWhiteSpace(proxySettings.Host))
			{
				var envVariableProxy = proxyEnvVariables();
				if (envVariableProxy != null)
				{
					proxySettings = envVariableProxy;
				}
				else
				{
					build.deployClient.proxy = new Proxy();
					build.deployClient.proxy.IsBypass = true;
					return;
				}
			}

			if (!string.IsNullOrWhiteSpace(proxySettings.UserName) && !string.IsNullOrWhiteSpace(proxySettings.Password))
				build.deployClient.proxy = new Proxy(proxySettings.Host, proxySettings.Port, proxySettings.UserName, proxySettings.Password);
			else
				build.deployClient.proxy = new Proxy(proxySettings.Host, proxySettings.Port);

			build.deployClient.proxy.IsBypass = false;
		}

		/// <summary>
		/// Trying to find proxy scope on the environment variables.
		/// </summary>
		/// <returns>Proxy instance</returns>
		private static ProxySettings proxyEnvVariables()
		{
			Uri uri;
			var httpHost = Environment.GetEnvironmentVariable("http_proxy");
			if (!string.IsNullOrWhiteSpace(httpHost) && Uri.TryCreate(httpHost, UriKind.Absolute, out uri))
			{
				var proxy = new ProxySettings();

				if (!String.IsNullOrEmpty(uri.UserInfo))
				{
					var credentials = uri.UserInfo.Split(':');
					if (credentials.Length > 1)
					{
						proxy.UserName = credentials[0];
						proxy.Password = credentials[1];
					}
				}

				//Regex for capturing the host and the port (if exists).
				var addressPattern = new Regex(@"^\w+://(?<host>[^/]+):(?<port>\d+)/?");
				var match = addressPattern.Match(uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.SafeUnescaped));
				if (match.Success)
				{
					if (!string.IsNullOrWhiteSpace(match.Groups["port"].Value))
						proxy.Port = int.Parse(match.Groups["port"].Value);

					proxy.Host = match.Groups["host"].Value;
				}

				return proxy;
			}

			return null;
		}
	}
}
