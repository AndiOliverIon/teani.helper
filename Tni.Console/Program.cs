// See https://aka.ms/new-console-template for more information
using Tni.Helper;
using Tni.Helper.Entities;
using Tni.Helper.Enums;

Console.WriteLine("Starting test of the helper library");

Logs();
//AzureStorageManagerTest();
Jobs();

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

async void Jobs()
{
    Job.Start(new JobProfile()
    {
        //How many lanes to be available for execution on parallel mode.
        NoLanes = 10,

        //How many lanes to be served for fast parallel jobs to be executed.
        ReservedLanesForPriorities = 2,

        //Seconds to await on signaling stop before clearing collections
        StopTimeout = 30,

        //If the job store should create some feedback logs of the execution using Helper.Log engine.
        Debug = true,
    });
        
    ///Add elements to standard queue list
    Job.AddSequenced(new JobItem()
    {
        Name = "Standard 1",
        Executor = SomeMethodThatWillBeRequestedOnTimeOfJob
    });
    Job.AddSequenced(new JobItem()
    {
        Name = "Standard 2",
        Executor = SomeMethodThatWillBeRequestedOnTimeOfJob
    });
    Job.AddSequenced(new JobItem()
    {
        Name = "Standard 3",
        Executor = SomeMethodThatWillBeRequestedOnTimeOfJob
    });

    ///Add multiple elements at once into queue list
    Job.AddSequenced(new List<JobItem>()
    {
        new JobItem()
        {
            Name = "Standard 1.1",
            Executor = SomeMethodThatWillBeRequestedOnTimeOfJob
        },
        new JobItem()
        {
            Name = "Standard 2.1",
            Executor = SomeMethodThatWillBeRequestedOnTimeOfJob
        },
        new JobItem()
        {
            Name = "Standard 3.1",
            Executor = SomeMethodThatWillBeRequestedOnTimeOfJob
        }
    });

    await Task.Delay(TimeSpan.FromSeconds(15));

    ///Add elements to parallel executions
    Job.AddParallel(new JobItem()
    {
        Name = "Parallel 1",
        LanePriority = eLanePriority.Standard,
        Executor = SomeMethodThatWillBeRequestedOnTimeOfJob
    });
    Job.AddParallel(new JobItem()
    {
        Name = "Parallel 2",
        LanePriority = eLanePriority.Standard,
        Executor = SomeMethodThatWillBeRequestedOnTimeOfJob
    });
    Job.AddParallel(new JobItem()
    {
        Name = "Parallel 3",
        LanePriority = eLanePriority.Standard,
        Executor = SomeMethodThatWillBeRequestedOnTimeOfJob
    });

    ///Add multiple elements to parallel executions
    Job.AddParallel(new List<JobItem>()
    {
        new JobItem()
        {
            Name = "Parallel 1.1",
            LanePriority = eLanePriority.Standard,
            Executor = SomeMethodThatWillBeRequestedOnTimeOfJob
        },
        new JobItem()
        {
            Name = "Parallel 2.1",
            LanePriority = eLanePriority.Standard,
            Executor = SomeMethodThatWillBeRequestedOnTimeOfJob
        },
        new JobItem()
        {
            Name = "Parallel 3.1",
            LanePriority = eLanePriority.Standard,
            Executor = SomeMethodThatWillBeRequestedOnTimeOfJob
        }
    });
}
async Task<object> SomeMethodThatWillBeRequestedOnTimeOfJob(JobItem job)
{
    await Task.Delay(TimeSpan.FromSeconds(2));
    //Simulate doing something when time arrived for the job to be executed.
    Log.Write(eMessageType.Debug, $"Sequenced [{job.Name}] called for execution.");

    return default;
}

async Task<object> ParallelJobs(JobItem job)
{
    await Task.Delay(TimeSpan.FromSeconds(2));
    //Simulate doing something when time arrived for the job to be executed.
    Log.Write(eMessageType.Debug, $"Parallel [{job.Name}] called for execution.");

    return default;
}
#endregion