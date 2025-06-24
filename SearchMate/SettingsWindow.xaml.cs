using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace SearchMate
{
    public partial class SettingsWindow : Window
    {
        public int MaxSearchResult { get; set; }
        public int MaxCacheFiles { get; set; }
        public int ExpirationTimeDays { get; set; }
        public bool EnableCache { get; set; }
        public bool AutoCleanCache { get; set; }
        public bool IsDarkTheme { get; set; }

        private readonly int[] allowedResults = { 500, 1000, 2000, 5000 };

        public SettingsWindow(int maxSearchResult, int maxCacheFiles, int expirationTimeDays, bool enableCache = true, bool autoCleanCache = true, bool isDarkTheme = false)
        {
            InitializeComponent();
            ApplyTheme(isDarkTheme);
            int idx = 0;
            for (int i = 0; i < allowedResults.Length; i++)
                if (allowedResults[i] == maxSearchResult) idx = i;
            MaxSearchResultCombo.SelectedIndex = idx;
            MaxCacheFilesBox.Text = maxCacheFiles.ToString();
            ExpirationTimeBox.Text = expirationTimeDays.ToString();
            EnableCacheBox.IsChecked = enableCache;
            AutoCleanCacheBox.IsChecked = autoCleanCache;
            SetCacheFieldsEnabled(enableCache);
            SetExpirationEnabled(enableCache && autoCleanCache);
            DarkThemeBox.IsChecked = isDarkTheme;
        }

        private void ApplyTheme(bool isDark)
        {
            var app = Application.Current;
            if (app == null) return;
            var dicts = app.Resources.MergedDictionaries;
            dicts.Clear();
            var themeDict = new ResourceDictionary();
            themeDict.Source = new System.Uri(isDark ? "DarkThemeResources.xaml" : "LightThemeResources.xaml", System.UriKind.Relative);
            dicts.Add(themeDict);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            bool hasError = false;
            MaxCacheFilesError.Visibility = Visibility.Collapsed;
            ExpirationTimeError.Visibility = Visibility.Collapsed;
            MaxCacheFilesBox.Tag = null;
            ExpirationTimeBox.Tag = null;

            if (!int.TryParse(MaxCacheFilesBox.Text, out int maxCache) || maxCache <= 0)
            {
                MaxCacheFilesError.Text = "Please enter a valid number.";
                MaxCacheFilesError.Visibility = Visibility.Visible;
                MaxCacheFilesBox.Tag = "error";
                hasError = true;
            }
            if (!int.TryParse(ExpirationTimeBox.Text, out int expDays) || expDays <= 0)
            {
                ExpirationTimeError.Text = "Please enter a valid number.";
                ExpirationTimeError.Visibility = Visibility.Visible;
                ExpirationTimeBox.Tag = "error";
                hasError = true;
            }
            if (hasError)
            {
                return;
            }

            if (MaxSearchResultCombo.SelectedItem is ComboBoxItem item &&
                int.TryParse(item.Content.ToString(), out int maxSearch))
            {
                MaxSearchResult = maxSearch;
                MaxCacheFiles = maxCache;
                ExpirationTimeDays = expDays;
                EnableCache = EnableCacheBox.IsChecked == true;
                AutoCleanCache = AutoCleanCacheBox.IsChecked == true;
                IsDarkTheme = DarkThemeBox.IsChecked == true;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Please enter valid numbers for all fields.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void EnableCacheBox_Checked(object sender, RoutedEventArgs e)
        {
            SetCacheFieldsEnabled(true);
            SetExpirationEnabled(AutoCleanCacheBox.IsChecked == true);
        }
        private void EnableCacheBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetCacheFieldsEnabled(false);
            SetExpirationEnabled(false);
        }
        private void SetCacheFieldsEnabled(bool enabled)
        {
            MaxCacheFilesBox.IsEnabled = enabled;
            AutoCleanCacheBox.IsEnabled = enabled;
            if (!enabled)
            {
                ExpirationTimeBox.IsEnabled = false;
            }
        }
        private void AutoCleanCacheBox_Checked(object sender, RoutedEventArgs e)
        {
            if (EnableCacheBox.IsChecked == true)
                SetExpirationEnabled(true);
        }
        private void AutoCleanCacheBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetExpirationEnabled(false);
        }
        private void SetExpirationEnabled(bool enabled)
        {
            ExpirationTimeBox.IsEnabled = enabled;
        }
        private void ClearCache_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear all cache?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    SearchCache.ClearAll();
                    MessageBox.Show("All cache cleared.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch
                {
                    MessageBox.Show("Failed to clear cache.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MaxCacheFilesBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            MaxCacheFilesError.Visibility = Visibility.Collapsed;
            MaxCacheFilesBox.Tag = null;
            if (!string.IsNullOrWhiteSpace(MaxCacheFilesBox.Text) && (!int.TryParse(MaxCacheFilesBox.Text, out int val) || val <= 0))
            {
                MaxCacheFilesError.Text = "Please enter a valid number.";
                MaxCacheFilesError.Visibility = Visibility.Visible;
                MaxCacheFilesBox.Tag = "error";
            }
        }
        private void ExpirationTimeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ExpirationTimeError.Visibility = Visibility.Collapsed;
            ExpirationTimeBox.Tag = null;
            if (!string.IsNullOrWhiteSpace(ExpirationTimeBox.Text) && (!int.TryParse(ExpirationTimeBox.Text, out int val) || val <= 0))
            {
                ExpirationTimeError.Text = "Please enter a valid number.";
                ExpirationTimeError.Visibility = Visibility.Visible;
                ExpirationTimeBox.Tag = "error";
            }
        }
    }
}
