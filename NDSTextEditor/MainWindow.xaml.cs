using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NDSTextEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<TextBox> indexTextBoxes = new List<TextBox>();
        private List<TextBox> rowindexTextBoxes = new List<TextBox>();
        private List<TextBox> contentTextBoxes = new List<TextBox>();
        private Dictionary<TextBox, string> originalTexts = new Dictionary<TextBox, string>();
        private string filePath="";
        private List<Row> rows = new List<Row>();
        private string header = "";

        public MainWindow()
        {
            InitializeComponent();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private void AddTextBoxPairsDynamically()
        {
            foreach(Row r in rows)
            {
                // Create a horizontal stack panel to hold the pair of text boxes
                StackPanel pairPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5)
                };

                // Create the index text box
                TextBox indexTextBox = new TextBox
                {
                    Width = 75,
                    Margin = new Thickness(0, 0, 10, 0),
                    Text = r.Pointer,
                    IsReadOnly = true
                };

                TextBox rowindexTextBox = new TextBox
                {
                    Width = 75,
                    Margin = new Thickness(0, 0, 10, 0),
                    Text = r.Index,
                    IsReadOnly = true
                };

                // Create the text display text box
                TextBox displayTextBox = new TextBox
                {
                    Width = 600,
                    Text = r.Text,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap
                };
                displayTextBox.LostFocus += DisplayTextBox_LostFocus;

                // Store the original text
                originalTexts[displayTextBox] = displayTextBox.Text;

                // Add the text boxes to the pair panel
                pairPanel.Children.Add(indexTextBox);
                pairPanel.Children.Add(rowindexTextBox);
                pairPanel.Children.Add(displayTextBox);

                // Add the pair panel to the main container
                TextBoxContainer.Children.Add(pairPanel);

                // Keep track of the text boxes
                indexTextBoxes.Add(indexTextBox);
                rowindexTextBoxes.Add(rowindexTextBox);
                contentTextBoxes.Add(displayTextBox);
            }

            /*for (int i = 1; i <= 1389; i++)
            {
                // Create a horizontal stack panel to hold the pair of text boxes
                StackPanel pairPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(5)
                };

                // Create the index text box
                TextBox indexTextBox = new TextBox
                {
                    Width = 150,
                    Margin = new Thickness(0, 0, 10, 0),
                    Text = i.ToString(),
                    IsReadOnly = true
                };

                // Create the text display text box
                TextBox displayTextBox = new TextBox
                {
                    Width = 600,
                    Text = $"Text for index {i}",
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap
                };
                displayTextBox.LostFocus += DisplayTextBox_LostFocus;

                // Store the original text
                originalTexts[displayTextBox] = displayTextBox.Text;

                // Add the text boxes to the pair panel
                pairPanel.Children.Add(indexTextBox);
                pairPanel.Children.Add(displayTextBox);

                // Add the pair panel to the main container
                TextBoxContainer.Children.Add(pairPanel);

                // Keep track of the text boxes
                indexTextBoxes.Add(indexTextBox);
                contentTextBoxes.Add(displayTextBox);
            }*/
        }

        private void DisplayTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox changedTextBox)
            {
                // Get the original and new text
                string originalText = originalTexts[changedTextBox];
                string newText = changedTextBox.Text;

                // Calculate the difference in length
                int originalLength = CountVisibleCharacters(originalText);
                int newLength = CountVisibleCharacters(newText);
                int lengthDifference = newLength - originalLength;

                int changedIndex = contentTextBoxes.IndexOf(changedTextBox);

                // Update the original text
                originalTexts[changedTextBox] = newText;

                for (int i = changedIndex + 1; i < indexTextBoxes.Count; i++)
                {
                    /*if (int.TryParse(indexTextBoxes[i].Text, out int currentIndex))
                    {*/
                    if (indexTextBoxes[i].Text == "00000000")
                    {
                        continue;
                    }
                    indexTextBoxes[i].Text = AddToLittleEndian(indexTextBoxes[i].Text, lengthDifference); /*(currentIndex + lengthDifference).ToString();*/
                    //}
                }
            }
        }

        private int CountVisibleCharacters(string text)
        {
            // Count visible characters, treating new lines as one character each
            Encoding shiftJIS = Encoding.GetEncoding("shift-jis");
            return shiftJIS.GetByteCount(text.Replace("\r\n", "\n").Replace("\r", "\n"));
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = openFileDialog.FileName;
                filePath = openFileDialog.FileName;
            }
        }
        private void StartFilling(object sender, RoutedEventArgs e)
        {
            string firstValue = FirstValueTextBox.Text;
            if(filePath == "")
            {
                MessageBox.Show("Please select a file first.");
                return;
            }
            else if(firstValue == "")
            {
                MessageBox.Show("Please enter the first value.");
            }
            else
            {
                StreamReader sr = new StreamReader(filePath, Encoding.GetEncoding("shift-jis"));
                string text = sr.ReadToEnd();
                sr.Close();

                FileStream fs = new FileStream(filePath, FileMode.Open);

                List<String> strings = text.Split(new char[] { '\0' }, StringSplitOptions.None).ToList();

                header = strings[0]+'\0';

                int wantedRange = strings.IndexOf(firstValue);

                List<String> reps = strings.GetRange(0, wantedRange-1);

                string rep = string.Join("\0", reps);

                Encoding shiftJIS = Encoding.GetEncoding("shift-jis");

                int actualNumOfReps = (shiftJIS.GetByteCount(rep)+2)/8;

                strings.RemoveRange(0, wantedRange);

                byte[] buffer = new byte[4];

                // Skip 4 bytes
                fs.Read(buffer, 0, buffer.Length);

                for (int i = 0; i < actualNumOfReps; i++)
                {
                    Row row = new Row();

                    // Read 4 bytes for the Pointer
                    fs.Read(buffer, 0, buffer.Length);
                    row.Pointer = BitConverter.ToString(buffer).Replace("-", "");

                    // Read next 4 bytes for the Index
                    fs.Read(buffer, 0, buffer.Length);
                    row.Index = BitConverter.ToString(buffer).Replace("-", "");

                    rows.Add(row);
                }

                fs.Close();

                int counter = 0;

                foreach (Row r in rows)
                {
                    if (counter == 0)
                    {
                        counter++;
                        continue;
                    }
                    if (r.Pointer != "00000000")
                    {
                        r.Text = strings[counter - 1];
                        counter++;
                    }
                }
                rows[rows.Count - 1].Index = "";
                AddTextBoxPairsDynamically();
            }
        }

        private static string AddToLittleEndian(string pointer, int diff)
        {
            int chunkSize = 2;
            List<string> chunks = new List<string>();

            for (int i = 0; i < pointer.Length; i += chunkSize)
            {
                if (i + chunkSize <= pointer.Length)
                {
                    chunks.Add(pointer.Substring(i, chunkSize));
                }
                else
                {
                    // Add the remaining characters if they are less than chunkSize
                    chunks.Add(pointer.Substring(i));
                }
            }

            // Convert the list to an array if needed
            string[] result = chunks.ToArray();

            string bigEndian = "";

            for (int i = result.Length - 1; i >= 0; i--)
            {
                bigEndian += result[i];
            }

            int pointerInt = Convert.ToInt32(bigEndian, 16);

            pointerInt += diff;

            chunks.Clear();

            string added = pointerInt.ToString("X8");

            for (int i = 0; i < added.Length; i += chunkSize)
            {
                if (i + chunkSize <= added.Length)
                {
                    chunks.Add(added.Substring(i, chunkSize));
                }
                else
                {
                    // Add the remaining characters if they are less than chunkSize
                    chunks.Add(added.Substring(i));
                }
            }

            result = chunks.ToArray();

            string littleEndian = "";

            for (int i = result.Length - 1; i >= 0; i--)
            {
                littleEndian += result[i];
            }

            return littleEndian;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].Pointer = indexTextBoxes[i].Text;
                rows[i].Text = contentTextBoxes[i].Text;
            }
