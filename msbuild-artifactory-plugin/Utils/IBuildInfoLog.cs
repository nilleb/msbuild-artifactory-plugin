using System;

namespace JFrog.Artifactory.Utils
{
	public interface IBuildInfoLog
	{
		void Info(string p);

		void Error(string p);

		void Error(string p, Exception we);

		void Debug(string p);
	}
}
