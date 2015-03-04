﻿using System;
using JFrog.Artifactory.Model;
using JFrog.Artifactory.TFSActivities;
using JFrog.Artifactory.Utils;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace msbuild_tests
{
	[TestClass]
	public class TfsArtifactoryWrapperTests
	{
		public TestContext TestContext { get; set; }

		[TestMethod]
		public void TfsArtifactoryWrapperTests_()
		{
			var mBuildDefinition = new Mock<IBuildDefinition>();
			mBuildDefinition.Setup(m => m.Name).Returns("artifactory-tfs-ext-unittests");
			var mBuild = new Mock<IBuildDetail>();
			mBuild.Setup(m => m.BuildDefinition).Returns(mBuildDefinition.Object);
			mBuild.Setup(m => m.BuildNumber).Returns("111");
			mBuild.Setup(m => m.StartTime).Returns(DateTime.Now);
			mBuild.Setup(m => m.FinishTime).Returns(DateTime.Now.AddMinutes(1));
			mBuild.SetupGet(m => m.SourceGetVersion).Returns("30ed32b045734d2c390231ad3fc29dc2f3d261d7");
			var buildInfoLog = new Mock<IBuildInfoLog>().Object;
			var mAgent = new Mock<Agent>();
			mAgent.Object.name = "testAgent";
			mAgent.Object.version = "0.1";
			mAgent.Setup(m => m.BuildAgentUrl()).Returns("http://google.com/?q=unit%20testing");
			var agent = mAgent.Object;
			var sourcesDirectory = TestContext.TestDeploymentDir;
			var wrapper = new ArtifactoryWrapper(ArtifactoryTestConstants.SERVER, ArtifactoryTestConstants.USER, ArtifactoryTestConstants.PASSWORD,
				@"DeploymentItems\ArtifactoryWrapper", "TFSArtifactoryIntegration", ArtifactoryTestConstants.REPOSITORY, mBuild.Object, buildInfoLog, agent,
				sourcesDirectory, true);
			wrapper.Deploy();
		}
	}
}
