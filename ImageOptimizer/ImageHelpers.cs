using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ImageOptimizer
{
    public class ImageHelpers
    {
        private static List<string> ImageExtensions = new List<string>() { ".jpg", ".bmp", ".gif", ".png" };
        public static bool IsImage(string filePath)
        {
            if (ImageExtensions.Contains(Path.GetExtension(filePath)))
                return true;
            return false;
        }
        public static void OptimizeAndCopy(string originalImagePath, string destinationImagePath, int maxWidthOrHeight)
        {
            Size MaxDimensions = new Size(maxWidthOrHeight, maxWidthOrHeight);
            Bitmap OriginalImage;
            try
            {
                OriginalImage = (Bitmap)Image.FromFile(originalImagePath);
            }
            catch
            {
                throw new Exception("File does not appear to be an image (" + originalImagePath + ").");
            }

            // if image is smaller than max dimensions, set max dimensions to image size
            // so already-small images do not get upsized
            if (OriginalImage.Height <= maxWidthOrHeight && OriginalImage.Width <= maxWidthOrHeight)
                MaxDimensions = new Size(OriginalImage.Width, OriginalImage.Height);

            // get the smallest reduction percentage from width and height
            float totalReductionPercentage = Math.Min((MaxDimensions.Width / OriginalImage.Width), (MaxDimensions.Height / OriginalImage.Height));

            int destinationWidth = (int)(OriginalImage.Width * totalReductionPercentage);
            int destinationHeight = (int)(OriginalImage.Height * totalReductionPercentage);

            // set the optimized image parameters and draw the image in the new file
            Bitmap OptimizedImage = new Bitmap(destinationWidth, destinationHeight);
            Graphics RawImage = Graphics.FromImage(OptimizedImage);
            RawImage.InterpolationMode = InterpolationMode.HighQualityBicubic;
            RawImage.DrawImage(OriginalImage, 0, 0, destinationWidth, destinationHeight);
            RawImage.Dispose();

            // set new image quality
            EncoderParameters Parameters = new EncoderParameters(1);
            Parameters.Param[0] = new EncoderParameter(Encoder.Quality, 80L);

            // get encoder info
            string ImageExtension = Path.GetExtension(originalImagePath);
            string ImageMimeType = string.Empty;
            ImageCodecInfo ImageCodec = null;
            switch(ImageExtension)
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
            foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageEncoders())
                if (codec.MimeType.Equals(ImageMimeType))
                    ImageCodec = codec;

            // set encoder info and save image to destination
            OptimizedImage.Save(destinationImagePath, ImageCodec, Parameters);
        }
    }
}
