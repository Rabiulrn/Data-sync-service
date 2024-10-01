using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ApiFetcher
{
    public class FtpFileTransferGomoti
    {
        public async Task SyncTransfer(string ftpServer, string ftpUsername, string ftpPassword, string directoryPath)
        {
           await Transfer(ftpServer, ftpUsername, ftpPassword, directoryPath);
        }
        private async Task Transfer(string ftpServer, string ftpUsername, string ftpPassword,string directoryPath)
        {
            //string ftpServer = "ftp://ftp.example.com";
            //string ftpUsername = "username";
            //string ftpPassword = "password";
            //string directoryPath = "directory_path";
            string fileName = "";

            try
            {
                // Get the list of files in the directory
                DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
                FileInfo latestFile = directoryInfo.GetFiles("*MEGHNA_All_Transactions*.log")
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();

                if (latestFile != null)
                {
                    fileName = latestFile.Name;
                    string filePath = latestFile.FullName;
                    string ftpFilePath = $"{ftpServer}/{"Secondary"+fileName}";

                    // Create a WebClient object to handle FTP operations
                    WebClient ftpClient = new WebClient();
                    ftpClient.Credentials = new NetworkCredential(ftpUsername, ftpPassword);

                    // Upload the file
                    LogWritter($"Uploading file {fileName}...");

                    using (FileStream fs = File.OpenRead(filePath))
                    {
                        ftpClient.UploadFile(ftpFilePath, WebRequestMethods.Ftp.UploadFile, filePath);
                    }

                    LogWritter("File upload complete.");
                }
                else
                {
                    LogWritter("No files found in the directory.");
                }
            }
            catch (Exception ex)
            {
                LogWritter($"Error: {ex.Message}");
            }
        }
        public void LogWritter(string message)
        {
            string filename = DateTime.Now.ToString("yyyy_MM_dd") + "_MigrationLog.log";
            string appPath = $@"D:\\demolog";
            int index = appPath.LastIndexOf("\\");
            appPath = appPath.Remove(index);
            string path = Path.Combine(appPath, "FTP_Lg_Log" + DateTime.Now.Year.ToString());
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            File.AppendAllText(path + "\\" + filename, "\r\n" + DateTime.Now.ToString("dd HH:mm:ss.f") + "\t" + message);

        }
    }
}
