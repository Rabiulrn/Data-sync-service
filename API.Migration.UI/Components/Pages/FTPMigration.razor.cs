
using ApiFetcher;
using Entities.Models;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace API.Migration.UI.Components.Pages
{
    public partial class FTPMigration
    {
       
        public string ftpServer { get; set; }
        public string ftpUsername { get; set; }
        public string ftpPassword { get; set; }
        public string directoryPath { get; set; }
        protected async Task OnClick(string text)
        {
          
            try
            {
                
                if (text == "FTP")
                {
                    
                   //FtpFileTransfer fetcher = new FtpFileTransfer();
                   //await fetcher.SyncTransfer(ftpServer, ftpUsername, ftpPassword, directoryPath);
                   // await Application.Current.MainPage.DisplayAlert("Success", "FTP Migration Completed", "Yes");
                }
             

            }
            catch (Exception es)
            {
                await Application.Current.MainPage.DisplayAlert("Failed",es.Message, "Close");
            }

        }
    }
}