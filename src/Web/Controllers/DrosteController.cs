using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using Web.Processing;

namespace Web.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class DrosteController : ControllerBase
  {
    private readonly ILogger _logger;

    public DrosteController(ILogger<DrosteController> logger)
    {
      _logger = logger;
    }

    [HttpPost]
    public FileResult CreateDroste([FromForm] DrosteRequest request)
    {
      using (var fileStream = request.Image.OpenReadStream())
      {
        var rendered = DrosteProcessor.CreateDroste(
          fileStream,
          JsonConvert.DeserializeObject<CropDimensions>(request.Crop),
          request.RotationDegrees,
          request.DisplayedWidth,
          request.Round);

        using (var outputStream = new MemoryStream())
        {
          rendered.Save(outputStream, PngFormat.Instance);
          var bytes = outputStream.ToArray();
          return File(bytes, "image/png");
        }
      }
    }
  }
}
