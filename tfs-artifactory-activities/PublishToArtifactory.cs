using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using JFrog.Artifactory.Utils;
using JFrog.Artifactory.Model;
using Microsoft.TeamFoundation.Build.Workflow.Activities;
using Microsoft.TeamFoundation.Build.Client;
using System.Security.Principal;
using JFrog.Artifactory.Utils.regexCapturing;
using Microsoft.TeamFoundation.Build.Activities.Extensions;

namespace tfs_artifactory_activities
{
	public sealed class PublishToArtifactory : CodeActivity
	{
		public InArgument<string> ArtifactoryUrl { get; set; }
		public InArgument<string> Username { get; set; }
		public InArgument<string> Password { get; set; }
		public InArgument<string> InputPattern { get; set; }
		public InArgument<string> OutputPattern { get; set; }
		public InArgument<bool> DoIndexBuildInfo { get; set; }

		public InArgument<string> TargetRepository { get; set; }

		protected override void Execute(CodeActivityContext context)
		{
			string artifactoryUrl = context.GetValue(this.ArtifactoryUrl);
			string username = context.GetValue(this.Username);
			string password = context.GetValue(this.Password);
			string targetRepository = context.GetValue(this.TargetRepository);
			string inputPattern = context.GetValue(this.InputPattern);
			string outputPattern = context.GetValue(this.OutputPattern);

			IBuildInfoLog log = new TfsBuildInfoLog(context);
			var client = new ArtifactoryBuildInfoClient(artifactoryUrl, username, password, log);

			BuildArtifacts.resolve(new ProjectModel.DeployAttribute { InputPattern = inputPattern, OutputPattern = outputPattern }, context.GetSourcesDirectory(), targetRepository);

			bool doIndexBuildInfo = context.GetValue(this.DoIndexBuildInfo);
			if (doIndexBuildInfo)
			{
				var buildDetail = context.GetBuildDetail();
				var bi = forgeBuildInfo(new NativeTFSAgent(context), buildDetail);
				client.sendBuildInfo(bi);
			}
		}

		private static Build forgeBuildInfo(Agent agent, IBuildDetail buildDetail)
		{
			var bi = new Build();
			bi.name = buildDetail.BuildDefinition.Name;
			bi.number = buildDetail.BuildNumber;
			bi.agent = agent;
			bi.principal = WindowsIdentity.GetCurrent().ToString();
			bi.durationMillis = Convert.ToInt64((DateTime.Now - buildDetail.StartTime).TotalMilliseconds);
			// It's difficult to say which build tool is being used. It should be an option filled by the TFS Template.
			bi.buildAgent = new BuildAgent() { name = "Generic", version = "Generic" };
			bi.buildRetention = new BuildRetention() { count = -1, deleteBuildArtifacts = true, buildNumbersNotToBeDiscarded = new List<string>() };
			bi.licenseControl = new LicenseControl() { runChecks = "false", autoDiscover = "false", includePublishedArtifacts = "false", licenseViolationsRecipients = new List<string>(), scopes = new List<string>() };
			bi.modules = new List<Module>();
			return bi;
		}
	}
}
