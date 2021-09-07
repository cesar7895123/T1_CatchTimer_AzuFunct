using CatchTimer_AzuFunct.Common.Models;
using CatchTimer_AzuFunct.Common.Responses;
using CatchTimer_AzuFunct.Functions.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CatchTimer_AzuFunct.Functions.Functions
{
    public static class CatchTimerApi
    {
        [FunctionName(nameof(CreateCatchTime))]
        public static async Task<IActionResult> CreateCatchTime(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "CatchTime/{IdEmployee}")] HttpRequest req,
            [Table("CatchTime", Connection = "AzureWebJobsStorage")] CloudTable ListCatchTimes,
            string IdEmployee,
            ILogger log)
        {
            log.LogInformation("New Catch Time in table");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            CatchTimer catchtime = JsonConvert.DeserializeObject<CatchTimer>(requestBody);

            //convert idEmployee to int
            int intIdEmployee;
            try
            {
                intIdEmployee = Convert.ToInt16(IdEmployee);
            }
            catch (Exception)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "The id Employee must be a number."
                });
            }

            //Valide if the operation is In or Out
            string filter = TableQuery.GenerateFilterConditionForInt("IdEmployee", QueryComparisons.Equal, intIdEmployee);
            TableQuery<Catch_TimerEntity> query = new TableQuery<Catch_TimerEntity>().Where(filter);
            TableQuerySegment<Catch_TimerEntity> completeCatchTimers = await ListCatchTimes.ExecuteQuerySegmentedAsync(query, null);

            int TypeOperation = 0;
            int sorting = 0;
            foreach (Catch_TimerEntity CatchTime in completeCatchTimers)
            {
                TypeOperation = TypeOperation == 0 ? 1 : 0;
                sorting++;
            }

            Catch_TimerEntity catchtimerEntity = new Catch_TimerEntity
            {
                ETag = "*",
                RowKey = sorting.ToString().PadLeft(6, '0') + intIdEmployee.ToString().PadLeft(5, '0') + Guid.NewGuid().ToString(),
                PartitionKey = "ListCatchTimes",
                IdEmployee = intIdEmployee,
                TypeEvent = TypeOperation,
                Time = DateTime.UtcNow,
                IsConsolidated = false
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

        [FunctionName(nameof(GetAllCatchTimes))]
        public static async Task<IActionResult> GetAllCatchTimes(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "CatchTime")] HttpRequest req,
            [Table("CatchTime", Connection = "AzureWebJobsStorage")] CloudTable ListCatchTimes,
            ILogger log)
        {
            log.LogInformation("Get all Catch Times received.");

            TableQuery<Catch_TimerEntity> query = new TableQuery<Catch_TimerEntity>();
            TableQuerySegment<Catch_TimerEntity> catchtimes = await ListCatchTimes.ExecuteQuerySegmentedAsync(query, null);

            string message = "Retrieved all Catch Times";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = catchtimes
            });
        }

        [FunctionName(nameof(GetAllCatchTimesById))]
        public static async Task<IActionResult> GetAllCatchTimesById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "CatchTime/{IdEmployee}")] HttpRequest req,
            [Table("CatchTime", Connection = "AzureWebJobsStorage")] CloudTable ListCatchTimes,
            string IdEmployee,
            ILogger log)
        {
            log.LogInformation($"Get all catch times by employee: {IdEmployee} received.");

            //convert idEmployee to int
            int intIdEmployee;
            try
            {
                intIdEmployee = Convert.ToInt16(IdEmployee);
            }
            catch (Exception)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "The id Employee must be a number."
                });
            }

            string filter = TableQuery.GenerateFilterConditionForInt("IdEmployee", QueryComparisons.Equal, intIdEmployee);
            TableQuery<Catch_TimerEntity> query = new TableQuery<Catch_TimerEntity>().Where(filter);
            TableQuerySegment<Catch_TimerEntity> completeCatchTimers = await ListCatchTimes.ExecuteQuerySegmentedAsync(query, null);

            if (completeCatchTimers == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "catch times not found."
                });
            }

            string message = $"Retrieved all Catch Time for id employee: {IdEmployee}";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = completeCatchTimers
            });
        }

        [FunctionName(nameof(UpdateCatchTime))]
        public static async Task<IActionResult> UpdateCatchTime(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "CatchTime/{Id}")] HttpRequest req,
            [Table("CatchTime", Connection = "AzureWebJobsStorage")] CloudTable ListCatchTimes,
            string Id,
            ILogger log)
        {
            log.LogInformation($"Update for catch time: {Id}, received.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            CatchTimer catchtimer = JsonConvert.DeserializeObject<CatchTimer>(requestBody);


            TableOperation findOperation = TableOperation.Retrieve<Catch_TimerEntity>("ListCatchTimes", Id);
            TableResult findResult = await ListCatchTimes.ExecuteAsync(findOperation);
            if (findResult.Result == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "catch timer not found."
                });
            }

            // Update catch time
            Catch_TimerEntity catchtimeEntity = (Catch_TimerEntity)findResult.Result;
            if (!string.IsNullOrEmpty(catchtimer.IsConsolidated.ToString()))
            {
                catchtimeEntity.IsConsolidated = catchtimer.IsConsolidated;
            }

            if (!string.IsNullOrEmpty(catchtimer.Time.ToString()))
            {
                catchtimeEntity.Time = catchtimer.Time;
            }

            if (!string.IsNullOrEmpty(catchtimer.TypeEvent.ToString()))
            {
                catchtimeEntity.TypeEvent = catchtimer.TypeEvent;
            }

            TableOperation addOperation = TableOperation.Replace(catchtimeEntity);
            await ListCatchTimes.ExecuteAsync(addOperation);

            string message = $"catch timer: {Id}, update in table.";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = catchtimeEntity
            });
        }

        [FunctionName(nameof(DeleteCatchTime))]
        public static async Task<IActionResult> DeleteCatchTime(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "CatchTime/{Id}")] HttpRequest req,
            [Table("CatchTime", "ListCatchTimes", "{id}", Connection = "AzureWebJobsStorage")] Catch_TimerEntity catchtimeEntity,
            [Table("CatchTime", Connection = "AzureWebJobsStorage")] CloudTable ListCatchTimes,
            string Id,
            ILogger log)
        {
            log.LogInformation($"Delete catch time: {Id}, received.");

            if (catchtimeEntity == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "catch time not found."
                });
            }

            await ListCatchTimes.ExecuteAsync(TableOperation.Delete(catchtimeEntity));
            string message = $"Todo: {catchtimeEntity.RowKey}, deleted.";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = catchtimeEntity
            });
        }
    }
}
