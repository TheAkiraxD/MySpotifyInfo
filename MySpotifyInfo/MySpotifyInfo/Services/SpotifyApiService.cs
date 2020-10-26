using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic.CompilerServices;
using MySpotifyInfo.Models;
using MySpotifyInfo.Services.Abstraction;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MySpotifyInfo.Services
{
    public class SpotifyApiService : ISpotifyApiService
    {
        private static string _spotifyClientId;
        private static string _spotifyClientSecret;
        private readonly IConfiguration _configuration;

        public SpotifyApiService(IConfiguration configuration) 
        {
            _configuration = configuration;
            _spotifyClientId = _configuration.GetSection("SpotifyKeys").GetSection("ClientId").Value;
            _spotifyClientSecret = _configuration.GetSection("SpotifyKeys").GetSection("ClientSecret").Value; 
        }

        public string BuildLoginRequestUrl(string callbackUrl)
        {
            var loginRequest = new LoginRequest(new Uri(callbackUrl), _spotifyClientId, LoginRequest.ResponseType.Code)
            {
                Scope = new[] { Scopes.UserReadCurrentlyPlaying, Scopes.UserReadRecentlyPlayed }
            };
 
            return loginRequest.ToUri().ToString();
        }

        public async Task<SpotifyClient> BuildClient(string authenticationToken, string callbackUrl)
        {
            var response = await new OAuthClient().RequestToken(
                new AuthorizationCodeTokenRequest(_spotifyClientId, _spotifyClientSecret, authenticationToken, new Uri(callbackUrl))
            );

            var config = SpotifyClientConfig
              .CreateDefault()
              .WithAuthenticator(new AuthorizationCodeAuthenticator(_spotifyClientId, _spotifyClientSecret, response));

            return new SpotifyClient(config);
        }

        public async Task<DisplayCard> BuildCurrentlyPlayingCard(SpotifyClient client)
        {
            var currentlyPlayingTrack = await client.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest(PlayerCurrentlyPlayingRequest.AdditionalTypes.Episode));

            var user = await GetUser(client);

            if(currentlyPlayingTrack != null)
            {
                var fullTrack = (FullTrack)currentlyPlayingTrack.Item;
                return new DisplayCard()
                {
                    UserName = user.DisplayName,
                    TrackName = fullTrack.Name,
                    ContextType = GetContextType(currentlyPlayingTrack.Context),
                    ContextName = fullTrack.Album.Name,
                    ContextImage = fullTrack.Album.Images.FirstOrDefault().Url,
                    ArtistNames = BuildArtistNames(fullTrack.Artists)
                };
            }
            return null;
        }

        public async Task<DisplayCard> BuildRecentlyPlayedCard(SpotifyClient client)
        {
            var displayCard = new DisplayCard();
            var user = await GetUser(client);
            var recentlyPlayedItem = (await client.Player.GetRecentlyPlayed(new PlayerRecentlyPlayedRequest() { Limit = 1, }))?.Items?.FirstOrDefault();
            var recentlyPlayerTrack = recentlyPlayedItem.Track;

            var itemContext = recentlyPlayedItem.Context;

            if(itemContext != null)
            {
                var contextType = GetContextType(itemContext);
                var itemId = GetIdFromHref(itemContext.Href);

                displayCard.UserName = user.DisplayName;
                displayCard.TrackName = recentlyPlayerTrack.Name;
                displayCard.ContextType = SpotifyContextType.playlist;
                displayCard.ArtistNames = BuildArtistNames(recentlyPlayerTrack.Artists);
                displayCard.ContextType = contextType;

                if (contextType == SpotifyContextType.playlist)
                {
                    await HandlePlaylist(client, displayCard, itemId);
                    return displayCard;
                }

                if (contextType == SpotifyContextType.artist)
                {
                    await HandleArtirst(client, displayCard, itemId);
                    return displayCard;
                }

                if (contextType == SpotifyContextType.album)
                {
                    await HandleAlbum(client, displayCard, itemId);
                    return displayCard;
                }
            }

            return displayCard;

        }

        private async Task HandleAlbum(SpotifyClient client, DisplayCard displayCard, string itemId)
        {
            var album = await GetAlbum(client, itemId);

            displayCard.ContextName = album.Name;
            displayCard.ContextImage = album.Images.FirstOrDefault().Url;
        }

        private async Task HandleArtirst(SpotifyClient client, DisplayCard displayCard, string itemId)
        {
            var artist = await GetArtist(client, itemId);

            displayCard.ContextName = artist.Name;
            displayCard.ContextImage = artist.Images.FirstOrDefault().Url;
        }

        private async Task HandlePlaylist(SpotifyClient client, DisplayCard displayCard, string itemId)
        {
            var playlistRequest = BuildPlaylistRequestWithFields();
            var playlist = await GetPlaylist(client, playlistRequest, itemId);

            displayCard.ContextName = playlist.Name;
            displayCard.ContextImage = playlist.Images.FirstOrDefault().Url;
        }

        private async Task<PrivateUser> GetUser(SpotifyClient client)
        {
            return await client.UserProfile.Current();
        }

        private async Task<FullAlbum> GetAlbum(SpotifyClient client, string id)
        {
            return await client.Albums.Get(id);
        }

        private async Task<FullArtist> GetArtist(SpotifyClient client, string id)
        {
            return await client.Artists.Get(id);
        }

        private SpotifyContextType GetContextType(Context context)
        {
            return (SpotifyContextType)Enum.Parse(typeof(SpotifyContextType), context.Type);
        }

        private string BuildArtistNames(List<SimpleArtist> artists)
        {
            return artists.Select(x => x.Name).Aggregate((current, next) => current + ", " + next);
        }
        private async Task<FullPlaylist> GetPlaylist(SpotifyClient client, PlaylistGetRequest request, string id)
        {
            return await client.Playlists.Get(id, request);
        }

        private string GetIdFromHref(string href)
        {
            return href.Substring(href.LastIndexOf("/") + 1);
        }

        private PlaylistGetRequest BuildPlaylistRequestWithFields()
        {
            var request = new PlaylistGetRequest();
            request.Fields.Add("name");
            request.Fields.Add("images(url)");
            return request;
        }

    }
}
