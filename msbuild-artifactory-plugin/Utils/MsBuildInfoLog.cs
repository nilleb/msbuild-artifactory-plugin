using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace JFrog.Artifactory.Utils
{
    public class MsBuildInfoLog: IBuildInfoLog
    {
        private const string PRE_FIX = "[Artifactory] ";
        private TaskLoggingHelper _log { set; get; }

        public MsBuildInfoLog(TaskLoggingHelper taskLogger)
        {
            _log = taskLogger;
        }

        /*
         *  Verbosity: All 
         */
        public void Error(string message)
        {
            _log.LogError(PRE_FIX + message);
        }

        /*
         *  Verbosity: All
         */
        public void Error(string message, Exception ex)
        {
            Error(message);
            _log.LogErrorFromException(ex, true);
        }

        /*
         *  Verbosity: Detailed=>Diagnostic 
         */
        public void Debug(string message) 
        {
            _log.LogMessage(MessageImportance.Low, PRE_FIX + message);
        }

        
        /*
         *  Verbosity: Minimal=>Normal=>Detailed=>Diagnostic 
         */
        public void Info(string message)
        {
            _log.LogMessage(MessageImportance.High, PRE_FIX + message);
        }

        /*
         *  Verbosity: Minimal=>Normal=>Detailed=>Diagnostic 
         */
        public void Warning(string message)
        {
            _log.LogWarning(PRE_FIX + message);
        }
    }
}
