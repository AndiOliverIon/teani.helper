// See https://aka.ms/new-console-template for more information
using Tni.Helper;
using Tni.Helper.Entities;

Console.WriteLine("Starting test of the helper library");

Logs();
AzureStorageManagerTest();

Console.WriteLine("All done");
Console.ReadKey();

#region Private methods
void Logs()
{
    Log.Start();

    Log.Write(eMessageType.Debug, $"Debug not stored");
    Log.Write(eMessageType.Information, $"Some information");
    Log.Write(eMessageType.Success, $"Some success");
    Log.Write(eMessageType.Error, $"Some error");
    Log.Write(eMessageType.Warning, $"Some warning");

    Log.Write(new Exception("Some exception, no stack"));
    Log.Write(new Exception("Some exception, with stack"), true);

    Log.StoreDebugMessages = true;
    Log.Write(Tni.Helper.Entities.eMessageType.Debug, $"Debug stored");
}
async void AzureStorageManagerTest()
{
    var asm = new AzureStorageManager("DefaultEndpointsProtocol=https;AccountName=ardisconfig;AccountKey=XZzYPMYC+bIwgorjJ3CdYxLeurgo1PXXmsXXtH2ZaZLQnYnaMLOs4iXy5l2BtXHbptEhkyx0TFSK4CtNwaYFEg==;EndpointSuffix=core.windows.net");

    //Specifying each time the container.
    var container = "ardis-paid-libraries";
    var list = await asm.GetList(container);
    var size = await asm.GetSize(container);

    await asm.UploadFile(new FileInfo(@"d:\temp\ProductionBatch.json"), true, container);
    await asm.DownloadFile(new FileInfo(@"c:\temp\ProductionBatch.json"), container);

    //Specifying the work container
    asm.WorkContainer = container;
    list = await asm.GetList();
    size = await asm.GetSize();

    await asm.UploadFile(new FileInfo(@"d:\temp\ProductionBatch.json"), true);
    FileInfo downloadedFile = await asm.DownloadFile(new FileInfo(@"c:\temp\ProductionBatch.json"));


    /*
         Examples
        var asm = new AzureStorageManager("{YourAzureConnectionString}");

        //Specifying each time the container.
        var container = "YourWorkingContainer";
        var list = await asm.GetList(container);
        var size = await asm.GetSize(container);

        await asm.UploadFile(new FileInfo(@"d:\temp\testfile.json"), true, container);
        FileInfo downloadedFile = await asm.DownloadFile(new FileInfo(@"c:\temp\testfile.json"), container);

        //Specifying the work container
        asm.WorkContainer = container;
        list = await asm.GetList();
        size = await asm.GetSize();

        await asm.UploadFile(new FileInfo(@"d:\temp\testfile.json"), true);
        FileInfo downloadedFile = await asm.DownloadFile(new FileInfo(@"c:\temp\testfile.json"));
    */

}
#endregion