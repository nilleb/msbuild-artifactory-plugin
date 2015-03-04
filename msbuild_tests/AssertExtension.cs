﻿using System;
using NUnit.Framework;

namespace msbuild_tests
{
    public static class AssertExtension
    {
        public static T Throws<T>(Action expressionUnderTest,
                                  string exceptionMessage = "Expected exception has not been thrown by target of invocation."
                                 ) where T : Exception
        {
            try
            {
                expressionUnderTest();
            }
            catch (T exception)
            {
                return exception;
            }

            Assert.Fail(exceptionMessage);
            return null;
        }
    }
}
