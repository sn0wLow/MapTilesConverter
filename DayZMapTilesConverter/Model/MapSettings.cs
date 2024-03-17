namespace DayZMapTilesConverter
{
    public abstract class MapSettings
    {
        public int Rows { get; set; } = 32;
        public int Columns { get; set; } = 32;
        public int TileWidth { get; set; } = 512;
        public int TileHeight { get; set; } = 512;
        public bool DrawRowsFirst { get; set; } = false;
    }
}
