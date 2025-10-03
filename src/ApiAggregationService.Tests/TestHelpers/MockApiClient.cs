// using System.Net.Http;
// using System.Threading.Tasks;
// using Moq;

// namespace ApiAggregationService.Tests.TestHelpers
// {
//     public class MockApiClient
//     {
//         private readonly Mock<HttpMessageHandler> _handlerMock;

//         public MockApiClient()
//         {
//             _handlerMock = new Mock<HttpMessageHandler>();
//         }

//         public void SetupResponse(string url, HttpResponseMessage response)
//         {
//             _handlerMock
//                 .Protected()
//                 .Setup<Task<HttpResponseMessage>>(
//                     "SendAsync",
//                     ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString() == url),
//                     ItExpr.IsAny<CancellationToken>()
//                 )
//                 .ReturnsAsync(response);
//         }

//         public HttpClient CreateClient()
//         {
//             var httpClient = new HttpClient(_handlerMock.Object)
//             {
//                 BaseAddress = new Uri("http://localhost/")
//             };
//             return httpClient;
//         }
//     }
// }