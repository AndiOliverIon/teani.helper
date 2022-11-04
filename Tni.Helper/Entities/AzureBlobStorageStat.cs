using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tni.Helper.Extensions;

namespace Tni.Helper.Entities
{
    /// <summary>
    /// Get status informations about an specified container from Azure Blob Storage.
    /// </summary>
    public class AzureBlobStorageStat
    {
        #region Properties
        public long Size { get; set; }
        public int Files { get; set; }

        public DateTime First { get; set; }
        public DateTime Last { get; set; }
        #endregion

        #region Computable properties
        public string SizeDisplay
        {
            get
            {
                return Size.DisplaySize();
            }
        }
        #endregion

        #region Override methods
        public override string ToString()
        {
            return $"Files: {this.Files}, size container: {this.Size.DisplaySize()}, First: {this.First.ToString("yyyy-MM-dd HH:mm:ss")}, Last: {this.Last.ToString("yyyy-MM-dd HH:mm:ss")}";
        }
        #endregion
    }
}
