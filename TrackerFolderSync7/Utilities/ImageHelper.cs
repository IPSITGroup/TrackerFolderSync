using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TrackerFolderSync7.Properties;

namespace TrackerFolderSync7.Utilities
{
    public static class ImageHelper
    {
        private static List<string> ImageExtensions = new List<string> { ".jpg", ".jpeg", ".gif", ".png" };

        public static bool IsImageFile(string filePath)
        {
            if (ImageExtensions.Contains(Path.GetExtension(filePath).ToLower()))
                return true;
            return false;
        }

        public static void OptimizeAndCopy(string originalImagePath, string destinationImagePath, int maxWidthOrHeight)
        {
            var MaxDimensions = new Size(maxWidthOrHeight, maxWidthOrHeight);
            Bitmap OriginalImage = null;

            try
            {
                OriginalImage = (Bitmap)Image.FromFile(originalImagePath);
            }
            catch (Exception ex)
            {
                Log.Error($"Failure processing image file: {originalImagePath}.", ex);
            }

            // If image is smaller than the max dimensions, set max dimensions to image size
            // so already-small images do not get upsized
            if (OriginalImage.Height <= maxWidthOrHeight
                && OriginalImage.Width <= maxWidthOrHeight)
                MaxDimensions = new Size(OriginalImage.Width, OriginalImage.Height);

            // Get the smallest reduction percentage from width and height
            var totalReductionPercentage = Math.Min(Decimal.Divide(MaxDimensions.Width, OriginalImage.Width), Decimal.Divide(MaxDimensions.Height, OriginalImage.Height));

            var destinationWidth = (int)(OriginalImage.Width * totalReductionPercentage);
            var destinationHeight = (int)(OriginalImage.Height * totalReductionPercentage);

            try
            {
                // Set the optimized image parameters and draw the image in the new file
                using (var OptimizedImage = new Bitmap(destinationWidth, destinationHeight))
                using (var RawImage = Graphics.FromImage(OptimizedImage))
                {
                    RawImage.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    RawImage.DrawImage(OriginalImage, 0, 0, destinationWidth, destinationHeight);

                    var Parameters = new EncoderParameters(1);
                    Parameters.Param[0] = new EncoderParameter(Encoder.Quality, 80L);

                    // Get encoder info
                    var ImageExtension = Path.GetExtension(originalImagePath);
                    var ImageMimeType = string.Empty;
                    ImageCodecInfo ImageCodec = null;

                    switch (ImageExtension)
                    {
                        case ".jpg":
                            ImageMimeType = "image/jpeg";
                            break;
                        case ".bmp":
                            ImageMimeType = "image/bmp";
                            break;
                        case ".gif":
                            ImageMimeType = "image/gif";
                            break;
                        case ".png":
                            ImageMimeType = "image/png";
                            break;
                    }

                    foreach (var codec in ImageCodecInfo.GetImageEncoders())
                        if (codec.MimeType.Equals(ImageMimeType))
                            ImageCodec = codec;

                    // Set encoder info and save image to destination
                    OptimizedImage.Save(destinationImagePath, ImageCodec, Parameters);
                }
            }
            catch (Exception ex)
            {
                ConsoleManager.ReportError(ex);
                Log.Error($"Failure optimizing image: {originalImagePath.Replace(Settings.Default.SchintranetJobsDirectory, "~Schintranet")}.", ex);
            }
        }
    }
}
