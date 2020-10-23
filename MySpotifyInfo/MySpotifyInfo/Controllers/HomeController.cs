using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using System;
using System.Threading.Tasks;

namespace MySpotifyInfo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private static SpotifyClient _client = null;
        private static string _spotifyClientId;
        private static string _spotifyClientSecret;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _spotifyClientId = _configuration.GetSection("SpotifyKeys").GetSection("ClientId").Value;
            _spotifyClientSecret = _configuration.GetSection("SpotifyKeys").GetSection("ClientSecret").Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult StartLoginRequest()
        {
            var callbackUrl = new Uri(Url.Action("Callback"));
            var loginRequest = new LoginRequest(callbackUrl, _spotifyClientId, LoginRequest.ResponseType.Code)
            {
                Scope = new[] { Scopes.UserReadCurrentlyPlaying, Scopes.UserReadRecentlyPlayed }
            };
            var loginRequestUrl = loginRequest.ToUri().ToString();

            if (string.IsNullOrEmpty(loginRequestUrl))
            {
                return NotFound();
            }

            return Ok(loginRequestUrl);
        }

        [HttpGet]
        public async Task<IActionResult> GetSpotifyAnswer()
        {
            if (_client == null)
            {
                return Ok();
            }

            var track = await _client.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest(PlayerCurrentlyPlayingRequest.AdditionalTypes.Episode));
            
            return Ok("buya!");
        }


        public async Task<IActionResult> Callback(string code)
        {
            var response = await new OAuthClient().RequestToken(
              new AuthorizationCodeTokenRequest(_spotifyClientId, _spotifyClientSecret, code, new Uri(Url.Action("Callback")))
            );

            var config = SpotifyClientConfig
              .CreateDefault()
              .WithAuthenticator(new AuthorizationCodeAuthenticator(_spotifyClientId, _spotifyClientSecret, response));

            _client = new SpotifyClient(config);
            return View();
        }

    }
}
