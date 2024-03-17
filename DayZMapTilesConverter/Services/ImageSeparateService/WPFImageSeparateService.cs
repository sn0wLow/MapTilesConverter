using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;

namespace DayZMapTilesConverter
{
    public class WPFImageSeparateService : IImageSeparateService
    {
        private Bitmap[,] _separatedTilesGrid = null!;
        private MapSeparateSettings _settings = null!;

        private readonly string _cacheDirectory;
        private readonly string _tileArchive;


        public WPFImageSeparateService()
        {
            var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _cacheDirectory = Path.Combine(exeDirectory, "cache", "separated");

            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }

            _tileArchive = Path.Combine(_cacheDirectory, Path.GetRandomFileName());
        }

        public async Task GenerateSeparatedTiles(MapSeparateSettings mapSeparateSettings, string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Selected Image '{filePath}' could not be found");
            }


            _settings = mapSeparateSettings;
            _separatedTilesGrid = new Bitmap[_settings.Rows, _settings.Columns];


            await Task.Run(() =>
            {
                var bitmap = new Bitmap(filePath);
                int mapTileWidth = (int)((double)bitmap.Width / _settings.Columns);
                int mapTileHeight = (int)((double)bitmap.Height / _settings.Rows);

                for (int i = 0; i < _settings.Rows; i++)
                {
                    for (int j = 0; j < _settings.Columns; j++)
                    {
                        int mapPosX, mapPosY, tileWidth, tileHeight;

                        if (_settings.AutoCalcTileSize)
                        {
                            tileWidth = mapTileWidth;
                            tileHeight = mapTileHeight;
                        }
                        else
                        {
                            tileWidth = _settings.TileWidth;
                            tileHeight = _settings.TileHeight;
                        }

                        mapPosX = j * mapTileWidth;
                        mapPosY = i * mapTileHeight;

                        Bitmap tile = new Bitmap(tileWidth, tileHeight);


                        //System.Windows.MessageBox.Show("bitmap.Width" + bitmap.Width + " importedWidth: " + importedWidth);

                        using (Graphics g = Graphics.FromImage(tile))
                        {
                            g.Clear(System.Drawing.Color.Transparent);
                            g.SmoothingMode = SmoothingMode.None;
                            g.InterpolationMode = InterpolationMode.NearestNeighbor;
                            g.PixelOffsetMode = PixelOffsetMode.Half;

                            var destRect = new Rectangle(0, 0, tileWidth, tileHeight);
                            var srcRect = new Rectangle(mapPosX, mapPosY, mapTileWidth, mapTileHeight);

                            g.DrawImage(bitmap, destRect, srcRect, GraphicsUnit.Pixel);
                        }


                        if (_settings.DrawRowsFirst)
                        {
                            _separatedTilesGrid[i, j] = tile;
                        }
                        else
                        {
                            int flattenedIndex = i * _settings.Columns + j;
                            int newI = flattenedIndex % _settings.Rows;
                            int newJ = flattenedIndex / _settings.Rows;
                            _separatedTilesGrid[newI, newJ] = tile;
                        }

                    }

                }

                bitmap.Dispose();
            });
        }



        public async Task<byte[]> GeneratePreviewImage(int maxWidth = 1024, int maxHeight = 1024)
        {
            if (_separatedTilesGrid is null)
            {
                throw new InvalidOperationException("Separated Tiles have not been created yet");
            }

            return await Task.Run(() =>
            {
                using var bitmap = new Bitmap(maxWidth, maxHeight);
                int rows = _separatedTilesGrid.GetLength(0);
                int cols = _separatedTilesGrid.GetLength(1);

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Transparent);


                    float exactTileWidth = (float)maxWidth / cols;
                    float exactTileHeight = (float)maxHeight / rows;

                    int tileWidth = (int)Math.Floor(exactTileWidth);
                    int tileHeight = (int)Math.Floor(exactTileHeight);

                    int lastTileWidth = maxWidth - (tileWidth * (cols - 1));
                    int lastTileHeight = maxHeight - (tileHeight * (rows - 1));

                    int baseRows = 6;
                    int baseColumns = 6;
                    float baseFontSize = 10;

                    int actualRows = _settings.Rows;
                    int actualColumns = _settings.Columns;

                    float rowScaleFactor = (float)baseRows / actualRows;
                    float columnScaleFactor = (float)baseColumns / actualColumns;
                    float minScaleFactor = Math.Min(rowScaleFactor, columnScaleFactor);
                    float dynamicFontSize = Math.Max(baseFontSize * minScaleFactor, 6);
                    Font textFont = new Font("Calibri", dynamicFontSize, FontStyle.Bold);

                    StringFormat stringFormat = new()
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                    };

                    var largestTextFitsInTile =
                    g.MeasureString($"[{actualColumns}|{actualColumns}]", textFont).Width <= tileWidth;


                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < cols; j++)
                        {
                            int posX, posY;

                            int desiredTileWidth = i == cols - 1 ? lastTileWidth : tileWidth;
                            int desiredTileHeight = j == rows - 1 ? lastTileHeight : tileHeight;


                            posY = i * tileHeight;
                            posX = j * tileWidth;

                            //Debug.WriteLine(i + " " + j + " | x:" + posX + " y:" + posY);


                            g.SmoothingMode = SmoothingMode.HighQuality;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                            var tile = _separatedTilesGrid[i, j];

                            using Bitmap resizedTile = ResizeImage(tile, desiredTileWidth, desiredTileHeight);

                            g.DrawImage(resizedTile, new Rectangle(posX, posY, desiredTileWidth, desiredTileHeight));

                            if (_settings.DrawCoordinates && largestTextFitsInTile)
                            {
                                string text = $"[{i}|{j}]";

                                SizeF textSize = g.MeasureString(text, textFont);

                                var fontRectBG = new Rectangle(posX, posY + 1,
                                    (int)textSize.Width, (int)textSize.Height);


                                using (var blackBG = new SolidBrush(Color.FromArgb(150, Color.Black)))
                                {
                                    g.FillRectangle(blackBG, fontRectBG);
                                }

                                g.DrawString(text, textFont, Brushes.White,
                                new PointF(posX + (textSize.Width / 2),
                                           posY + (textSize.Height / 2)), stringFormat);
                            }



                            //Brush outlineBrush = Brushes.Black;
                            //int outlineThickness = 1;
                            //for (int dx = -outlineThickness; dx <= outlineThickness; dx++)
                            //{
                            //    for (int dy = -outlineThickness; dy <= outlineThickness; dy++)
                            //    {
                            //        if (dx != 0 && dy != 0)
                            //        {
                            //            g.DrawRectangle();
                            //            g.DrawString(text, textFont, outlineBrush,
                            //                new PointF(x + dx + (textSize.Width / 2),
                            //                           y + dy + (textSize.Height / 2)), stringFormat);
                            //        }
                            //    }
                            //}


                        }
                    }
                }

                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);

                return ms.ToArray();
            });
        }


        private Bitmap ResizeImage(Bitmap originalImage, int width, int height)
        {
            var destRect = new System.Drawing.Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.None;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;

                graphics.Clear(System.Drawing.Color.Transparent);

                graphics.DrawImage(originalImage, destRect, 0, 0, originalImage.Width, originalImage.Height, GraphicsUnit.Pixel);
            }

            return destImage;
        }

        public async Task CacheTiles()
        {
            if (_separatedTilesGrid is null)
            {
                throw new InvalidOperationException("Separated Tiles have not been created yet");
            }

            var di = new DirectoryInfo(_cacheDirectory);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }


            await Task.Run(() =>
            {
                using var zipStream = new FileStream(_tileArchive, FileMode.Create);
                using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

                for (int i = 0; i < _separatedTilesGrid.GetLength(0); i++)
                {
                    for (int j = 0; j < _separatedTilesGrid.GetLength(1); j++)
                    {
                        var entry = archive.CreateEntry($"tile_{i}_{j}.png", CompressionLevel.Optimal);
                        var tile = _separatedTilesGrid[i, j];

                        using var entryStream = entry.Open();
                        tile.Save(entryStream, ImageFormat.Png);
                        tile.Dispose();
                        tile = null;
                    }
                }

            });

            _separatedTilesGrid = null!;
        }



        public async Task<string> SaveCachedTiles()
        {
            if (!File.Exists(_tileArchive))
            {
                throw new InvalidOperationException("No Cached Archive found to extract tiles.");
            }

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "ZIP files (*.zip)|*.zip|All files (*.*)|*.*",
                DefaultExt = ".zip",
                FileName = "Tiles Archive"
            };

            string savedFilePath = string.Empty;
            bool? result = saveFileDialog.ShowDialog();

            if (result == true)
            {
                savedFilePath = saveFileDialog.FileName;

                await Task.Run(() => File.Move(_tileArchive, savedFilePath, overwrite: true));
            }

            return savedFilePath;
        }


    }
}
