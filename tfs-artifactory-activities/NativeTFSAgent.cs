using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JFrog.Artifactory.Model;
using Microsoft.TeamFoundation.Build.Activities.Extensions;

namespace JFrog.Artifactory.TFSActivities
{
	class NativeTfsAgent: Agent
	{
		private readonly string _uri;
		private readonly Dictionary<string, string> _variables;

		public NativeTfsAgent(CodeActivityContext context)
		{
			_uri = context.GetBuildDetail().Uri.ToString();
			_variables = populateVariables(context);
		}

		private static Dictionary<string, string> populateVariables(CodeActivityContext context)
		{
			var variables = new Dictionary<string, string>();
			var fieldInfos = typeof(WellKnownEnvironmentVariables).GetFields(BindingFlags.Public | BindingFlags.Static);
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
