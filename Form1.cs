using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
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
        //string[] fileNamesToMerge;
        string mergedFileName;

        public xoppFileMerger()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Choose Files to Merge
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Xournal++ file (*.xopp)|*.xopp";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                //fileNamesToMerge = openFileDialog.FileNames;
                foreach (string fileName in openFileDialog.FileNames/*fileNamesToMerge*/)
                {
                    treeView1.Nodes.Add(new TreeNode(fileName));
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Clear
            treeView1.Nodes.Clear();
            progressBar1.Value = 0;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Check if any files were selected
            if (treeView1.Nodes == null || treeView1.Nodes.Count == 0)
            {
                MessageBox.Show("No files were selected!");
                return;
            }

            string tempPath = Path.GetTempPath();
            DirectoryInfo tempFolder = Directory.CreateDirectory(tempPath + "/xoppFileMerger/"); // create temp folder

            string mergedXmlString = "";

            progressBar1.Maximum = treeView1.Nodes.Count + 1; // Set number of steps as max value of progress

            for (int i = 0; i < treeView1.Nodes.Count; i++)
            {
                string fileName = treeView1.Nodes[i].Text;

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

                            // Remove starting tags and ending tag
                            var lines = Regex.Split(xmlString, "\r\n|\r|\n");
                            lines = lines.Where(line => !Regex.IsMatch(line, "\\<\\?xml.*\\?\\>|\\<xournal.*\\>|\\<title\\>.*\\<\\/title\\>|\\<preview\\>.*\\<\\/preview\\>")).ToArray();
                            xmlString = string.Join("\n", lines);
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
