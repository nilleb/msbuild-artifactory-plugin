using JFrog.Artifactory.Model;
using Microsoft.TeamFoundation.Build.Activities.Extensions;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Build.Workflow.Activities;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace tfs_artifactory_activities
{
	class NativeTFSAgent: Agent
	{
		private string _uri;
		private Dictionary<string, string> _variables;

		public NativeTFSAgent(CodeActivityContext context)
		{
			this._uri = context.GetBuildDetail().Uri.ToString();
			Dictionary<string, string> variables = populateVariables(context);
			this._variables = variables;
		}

		private static Dictionary<string, string> populateVariables(CodeActivityContext context)
		{
			Dictionary<string, string> variables = new Dictionary<string, string>();
			FieldInfo[] fieldInfos = typeof(WellKnownEnvironmentVariables).GetFields(BindingFlags.Public | BindingFlags.Static);
			var names = fieldInfos.Select(f => f.Name);

			foreach (var name in names)
			{
				var v = context.GetEnvironmentVariable(name);
				if (!string.IsNullOrEmpty(v))
					variables[name] = v;
			}
			return variables;
		}

		public override IDictionary<string, string> BuildAgentEnvironment()
		{
			return _variables;
		}

		public override string BuildAgentUrl()
		{
			return _uri;
		}
	}
}
