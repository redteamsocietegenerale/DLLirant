using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DLLirant.NET.Classes
{
    internal class FileOperations
    {
        public static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
                try
                {
                    Directory.Delete(path, true);
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { MessageBox.Show("ERROR: The output directory is used by another process"); }
        }

        public static void CopyFileToDir(string file, string outputDir)
        {
            if (!File.Exists($"{outputDir}/{Path.GetFileName(file)}"))
                File.Copy(file, $"{outputDir}/{Path.GetFileName(file)}");
        }

        public static void RenameFile(string path, string newpath)
        {
            if (File.Exists(path) && !File.Exists(newpath))
                File.Move(path, newpath);
        }

        public static void CopyFilesDirToDir(string sourceDir, string targetDir, List<string> ignoreList = null)
        {
            if (Directory.Exists(sourceDir)) {
                foreach (string ignoreFile in ignoreList)
                {
                    foreach (string file in Directory.GetFiles(sourceDir))
                    {
                        if (Path.GetFileName(file) != ignoreFile)
                        {
                            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));
                        }
                    }
                }
            }
        }

        public static void RecreateDirectories(List<string> directories)
        {
            foreach (string dir in directories)
            {
                DeleteDirectory(dir);
                CreateDirectory(dir);
            }
        }
    }
}
