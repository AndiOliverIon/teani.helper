using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Text.Json;

namespace Tni.Helper.Entities
{
    public class LogItem
    {
        #region Constructors
        public LogItem(eMessageType messageType, string message)
        {
            MessageType = messageType;
            Message = message;
        }
        public LogItem() { }
        #endregion

        #region Properties
        public DateTime RecordedAt { get; set; } = DateTime.Now;
        public eMessageType MessageType { get; set; }
        public string Message { get; set; }
        #endregion

        #region Override methods
        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
        #endregion
    }
}
