using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using Web.Processing;

namespace Web.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class DrosteController : ControllerBase
  {
    private readonly DrosteProcessor _processor;
    private readonly ILogger _logger;

    public DrosteController(
      DrosteProcessor processor,
      ILogger<DrosteController> logger)
    {
      _processor = processor;
      _logger = logger;
    }

    [HttpPost]
    public FileResult CreateDroste([FromForm] DrosteRequest request)
    {
      using (var fileStream = request.Image.OpenReadStream())
      {
        var rendered = _processor.CreateDroste(
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

    [HttpPost("make")]
    public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request)
    {
      var sendGridKey = Environment.GetEnvironmentVariable("sendgrid_key");
      var sendGridClient = new SendGridClient(sendGridKey);
      var from = new EmailAddress("info@dopeyinfinitymug.com");
      var to = new EmailAddress("ryan.aidan+mugs@gmail.com");
      var subject = "Infinity Mug Request";
      var plainTextContent = "Please create an infinity mug for " + request.UserEmail;
      var msg = MailHelper.CreateSingleEmail(
        from,
        to,
        subject,
        plainTextContent,
        null);
      msg.AddCc(new EmailAddress(request.UserEmail));

      var base64Data = Regex
        .Match(request.ImageBase64, @"data:image/(?<type>.+?),(?<data>.+)")
        .Groups["data"]
        .Value;

      msg.Attachments = new List<Attachment>
      {
        new Attachment
        {
          Content = base64Data,
          Type = "image/png",
          Filename = "mug.png",
          ContentId = "mug.png",
          Disposition = "inline"
        }
      };

      try
      {
        var sendResponse = await sendGridClient.SendEmailAsync(msg);
        _logger.LogInformation(
          "Send email response: {Status} {Body}",
          sendResponse.StatusCode,
          await sendResponse.Body.ReadAsStringAsync());
        if (sendResponse.StatusCode >= HttpStatusCode.BadRequest)
        {
          return BadRequest();
        }
      }
      catch (Exception ex)
      {
        return BadRequest(ex);
      }

      // var mockup = await _printfulClient.GenerateMockup(Convert.FromBase64String(base64Data));
      return Ok();
    }
  }

  public class SendEmailRequest
  {
    public string ImageBase64 { get; set; }
    public string UserEmail { get; set; }
  }
}
