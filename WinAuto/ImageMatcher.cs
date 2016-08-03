﻿using System;
using System.Drawing;

namespace WinAuto
{
    /// <summary>
    /// Per pixel image matching, yet relatively fast.
    /// </summary>
    public class ImageMatcher
    {
        /// <summary>
        /// Possible color threshold. Lower value means ImageMatcher can find even not 100% matching images, but it can be slower.
        /// Default is 1(so ImageMatcher looks for exact match).
        /// </summary>
        public float Threshold { set; get; } = 1;

        static Bitmap applyFilters(Bitmap bitmap)
        {
            var filter = new GrayScale();

            return filter.Apply(bitmap);
        }
        static bool compareColorChannel(byte channel1, byte channel2, float threshold)
        {
            threshold = Math.Min(threshold, 1f);
            threshold = Math.Max(threshold, 0.1f);
            float max = Math.Max(channel1, channel2);
            float min = Math.Min(channel1, channel2);

            return (1 - ((max - min) / 255) >= threshold);
        }

        static Color roundAlphaChannel(Color color)
        {
            // Color is immutable
            return Color.FromArgb(color.A == 255 ? 255 : 0, color.R, color.G, color.B);
        }
        static bool compareColors(Color templateColor, Color sourceColor, float threshold)
        {
            templateColor = roundAlphaChannel(templateColor);

            if (templateColor.A == 0)
                return true;

            return (
                compareColorChannel(templateColor.R, sourceColor.R, threshold) &&
                compareColorChannel(templateColor.G, sourceColor.G, threshold) &&
                compareColorChannel(templateColor.B, sourceColor.B, threshold)
                );
        }
        static Rectangle? matchBitmaps(Bitmap source, Bitmap template, Rectangle searchZone, float threshold)
        {
            var maybeFound = false;

            if (searchZone.X + searchZone.Width > source.Width)
                searchZone.Width = Math.Abs(searchZone.X - source.Width);
            if (searchZone.Y + searchZone.Height > source.Height)
                searchZone.Height = Math.Abs(searchZone.Y - source.Height);

            for (int sY = searchZone.Y; sY < searchZone.Y + searchZone.Height - template.Height - 1; sY++)
            {
                for (int sX = searchZone.X; sX < searchZone.X + searchZone.Width - template.Width - 1; sX++)
                {
                    for (int tY = 0; tY < template.Height; tY++)
                    {
                        for (int tX = 0; tX < template.Width; tX++)
                        {
                            var tPixel = template.GetPixel(tX, tY);
                            var sPixel = source.GetPixel(sX + tX, sY + tY);

                            maybeFound = compareColors(tPixel, sPixel, threshold);

                            if (!maybeFound)
                                break;
                        }
                        if (!maybeFound)
                            break;
                    }
                    if (maybeFound)
                        return new Rectangle(sX, sY, template.Width, template.Height);
                }
            }
            return null;
        }

        /// <summary>
        /// Tries to match template image with source image. Per pixel searching, no magic.
        /// Regardless of this approach, algorithm seems to be pretty fast for simple automatization purposes.
        /// If you want to speed matching as much as possible, try to look inside smaller areas
        /// and try to experiment with your template images. More unique
        /// the template is, more speed you can get.
        /// </summary>
        /// <param name="source">Source image(eg. screenshot)</param>
        /// <param name="template">Template image</param>
        /// <returns>
        /// Rectangle holding position and size of found template
        /// Null if nothing was found
        /// </returns>
        public Rectangle? FindTemplate(Image source, Image template)
        {
            var sourceRect = new Rectangle(new Point(0, 0), source.Size);

            return FindTemplate(source, template, sourceRect);
        }
        /// <summary>
        /// Tries to match template image with source image. Per pixel searching, no magic.
        /// Regardless of this approach, algorithm seems to be pretty fast for simple automatization purposes.
        /// If you want to speed matching as much as possible, try to look inside smaller areas
        /// and try to experiment with your template images. More unique
        /// the template is, more speed you can get.
        /// </summary>
        /// <param name="source">Source image(eg. screenshot)</param>
        /// <param name="template">Template image</param>
        /// <param name="sourceRect">
        /// Rectangle within source image determining searching area.
        /// This can speed up whole matching process in case you provide screenshot but want to scan just small portion of its actual size.
        /// </param>
        /// <returns>
        /// Rectangle holding position and size of found template
        /// Null if nothing was found
        /// </returns>
        public Rectangle? FindTemplate(Image source, Image template, Rectangle sourceRect)
        {
            var screenBitmap = new Bitmap(source);
            var templateBitmap = new Bitmap(template);

            // Making images gray scaled is not that good idea.
            // screenBitmap = applyFilters(screenBitmap);
            // templateBitmap = applyFilters(templateBitmap);

            var templateRect = matchBitmaps(screenBitmap, templateBitmap, sourceRect, Threshold);

            screenBitmap.Dispose();
            templateBitmap.Dispose();
            return templateRect;
        }

        /// <summary>
        /// Crops image.
        /// </summary>
        /// <param name="image">Image to be cropped.</param>
        /// <param name="rectangle">Rectangle of the new image</param>
        /// <returns>Cropped image</returns>
        public static Image CropImage(Image image, Rectangle rectangle)
        {
            image = ((Bitmap)image).Clone(rectangle, image.PixelFormat);
            return image;
        }
    }
}
