using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using BdFunctionApp001.Models;
using System.Threading;
using System.Globalization;

namespace BdFunctionApp
{
    public static class BdFunction
    {
        // Function to post something into the body - alternatively I could make a Function to post to the URL query string
        [FunctionName("BdFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");


            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);


            //Simple input validation - to avoid hackers posting to the endpoint or query string
            string password = data?.password;

            if(password != "3mLm7u!MULZg")
            {
                return (ActionResult)new BadRequestObjectResult($"Wrong password");
            }

            //Extra strict input validation - can also be: not numbers or "<"
            string documentName = data?.name;

            if(documentName.Length > 18)
            {
                return (ActionResult)new BadRequestObjectResult($"Document name is too long");
            }



            var randomScore = new Random().NextDouble();

            BdTextScore textScore = new BdTextScore()
            {
                DocumentName = data?.name,
                TextSentimentScore = randomScore
            };

            var connectionstring = config.GetConnectionString("SqlConnectionString");

            // log.LogInformation("Connectionstring is: " + connectionstring);
            log.LogInformation("Connectionstring is: " + connectionstring.Length);

            using (SqlConnection conn = new SqlConnection(connectionstring))
            {
                conn.Open();
                var text = $"INSERT INTO DocumentTextScore (DocumentName, TextSentimentScore)" +
                    //"VALUES ('Test data', 1.111)";
                    "VALUES ('" + textScore.DocumentName + "', " + textScore.TextSentimentScore + ")";

                using (SqlCommand cmd = new SqlCommand(text, conn))
                {
                    // Execute the command and log the # rows affected.
                    var rows = await cmd.ExecuteNonQueryAsync();
                    log.LogInformation($"{rows} rows were updated");
                }
            }

            return (ActionResult)new OkObjectResult($"Text sentiment score is: {textScore.TextSentimentScore}");
        }
    }
}
