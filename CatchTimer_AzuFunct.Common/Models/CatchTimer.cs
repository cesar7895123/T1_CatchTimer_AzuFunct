﻿using System;

namespace CatchTimer_AzuFunct.Common.Models
{
    public class CatchTimer
    {
        public string IdEmployee { get; set; }

        public DateTime Time { get; set; }

        /// <summary>
        /// 0: In to work
        /// 1: Out to work
        /// </summary>
        public int TypeEvent { get; set; }

        public bool isConsolidated { get; set; }
    }
}