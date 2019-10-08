using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Web.Processing
{
  public class PrintfulClient
  {
    private const int MugProductId = 19;
    private const int Mug15OzVariantId = 4830;

    private readonly HttpClient _client;

    public PrintfulClient(HttpClient client)
    {
      _client = client;
      _client.BaseAddress = new Uri("https://api.printful.com/");

      string apiKey = Environment.GetEnvironmentVariable("printful_key");
      byte[] apiKeyBytes = Encoding.UTF8.GetBytes(apiKey);
      string apiKeyBase64 = Convert.ToBase64String(apiKeyBytes);

      _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", apiKeyBase64);
    }

    public async Task<byte[]> GenerateMockup(byte[] drosteBytes)
    {
      var fileGuid = Guid.NewGuid();
      string fileName = $"{fileGuid:N}.png";
      string localFilePath = $"wwwroot/{fileName}";

      try
      {
        File.WriteAllBytes(localFilePath, drosteBytes);
        return await GenerateMockupImage(fileName);
      }
      finally
      {
        File.Delete(localFilePath);
      }
    }

    private async Task<byte[]> GenerateMockupImage(string fileName)
    {
      var generationTaskResponse =
        await _client.PostAsync(
          $"/mockup-generator/create-task/{MugProductId}",
          new
          {
            varant_ids = new[] { Mug15OzVariantId },
            format = "png",
            files = new[]
            {
              new
              {
                placement = "default",
                image_url = $"https://dopeyinfinitymug.com/{fileName}",
                position = new
                {
                  area_width = 9,
                  area_height = 3.8,
                  width = 3,
                  height = 3.8,
                  top = 0,
                  left = 3
                }
              }
            }
          },
          new JsonMediaTypeFormatter());

      var generationTask = await generationTaskResponse.Content.ReadAsAsync<GenerationTaskResult>();

      while (true)
      {
        var generationTaskResultResponse = await _client.GetAsync(
          $"/mockup-generator/task?task_key={generationTask.result.task_key}");

        var generationTaskResult =
          await generationTaskResultResponse.Content.ReadAsAsync<GenerationTaskResult>();

        if (generationTaskResult.result.status == "completed")
        {
          var mockupImage =
            await _client.GetByteArrayAsync(generationTaskResult.result.mockups[0].mockup_url);
          return mockupImage;
        }

        if (generationTaskResult.result.status == "failed")
        {
          throw new InvalidOperationException(generationTaskResult.result.error);
        }
      }
    }
  }

  public class GenerationTaskResult
  {
    public int code { get; set; }

    public GenerationTask result { get; set; }
  }

  public class GenerationTask
  {
    public string task_key { get; set; }

    public string status { get; set; }

    public string error { get; set; }

    public GenerationTaskMockup[] mockups { get; set; }
  }

  public class GenerationTaskMockup
  {
    public string placement { get; set; }

    public int[] variant_ids { get; set; }

    public string mockup_url { get; set; }
  }
}
