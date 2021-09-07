using System;

namespace CatchTimer_AzuFunct.Common.Models
{
    public class CatchTimer
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
