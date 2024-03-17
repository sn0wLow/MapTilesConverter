namespace DayZMapTilesConverter
{
    public interface IImageMergeService
    {
        public Task GenerateMergedMap(MapMergeSettings mapMergeSettings, IEnumerable<string> filePaths);
        public Task<byte[]> GeneratePreviewImage(int maxWidth, int maxHeight);
        public Task CacheMergedMap();
        public Task<string> SaveCachedMap();
    }
}
