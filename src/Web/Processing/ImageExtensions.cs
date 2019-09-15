using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Shapes;

namespace Web.Processing
{
  public static class ImageExtensions
  {
    public static IImageProcessingContext ApplyRoundedCorners(
      this IImageProcessingContext ctx)
    {
      (int width, int height) = ctx.GetCurrentSize();
      IPathCollection corners = BuildCorners(width, height);

      var graphicOptions = new GraphicsOptions(true)
      {
        // enforces that any part of this shape that has color is punched out of the background
        AlphaCompositionMode = PixelAlphaCompositionMode.DestOut
      };

      // use any color (not Transparent), so the corners will be clipped
      return ctx.Fill(graphicOptions, Rgba32.LimeGreen, corners);
    }

    private static IPathCollection BuildCorners(int imageWidth, int imageHeight)
    {
      float widthCut = imageWidth / 2f;
      float heightCut = imageHeight / 2f;

      var rect = new RectangularPolygon(-0.5f, -0.5f, widthCut, heightCut);
      IPath cornerTopLeft =
        rect.Clip(new EllipsePolygon(widthCut - 0.5f, heightCut - 0.5f, imageWidth, imageHeight));

      float rightPos = imageWidth - cornerTopLeft.Bounds.Width + 1;
      float bottomPos = imageHeight - cornerTopLeft.Bounds.Height + 1;
      IPath cornerTopRight =
        rect.Clip(new EllipsePolygon(-0.5f, heightCut - 0.5f, imageWidth, imageHeight))
          .Translate(rightPos, 0);
      IPath cornerBottomRight = cornerTopLeft.RotateDegree(180).Translate(rightPos, bottomPos);
      IPath cornerBottomLeft = cornerTopRight.RotateDegree(180).Translate(-rightPos, bottomPos);

      return new PathCollection(cornerTopLeft, cornerBottomLeft, cornerTopRight, cornerBottomRight);
    }
  }
}
