using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tni.Helper.Entities
{
    /// <summary>
    /// Description on how the job should be initialized.
    /// </summary>
    public class JobProfile
    {
        /// <summary>
        /// How many lanes to initiate for parallel executions.
        /// By default - 10 lanes.
        /// </summary>
        public int NoLanes { get; set; } = 10;

        /// <summary>
        /// How many lanes to be declared for priority jobs.
        /// By default - 2 lanes;
        /// </summary>
        public int ReservedLanesForPriorities { get; set; } = 2;

        /// <summary>
        /// Amount of seconds in which to wait for jobs to executed
        /// before clearing the lanes and sequenced items.
        /// </summary>
        public int StopTimeout { get; set; } = 60;

        /// <summary>
        /// When activated, various debug messages will be proposed to Helper.Log class
        /// to write some feedback of the executions.
        /// </summary>
        public bool Debug { get; set; }

        public void Validate()
        {
            if (NoLanes <= 0)
                throw new InvalidOperationException("No positive number for lanes of execution was specified.");
        }
    }
}
