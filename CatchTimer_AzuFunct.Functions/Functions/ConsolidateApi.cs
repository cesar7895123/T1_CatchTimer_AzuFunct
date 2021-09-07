using CatchTimer_AzuFunct.Common.Responses;
using CatchTimer_AzuFunct.Functions.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Threading.Tasks;

namespace CatchTimer_AzuFunct.Functions.Functions
{
    public static class ConsolidateApi
    {
        [FunctionName(nameof(CreateConsolidate))]
        public static async Task<IActionResult> CreateConsolidate(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Consolidate")] HttpRequest req,
           [Table("CatchTime", Connection = "AzureWebJobsStorage")] CloudTable ListCatchTimes,
           [Table("Consolidate", Connection = "AzureWebJobsStorage")] CloudTable ListConsolidates,
           ILogger log)
        {
            log.LogInformation($"Consolidate started.");

            string filter = TableQuery.GenerateFilterConditionForBool("IsConsolidated", QueryComparisons.Equal, false);
            TableQuery<Catch_TimerEntity> query = new TableQuery<Catch_TimerEntity>().Where(filter);
            TableQuerySegment<Catch_TimerEntity> CollectionTimes = await ListCatchTimes.ExecuteQuerySegmentedAsync(query, null);

            int Minutos = 0, IdEmployee = 0, OldMinutes = 0, cantConsolitaded = 0;
            DateTime Date = DateTime.Now;
            string filter2 = "", filter3 = "", rowid0 = "", rowid1 = "";

            ConsolidateEntity consolidateEntity = new ConsolidateEntity();

            foreach (Catch_TimerEntity CatchTimes in CollectionTimes)
            {
                if (CatchTimes.TypeEvent == 0)
                {
                    IdEmployee = CatchTimes.IdEmployee;
                    Date = CatchTimes.Time;
                    Minutos = 0;
                    rowid0 = CatchTimes.RowKey;

                    filter2 = TableQuery.GenerateFilterConditionForInt("IdEmployee", QueryComparisons.Equal, IdEmployee);
                    TableQuery<ConsolidateEntity> query2 = new TableQuery<ConsolidateEntity>().Where(filter2);
                    TableQuerySegment<ConsolidateEntity> consolidates = await ListCatchTimes.ExecuteQuerySegmentedAsync(query2, null);

                    if (consolidates.Results != null)
                    {
                        OldMinutes = 0;
                        foreach (ConsolidateEntity consolidate in consolidates)
                        {
                            if (consolidate.Fecha == Date.Date)
                            {
                                OldMinutes += consolidate.Minutos;
                            }
                        }
                    }
                }
                if (CatchTimes.TypeEvent == 1 && CatchTimes.IdEmployee == IdEmployee)
                {
                    Minutos = (int)(CatchTimes.Time - Date).TotalMinutes;
                    rowid1 = CatchTimes.RowKey;

                    filter3 = TableQuery.GenerateFilterConditionForInt("IdEmployee", QueryComparisons.Equal, IdEmployee);
                    TableQuery<ConsolidateEntity> query3 = new TableQuery<ConsolidateEntity>().Where(filter3);
                    TableQuerySegment<ConsolidateEntity> ConsolidatesByEmployee = await ListConsolidates.ExecuteQuerySegmentedAsync(query3, null);

                    foreach (ConsolidateEntity Consolidate in ConsolidatesByEmployee)
                    {
                        if(Consolidate.Fecha == Date.Date)
                        {
                            await ListConsolidates.ExecuteAsync(TableOperation.Delete(Consolidate));
                        }
                    }

                    consolidateEntity = new ConsolidateEntity
                    {
                        ETag = "*",
                        RowKey = Guid.NewGuid().ToString(),
                        PartitionKey = "Consolidate",
                        IdEmployee = IdEmployee,
                        Fecha = Date.Date,
                        Minutos = Minutos + OldMinutes
                    };

                    TableOperation addOperation = TableOperation.Insert(consolidateEntity);
                    await ListConsolidates.ExecuteAsync(addOperation);

                    for (int i = 0; i < 2; i++)
                    {
                        TableOperation findOperation = TableOperation.Retrieve<Catch_TimerEntity>("ListCatchTimes", i==0?rowid0: rowid1);
                        TableResult findResult = await ListCatchTimes.ExecuteAsync(findOperation);
                        if (findResult.Result != null)
                        {
                            Catch_TimerEntity catchtimeEntity = (Catch_TimerEntity)findResult.Result;
                            catchtimeEntity.IsConsolidated = true;
                            TableOperation OperationUpt = TableOperation.Replace(catchtimeEntity);
                            await ListCatchTimes.ExecuteAsync(OperationUpt);
                        }
                    }
                    cantConsolitaded++;
                }
            }

            string message = $"Consolidate complete new record(s)= {cantConsolitaded}.";
            log.LogInformation(message);

            return new OkObjectResult(message);
        }

        [FunctionName(nameof(GetConsolidadeByDate))]
        public static async Task<IActionResult> GetConsolidadeByDate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Consolidate/{id}")] HttpRequest req,
            [Table("Consolidate", Connection = "AzureWebJobsStorage")] CloudTable ListConsolidates,
            string id,
            ILogger log)
        {
            log.LogInformation($"Get consolidates by date: {id}, received.");

            DateTime DateFound = Convert.ToDateTime(id).Date;

            string filter = TableQuery.GenerateFilterConditionForDate("Fecha", QueryComparisons.Equal, DateFound);
            TableQuery<ConsolidateEntity> query = new TableQuery<ConsolidateEntity>().Where(filter);
            TableQuerySegment<ConsolidateEntity> ConsolidatesByDate = await ListConsolidates.ExecuteQuerySegmentedAsync(query, null);

            if (ConsolidatesByDate.Results == null || ConsolidatesByDate.Results.Count == 0)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "not found consolidates."
                });
            }

            string message = $"List consolidates retrieved.";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = ConsolidatesByDate.Results
            });
        }
    }
}
