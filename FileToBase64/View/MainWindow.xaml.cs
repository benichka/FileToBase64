using FileToBase64.ViewModel;
using System.Threading.Tasks;
using System.Windows;

namespace FileToBase64.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>Page view model</summary>
        private MainWindowViewModel ViewModel { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();
        }

        /// <summary>
        /// Handle the file drop onto the "file to clipboard" button.
        /// </summary>
        /// <param name="sender">UI element that sent the event.</param>
        /// <param name="e">Event information.</param>
        private async void DropToSerialise(object sender, DragEventArgs e)
        {
            // Try to get the file that was dragged onto the button.
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                await ((MainWindowViewModel)this.DataContext).HandleProvidedFile(e);
            }
        }
    }
}
