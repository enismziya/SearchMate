using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SearchMate
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {

        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderText.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;

        }
        private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }
    }
}