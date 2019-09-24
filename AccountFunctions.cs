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
    public static class AccountFunctions
    {
        private const string AuthenticationType = "Azure Active Directory";
        private static decimal _balance = 0;

        [FunctionName("Balance")]
        public static async Task<IActionResult> Balance(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Getting account balance.");

            return (ActionResult)new OkObjectResult(new BalanceResponse
                    { Balance = _balance, AuthenticationType = AuthenticationType });
        }

        [FunctionName("Credit")]
        public static async Task<IActionResult> Credit(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Crediting account.");

            return await CreditOrDebit(req, true);
        }

        [FunctionName("Debit")]
        public static async Task<IActionResult> Debit(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Debiting account.");

            return await CreditOrDebit(req, false);
        }

        private static async Task<IActionResult> CreditOrDebit(HttpRequest req, bool credit)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            if (data?.Amount == null)
            {
                return new BadRequestObjectResult("Please pass an amount in the request body");
            }

            decimal amount = (decimal)data?.Amount;
            if (credit)
            {
                _balance += amount;
            }
            else
            {
                _balance -= amount;
            }

            return new OkObjectResult(new CreditOrDebitResponse
                {Amount = amount, AuthenticationType = AuthenticationType}
            );
        }

        [FunctionName("GetClaims")]
        public static async Task<IActionResult> GetClaims(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
            HttpRequest request,
            ILogger log)
        {
            if (!Thread.CurrentPrincipal.Identity.IsAuthenticated)
            {
                return new UnauthorizedResult();
            }

            log.LogInformation("User authenticated");
            var claimsPrincipal = (ClaimsPrincipal)Thread.CurrentPrincipal;
            if (claimsPrincipal is null)
            {
                var message = "ClaimsPrincipal is null";
                log.LogInformation(message);
                return new OkObjectResult(message);
            }
            if (claimsPrincipal.Claims is null)
            {
                var message = "ClaimsPrincipal.Claims is null";
                log.LogInformation(message);
                return new OkObjectResult(message);
            }

            var claims = claimsPrincipal.Claims.ToDictionary(c => c.Type, c => c.Value);

            return new OkObjectResult(claims);
        }
    }
}
