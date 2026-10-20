using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace mydesktop
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer clockTimer = new DispatcherTimer();

        // Prompt and input limit
        private const string Prompt = "> ";
        private const int MaxInputLength = 10; // only 10 characters allowed after prompt

        public MainWindow()
        {
            InitializeComponent();

            clockTimer.Interval = TimeSpan.FromSeconds(1);
            clockTimer.Tick += UpdateClock;
            clockTimer.Start();

            UpdateClock(null, null);

            // Ensure command box starts with prompt and limit is set
            CommandBox.Text = Prompt;
            CommandBox.CaretIndex = CommandBox.Text.Length;
            CommandBox.MaxLength = Prompt.Length + MaxInputLength;

            // Hook previews to protect the prompt and enforce max input length
            CommandBox.PreviewKeyDown += CommandBox_PreviewKeyDown;
            CommandBox.PreviewTextInput += CommandBox_PreviewTextInput;
            DataObject.AddPastingHandler(CommandBox, new DataObjectPastingEventHandler(CommandBox_OnPaste));
            CommandBox.GotFocus += CommandBox_GotFocus;
        }

        private void UpdateClock(object? sender, EventArgs? e)
        {
            DateTime now = DateTime.Now;

            ClockText.Text = now.ToString("HH:mm:ss");
            DateText.Text = now.ToString("dd MMMM yyyy");
            DayText.Text = now.ToString("dddd");
        }

        private void CommandBox_GotFocus(object? sender, RoutedEventArgs e)
        {
            EnsurePromptAndCaret();
        }

        private void EnsurePromptAndCaret()
        {
            if (string.IsNullOrEmpty(CommandBox.Text) || !CommandBox.Text.StartsWith(Prompt))
            {
                CommandBox.Text = Prompt;
            }

            if (CommandBox.CaretIndex < Prompt.Length)
            {
                CommandBox.CaretIndex = CommandBox.Text.Length;
            }
        }

        private void CommandBox_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            // Protect the prompt from deletion/movement and handle Escape
            EnsurePromptAndCaret();

            int selStart = CommandBox.SelectionStart;
            int selLength = CommandBox.SelectionLength;

            // Prevent Backspace deleting the prompt
            if (e.Key == Key.Back)
            {
                if (selLength > 0)
                {
                    // If selection starts before prompt, block deleting prompt
                    if (selStart < Prompt.Length)
                    {
                        // move selection start to end of prompt
                        CommandBox.SelectionStart = Prompt.Length;
                        CommandBox.SelectionLength = Math.Max(0, selLength - (Prompt.Length - selStart));
                    }

                    if (CommandBox.SelectionStart < Prompt.Length)
                    {
                        e.Handled = true;
                        return;
                    }
                }
                else
                {
                    if (CommandBox.CaretIndex <= Prompt.Length)
                    {
                        e.Handled = true;
                        return;
                    }
                }
            }
            // Prevent Delete affecting prompt
            else if (e.Key == Key.Delete)
            {
                if (selStart < Prompt.Length)
                {
                    e.Handled = true;
                    return;
                }
            }
            // Prevent moving caret left into prompt area
            else if (e.Key == Key.Left)
            {
                if (CommandBox.CaretIndex <= Prompt.Length && CommandBox.SelectionLength == 0)
                {
                    e.Handled = true;
                    return;
                }
            }
            // Home should go to just after prompt
            else if (e.Key == Key.Home)
            {
                CommandBox.CaretIndex = Prompt.Length;
                e.Handled = true;
                return;
            }
            // Escape resets to prompt (prevents the prompt from vanishing)
            else if (e.Key == Key.Escape)
            {
                CommandBox.Text = Prompt;
                CommandBox.CaretIndex = CommandBox.Text.Length;
                e.Handled = true;
                return;
            }

            // Let other keys through. Text input length is enforced in PreviewTextInput and Paste handler.
        }

        private void CommandBox_PreviewTextInput(object? sender, TextCompositionEventArgs e)
        {
            EnsurePromptAndCaret();

            int selStart = CommandBox.SelectionStart;
            int selLength = CommandBox.SelectionLength;

            string currentInput = CommandBox.Text.Length > Prompt.Length ? CommandBox.Text.Substring(Prompt.Length) : string.Empty;
            int inputStartIndex = Math.Max(0, selStart - Prompt.Length);
            int selectionLengthInInput = Math.Max(0, Math.Min(selLength, currentInput.Length - inputStartIndex));

            int newLength = currentInput.Length - selectionLengthInInput + e.Text.Length;
            if (newLength > MaxInputLength)
            {
                e.Handled = true; // reject extra characters
            }
        }

        private void CommandBox_OnPaste(object? sender, DataObjectPastingEventArgs e)
        {
            if (!e.SourceDataObject.GetDataPresent(DataFormats.Text)) { e.CancelCommand(); return; }

            string paste = e.SourceDataObject.GetData(DataFormats.Text) as string ?? string.Empty;
            EnsurePromptAndCaret();

            int selStart = CommandBox.SelectionStart;
            int selLength = CommandBox.SelectionLength;

            string currentInput = CommandBox.Text.Length > Prompt.Length ? CommandBox.Text.Substring(Prompt.Length) : string.Empty;
            int inputStartIndex = Math.Max(0, selStart - Prompt.Length);
            int selectionLengthInInput = Math.Max(0, Math.Min(selLength, currentInput.Length - inputStartIndex));

            int newLength = currentInput.Length - selectionLengthInInput + paste.Length;
            if (newLength > MaxInputLength)
            {
                int allowed = MaxInputLength - (currentInput.Length - selectionLengthInInput);
                if (allowed <= 0)
                {
                    e.CancelCommand();
                    return;
                }

                // Insert truncated paste manually and cancel default paste
                string truncated = paste.Substring(0, allowed);
                int insertPos = selStart;
                CommandBox.Text = CommandBox.Text.Remove(selStart, selLength).Insert(insertPos, truncated);
                CommandBox.CaretIndex = insertPos + truncated.Length;
                e.CancelCommand();
            }
            // else let paste happen normally
        }

        private void CommandBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            string raw = CommandBox.Text ?? "";
            string command = raw
                .Replace(">", "")
                .Trim()
                .ToUpperInvariant();

            // Echo the command to output (keeps history and feedback)
            AppendOutput($"> {raw.Trim()}");

            switch (command)
            {
                case "F":
                    LaunchApp("Firefox");
                    break;

                case "C":
                    LaunchApp("Cuphead");
                    break;

                case "TT":
                    LaunchApp("Turing Complete");
                    break;

                case "TH":
                    LaunchApp("Thonny");
                    break;

                case "BL":
                    LaunchApp("Blender");
                    break;

                case "A":
                    LaunchApp("Arduino");
                    break;

                case "CA":
                    LaunchApp("Canva");
                    break;

                case "HELP":
                    ShowHelp();
                    break;

                case "CLEAR":
                    OutputText.Clear();
                    break;

                case "APPS":
                    ShowDiscoveredApps();
                    break;

                case "SAVEAPPS":
                    try
                    {
                        AppDiscovery.SaveApplications();
                        AppendOutput("Applications saved to AppLibrary.json.");
                    }
                    catch (Exception ex)
                    {
                        AppendOutput($"Failed to save applications: {ex.Message}");
                    }
                    break;

                case "":
                    // do nothing for empty command
                    break;

                default:
                    AppendOutput($"Unknown command: {command}");
                    break;
            }

            // Reset command line
            CommandBox.Text = Prompt;
            CommandBox.CaretIndex = CommandBox.Text.Length;

            e.Handled = true;
        }

        private void ShowDiscoveredApps()
        {
            var apps = AppDiscovery.FindApplications();

            string result = $"Found {apps.Count} applications:\n\n";

            foreach (var app in apps)
            {
                result += $"{app.Name}   [{app.Type}]\n";
            }

            OutputText.Text = result;
        }

        private void ShowHelp()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Available commands:");
            sb.AppendLine();
            sb.AppendLine("F     Firefox          C     Cuphead");
            sb.AppendLine("CA    Canva            TT    Turing Complete");
            sb.AppendLine("TH    Thonny           BL    Blender");
            sb.AppendLine("A     Arduino IDE");
            sb.AppendLine();
            sb.AppendLine("HELP  Show commands");
            sb.AppendLine("CLEAR Clear command area");

            AppendOutput(sb.ToString());
        }

        /// <summary>
        /// Appends a line to the output box and scrolls to the end.
        /// Keeps a running history instead of replacing the content each time.
        /// </summary>
        private void AppendOutput(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            // Ensure we append on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => AppendOutput(text));
                return;
            }

            // Normalize line endings and guarantee trailing newline
            string normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
            if (!normalized.EndsWith("\n"))
                normalized += "\n";

            OutputText.AppendText(normalized);
            OutputText.CaretIndex = OutputText.Text.Length;
            OutputText.ScrollToEnd();
        }

        private void LaunchApp(string appName)
        {
            AppendOutput($"Searching desktop for: {appName} ...");

            string desktopPath = Environment.GetFolderPath(
                Environment.SpecialFolder.DesktopDirectory
            );

            if (!Directory.Exists(desktopPath))
            {
                AppendOutput("Desktop folder not found.");
                return;
            }

            string[] files = Directory.GetFiles(desktopPath);

            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);

                if (fileName.Contains(
                    appName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    AppendOutput($"Launching {fileName}...");
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = file,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        AppendOutput($"Failed to launch: {ex.Message}");
                    }

                    return;
                }
            }

            AppendOutput($"Couldn't find {appName} on the Desktop.");
        }
    }
}