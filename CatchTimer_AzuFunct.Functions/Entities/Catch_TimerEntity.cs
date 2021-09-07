using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace CatchTimer_AzuFunct.Functions.Entities
{
    public class Catch_TimerEntity : TableEntity
    {
        public int IdEmployee { get; set; }

        public DateTime Time { get; set; }

        /// <summary>
        /// 0: In to work
        /// 1: Out to work
        /// </summary>
        public int TypeEvent { get; set; }

        public bool IsConsolidated { get; set; }
    }
}
