using System;
using System.Activities;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
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

		public ArtifactoryWrapper(string artifactoryUrl, string username, string password,
			string inputPattern, string outputPattern, string targetRepository,
			IBuildDetail buildDetail, IBuildInfoLog log, Agent agent, string sourcesDirectory,
			bool doIndexBuildInfo)
		{
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
			wrap(_artifactoryUrl, _username, _password, _inputPattern, _outputPattern, _targetRepository, _buildDetail, _log, _agent, _sourcesDirectory, _doIndexBuildInfo);
		}

		private void wrap(string artifactoryUrl, string username, string password,
			string inputPattern, string outputPattern, string targetRepository,
			IBuildDetail buildDetail, IBuildInfoLog log, Agent agent, string sourcesDirectory,
			bool doIndexBuildInfo)
		{
			var client = new ArtifactoryBuildInfoClient(artifactoryUrl, username, password, log);

			CodeActivityContext context;
			var bi = forgeBuildInfo(agent, buildDetail);
			if (bi != null)
			{
				client.setConnectionTimeout(bi.deployClient);

				var artifacts =
					BuildArtifacts.resolve(
						new ProjectModel.DeployAttribute { InputPattern = inputPattern, OutputPattern = outputPattern },
						sourcesDirectory,
						targetRepository);

				foreach (var artifact in artifacts)
					client.deployArtifact(artifact);

				if (artifacts.Any() && doIndexBuildInfo)
					client.sendBuildInfo(bi);
			}
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
					url = agent.BuildAgentUrl(),
					vcsRevision = buildDetail.SourceGetVersion,
					modules = new List<Module>(),
					deployClient = new DeployClient { timeout = 0 }
				};
			return bi;
		}
	}
}