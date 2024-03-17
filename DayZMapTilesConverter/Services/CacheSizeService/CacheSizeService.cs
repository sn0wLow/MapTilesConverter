using System.IO;

namespace DayZMapTilesConverter
{
    public class CacheSizeService : ICacheSizeService
    {
        public event Action? OnCacheSizeUpdated;

        private readonly DirectoryInfo _cacheDirectory;
        private static readonly string[] SizeSuffixes = ["bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];
        public CacheSizeService()
        {
            var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _cacheDirectory = new DirectoryInfo(Path.Combine(exeDirectory, "cache"));
        }
        public string? SizeString { get; set; }
        public long? CacheSize { get; set; }
        public string? SizeSuffix { get; set; }

        public async Task UpdateCacheSizeAsync()
        {
            if (!_cacheDirectory.Exists)
            {
                SizeString = null;
                CacheSize = null;
                SizeSuffix = null;

                return;
            }

            await Task.Run(() =>
            {
                UpdateSizeSuffix(DirSize(_cacheDirectory));
            });

            OnCacheSizeUpdated?.Invoke();
        }

        private void UpdateSizeSuffix(long value, int decimalPlaces = 0)
        {
            if (value == 0)
            {
                SizeString = null;
                CacheSize = null;
                SizeSuffix = null;
                return;
            }

            if (decimalPlaces < 0)
            {
                throw new ArgumentOutOfRangeException("decimalPlaces");
            }


            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            if (value < 0)
            {
                SizeString = "-" + string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
            }
            else
            {
                SizeString = string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
            }



            CacheSize = value;
            SizeSuffix = SizeSuffixes[mag];
            return;
        }

        private static long DirSize(DirectoryInfo dir)
        {
            return dir.GetFiles().Sum(fi => fi.Length) +
                   dir.GetDirectories().Sum(di => DirSize(di));
        }
    }
}
