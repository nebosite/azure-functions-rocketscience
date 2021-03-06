﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using Microsoft.Azure.Functions.AFRocketScience;
using Swagger.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.AFRocketScienceTests
{

    [ExcludeFromCodeCoverage]
    [TestClass]
    public class VehicleTests
    {
        //------------------------------------------------------------------------------
        //  Helper to make httprequests
        //------------------------------------------------------------------------------
        IRocketScienceRequest MakeRequest(string urlParameters, string bodyJson = null, string[][] headers = null)
        {
            var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"http://foo.bar.com/app?{urlParameters}");
            if(bodyJson != null)
            {
                request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
            }
            if(headers != null)
            {
                foreach(var headerParts in headers)
                {
                    request.Headers.Add(headerParts[0], headerParts[1]);
                }
            }
            return new RSHttpRequestMessage(request);
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
            var request = MakeRequest("stringTHING=  Bumper crop  "
                + "&intthing=22"
                + "&floatthing=3.3"
                + "&datething=2017/2/3 14:22:11"
                + "&EnumTHing=bAR"
                + "&manyints=  3,23,42, 99 ");
            var result = Vehicle.ReadParameters<HappyParameters>(request);

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
            var request = MakeRequest("intthing=blah");

            var result = Assert.ThrowsException<ServiceOperationException>(() => Vehicle.ReadParameters<HappyParameters>(request));
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
            var request = MakeRequest("dateThingUtc=2017/2/3 14:22:11");
            var result = Vehicle.ReadParameters<HappyParameters>(request);
            AssertEx.AreEqual(new DateTime(2017, 2, 3, 14, 22, 11, DateTimeKind.Utc), result.DateThingUtc);
        }

        class HasRequired 
        {
            [FunctionParameter(IsRequired =true)]
            public string StringThing { get; set; }
        }

        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void ReadParameters_Throws_OnMissingRequiredParameters()
        {
            var request = MakeRequest("");

            var result = Assert.ThrowsException<ServiceOperationException>(() => Vehicle.ReadParameters<HasRequired>(request));
            AssertEx.AreEqual("Missing required parameter 'StringThing'", result.Message);
            AssertEx.AreEqual(ServiceOperationError.BadParameter, result.ErrorCode);
        }

        class HasRequiredBody
        {
            public class BodyItem
            {
                [FunctionParameter(IsRequired = true)]
                public string Name { get; set; }
            }

            [FunctionParameter(Source = ParameterIn.Body, IsRequired = true)]
            public BodyItem[] BodyItems { get; set; }
        }


        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void ReadParameters_Throws_OnMissingRequiredzzzzBodyParameter()
        {
            var request = MakeRequest("");
            var result = Assert.ThrowsException<ServiceOperationException>(() => Vehicle.ReadParameters<HasRequiredBody>(request));
            AssertEx.AreEqual("The POST body response is missing\r\nMissing required parameter 'BodyItems'", result.Message);
            AssertEx.AreEqual(ServiceOperationError.BadParameter, result.ErrorCode);

            request = MakeRequest("", "");
            result = Assert.ThrowsException<ServiceOperationException>(() => Vehicle.ReadParameters<HasRequiredBody>(request));
            AssertEx.AreEqual("Missing required parameter 'BodyItems'", result.Message);
            AssertEx.AreEqual(ServiceOperationError.BadParameter, result.ErrorCode);

            request = MakeRequest("", "[{\"Name\":\"foo\"},{}]");
            result = Assert.ThrowsException<ServiceOperationException>(() => Vehicle.ReadParameters<HasRequiredBody>(request));
            AssertEx.AreEqual("Item 2: missing required parameter 'Name'", result.Message);
            AssertEx.AreEqual(ServiceOperationError.BadParameter, result.ErrorCode);
        }

        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void ReadParameters_Throws_OnUnknownParameters()
        {
            var request = MakeRequest("turtLE=blah&NotAParameter=1");

            var result = Assert.ThrowsException<ServiceOperationException>(() => Vehicle.ReadParameters<HappyParameters>(request));
            AssertEx.AreEqual("Unknown URI parameter:  'turtLE'\r\nUnknown URI parameter:  'NotAParameter'", result.Message);
            AssertEx.AreEqual(ServiceOperationError.BadParameter, result.ErrorCode);
        }

        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void ReadParameters_HandlesDollarParameters()
        {
            var request = MakeRequest("$skip=30");
            var result = Vehicle.ReadParameters<HappyParameters>(request);

            AssertEx.AreEqual(30, result.Query_Skip);

            Assert.ThrowsException<ServiceOperationException>(() => Vehicle.ReadParameters<HappyParameters>(MakeRequest("Query_skip=30")));
        }

        class ParamsInHeader
        {
            [FunctionParameter(Source = ParameterIn.Header,  RemoveRequiredPrefix = "zorba:")]
            public double Prefixed { get; set; }

            [FunctionParameter(Source = ParameterIn.Header)]
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

            var headers = new List<string[]>();
            headers.Add(new string[] { "PREFIxed", "zorba:99.8" });
            headers.Add(new string[] { "NORMal", testGuid.ToString() });
            var request = MakeRequest("bob=hi", null, headers.ToArray());
            var result = Vehicle.ReadParameters<ParamsInHeader>(request);

            AssertEx.AreEqual(testGuid, result.Normal);
            AssertEx.AreEqual(99.8, result.Prefixed);
            AssertEx.AreEqual("hi", result.Bob);
        }

    }
}
