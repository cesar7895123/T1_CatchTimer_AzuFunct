using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;
using CatchTimer_AzuFunct.Common.Models;
using CatchTimer_AzuFunct.Functions.Entities;
using CatchTimer_AzuFunct.Common.Responses;

namespace CatchTimer_AzuFunct.Functions.Functions
{
    public static class CatchTimerApi
    {
        [FunctionName(nameof(CreateCatchTime))]
        public static async Task<IActionResult> CreateCatchTime(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "CatchTimes/{IdEmployee}")] HttpRequest req,
            [Table("CatchTimes", Connection = "AzureWebJobsStorage")] CloudTable ListCatchTimes,
            string IdEmployee,
            ILogger log)
        {
            log.LogInformation("New Catch Time in table");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            CatchTimer catchtime = JsonConvert.DeserializeObject<CatchTimer>(requestBody);

            //Valide if the operation is In or Out
            string filter = TableQuery.GenerateFilterCondition("IdEmployee", QueryComparisons.Equal, IdEmployee);
            TableQuery<Catch_TimerEntity> query = new TableQuery<Catch_TimerEntity>().Where(filter);
            TableQuerySegment<Catch_TimerEntity> completedCatchTimers = await ListCatchTimes.ExecuteQuerySegmentedAsync(query, null);

            int TypeOperation = 0;
            foreach (Catch_TimerEntity CatchTime in completedCatchTimers)
            {
                TypeOperation = TypeOperation == 0?1:0;
            }

            Catch_TimerEntity catchtimerEntity = new Catch_TimerEntity
            {
                ETag = "*",
                RowKey = Guid.NewGuid().ToString(),
                PartitionKey = "ListCatchTimes",
                IdEmployee = IdEmployee,
                TypeEvent = TypeOperation,
                Time = DateTime.UtcNow,
                isConsolidated = false
            };

            TableOperation addOperation = TableOperation.Insert(catchtimerEntity);
            await ListCatchTimes.ExecuteAsync(addOperation);

            string message = $"New Catch Time to employee: {IdEmployee}, received.";
            log.LogInformation(message);

            return new OkObjectResult(new Response 
            {
                IsSuccess = true,
                Message = message,
                Result = catchtimerEntity
            }
            );
        }
    }
}
