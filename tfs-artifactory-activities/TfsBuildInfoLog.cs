using JFrog.Artifactory.Utils;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.Workflow.Activities;
using Microsoft.TeamFoundation.Build.Client;

namespace JFrog.Artifactory.TFSActivities
{
	class TfsBuildInfoLog: IBuildInfoLog
	{
		CodeActivityContext _context;

		public TfsBuildInfoLog(CodeActivityContext context)
		{
			_context = context;
		}

		public void Info(string p)
		{
			_context.TrackBuildMessage(p, BuildMessageImportance.Normal);
		}

		public void Error(string p)
		{
			_context.TrackBuildError(p);
		}

		public void Error(string p, Exception we)
		{
			_context.TrackBuildError(p + "\r\nexception caught: " + we.ToString());
		}

		public void Debug(string p)
		{
			_context.TrackBuildMessage(p, BuildMessageImportance.Low);
		}
	}
}
