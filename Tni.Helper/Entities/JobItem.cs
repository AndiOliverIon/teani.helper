using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tni.Helper.Enums;

namespace Tni.Helper.Entities
{
    /// <summary>
    /// Describe on particular job to be executed.
    /// </summary>
    public class JobItem
    {
        #region Properties
        /// <summary>
        /// Name of the job, usefull when unique jobs must run once at each time.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Priority lane declaration for parallelism
        /// </summary>
        public eLanePriority LanePriority { get; set; }

        /// <summary>
        /// Declaration if only one job with this name should be executed at any given time.
        /// </summary>
        public bool UniqueOnExecution { get; set; }

        /// <summary>
        /// Time when job was actually started.
        /// </summary>
        public DateTime? Start { get; internal set; }

        /// <summary>
        /// Time when job ended.
        /// </summary>
        public DateTime? End { get; internal set; }

        /// <summary>
        /// Duration elapsed for the execution
        /// </summary>
        public TimeSpan? Duration
        {
            get
            {
                if (!Start.HasValue || !End.HasValue) return default;
                return End.Value - Start.Value;
            }
        }

        /// <summary>
        /// Which callback declared must be executed when job comes to execution.
        /// </summary>
        public Func<JobItem, Task<object>> Executor { get; set; }
        #endregion

        #region Public methods
        public virtual void Validate()
        {
            if (Executor == default)
                throw new InvalidOperationException($"No executor was declared for the job [{Name}]");
        }
        #endregion
    }
}
