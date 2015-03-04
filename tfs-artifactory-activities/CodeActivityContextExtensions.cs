using System;
using System.Activities;
using Microsoft.TeamFoundation.Build.Activities.Extensions;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow.Activities;

namespace JFrog.Artifactory.TFSActivities
{
	static class CodeActivityContextExtensions
	{
		public static string GetEnvironmentVariable(this CodeActivityContext context, string name)
		{
			IEnvironmentVariableExtension extension = context.GetExtension<IEnvironmentVariableExtension>();
			return extension.GetEnvironmentVariable<String>(context, name);
		}

		public static IBuildDetail GetBuildDetail(this CodeActivityContext context)
		{
			return context.GetExtension<IBuildDetail>();
		}

		public static string GetSourcesDirectory(this CodeActivityContext context)
		{
			return context.GetEnvironmentVariable(WellKnownEnvironmentVariables.SourcesDirectory);
		}
	}
}
