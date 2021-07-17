using System;
using System.Diagnostics;
using System.Net.Http;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace LDTTeam.Authentication.Server.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ErrorModel> _logger;

        public Exception? Exception = null;
        
        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            IExceptionHandlerFeature error = HttpContext
                .Features
                .Get<IExceptionHandlerFeature>();

            Exception = error.Error;

            if (Exception.InnerException is HttpRequestException exception)
                Exception = exception;
            
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }
    }
}