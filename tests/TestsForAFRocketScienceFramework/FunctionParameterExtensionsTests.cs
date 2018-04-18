using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using Microsoft.Azure.Functions.AFRocketScience;

namespace Microsoft.Azure.Functions.AFRocketScienceTests
{

    [TestClass]
    public class FunctionParameterExtensionsTests
    {
        //------------------------------------------------------------------------------
        //  Helper to make httprequests
        //------------------------------------------------------------------------------
        HttpRequestMessage MakeHttpRequest(string urlParameters)
        {
            return new HttpRequestMessage(HttpMethod.Post, $"http://foo.bar.com/app?{urlParameters}");
        }

        public enum TestBlots
        {
            Foo = 1,
            Bar = 2
        }

        class HappyParameters : StandardRestQueryParameters
        {
            public string StringThing { get; set; }
            public int IntThing { get; set; }
            public long LongThing { get; set; }
            public double FloatThing { get; set; }
            public DateTime DateThing { get; set; }
            public DateTime DateThingUtc { get; set; }
            public TestBlots EnumThing { get; set; }

            public int[] ManyInts { get; set; }
        }

        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void ReadParameters_HappyPath_Works()
        {
            var request = MakeHttpRequest("stringTHING=  Bumper crop  "
                + "&intthing=22"
                + "&floatthing=3.3"
                + "&datething=2017/2/3 14:22:11"
                + "&EnumTHing=bAR"
                + "&manyints=  3,23,42, 99 ");
            var result = request.ReadParameters<HappyParameters>();

            AssertEx.AreEqual("Bumper crop", result.StringThing);
            AssertEx.AreEqual(22, result.IntThing);
            AssertEx.AreEqual(3.3, result.FloatThing);
            AssertEx.AreEqual(new DateTime(2017, 2, 3, 14, 22, 11, DateTimeKind.Local), result.DateThing);
            AssertEx.AreEqual(TestBlots.Bar, result.EnumThing);
            AssertEx.AreEqual(new int[] { 3, 23, 42, 99 }, result.ManyInts);
        }

        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void ReadParameters_Throws_WhenValueCantParse()
        {
            var request = MakeHttpRequest("intthing=blah");

            var result = Assert.ThrowsException<ServiceOperationException>(() => request.ReadParameters<HappyParameters>());
            AssertEx.AreEqual("Error on (Int32) property 'IntThing': Input string was not in a correct format.", result.Message);
            AssertEx.AreEqual(ServiceOperationError.BadParameter, result.ErrorCode);
        }

        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void ReadParameters_HandlesUtcDates()
        {
            var request = MakeHttpRequest("dateThingUtc=2017/2/3 14:22:11");
            var result = request.ReadParameters<HappyParameters>();
            AssertEx.AreEqual(new DateTime(2017, 2, 3, 14, 22, 11, DateTimeKind.Utc), result.DateThingUtc);
        }

        class HasRequired 
        {
            [FunctionParameterRequired]
            public string StringThing { get; set; }
        }

        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void ReadParameters_Throws_OnMissingRequiredParameters()
        {
            var request = MakeHttpRequest("");

            var result = Assert.ThrowsException<ServiceOperationException>(() => request.ReadParameters<HasRequired>());
            AssertEx.AreEqual("Missing required parameter 'StringThing'", result.Message);
            AssertEx.AreEqual(ServiceOperationError.BadParameter, result.ErrorCode);
        }

        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void ReadParameters_Throws_OnUnknownParameters()
        {
            var request = MakeHttpRequest("turtLE=blah&NotAParameter=1");

            var result = Assert.ThrowsException<ServiceOperationException>(() => request.ReadParameters<HappyParameters>());
            AssertEx.AreEqual("Unknown uri parameter 'turtLE'\r\nUnknown uri parameter 'NotAParameter'", result.Message);
            AssertEx.AreEqual(ServiceOperationError.BadParameter, result.ErrorCode);
        }

        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void ReadParameters_HandlesDollarParameters()
        {
            var request = MakeHttpRequest("__top=20&$skip=30");
            var result = request.ReadParameters<HappyParameters>();

            AssertEx.AreEqual(20, result.__Top);
            AssertEx.AreEqual(30, result.__Skip);
        }

        class ParamsInHeader
        {
            [FunctionParameterFromHeader(RemoveRequiredPrefix = "zorba:")]
            public double Prefixed { get; set; }

            [FunctionParameterFromHeader]
            public Guid Normal { get; set; }

            public string Bob { get; set; }
        }
        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void ReadParameters_HandlesHeaderParameters()
        {
            var testGuid = Guid.NewGuid();
            var request = MakeHttpRequest("bob=hi");
            request.Headers.Add("PREFIxed", "zorba:99.8");
            request.Headers.Add("NORMal", testGuid.ToString());
            var result = request.ReadParameters<ParamsInHeader>();

            AssertEx.AreEqual(testGuid, result.Normal);
            AssertEx.AreEqual(99.8, result.Prefixed);
            AssertEx.AreEqual("hi", result.Bob);
        }

    }
}
