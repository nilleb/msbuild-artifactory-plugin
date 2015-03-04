using System.Activities;
using JFrog.Artifactory.Utils;

namespace JFrog.Artifactory.TFSActivities
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
			var artifactoryUrl = context.GetValue(this.ArtifactoryUrl);
			var username = context.GetValue(this.Username);
			var password = context.GetValue(this.Password);
			var targetRepository = context.GetValue(this.TargetRepository);
			var inputPattern = context.GetValue(this.InputPattern);
			var outputPattern = context.GetValue(this.OutputPattern);

			var buildDetail = context.GetBuildDetail();
			IBuildInfoLog log = new TfsBuildInfoLog(context);
			var nativeTfsAgent = new NativeTfsAgent(context);
			var sourcesDirectory = context.GetSourcesDirectory();
			var doIndexBuildInfo = context.GetValue(this.DoIndexBuildInfo);

			new ArtifactoryWrapper(artifactoryUrl, username, password, inputPattern, outputPattern, targetRepository, buildDetail, log, nativeTfsAgent, sourcesDirectory, doIndexBuildInfo).Deploy();
		}
	}
}
