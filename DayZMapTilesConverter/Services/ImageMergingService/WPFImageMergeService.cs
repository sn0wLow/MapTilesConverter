using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Windows;

namespace DayZMapTilesConverter
{
    public class WPFImageMergeService : IImageMergeService
    {
        public Bitmap? MergedImage { get; private set; }

        private readonly string _cacheDirectory;
        private readonly string _imageCachePath;

        public WPFImageMergeService()
        {
            var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _cacheDirectory = Path.Combine(exeDirectory, "cache", "merged");

            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }

            _imageCachePath = Path.Combine(_cacheDirectory, Path.GetRandomFileName());
        }
        public async Task GenerateMergedMap(MapMergeSettings mapMergeSettings, IEnumerable<string> filePaths)
        {

            var originalTileWidth = mapMergeSettings.DayZTileWidth;
            var originalTileHeight = mapMergeSettings.DayZTileHeight;



            int bitmapWidth, bitmapHeight, overlapX = 0, overlapY = 0;

            if (mapMergeSettings.FixDayZTiles)
            {
                overlapX = (int)(16 * ((double)mapMergeSettings.TileWidth / originalTileWidth));
                overlapY = (int)(16 * ((double)mapMergeSettings.TileHeight / originalTileHeight));

                bitmapWidth = mapMergeSettings.TileWidth * mapMergeSettings.Columns - ((mapMergeSettings.Columns - 1) * (2 * overlapX) + (2 * overlapX));
                bitmapHeight = mapMergeSettings.TileWidth * mapMergeSettings.Rows - ((mapMergeSettings.Rows - 1) * (2 * overlapY) + (2 * overlapY));
            }
            else
            {
                bitmapWidth = mapMergeSettings.TileWidth * mapMergeSettings.Columns;
                bitmapHeight = mapMergeSettings.TileWidth * mapMergeSettings.Rows;
            }

            await Task.Run(() =>
            {
                Bitmap outputImage = new Bitmap(bitmapWidth, bitmapHeight);
                int imageCount = filePaths.Count();

                using Graphics g = Graphics.FromImage(outputImage);

                g.Clear(System.Drawing.Color.Transparent);

                for (int i = 0; i < mapMergeSettings.Rows * mapMergeSettings.Columns; i++)
                {
                    if (i >= imageCount)
                    {
                        break;
                    }

                    int tileX, tileY;

                    if (mapMergeSettings.DrawRowsFirst)
                    {
                        tileX = i % mapMergeSettings.Columns;
                        tileY = i / mapMergeSettings.Columns;
                    }
                    else
                    {
                        tileX = i / mapMergeSettings.Rows;
                        tileY = i % mapMergeSettings.Rows;
                    }

                    int x = tileX * (mapMergeSettings.TileWidth);
                    int y = tileY * (mapMergeSettings.TileHeight);

                    if (mapMergeSettings.FixDayZTiles)
                    {
                        int offsetX = ((overlapX * 2) * (tileX)) + (overlapX);
                        int offsetY = ((overlapY * 2) * (tileY)) + (overlapY);

                        x -= (offsetX);
                        y -= (offsetY);
                    }

                    var filePath = filePaths.ElementAt(i);

                    if (!File.Exists(filePath))
                    {
                        throw new FileNotFoundException($"Selected Tile '{filePath}' could not be found");
                    }

                    using Bitmap bitmap = new(filePath);
                    g.SmoothingMode = SmoothingMode.None;
                    g.InterpolationMode = InterpolationMode.Bicubic;
                    g.PixelOffsetMode = PixelOffsetMode.None;
                    //g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    //g.CompositingQuality = CompositingQuality.HighQuality;

                    g.DrawImage(bitmap, new System.Drawing.Rectangle(x, y, mapMergeSettings.TileWidth, mapMergeSettings.TileHeight));
                }

                MergedImage = outputImage;
            });

        }

