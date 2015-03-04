namespace msbuild_tests
{
	class ArtifactoryTestConstants
	{
		public static string SERVER { get { return "http://localhost:8080/artifactory"; } }

		public static string USER { get { return "artifactory"; } }

		public static string PASSWORD { get { return "admin"; } }

		public static string REPOSITORY { get { return "sandbox"; } }
	}
}
