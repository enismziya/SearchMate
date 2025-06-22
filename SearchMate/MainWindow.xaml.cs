using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Windows.Threading;
using System.Collections.Concurrent;
using System.Text.Json;

namespace SearchMate
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            _searchDelayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _searchDelayTimer.Tick += SearchDelayTimer_Tick;
        }

        private readonly DispatcherTimer _searchDelayTimer;
        private string _pendingSearchText = string.Empty;

        private async void SearchDelayTimer_Tick(object sender, EventArgs e)
        {
            _searchDelayTimer.Stop();
            await SearchFilesAsync(_pendingSearchText);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderText.Visibility = string.IsNullOrEmpty(SearchBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;
            _pendingSearchText = SearchBox.Text;
            _searchDelayTimer.Stop();
            if (!string.IsNullOrWhiteSpace(_pendingSearchText))
                _searchDelayTimer.Start();
            else
                ResultsList.ItemsSource = null;
        }

        private async Task SearchFilesAsync(string searchText)
        {
            LoadingBar.Visibility = Visibility.Visible;
            ResultsList.ItemsSource = null;
            await Task.Delay(100);
            var results = await Task.Run(() => FindFiles(searchText));
            ResultsList.ItemsSource = results;
            LoadingBar.Visibility = Visibility.Collapsed;
        }

        private List<string> FindFiles(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText) || searchText.Length < 3)
                return new List<string>();

            if (SearchCache.TryLoad(searchText, out var cached))
            {
                Console.WriteLine("\uD83D\uDD04 Cache'ten alındı.");
                return cached;
            }

            var found = new ConcurrentBag<string>();
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);

            Parallel.ForEach(drives, drive =>
            {
                try
                {
                    RecursiveSearch(drive.RootDirectory.FullName, searchText, found, 500);
                }
                catch { }
            });

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            try { RecursiveSearch(desktopPath, searchText, found, 500); } catch { }

            var resultList = found.Distinct().ToList();

            if (resultList.Count > 0)
                SearchCache.Save(searchText, resultList);

            return resultList;
        }

        private void RecursiveSearch(string directory, string searchText, ConcurrentBag<string> found, int maxResults)
        {
            if (found.Count >= maxResults) return;

            try
            {
                foreach (var file in Directory.EnumerateFiles(directory, "*" + searchText + "*"))
                {
                    found.Add(file);
                    if (found.Count >= maxResults) return;
                }

                Parallel.ForEach(Directory.EnumerateDirectories(directory), dir =>
                {
                    if (found.Count < maxResults)
                        RecursiveSearch(dir, searchText, found, maxResults);
                });
            }
            catch { }
        }

        private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ResultsList.SelectedItem is string filePath && File.Exists(filePath))
            {
                var argument = $"/select,\"{filePath}\"";
                System.Diagnostics.Process.Start("explorer.exe", argument);
            }
        }
    }

    public static class SearchCache
    {
        private static readonly string cacheFolder = "cache";

        static SearchCache()
        {
            if (!Directory.Exists(cacheFolder))
                Directory.CreateDirectory(cacheFolder);
        }

        public static bool TryLoad(string searchText, out List<string> results)
        {
            results = null;
            string path = GetCacheFilePath(searchText);

            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    results = JsonSerializer.Deserialize<List<string>>(json);
                    return true;
                }
                catch { }
            }
            return false;
        }

        public static void Save(string searchText, List<string> results)
        {
            if (results == null || results.Count == 0)
                return;

            string path = GetCacheFilePath(searchText);
            try
            {
                string json = JsonSerializer.Serialize(results);
                File.WriteAllText(path, json);
            }
            catch { }
        }

        private static string GetCacheFilePath(string searchText)
        {
            string fileName = searchText.ToLower().Replace(" ", "_") + ".json";
            return Path.Combine(cacheFolder, fileName);
        }
    }
}
