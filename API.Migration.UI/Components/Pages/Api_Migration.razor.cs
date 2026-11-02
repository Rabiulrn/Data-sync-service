
using ApiFetcher;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace API.Migration.UI.Components.Pages
{
    public partial class Api_Migration
    {
        public string Source { get; set; }
        public string Destincation { get; set; }

       
        protected async Task OnClick(string text)
        {
          
            try
            {
                //if (text == "MigrateAllTransaction")
                //{
                //   AllTransactionMigrationsShitalokhya fetcher = new AllTransactionMigrationsShitalokhya();
                //   await fetcher.Sync(Source, Destincation);
                //    await Application.Current.MainPage.DisplayAlert("Success", "Migration Completed", "Yes");
                //}
                //if (text == "MigrateETCTransaction")
                //{
                //    ETCTransactionMigrationMeghna fetcher = new ETCTransactionMigrationMeghna();
                //    await fetcher.Sync(Source, Destincation);
                //    await Application.Current.MainPage.DisplayAlert("Success", "Migration Completed", "Yes");
                //}
                //if (text == "MigrateVehicleRegistration")
                //{
                //   VehicleRegistrationMigration fetcher = new VehicleRegistrationMigration();
                //   await fetcher.Sync(Source, Destincation);
                //    await Application.Current.MainPage.DisplayAlert("Success", "Migration Completed", "Yes");
                //} 
                //if (text == "IMAGEMIGRATIONS")
                //{
                //    ImagesMigrationGomoti fetcher = new ImagesMigrationGomoti();
                //    await fetcher.Sync(Source, Destincation);
                //    await Application.Current.MainPage.DisplayAlert("Success", "Migration Completed", "Yes");
                //}
                //if (text == "UPAYTEST")
                //{
                //    UpayTest fetcher = new UpayTest();
                //  var t= await fetcher.MakeRequest();
                //    await Application.Current.MainPage.DisplayAlert("Success", t, "Yes");
                //}
                //if (text == "Replace")
                //{
                //    ReplaceRegNo replace = new ReplaceRegNo();
                //    await replace.sync();
                //    await Application.Current.MainPage.DisplayAlert("Success", "Migration Completed", "Yes");
                //} 
                //if (text == "ServiceTest")
                //{
                //    RhdDataTransSer serv = new RhdDataTransSer();
                //    serv.SyncData();
                //    await Application.Current.MainPage.DisplayAlert("Success", "Migration Completed", "Yes");
                //}

                //if (text == "TestAllFunctionsServices")
                //{
                //    Test serv = new Test();
                //    serv.TestAllFunctions();
                //    await Application.Current.MainPage.DisplayAlert("Success", "Migration Completed", "Yes");
                //}

            }
            catch (Exception es)
            {
                await Application.Current.MainPage.DisplayAlert("Failed",es.Message, "Close");
            }

        }
    }
}