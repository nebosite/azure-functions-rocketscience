using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Linq;
using Microsoft.Azure.Functions.AFRocketScience;
using Newtonsoft.Json;
using System.Net.Http;

namespace Microsoft.Azure.Functions.AFRocketScienceTests
{

    [TestClass]
    public class ControllerHandlerBaseTests
    {
        class TestResponse
        {
            public int Count { get; set; }
            public string ErrorMessage { get; set; }
            public string ErrorCode { get; set; }
            public string[] Values { get; set; }
        }

        class RandomException : Exception
        {
            string _stackTrace;
            public RandomException(string message, string stackTrace) : base(message)
            {
                _stackTrace = stackTrace;
            }

            public override string StackTrace => _stackTrace;
        }

        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void Error_ReturnsFatal_OnNormalException()
        {
            var target = new ControllerHandlerBase();
            var mockLogger = new MockLogger();
            var output = new RandomException("Bumper Boats", "Abba\\Dabba\\foobar:line 232\r\nShoe\\Lollipop\\gumby:line 444");
            var result = target.Error(output, mockLogger);
            AssertEx.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
            AssertEx.AreEqual(1, mockLogger.Errors.Count);

            var stuff = JsonConvert.DeserializeObject<TestResponse>(
                result.Content.ReadAsStringAsync().Result);

            AssertEx.AreEqual(0, stuff.Count);
            AssertEx.AreEqual(new string[0], stuff.Values);
            AssertEx.AreEqual(ServiceOperationError.FatalError.ToString(), stuff.ErrorCode);
            AssertEx.StartsWith("There was a fatal service error.\r\nThe Log Key for this error is", stuff.ErrorMessage);
            AssertEx.EndsWith("Debug hint: Bumper Boats (foobar:line 232)", stuff.ErrorMessage);
        }

        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void Error_ReturnsNonFatal_OnServiceException()
        {
            var target = new ControllerHandlerBase();
            var mockLogger = new MockLogger();
            var output = new ServiceOperationException(ServiceOperationError.BadParameter, "Yuba bears");
            var result = target.Error(output, mockLogger);
            AssertEx.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
            AssertEx.AreEqual(1, mockLogger.Errors.Count);

            var stuff = JsonConvert.DeserializeObject<TestResponse>(
                result.Content.ReadAsStringAsync().Result);

            AssertEx.AreEqual(0, stuff.Count);
            AssertEx.AreEqual(new string[0], stuff.Values);
            AssertEx.AreEqual(ServiceOperationError.BadParameter.ToString(), stuff.ErrorCode);
            AssertEx.StartsWith("Yuba bears\r\nThe Log Key for this error is", stuff.ErrorMessage);
        }

        class Foo { public int N { get; set; } }

        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void StandardResponse_Constructor_HandlesArraysAndObjects_Differently()
        {
            // Arrays of valuetypes must be boxed first
            var values = new[] { 9, 88, 12 };
            Assert.ThrowsException<ArgumentException>(() => new ServiceResponse(values));
            var valueArrayTarget = new ServiceResponse( values.Cast<object>().ToArray());
            AssertEx.AreEqual(3, valueArrayTarget.Count);
            AssertEx.AreEqual(null, valueArrayTarget.ErrorCode);
            AssertEx.AreEqual(3, valueArrayTarget.Values.Length);
            AssertEx.AreEqual(9, valueArrayTarget.Values[0]);
            AssertEx.AreEqual(88, valueArrayTarget.Values[1]);
            AssertEx.AreEqual(12, valueArrayTarget.Values[2]);

            // An array of objects should work the same
            var objectArrayTarget = new ServiceResponse(new[] { new Foo() { N = 33 }, new Foo() { N = 22 } });
            AssertEx.AreEqual(2, objectArrayTarget.Count);
            AssertEx.AreEqual(null, objectArrayTarget.ErrorCode);
            AssertEx.AreEqual(2, objectArrayTarget.Values.Length);
            AssertEx.AreEqual(33, ((Foo)(objectArrayTarget.Values[0])).N);
            AssertEx.AreEqual(22, ((Foo)(objectArrayTarget.Values[1])).N);

            // Plain objects get stuck in an array
            var objectTarget = new ServiceResponse("text");
            AssertEx.AreEqual(1, objectTarget.Count);
            AssertEx.AreEqual(null, objectTarget.ErrorCode);
            AssertEx.AreEqual(1, objectTarget.Values.Length);
            AssertEx.AreEqual("text", objectTarget.Values[0]);

            // null objects return empty array
            var nullObjectTarget = new ServiceResponse(null);
            AssertEx.AreEqual(0, nullObjectTarget.Count);
            AssertEx.AreEqual(null, nullObjectTarget.ErrorCode);
            AssertEx.AreEqual(0, nullObjectTarget.Values.Length);

        }


        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void Ok_ConvertsOutput_ToJson()
        {
            // valuetypes are serialized as one element value array
            var target = new ControllerHandlerBase();
            var result = target.Ok("A string");
            AssertEx.AreEqual(HttpStatusCode.OK, result.StatusCode);
            AssertEx.AreEqual(
@"{
  ""Count"": 1,
  ""ErrorCode"": null,
  ""Values"": [
    ""A string""
  ],
  ""ErrorMessage"": null
}"
            , result.Content.ReadAsStringAsync().Result);
        }

        
        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void SafelyTry_ReturnsStandardResponseObject_OnSuccess()
        {
            // valuetypes are serialized as one element value array
            var target = new ControllerHandlerBase();
            var mockLogger = new MockLogger();
            var result = target.SafelyTry(mockLogger, () => "Hi");
            AssertEx.AreEqual(HttpStatusCode.OK, result.StatusCode);

            var stuff = JsonConvert.DeserializeObject<TestResponse>(
                result.Content.ReadAsStringAsync().Result);

            AssertEx.AreEqual(null, stuff.ErrorMessage);
            AssertEx.AreEqual(1, stuff.Count);
            AssertEx.AreEqual(new string[] { "Hi" }, stuff.Values);
        }

        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void SafelyTry_ReturnsRawValue_WhenHttpResponseMessage()
        {
            // valuetypes are serialized as one element value array
            var target = new ControllerHandlerBase();
            var mockLogger = new MockLogger();
            var output = new HttpResponseMessage(HttpStatusCode.PartialContent)
            {
                Content = new StringContent("Gubmy")
            };

            var result = target.SafelyTry(mockLogger, () =>output);
            AssertEx.AreEqual(HttpStatusCode.PartialContent, result.StatusCode);

            AssertEx.AreEqual("Gubmy", result.Content.ReadAsStringAsync().Result);
        }


        //------------------------------------------------------------------------------
        //
        //------------------------------------------------------------------------------
        [TestMethod]
        [TestCategory("CheckInGate")]
        public void SafelyTry_ReturnsError_OnException()
        {
            // valuetypes are serialized as one element value array
            var target = new ControllerHandlerBase();
            var mockLogger = new MockLogger();


            var result = target.SafelyTry(mockLogger, () => throw new Exception("bob wins"));
            AssertEx.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);

            var stuff = JsonConvert.DeserializeObject<TestResponse>(
                result.Content.ReadAsStringAsync().Result);

            AssertEx.StartsWith("There was a fatal service error.", stuff.ErrorMessage);
        }

    }
}
