using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimaticArcorWebApi.Exceptions;

namespace SimaticArcorWebApi.Helpers
{
  public static class SimaticServerHelper
  {
    /// <summary>
    /// It throw a formatted exception if the Http response is not valid
    /// </summary>
    /// <param name="token"></param>
    /// <param name="response"></param>
    public static void CheckFaultResponse(CancellationToken token, HttpResponseMessage response, ILogger logger)
    {
      if (response.StatusCode >= HttpStatusCode.BadRequest)
      {
        if(response.StatusCode == HttpStatusCode.Unauthorized)
        {
          logger.LogError($"Unauthorized: Token must be refreshed.");
        }

        dynamic result = "";
        response.Content.ReadAsStringAsync().ContinueWith(task =>
        {
          try
          {
            result = JsonConvert.DeserializeObject<dynamic>(task.Result);
            logger.LogError($"{result}");
          }
          catch(JsonReaderException re)
          {
            logger.LogError($"Not a JSON response! Response: {task.Result}, Exception: {re.Message}");
          }
          catch (Exception e)
          {
            logger.LogError($"Error checking the response. Response: {task.Result}, Exception: {e.Message}");
            // throw;
          }

        }, token).Wait(token);

        throw new SimaticApiException((Nancy.HttpStatusCode)response.StatusCode, result);
      }
    }

  }
}
