using Microsoft.AspNetCore.Http;

namespace Web.Controllers
{
  public class DrosteRequest
  {
    public IFormFile Image { get; set; }
    public string Crop { get; set; }
    public int DisplayedWidth { get; set; }
    public int RotationDegrees { get; set; }
    public bool Round { get; set; }
  }

  public class CropDimensions
  {
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }

    public double Width => X2 - X1;
    public double Height => Y2 - Y1;

    public void Scale(int originalImageWidth, int displayedWidth)
    {
      var multiplier = (double)originalImageWidth / displayedWidth;
      X1 = X1 * multiplier;
      Y1 = Y1 * multiplier;
      X2 = X2 * multiplier;
      Y2 = Y2 * multiplier;
    }
  }
}
