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
        private const string SettingsFile = "settings.json";
        private SettingsData _settings = null!;
        private readonly DispatcherTimer _searchDelayTimer;
        private string _pendingSearchText = string.Empty;
        public int maxSearchResult;

        public MainWindow()
        {
            LoadSettings();
            InitializeComponent();
            _searchDelayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _searchDelayTimer.Tick += SearchDelayTimer_Tick;
            ApplySettings();
        }

        private void LoadSettings()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    var json = File.ReadAllText(SettingsFile);
                    _settings = JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
                }
                catch { _settings = new SettingsData(); }
            }
            else
            {
                _settings = new SettingsData();
            }
        }

        private void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch { }
        }

        private void ApplySettings()
        {
            maxSearchResult = _settings.MaxSearchResult;
            SearchCache.MaxCacheFiles = _settings.MaxCacheFiles;
            SearchCache.ExpirationTime = _settings.AutoCleanCache ? TimeSpan.FromDays(_settings.ExpirationTimeDays) : TimeSpan.Zero;
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

        private async void SearchDelayTimer_Tick(object? sender, EventArgs e)
        {
            _searchDelayTimer.Stop();
            await SearchFilesAsync(_pendingSearchText);
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
            if (string.IsNullOrWhiteSpace(searchText))
                return new List<string>();

            if (searchText.Length >= 3 && _settings.EnableCache && SearchCache.TryLoad(searchText, out var cached))
            {
                cached = cached.Where(File.Exists).ToList();
                if (cached.Count > 0)
                    return cached;
            }

            var found = new ConcurrentBag<string>();
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);

            Parallel.ForEach(drives, drive =>
            {
                try { RecursiveSearch(drive.RootDirectory.FullName, searchText, found, maxSearchResult); } catch { }
            });

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            try { RecursiveSearch(desktopPath, searchText, found, maxSearchResult); } catch { }

            var resultList = found.Distinct().ToList();

            if (resultList.Count > 0 && searchText.Length >= 3 && _settings.EnableCache)
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
            if (ResultsList.SelectedItem is string filePath)
            {
                if (File.Exists(filePath))
                {
                    var argument = $"/select,\"{filePath}\"";
                    System.Diagnostics.Process.Start("explorer.exe", argument);
                }
                else
                {
                    MessageBox.Show("This file is no longer exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    string currentSearch = _pendingSearchText;
                    if (!string.IsNullOrWhiteSpace(currentSearch))
                    {
                        SearchCache.RemoveFromCache(currentSearch, filePath);
                        var updatedResults = ((List<string>)ResultsList.ItemsSource).Where(f => f != filePath).ToList();
                        ResultsList.ItemsSource = updatedResults;
                    }
                }
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            bool wasCacheEnabled = _settings.EnableCache;

            var settingsWindow = new SettingsWindow(
                _settings.MaxSearchResult,
                _settings.MaxCacheFiles > 0 ? _settings.MaxCacheFiles : 100,
                _settings.ExpirationTimeDays > 0 ? _settings.ExpirationTimeDays : 30,
                _settings.EnableCache,
                _settings.AutoCleanCache);
            settingsWindow.Owner = this;
            if (settingsWindow.ShowDialog() == true)
            {
                _settings.MaxSearchResult = settingsWindow.MaxSearchResult;
                _settings.MaxCacheFiles = settingsWindow.MaxCacheFiles;
                _settings.ExpirationTimeDays = settingsWindow.ExpirationTimeDays;
                _settings.EnableCache = settingsWindow.EnableCache;
                _settings.AutoCleanCache = settingsWindow.AutoCleanCache;

                if (wasCacheEnabled && !_settings.EnableCache)
                {
                    SearchCache.ClearAll();
                }

                SaveSettings();
                ApplySettings();
            }
        }
    }

    public static class SearchCache
    {
        private static readonly string cacheFolder = "cache";
        public static int MaxCacheFiles = 100;
        public static TimeSpan ExpirationTime = TimeSpan.FromDays(30);

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
                File.SetLastWriteTime(path, DateTime.Now);
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
            CleanupOldCacheFiles();
            string path = GetCacheFilePath(searchText);
            try
            {
                string json = JsonSerializer.Serialize(results);
                File.WriteAllText(path, json);
            }
            catch { }
        }

        public static void RemoveFromCache(string searchText, string filePathToRemove)
        {
            string path = GetCacheFilePath(searchText);
            if (!File.Exists(path)) return;
            try
            {
                var json = File.ReadAllText(path);
                var list = JsonSerializer.Deserialize<List<string>>(json);
                if (list == null) return;
                list.RemoveAll(f => f == filePathToRemove);
                File.WriteAllText(path, JsonSerializer.Serialize(list));
            }
            catch { }
        }

        public static void ClearAll()
        {
            try
            {
                if (Directory.Exists(cacheFolder))
                {
                    foreach (var file in Directory.GetFiles(cacheFolder, "*.json"))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch { }
        }

        private static void CleanupOldCacheFiles()
        {
            var files = Directory.GetFiles(cacheFolder, "*.json")
                .Select(path => new FileInfo(path))
                .OrderBy(f => f.LastWriteTime)
                .ToList();
            if (ExpirationTime > TimeSpan.Zero)
            {
                foreach (var file in files)
                {
                    if (DateTime.Now - file.LastWriteTime > ExpirationTime)
                    {
                        try { file.Delete(); } catch { }
                    }
                }
            }
            if (MaxCacheFiles > 0)
            {
                while (files.Count >= MaxCacheFiles)
                {
                    try
                    {
                        files[0].Delete();
                        files.RemoveAt(0);
                    }
                    catch { break; }
                }
            }
        }

        private static string GetCacheFilePath(string searchText)
        {
            string fileName = searchText.ToLower().Replace(" ", "_") + ".json";
            return Path.Combine(cacheFolder, fileName);
        }
    }

    public class SettingsData
    {
        public int MaxSearchResult { get; set; } = 1000;
        public int MaxCacheFiles { get; set; } = 100;
        public int ExpirationTimeDays { get; set; } = 30;
        public bool EnableCache { get; set; } = true;
        public bool AutoCleanCache { get; set; } = true;
    }
}
