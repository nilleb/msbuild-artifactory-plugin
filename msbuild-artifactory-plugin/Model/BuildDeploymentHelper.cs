﻿using JFrog.Artifactory.Utils;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JFrog.Artifactory.Model
{
    class BuildDeploymentHelper
    {
        public void deploy(ArtifactoryBuild task, Build build, MsBuildInfoLog log) 
        {
            ArtifactoryBuildInfoClient client = new ArtifactoryBuildInfoClient(task.Url, task.User, task.Password, log);
            client.setProxy(build.deployClient);
            client.setConnectionTimeout(build.deployClient);

            try
            {
                if (task.DeployEnabled != null && task.DeployEnabled.Equals("true"))
                {
                    /* Deploy every artifacts from the Map< module.name : artifact.name > => List<DeployDetails> */
                    task.deployableArtifactBuilderMap.ToList().ForEach(entry => entry.Value.ForEach(artifact => client.deployArtifact(artifact)));
                }

                if (task.BuildInfoEnabled != null && task.BuildInfoEnabled.Equals("true"))
                {
                    //Upload Build Info json file to Artifactory
                    log.Info("Uploading build info to Artifactory...");
                    /* Send Build Info  */
                    client.sendBuildInfo(build);
                }
            }
            catch (Exception e)
            {
                log.Error("Exception has append from ArtifactoryBuildInfoClient: " + e.Message, e);
                throw new Exception("Exception has append from ArtifactoryBuildInfoClient: " + e.Message, e);
            }
            finally 
            {
                client.Dispose();
            }         
        }
    }
}
