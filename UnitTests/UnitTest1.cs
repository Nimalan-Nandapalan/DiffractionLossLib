using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SRTM;

namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var srtmData = new SRTMData(@"C:\temp\srtm-cache");

            int expected = 136;

            // -33.47330 151.31728
            int? result = srtmData.GetElevation(-33.47330, 151.31728);

            Assert.AreEqual(expected, result);
        }
    }
}
