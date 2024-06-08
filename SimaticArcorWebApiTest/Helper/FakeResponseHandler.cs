using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SimaticArcorWebApiTest.Helper
{
  public class FakeResponseHandler : DelegatingHandler
  {
    private readonly Dictionary<Uri, HttpResponseMessage> _FakeResponses = new Dictionary<Uri, HttpResponseMessage>();

    public void AddFakeResponse(Uri uri, HttpResponseMessage responseMessage)
    {
      if (_FakeResponses.ContainsKey(uri))
        _FakeResponses[uri] = responseMessage;
      else
        _FakeResponses.Add(uri, responseMessage);
    }

    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
      if (_FakeResponses.ContainsKey(request.RequestUri))
      {
        return _FakeResponses[request.RequestUri];
      }
      else
      {
        return new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request };
      }

    }
    
  }
}