        public async Task<byte[]> GeneratePreviewImage(int maxWidth = 1024, int maxHeight = 1024)
        {
            if (MergedImage is null)
            {
                throw new InvalidOperationException("Merged Map has not been created yet");
            }

            int originalWidth = MergedImage!.Width;
            int originalHeight = MergedImage!.Height;

            double ratioX = (double)maxWidth / originalWidth;
            double ratioY = (double)maxHeight / originalHeight;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = (int)Math.Round(originalWidth * ratio);
            int newHeight = (int)Math.Round(originalHeight * ratio);

            return await Task.Run(() =>
            {
                Bitmap newBitmap = new Bitmap(newWidth, newHeight);
                using (Graphics g = Graphics.FromImage(newBitmap))
                {
                    g.SmoothingMode = SmoothingMode.None;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.DrawImage(MergedImage!, 0, 0, newWidth, newHeight);
                }

                using MemoryStream ms = new MemoryStream();

                newBitmap.Save(ms, ImageFormat.Png);
                newBitmap.Dispose();

                return ms.ToArray();
            });

        }

        //public async Task CacheMergedMap()
        //{
        //    if (MergedImage == null)
        //    {
        //        throw new InvalidOperationException("There is no image to cache");
        //    }

        //    var di = new DirectoryInfo(_cacheDirectory);

        //    foreach (FileInfo file in di.GetFiles())
        //    {
        //        file.Delete();
        //    }

        //    await Task.Run(() => MergedImage.Save(_imageCachePath, ImageFormat.Png));

        //    MergedImage.Dispose();
        //    MergedImage = null;
        //}

        //public async Task<string> SaveCachedMap()
        //{
        //    if (!File.Exists(_imageCachePath))
        //    {
        //        throw new InvalidOperationException("No cached image available to save");
        //    }

        //    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
        //    {
        //        Filter = "Image files (*.png)|*.png|All files (*.*)|*.*",
        //        DefaultExt = ".png",
        //        FileName = "Map"
        //    };

        //    bool? result = await Application.Current.Dispatcher.InvokeAsync(() => saveFileDialog.ShowDialog());

        //    if (result == true)
        //    {
        //        string filename = saveFileDialog.FileName;

        //        await Task.Run(() =>
        //        {
        //            File.Move(_imageCachePath, filename, true);
        //        });

        //        return filename;
        //    }

        //    return string.Empty;
        //}


        public async Task CacheMergedMap()
        {
            if (MergedImage == null)
            {
                throw new InvalidOperationException("Merged Map has not been created yet");
            }

            var di = new DirectoryInfo(_cacheDirectory);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            await Task.Run(async () =>
            {
                using var imageStream = new MemoryStream();
                MergedImage.Save(imageStream, ImageFormat.Png);
                imageStream.Position = 0;

                await using (var fileStream = File.Create(_imageCachePath))
                await using (var compressionStream = new GZipStream(fileStream, CompressionLevel.Optimal))
                {
                    await imageStream.CopyToAsync(compressionStream);
                }
            });



            MergedImage.Dispose();
            MergedImage = null;
        }

        public async Task<string> SaveCachedMap()
        {

            if (!File.Exists(_imageCachePath))
            {
                throw new InvalidOperationException("No cached image available to save");
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Image files (*.png)|*.png|All files (*.*)|*.*",
                DefaultExt = ".png",
                FileName = "Map"
            };

            bool? result = await Application.Current.Dispatcher.InvokeAsync(() => saveFileDialog.ShowDialog());

            if (result == true)
            {
                string filename = saveFileDialog.FileName;

                await Task.Run(async () =>
                {
                    await using (var compressedStream = File.OpenRead(_imageCachePath))
                    await using (var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                    await using (var decompressedStream = new FileStream(filename, FileMode.Create))
                    {
                        await decompressionStream.CopyToAsync(decompressedStream);
                    }
                });

                return filename;
            }


            return string.Empty;
        }









    }
}
