using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace xopp_Merger
{
    public partial class xoppFileMerger : Form
    {
        string[] fileNamesToMerge;
        string mergedFileName;

        public xoppFileMerger()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Xournal++ file (*.xopp)|*.xopp";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                fileNamesToMerge = openFileDialog.FileNames;
                foreach (string fileName in fileNamesToMerge)
                {
                    treeView1.Nodes.Add(new TreeNode(fileName));
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string tempPath = Path.GetTempPath();
            DirectoryInfo tempFolder = Directory.CreateDirectory(tempPath + "/xoppFileMerger/"); // create temp folder

            string mergedXmlString = "";

            progressBar1.Maximum = fileNamesToMerge.Length + 1; // Set number of steps as max value of progress

            for (int i = 0; i < fileNamesToMerge.Length; i++)
            {
                string fileName = fileNamesToMerge[i];

                // Store a copy of the files in the temp folder
                string tempFileName = tempFolder.FullName + Path.GetFileName(fileName);
                File.Copy(fileName, tempFileName, true); // overwrite if it exists

                // Decompress the files to xml
                using (FileStream compressedStream = File.Open(tempFileName, FileMode.Open))
                using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                using (var resultStream = new MemoryStream())
                {
                    zipStream.CopyTo(resultStream);
                    if (i == 0)
                    {
                        using (StreamReader streamReader = new StreamReader(resultStream))
                        {
                            // Read xml file as a string
                            resultStream.Seek(0, SeekOrigin.Begin);
                            string xmlString = streamReader.ReadToEnd();

                            // Remove the xournal ending tag
                            xmlString = xmlString.Replace("</xournal>","");

                            // Append to the output string
                            mergedXmlString += xmlString;
                        }
                    }
                    else
                    {
                        using (StreamReader streamReader = new StreamReader(resultStream))
                        {
                            // Read xml file as a string
                            resultStream.Seek(0, SeekOrigin.Begin);
                            string xmlString = streamReader.ReadToEnd();

                            // Remove first 4 lines and ending tag
                            var lines = Regex.Split(xmlString, "\r\n|\r|\n").Skip(4);
                            xmlString = string.Join(Environment.NewLine, lines.ToArray());
                            xmlString = xmlString.Replace("</xournal>", "");

                            // Append to the output string
                            mergedXmlString += xmlString;
                        }
                    }
                }

                // Update progress
                progressBar1.Value = i;
            }

            // Add the ending tag to the string and remove empty lines
            mergedXmlString += "\n</xournal>";
            mergedXmlString = Regex.Replace(mergedXmlString, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);

            // Save string and compress to xopp file
            using (var compressedStream = new FileStream(tempFolder + "merged.xopp", FileMode.Create))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                var data = Encoding.ASCII.GetBytes(mergedXmlString);
                zipStream.Write(data, 0, data.Length);
            }
            mergedFileName = tempFolder + "merged.xopp";

            // Process complete
            progressBar1.Value = progressBar1.Maximum;
            MessageBox.Show("Merge completed!");

            // Save the merged file
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Xournal++ file (*.xopp)|*.xopp";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (!string.IsNullOrEmpty(mergedFileName))
                {
                    File.Copy(mergedFileName, saveFileDialog.FileName, true); // overwrite if user chooses to
                }
                else
                {
                    MessageBox.Show("No merged file found. Try merging again.");
                }
            }

            // Cleanup
            foreach(FileInfo file in tempFolder.EnumerateFiles())
            {
                file.Delete();
            }
            tempFolder.Delete();
        }
    }
}
