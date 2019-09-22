using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ServerlessAuthenticationWithAzureActiveDirectory
{
    public static class GreetingsEngine
    {
        [FunctionName("Greeting")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult(new GreetingResponse
                    { Greeting = $"Hello, {name}", AuthenticationType = "Azure Active Directory" })
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        [FunctionName("GetClaims")]
        public static async Task<IActionResult> GetClaims(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
            HttpRequest request,
            ILogger log)
        {
            if (Thread.CurrentPrincipal.Identity.IsAuthenticated)
            {
                var claimsPrincipal = (ClaimsPrincipal)Thread.CurrentPrincipal;
                var claims = claimsPrincipal.Claims.ToDictionary(c => c.Type, c => c.Value);
                // Could use the claims here. For this sample, just return it!
                return new OkObjectResult(claims);
            }

            return new UnauthorizedResult();
        }
    }
}
