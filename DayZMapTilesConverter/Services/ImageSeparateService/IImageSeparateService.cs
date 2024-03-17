namespace DayZMapTilesConverter
{
    public interface IImageSeparateService
    {
        public Task GenerateSeparatedTiles(MapSeparateSettings mapSeparateSettings, string filePath);
        public Task<byte[]> GeneratePreviewImage(int maxWidth, int maxHeight);
        public Task CacheTiles();
        public Task<string> SaveCachedTiles();
    }
}
