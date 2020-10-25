using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MySpotifyInfo.Models
{
    public class DisplayCard
    {
        public string UserName { get; set; }
        public string TrackName { get; set; }
        public string AwayElapsedTime { get; private set; }
        public string ContextName { get; set; }
        public string ContextImage { get; set; }
        public string ArtistNames { get; set; }
        public SpotifyContextType ContextType { get; set; }

        public void SetAwayElapsedTime()
        {
        } 
    }
}
