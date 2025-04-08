//using Microsoft.AspNetCore.Http;
//using Microsoft.Data.SqlClient;
//using Microsoft.Extensions.Logging;
//using Moq;
//using Newtonsoft.Json;
//using PostApiService.Middlewares;
//using PostApiService.Models;
//using System.Net;

//namespace PostApiService.Tests.UnitTests
//{
//    public class ExceptionMiddlewareTests
//    {
//        [Fact]
//        public async Task Middleware_ShouldReturn_InternalServerError_WhenSqlExceptionThrown()
//        {
//            // Arrange
//            var loggerMock = new Mock<ILogger<GlobalExceptionMiddleware>>();
//            RequestDelegate next = (ctx) => throw CreateSqlException();

//            var middleware = new GlobalExceptionMiddleware(next, loggerMock.Object);

//            var context = new DefaultHttpContext();
//            context.Response.Body = new MemoryStream();

//            // Act
//            await middleware.Invoke(context);

//            // Assert
//            Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);

//            Assert.NotNull(context.Response.Body);

//            context.Response.Body.Seek(0, SeekOrigin.Begin);
//            var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

//            var errorResponse = JsonConvert.DeserializeObject<ApiResponse<string>>(responseBody);

//            Assert.NotNull(errorResponse);
//            Assert.Contains("Database", errorResponse.Message);
//        }

//        private static SqlException CreateSqlException()
//        {
//            try
//            {
//                using var connection = new SqlConnection("Server=invalid;Database=invalid;User Id=invalid;Password=invalid;");
//                connection.Open();
//            }
//            catch (SqlException ex)
//            {
//                return ex;
//            }

//            throw new Exception("SqlException was not thrown as expected.");
//        }
//    }
//}
