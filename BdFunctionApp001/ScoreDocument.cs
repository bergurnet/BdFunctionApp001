using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BdFunctionApp001
{
    public static class ScoreDocument
    {
        [FunctionName("ScoreDocument")]

        //Denne linje binder tingene sammen (anderledes end i bogen, som bruger version 1 - denne m�de er mere simpel (husk at f� connection med i local.settings.json)
        public static async Task Run([BlobTrigger("documents/{name}", Connection = "blobStorageConnection")]Stream myBlob, string name, ILogger log, ExecutionContext context)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            //Taget fra "Video: Skriv til database fra Blob storage function" (anbefales at g�re p� denne m�de og ikke som i bogen
            var blobContent = new StreamReader(myBlob).ReadToEnd();

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional:true, reloadOnChange:true)
                .AddEnvironmentVariables()
                .Build();
            //Dette er det vigtigste at forst�:
            var connectionString = config.GetConnectionString("SqlConnectionString");

            //Using statement sikrer, at forbindelsen til databasen bliver lukket p�nt efter brug, ellers f�r jeg fejl p� et tidspunkt
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var sqlText = $"INSERT INTO DocumentTextScore (DocumentName, TextSentimentScore) VALUES('{name}', 0.998)";

                using (SqlCommand cmd = new SqlCommand(sqlText, conn))
                {
                    var rows = await cmd.ExecuteNonQueryAsync();
                    log.LogInformation($"{rows} rows were updated");
                }
            }
        }
    }
}