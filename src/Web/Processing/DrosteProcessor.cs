using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using SixLabors.Primitives;
using Web.Controllers;

namespace Web.Processing
{
  public static class DrosteProcessor
  {
    public static Image CreateDroste(
      Stream imageStream,
      CropDimensions crop,
      int rotationDegrees,
      int displayedWidth,
      bool round)
    {
      var image = Image.Load<Rgba32>(imageStream);
      crop.Scale(image.Width, displayedWidth);

      image.Mutate(new AutoOrientProcessor());

      for (int copy = 1; copy <= 4; copy++)
      {
        AddScaledOverlay(image, crop, rotationDegrees, round);
      }

      return image;
    }

    private static void AddScaledOverlay(
      Image<Rgba32> image,
      CropDimensions crop,
      int rotationDegrees,
      bool round)
    {
      double widthScale = crop.Width / image.Width;

      var scaledCopy = image.Clone();
      scaledCopy.Mutate(o =>
      {
        if (round)
        {
          o.ApplyRoundedCorners();
        }
        o.Resize((int)(widthScale * image.Width), (int)(widthScale * image.Height))
          .Rotate(rotationDegrees);
      });

      image.Mutate(o =>
        o.DrawImage(
          scaledCopy,
          new Point((int)crop.X1, (int)crop.Y1),
          GraphicsOptions.Default));
    }
  }
}
