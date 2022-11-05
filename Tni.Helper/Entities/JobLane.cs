using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tni.Helper.Entities
{
    internal class JobLane
    {
        #region Fields
        private CancellationToken _cancel;
        private ConcurrentQueue<JobItem> _queue { get; set; } = new ConcurrentQueue<JobItem>();
        #endregion

        #region Constructors
        internal JobLane(int laneNumber, JobProfile jobProfile, CancellationToken cancellationToken)
        {
            LaneNumber = laneNumber;
            JobProfile = jobProfile;
            CancellationToken = cancellationToken;
            Task.Factory.StartNew(Perform, TaskCreationOptions.LongRunning);
        }
        #endregion

        #region Properties
        internal static JobProfile JobProfile { get; set; }
        internal int LaneNumber { get; set; }
        internal CancellationToken CancellationToken { get; set; }
        internal bool IsExecuting { get; set; }
        #endregion

        #region Computable properties
        public int JobsCount
        {
            get
            {
                return _queue.Count();
            }
        }
        #endregion

        #region Internal methods
        internal void Enqueue(JobItem job)
        {
            _queue.Enqueue(job);
        }
        #endregion

        #region Private methods
        private async Task Perform()
        {
            while (!_cancel.IsCancellationRequested)
            {
                while (_queue.Count > 0)
                {
                    try
                    {
                        var job = default(JobItem);
                        if (_queue.TryDequeue(out job))
                        {
                            IsExecuting = true;

                            try
                            {
                                job.Start = DateTime.Now;
                                await job.Executor(job);
                            }
                            catch (Exception iex)
                            {
                                Log.Write(eMessageType.Error, $"Job [{job.Name}] was executed on lane [{LaneNumber}]." +
                                $" But executor failed with error: {iex.Message}, stack: {iex.StackTrace}");
                            }
                            finally
                            {
                                job.End = DateTime.Now;
                            }

                            IsExecuting = false;

                            if (JobProfile.Debug)
                            {
                                string durationStr = job.Duration.HasValue ? $", Duration: {job.Duration}" : string.Empty;
                                if (JobProfile.Debug)
                                    Log.Write(eMessageType.Debug, $"Job [{job.Name}] was executed on lane [{LaneNumber}]. " +
                                        $"Started: {job.Start}, Ended: {job.End}{durationStr}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Write(eMessageType.Error,
                            $"Error while executing job, lane [{LaneNumber}]: {ex.Message}, stack: {ex.StackTrace}");
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
        #endregion
    }
}
