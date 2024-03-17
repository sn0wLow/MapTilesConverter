namespace DayZMapTilesConverter
{
    public interface ICacheSizeService
    {
        public event Action? OnCacheSizeUpdated;
        public string? SizeString { get; set; }
        public long? CacheSize { get; set; }
        public string? SizeSuffix { get; set; }

        public Task UpdateCacheSizeAsync();
    }
}
