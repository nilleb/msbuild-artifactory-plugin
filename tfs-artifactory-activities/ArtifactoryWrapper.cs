using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using JFrog.Artifactory.Model;
using JFrog.Artifactory.Utils;
using JFrog.Artifactory.Utils.regexCapturing;
using Microsoft.TeamFoundation.Build.Client;

namespace JFrog.Artifactory.TFSActivities
{
	public class ArtifactoryWrapper
	{
		private readonly bool _doIndexBuildInfo;
		private readonly IBuildDetail _buildDetail;
		private readonly string _outputPattern;
		private readonly string _inputPattern;
		private readonly string _artifactoryUrl;
		private readonly string _username;
		private readonly string _password;
		private readonly string _targetRepository;
		private readonly string _sourcesDirectory;
		private readonly Agent _agent;
		private readonly IBuildInfoLog _log;
		private readonly string _moduleName;

		public ArtifactoryWrapper(string artifactoryUrl, string username, string password,
			string inputPattern, string outputPattern, string targetRepository,
			IBuildDetail buildDetail, IBuildInfoLog log, Agent agent, string sourcesDirectory,
			bool doIndexBuildInfo)
		{
			_moduleName = buildDetail.BuildDefinition.Name;
			_log = log;
			_agent = agent;
			_sourcesDirectory = sourcesDirectory;
			_targetRepository = targetRepository;
			_password = password;
			_username = username;
			_artifactoryUrl = artifactoryUrl;
			_inputPattern = inputPattern;
			_outputPattern = outputPattern;
			_buildDetail = buildDetail;
			_doIndexBuildInfo = doIndexBuildInfo;
		}

		public ArtifactoryWrapper(string moduleName, string artifactoryUrl, string username, string password,
			string inputPattern, string outputPattern, string targetRepository,
			IBuildDetail buildDetail, IBuildInfoLog log, Agent agent, string sourcesDirectory,
			bool doIndexBuildInfo)
		{
			_moduleName = moduleName;
			_log = log;
			_agent = agent;
			_sourcesDirectory = sourcesDirectory;
			_targetRepository = targetRepository;
			_password = password;
			_username = username;
			_artifactoryUrl = artifactoryUrl;
			_inputPattern = inputPattern;
			_outputPattern = outputPattern;
			_buildDetail = buildDetail;
			_doIndexBuildInfo = doIndexBuildInfo;
		}

		public void Deploy()
		{
			var client = new ArtifactoryBuildInfoClient(_artifactoryUrl, _username, _password, _log);

			var bi = forgeBuildInfo(_agent, _buildDetail);
			if (bi != null)
			{
				client.setConnectionTimeout(bi.deployClient);

				var artifacts =
					BuildArtifacts.resolve(
						new ProjectModel.DeployAttribute { InputPattern = _inputPattern, OutputPattern = _outputPattern },
						_sourcesDirectory,
						_targetRepository);

				completeModuleInfo(artifacts, bi);

				foreach (var artifact in artifacts)
					client.deployArtifact(artifact);

				if (artifacts.Any() && _doIndexBuildInfo)
					client.sendBuildInfo(bi);
			}
		}

		private void completeModuleInfo(List<DeployDetails> artifacts, Build bi)
		{
			var md = new ModuleDetails(_moduleName);
			md.AddDeployDetails(artifacts);
			bi.modules.Add(md.Module);
		}

		private Build forgeBuildInfo(Agent agent, IBuildDetail buildDetail)
		{
			Build bi = null;
			var windowsIdentity = WindowsIdentity.GetCurrent();
			if (windowsIdentity != null)
				bi = new Build
				{
					name = buildDetail.BuildDefinition.Name,
					number = buildDetail.BuildNumber,
					started = string.Format(CultureInfo.InvariantCulture, "{0:" + Build.ARTIFACTORY_DATE_FORMAT + "}", buildDetail.StartTime),
					startedDateMillis = buildDetail.StartTime.ToSecondsFromTheEpoch().ToString(),
					agent = agent,
					principal = windowsIdentity.User.Value,
					artifactoryPrincipal = _username,
					durationMillis = Convert.ToInt64((DateTime.Now - buildDetail.StartTime).TotalMilliseconds),
					buildAgent = new BuildAgent { name = "Generic", version = "Generic" },
					type = "TFS",
					buildRetention =
						new BuildRetention { count = -1, deleteBuildArtifacts = true, buildNumbersNotToBeDiscarded = new List<string>() },
					licenseControl =
						new LicenseControl
						{
							runChecks = "false",
							autoDiscover = "false",
							includePublishedArtifacts = "false",
							licenseViolationsRecipients = new List<string>(),
							scopes = new List<string>()
						},
					url = string.Format("{0}?url={1}", agent.BuildAgentUrl(), buildDetail.BuildServer.TeamProjectCollection.Uri.AbsoluteUri),
					vcsRevision = buildDetail.SourceGetVersion,
					modules = new List<Module>(),
					deployClient = new DeployClient { timeout = 0 }
				};
			return bi;
		}
	}
}