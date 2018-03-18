using FileToBase64.Command;
using FileToBase64.Helper;
using log4net;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows.Forms;

namespace FileToBase64.ViewModel
{
    /// <summary>
    /// ViewModel for the MainWindow.
    /// </summary>
    internal class MainWindowViewModel: ViewModelBase
    {
        /// <summary>Logger.</summary>
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private bool _ConvertInProgress;
        /// <summary>Boolean indicating whether a serialisation or deserialisation is in progress.</summary>
        public bool ConvertInProgress
        {
            get { return this._ConvertInProgress; }
            set
            {
                SetProperty(CheckCommands, ref this._ConvertInProgress, value);
            }
        }

        #region Properties
        private FileInfo _File;
        /// <summary>File to be serialised/deserialised to base64.</summary>
        public FileInfo File
        {
            get => this._File;
            private set
            {
                SetProperty(CheckCommands, ref this._File, value);
                if (value != null)
                {
                    _log.Debug($"File was set. Value: \"{value.FullName}\".");
                    FolderPath = File.Name;
                }
            }
        }

        private string _FolderPath;
        /// <summary>Folder path for the target file.</summary>
        public string FolderPath
        {
            get => this._FolderPath;
            private set
            {
                _log.Debug($"FolderPath was set. Value: \"{value}\".");
                SetProperty(CheckCommands, ref this._FolderPath, value);
            }
        }

        private string _Information;
        /// <summary>Information text.</summary>
        public string Information
        {
            get => this._Information;
            private set
            {
                _log.Debug($"Information was set. Value: \"{value}\".");
                SetProperty(CheckCommands, ref this._Information, value);
            }
        }
        #endregion Properties

        #region Commands
        /// <summary>Command associated with the button to choose the file to serialise to base64.</summary>
        public CommandHandler SelectFileToSerialiseCommand { get; private set; }

        /// <summary>Command associated with the button to convert the clipboard to a file.</summary>
        public CommandHandler ConvertClipboardToFileCommand { get; private set; }

