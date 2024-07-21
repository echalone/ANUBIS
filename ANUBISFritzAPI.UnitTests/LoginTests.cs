using System.Net;
using System;
using Moq.Contrib.HttpClient;

namespace ANUBISFritzAPI.UnitTests
{
    [TestClass]
    public class LoginTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            //var dto = new Person { Id = personId, Name = "Dave", Age = 42 };
            var mockUrl = $"http://fritz.box//login_sid.lua";

            var mockResponse =
@"<SessionInfo>
    <SID>0000000000000000</SID>
    <Challenge>a0fa42bb</Challenge>
    <BlockTime>0</BlockTime>
</SessionInfo>";


            
            var response = mockHandler.SetupRequest(HttpMethod.Get, "http://fritz.box/login_sid.lua").ReturnsResponse(HttpStatusCode.OK, mockResponse);
            mockHandler.SetReturnsDefault(response);

            // Inject the handler or client into your application code
            var httpClient = mockHandler.CreateClient();

            var api = new FritzAPI(new FritzAPIOptions()
                                        {
                                            User = "fritz5057",
                                            Password = "sommer0174",
                                        }, httpClient);

            api.Login();

        }
    }
}