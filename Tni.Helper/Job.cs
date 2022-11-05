using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Tni.Helper.Entities;
using Tni.Helper.Enums;

namespace Tni.Helper
{
    /// <summary>
    /// Job executions sequenced and parallel
    /// pending on the amount of lanes declared.
    /// </summary>
    public class Job
    {
        #region Fields
        private static CancellationTokenSource _cancelSource = new CancellationTokenSource();
        private static List<JobLane> _lanes = new List<JobLane>();
        private static ConcurrentQueue<JobItem> _sequencedJobItems = new ConcurrentQueue<JobItem>();
        #endregion

        #region Properties
        public static JobProfile Profile { get; set; }
        #endregion

        #region Public methods
        /// <summary>
        /// Start of the job based on provided specifications
        /// </summary>
        /// <param name="profile"></param>
        public static void Start(JobProfile? jobProfile = null)
        {
            jobProfile = jobProfile ?? new JobProfile();
            _cancelSource = new CancellationTokenSource();
            jobProfile.Validate();
            Profile = jobProfile;

            _lanes.Clear();
            _sequencedJobItems.Clear();

            for (int i = 0; i < Profile.NoLanes; i++)
                _lanes.Add(new JobLane(i + 1, Profile, _cancelSource.Token));

            Task.Factory.StartNew(JobCycler, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// This sends a signal for elements to stop and the collections to be cleared
        /// for any items that are still not executed.
        /// </summary>
        public static async void Stop()
        {
            _cancelSource.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(Profile.StopTimeout));
            _lanes.Clear();
            _sequencedJobItems.Clear();
        }

        /// <summary>
        /// This ads multiple jobs to the sequence queue to be executed.
        /// </summary>
        /// <param name="jobs"></param>
        public static void AddSequenced(List<JobItem> jobs)
        {
            try
            {
                if (!jobs.Any())
                {
                    Log.Write(eMessageType.Debug, $"[Job:AddSequenced] No jobs were provided");
                    return;
                }

                jobs.ForEach(f => AddSequenced(f));
            }
            catch (Exception ex)
            {
                Helper.Log.Write(eMessageType.Error, $"[Job:AddSequenced] {ex.Message}, stack: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// This ads an item to be executed into a queue.
        /// </summary>
        /// <param name="item"></param>
        public static void AddSequenced(JobItem job)
        {
            try
            {
                if (job.UniqueOnExecution)
                {
                    if (_sequencedJobItems.Any(a => a.Name.Equals(job.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        Log.Write(eMessageType.Debug, $"[Job:AddSequenced] Job [{job.Name}] skipped, declared unique and there were another");
                        return;
                    }
                }

                job.Validate();
                _sequencedJobItems.Enqueue(job);
            }
            catch (Exception ex)
            {
                Log.Write(eMessageType.Error, $"[Job:AddSequenced] {ex.Message}, stack: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// This ads multiple jobs to the parallel queue to be executed.
        /// </summary>
        /// <param name="jobs"></param>
        public static void AddParallel(List<JobItem> jobs)
        {
            try
            {
                if (!jobs.Any())
                {
                    Log.Write(eMessageType.Debug, $"[Job:AddParallel] No jobs were provided");
                    return;
                }

                jobs.ForEach(f => AddParallel(f));
            }
            catch (Exception ex)
            {
                Helper.Log.Write(eMessageType.Error, $"[Job:AddParallel] {ex.Message}, stack: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// This ads an item to be executed, pending on its priority
        /// to one of the declared lanes of executions.
        /// </summary>
        /// <param name="item"></param>
        public static void AddParallel(JobItem job)
        {
            try
            {
                job.Validate();

                var lane = DetermineJobLane(job);

                if (Profile.Debug)
                    Log.Write(eMessageType.Debug, $"Lane [{lane.LaneNumber}] as assigned to [{job.Name}]");

                lane.Enqueue(job);
            }
            catch (Exception ex)
            {
                Log.Write(eMessageType.Error, $"[Job:Add] {ex.Message}, stack: {ex.StackTrace}");
            }
        }
        #endregion

        #region Private methods
        private static JobLane DetermineJobLane(JobItem job)
        {
            var available = job.LanePriority == eLanePriority.Fast ? _lanes
                : _lanes.OrderBy(ob => ob.LaneNumber).Skip(Profile.ReservedLanesForPriorities)
                .ToList();

            //Determine if into the set selected there is one which currently is not executing.
            //if so, take the most to the left
            var lane = available.Where(w => w.JobsCount.Equals(0) && !w.IsExecuting)
            .OrderByDescending(obd => obd.LaneNumber)
            .FirstOrDefault();

            if (lane != default)
                return lane;

            //Determine into current set which has the most minimal set of jobs and take that one
            //if so, take the most to the left.
            var lowestJobsCount = available.Select(s => s.JobsCount).Min();

            lane = available.Where(w => w.JobsCount <= lowestJobsCount)
                .OrderBy(ob => ob.IsExecuting)
                .ThenBy(tb => tb.JobsCount)
                .ThenBy(tb => tb.LaneNumber)
                .First();

            return lane;
        }
        private async static void JobCycler()
        {
            while (!_cancelSource.IsCancellationRequested)
            {
                while (_sequencedJobItems.Any())
                {
                    var job = default(JobItem);
                    try
                    {
                        if (_sequencedJobItems.TryDequeue(out job))
                        {
                            job.Start = DateTime.Now;
                            await job.Executor(job);
                            job.End = DateTime.Now;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Write(eMessageType.Error,
                            $"Error while executing job preservation [{(job != default ? job.Name : string.Empty)}]" +
                            $" {ex.Message}, stack: {ex.StackTrace}");
                    }
                    finally
                    {
                        if (job != default)
                            job.End = DateTime.Now;
                    }

                    if (Profile.Debug)
                    {
                        string durationStr = job.Duration.HasValue ? $", Duration: {job.Duration}" : string.Empty;
                        Log.Write(eMessageType.Debug, $"Job [{job.Name}] was executed on sequenced mode. " +
                            $"Started: {job.Start}, Ended: {job.End}{durationStr}");
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
        #endregion
    }
}
