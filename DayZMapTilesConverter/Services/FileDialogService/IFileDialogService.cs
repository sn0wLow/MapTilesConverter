namespace DayZMapTilesConverter
{
    public interface IFileDialogService
    {
        //public Task OpenFileDialogAsync(string extensions, string filter, bool multiSelect);
        public Task<string?> GetFilePathAsync(string extensions, string filter);
        public Task<IEnumerable<string>?> GetFilePathsAsync(string extensions, string filter);
    }
}
