using Blazored.LocalStorage;
using BlazorPanzoom;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace DayZMapTilesConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string[] WebViewContextMenuItemsToRemove =
                ["share", "webSelect", "webCapture",
                "inspectElement", "saveAs", "reload",
                "copyLinkToHighlight", "openLinkInNewWindow",
                "saveLinkAs", "copyLinkLocation"];

        public MainWindow()
        {
            InitializeComponent();

            var services = new ServiceCollection();
            services.AddWpfBlazorWebView();
            services.AddMudServices(config =>
            {
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;

                config.SnackbarConfiguration.PreventDuplicates = false;
                config.SnackbarConfiguration.NewestOnTop = false;
                config.SnackbarConfiguration.ShowCloseIcon = true;
                config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
            });

            services.AddBlazoredLocalStorage();
            services.AddBlazorPanzoomServices();
            services.AddScoped<IFileDialogService, WPFFileDialogService>();
            services.AddSingleton<IImageMergeService, WPFImageMergeService>();
            services.AddSingleton<IImageSeparateService, WPFImageSeparateService>();
            services.AddSingleton<ICacheSizeService, CacheSizeService>();


#if DEBUG
            services.AddBlazorWebViewDeveloperTools();
#endif
            blazorWebView.Services = services.BuildServiceProvider();
            blazorWebView.BlazorWebViewInitializing += WebViewInitializing;
            blazorWebView.BlazorWebViewInitialized += BlazorWebView_Initialized;
        }

        private void WebViewInitializing(object? sender, BlazorWebViewInitializingEventArgs e)
        {
            var userDataFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "WebView2UserData");
            Directory.CreateDirectory(userDataFolder);

            e.UserDataFolder = userDataFolder;
        }

        private void BlazorWebView_Initialized(object? sender, BlazorWebViewInitializedEventArgs e)
        {
            e.WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            e.WebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;

#if !DEBUG
            e.WebView.CoreWebView2.ContextMenuRequested += CoreWebView2_ContextMenuRequested;
#endif
        }

        private void CoreWebView2_ContextMenuRequested(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2ContextMenuRequestedEventArgs e)
        {
            // For editable elements such as <input> and <textarea> we enable the context menu but remove items we don't want in this app
            var menuIndexesToRemove =
                e.MenuItems
                    .Select((m, i) => (m, i))
                    .Where(m => WebViewContextMenuItemsToRemove.Contains(m.m.Name))
                    .Select(m => m.i)
                    .Reverse();

            foreach (var menuIndexToRemove in menuIndexesToRemove)
            {
                e.MenuItems.RemoveAt(menuIndexToRemove);
            }

            // Trim extra separators from the end
            while (e.MenuItems.Any() && e.MenuItems.Last().Kind == Microsoft.Web.WebView2.Core.CoreWebView2ContextMenuItemKind.Separator)
            {
                e.MenuItems.RemoveAt(e.MenuItems.Count - 1);
            }
        }
    }
}