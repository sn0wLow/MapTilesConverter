namespace DayZMapTilesConverter
{
    public class MapMergeSettings : MapSettings
    {
        public bool FixDayZTiles { get; set; } = false;
        public int DayZTileWidth { get; set; } = 512;
        public int DayZTileHeight { get; set; } = 512;
    }
}