        /// <summary>Command associated with the "browse..." button.</summary>
        public CommandHandler SelectTargetPathCommand { get; private set; }
        #endregion Commands

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MainWindowViewModel()
        {
            _log.Debug("Application started successfully.");

            // When the app launch, we know that no serialisation or deserialisation is in progress.
            ConvertInProgress = false;

            // Initialise all commands.
            SelectFileToSerialiseCommand = new CommandHandler(ChooseFileToSerialise, () => !ConvertInProgress);

            ConvertClipboardToFileCommand = new CommandHandler(ConvertClipboardToFile, () => !ConvertInProgress);

            SelectTargetPathCommand = new CommandHandler(SelectTargetPath, () => !ConvertInProgress);

            _log.Debug("All commands instanciated.");

            // When the application starts, we set the target folder to the desktop.
            FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        /// <summary>
        /// Choose the file to be serialised and serialises it to the clipboard.
        /// </summary>
        private async void ChooseFileToSerialise()
        {
            Information = "File selection...";
            _log.Debug("file selection.");

            // TODO: select the source file in an async way.
            // -> right now, if we await it, we've got an error " task Current thread must be set to single thread apartment (STA)".
            //string sourceFileFullPath = await Task.Run(() => SelectSourceFile());
            string sourceFileFullPath = SelectSourceFile();

            ConvertInProgress = true;

            if (sourceFileFullPath != null)
            {
                _log.Debug($"File chosen for serialisation: {sourceFileFullPath}.");

                File = new FileInfo(sourceFileFullPath);

                await SerialiseFileToClipboard();
            }
            else
            {
                Information = string.Empty;
                _log.Debug("no file were selected.");
            }

            ConvertInProgress = false;

            CheckCommands();
        }

        /// <summary>
        /// Process the file that has been directly provided to the application (via a drag and drop for instance).
        /// </summary>
        /// <param name="e">Event arguments sent to the object that called the method. In there should be the path to the file to serialise.</param>
        public async Task HandleProvidedFile(EventArgs e)
        {
            switch (e.GetType().Name)
            {
                case nameof(DragEventArgs):
                    Information = $"Extracting file name to serialise to base64...";
                    _log.Debug("Files were drag and dropped.");

                    // You can have more than one file.
                    string[] files = (string[])((System.Windows.DragEventArgs)e).Data.GetData(DataFormats.FileDrop);
                    if (files.Length > 1)
                    {
                        Information = "Only 1 file can be drag and dropped.";
                        _log.Warn($"Too many files were drag and dropped: expected 1, got {files.Length}. Files were: {String.Join(", ", files)}.");
                    }
                    else
                    {
                        File = new FileInfo(files[0]);
                        Information = File.Name;
                        _log.Info($"File {File.FullName} has been drag and dropped to the application.");

                        await SerialiseFileToClipboard();
                    }
                    break;
                default:
                    Information = "Unsupported file processing.";
                    _log.Warn($"Unsupported file processing; received event of type {e.GetType().Name}.");
                    break;
            }
        }

        /// <summary>
        /// Serialise the chosen file to the clipboard.
        /// </summary>
        private async Task SerialiseFileToClipboard()
        {
            Information = "Conversion in progress...";
            _log.Info($"Starting conversion of {File.FullName}.");
            try
            {
                var result = await Base64ToXmlHelper.FileToXml(File);
                _log.Info($"{File.FullName} was successfully serialised.");

                _log.Debug("Trying to copy the serialised file to the clipboard.");
                try
                {
                    System.Windows.Clipboard.SetDataObject(result);

                    Information = "File serialised; content copied to clipboard.";
                    _log.Info("Serialised file copied to the clipboard.");
                }
                catch (Exception ex)
                {
                    Information = "Error while trying to copy the result to the clipboard.";
                    _log.Error($"Error while trying to copy the result to the clipboard: {ex.ToString()}");
                }
            }
            catch (Exception ex)
            {
                Information = "Problem during processing.";
                _log.Error($"Problem when serialising the file: {ex.ToString()}.");
            }
        }

        /// <summary>
        /// Convert the clipboard content to a file.<para />
        /// The clipboard content is checked first.
        /// </summary>
        private async void ConvertClipboardToFile()
        {
            // If a file were chosen, the path is the file name. It's not a folder path, so
            // the program will crash (it will try to search a folder with the name and extension
            // of the file name).
            // -> We null the File and reset the folder path to the desktop.
            if (File != null)
            {
                _log.Info(  "A file was previously set (a file was previously serialised) and the destination folder didn't change." +
                            "The target folder will be set to the desktop.");
                File = null;
                FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            ConvertInProgress = true;

            _log.Debug("Trying to read the clipboard content.");
            try
            {
                var serialisedContentAsString = System.Windows.Clipboard.GetDataObject().GetData(DataFormats.StringFormat) as string;
                _log.Debug("Serialised file retrieved from the clipboard.");

                Information = "Deserialising file...";
                _log.Info("Deserialising file.");

                string targetFileName = await Base64ToXmlHelper.XmlToFile(serialisedContentAsString, FolderPath);

                Information = "File deserialised.";
                _log.Info($"File has been deserialised to {targetFileName}.");
            }
            catch (IOException ex)
            {
                Information = "Problem creating the file.";
                _log.Error($"Problem creating the file: {ex.ToString()}.");
            }
            catch (FormatException ex)
            {
                Information = "Problem reading the clipboard data.";
                _log.Error($"Problem reading the clipboard data: {ex.ToString()}.");
            }
            catch (Exception ex)
            {
                Information = "Problem during processing.";
                _log.Error($"Problem during processing: {ex.ToString()}.");
            }

            ConvertInProgress = false;

            CheckCommands();
        }

        /// <summary>
        /// Open a window to choose a folder where the deserialised file will be saved.
        /// </summary>
        private void SelectTargetPath()
        {
            // TODO: call the folder browser dialog using the MVVM pattern.

            var fbd = new FolderBrowserDialog();

            fbd.SelectedPath = FolderPath;

            var result = fbd.ShowDialog();

            if (result == DialogResult.OK)
            {
                FolderPath = fbd.SelectedPath.ToString();

                // In order to know that the folder path is different from the one chosen for an input file,
                // File is set to null.
                File = null;
            }
        }

        /// <summary>
        /// Open a window to choose a file to serialise to base 64 and to put into the clipboard.
        /// </summary>
        /// <returns>The selected file path if the user chose one, null otherwise.</returns>
        private string SelectSourceFile()
        {
            var fbd = new Microsoft.Win32.OpenFileDialog();

            // When selecting a shortcut, we want to serialise the shortcut and not the target.
            fbd.DereferenceLinks = false;

            fbd.InitialDirectory = FolderPath;

            var result = fbd.ShowDialog();

            if (result.HasValue && result.Value)
            {
                return fbd.FileName;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if every command is executable.
        /// </summary>
        private void CheckCommands()
        {
            SelectFileToSerialiseCommand.RaiseCanExecuteChanged();
            ConvertClipboardToFileCommand.RaiseCanExecuteChanged();
            SelectTargetPathCommand.RaiseCanExecuteChanged();
        }
    }
}
