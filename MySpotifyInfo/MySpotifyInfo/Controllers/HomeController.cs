using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySpotifyInfo.Services.Abstraction;
using SpotifyAPI.Web;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MySpotifyInfo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ISpotifyApiService _spotifyApiService;
        private static SpotifyClient _spotifyClient = null;
        private static string _callbackUrl;
        private const string APPLICATION_BASE_URL = "http://localhost:62543"; //TODO: for the love of god, get this from the httpcontext

        public HomeController(ILogger<HomeController> logger, ISpotifyApiService spotifyApiService)
        {
            _logger = logger;
            _spotifyApiService = spotifyApiService;
        }

        public IActionResult Index()
        {
            if (string.IsNullOrEmpty(_callbackUrl))
            {
                _callbackUrl = APPLICATION_BASE_URL + Url.Action("Callback");
            }
            return View();
        }

        [HttpGet]
        public IActionResult StartLoginRequest()
        {
            var loginRequestUrl = _spotifyApiService.BuildLoginRequestUrl(_callbackUrl);
            if (string.IsNullOrEmpty(loginRequestUrl))
            {
                return NotFound();
            }
            return Ok(loginRequestUrl);
        }

        [HttpGet]
        public async Task<IActionResult> GetSpotifyAnswer()
        {
            if (_spotifyClient == null)
            {
                return Ok();
            }

            var currentlyPlayingCard = await _spotifyApiService.BuildCurrentlyPlayingCard(_spotifyClient);
            if(currentlyPlayingCard != null)
            {
                return Ok(currentlyPlayingCard);
            }

            var recentlyPlayedItem = await _spotifyApiService.BuildRecentlyPlayedCard(_spotifyClient);

            return Ok(recentlyPlayedItem);
        }

        public async Task<IActionResult> Callback(string code)
        {
            _spotifyClient = await _spotifyApiService.BuildClient(code, _callbackUrl);
            return View();
        }


    }
}
