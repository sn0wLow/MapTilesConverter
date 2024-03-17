using Microsoft.Win32;
using MudBlazor;
using System.Text.Json;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DayZMapTilesConverter
{
    public class WPFFileDialogService : IFileDialogService
    {
        public async Task<string?> GetFilePathAsync(string extensions, string filter)
        {
            var tcs = new TaskCompletionSource<string?>();

            System.Threading.SynchronizationContext.Current!.Post(_ =>
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    DefaultExt = extensions,
                    Filter = filter,
                    FilterIndex = 2,
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    tcs.SetResult(openFileDialog.FileName);
                }
                else
                {
                    tcs.SetResult(null);
                }
            }, null);

            return await tcs.Task;
        }

        public async Task<IEnumerable<string>?> GetFilePathsAsync(string extensions, string filter)
        {
            var tcs = new TaskCompletionSource<IEnumerable<string>?>();

            System.Threading.SynchronizationContext.Current!.Post((_) => {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    DefaultExt = extensions,
                    Filter = filter,
                    Multiselect = true
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    tcs.SetResult(openFileDialog.FileNames);
                }
                else
                {
                    tcs.SetResult(null);
                }

            }, null);



            return await tcs.Task;
        }




    }
}
