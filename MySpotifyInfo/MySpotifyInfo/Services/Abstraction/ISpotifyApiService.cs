using MySpotifyInfo.Models;
using SpotifyAPI.Web;
using System.Threading.Tasks;

namespace MySpotifyInfo.Services.Abstraction
{
    public interface ISpotifyApiService
    {
        string BuildLoginRequestUrl(string callbackUrl);
        Task<SpotifyClient> BuildClient(string authenticationToken, string callbackUrl);
        Task<DisplayCard> BuildCurrentlyPlayingCard(SpotifyClient client);
        Task<DisplayCard> BuildRecentlyPlayedCard(SpotifyClient client);
    }
}
