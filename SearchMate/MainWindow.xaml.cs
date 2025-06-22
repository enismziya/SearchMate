using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Windows.Threading;
using System.Collections.Concurrent;


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
            var found = new ConcurrentBag<string>();
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);

            Parallel.ForEach(drives, drive =>
            {
                try
                {
                    RecursiveSearch(drive.RootDirectory.FullName, searchText, found, 500);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            Console.WriteLine("Masaüstü: " + desktopPath);
            try
            {
                RecursiveSearch(desktopPath, searchText, found, 500);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return found.Distinct().ToList();
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }
    }
}