using System;

namespace JFrog.Artifactory.Utils
{
	public static class DateTimeExtensions
	{
		public static int ToSecondsFromTheEpoch(this DateTime time)
		{
			return ((Int32)(time.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
		}
	}
}