/*
            string fileContent = header;

            foreach (Row r in rows)
            {
                byte[] bytesPointer = HexStringToBytes(r.Pointer);
                byte[] bytesIndex = HexStringToBytes(r.Index);

                string pointer = Encoding.GetEncoding("shift_jis").GetString(bytesPointer);
                string index = Encoding.GetEncoding("shift_jis").GetString(bytesIndex);

                fileContent += pointer + index;
            }

            foreach (Row r in rows)
            {
                fileContent += r.Text + '\0';
            }*/
            // Create and configure the SaveFileDialog
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "MSG files (*.msg)|*.msg|All Files (*.*)|*.*", // Filter for file types
                Title = "Save a Text File"
            };

            // Show the SaveFileDialog and check if the user clicked "Save"
            if (saveFileDialog.ShowDialog() == true)
            {
                // Get the file path from the dialog
                string filePath = saveFileDialog.FileName;

                File.AppendAllText(filePath, header);

                foreach (Row r in rows)
                {
                    byte[] bytesPointer = HexStringToBytes(r.Pointer);
                    byte[] bytesIndex = HexStringToBytes(r.Index);
                    try
                    {
                        // Create and write to the file
                        using (FileStream fs = new FileStream(filePath, FileMode.Append, FileAccess.Write))
                        {
                            fs.Write(bytesPointer, 0, bytesPointer.Length);
                            if(r.Index != "")
                            {
                                fs.Write(bytesIndex, 0, bytesIndex.Length);
                            }
                        }
                    }
                    catch (IOException ex)
                    {
                        // Handle potential IO exceptions
                        MessageBox.Show($"An error occurred while saving the file: {ex.Message}");
                    }

                    /*string pointer = Encoding.GetEncoding("shift_jis").GetString(bytesPointer);
                    string index = Encoding.GetEncoding("shift_jis").GetString(bytesIndex);*/

                    //fileContent += pointer + index;
                }

                foreach (Row r in rows)
                {
                    try
                    {
                        if(r.Text == null || r.Text == "")
                        {
                            continue;
                        }
                        // Create and write to the file
                        using (var writer = new StreamWriter(filePath, true, Encoding.GetEncoding("shift-jis")))
                        {
                            writer.Write(r.Text.Replace("\r\n","\n"));
                            writer.Write("\0");
                        }/*
                        File.AppendAllText(filePath, r.Text);
                        File.AppendAllText(filePath, "\0");*/
                    }
                    catch (IOException ex)
                    {
                        // Handle potential IO exceptions
                        MessageBox.Show($"An error occurred while saving the file: {ex.Message}");
                    }

                    //fileContent += r.Text + '\0';
                }


                MessageBox.Show("File saved successfully!");

                /*try
                {
                    // Create and write to the file
                    File.WriteAllText(filePath, fileContent);
                    MessageBox.Show("File saved successfully!");
                }
                catch (IOException ex)
                {
                    // Handle potential IO exceptions
                    MessageBox.Show($"An error occurred while saving the file: {ex.Message}");
                }*/
            }
        }

        static byte[] HexStringToBytes(string hex)
        {
            int numberOfBytes = hex.Length / 2;
            byte[] bytes = new byte[numberOfBytes];

            for (int i = 0; i < numberOfBytes; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            return bytes;
        }
    }

    internal class Row
    {
        public string Pointer { get; set; }
        public string Text { get; set; }
        public string Index { get; set; }
    }
}