using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
    public DrosteController(IConfiguration configuration)
    {
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

    [HttpPost("make")]
    public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request)
    {
      var sendGridKey = Environment.GetEnvironmentVariable("sendgrid_key");
      var sendGridClient = new SendGridClient(sendGridKey);
      var from = new EmailAddress(request.FromEmail);
      var to = new EmailAddress("ryan.aidan@gmail.com");
      var subject = "Infinity Mug Request";
      var plainTextContent = "Please create an infinity mug for " + request.FromEmail;
      var msg = MailHelper.CreateSingleEmail(
        from,
        to,
        subject,
        plainTextContent,
        null);

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
        var sendEmailResponse = await sendGridClient.SendEmailAsync(msg);
        return Ok(sendEmailResponse);
      }
      catch (Exception ex)
      {
        return BadRequest(ex);
      }
    }
  }

  public class SendEmailRequest
  {
    public string ImageBase64 { get; set; }
    public string FromEmail { get; set; }
  }
}
