using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace CatchTimer_AzuFunct.Functions.Entities
{
    public class ConsolidateEntity : TableEntity
    {
        public int IdEmployee { get; set; }

        public DateTime Fecha { get; set; }

        public int Minutos { get; set; }
    }
}
