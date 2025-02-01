using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;

namespace VideoProcessorX.IntegrationTests
{
    public class VideosControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public VideosControllerTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task UploadVideo_Should_Return_Ok_When_Valid_File()
        {
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(File.ReadAllBytes("test.mp4")), "file", "test.mp4");

            var response = await _client.PostAsync("/api/videos/upload", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

}