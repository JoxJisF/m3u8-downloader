//using HtmlAgilityPack;
using m3u8_Linker;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace u3m8_twitch
{
    internal struct TwitchLinkerResult : ILinker
    {
        public TwitchLinkerResult(string name, string url, string quality)
        {
            Name = name;
            Url = url;
            Quality = quality;
        }

        public string Name { get; set; }

        public string Url { get; set; }

        public string Quality { get; set; }
    }



    public class TwitchLinker
    {
        static Api api = new Api();

        public static async Task<List<ILinker>> Get(string url)
        {
            // client.DefaultRequestHeaders.UserAgent

            //  string gql = await client.GetStringAsync("https://gql.twitch.tv/gql");


            //  string message = await client.GetStringAsync(url);

            var name = url.Split("/").Last();


            try
            {

                var m3u8List = await api.Get_m3u8(name);
                var lines = m3u8List.Split("\n", StringSplitOptions.RemoveEmptyEntries);
                var source = lines.FirstOrDefault(x => x.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

                if (source == null)
                    return new List<ILinker>();

                return new List<ILinker>
                {
                    new TwitchLinkerResult(name + " Source", source, "Source")
                };
            }
            catch (GQLException)
            {
                return new List<ILinker>();
            }
            catch (ArgumentNullException)
            {
                return new List<ILinker>();
            }
        }
    }

    internal class GQLException : Exception
    {
        public GQLException(string message) : base(message) { }

    }

    internal class Api
    {
        public static string clientId = "kimne78kx3ncx6brgo4mv6wki5h1ko";
        //public Dictionary<string, string> mainHeader { get; } = new Dictionary<string, string>() { { "Client-Id", clientId } };

        static HttpClient client = new HttpClient();

        internal async Task<string> Get_m3u8(string login)
        {
            //var firstData = await client.GetStringAsync("https://www.twitch.tv/login");

            //if (firstData == null)
            //{
            //    throw new ArgumentNullException(nameof(firstData));
            //}

            //var htmlDoc = new HtmlDocument();
            //htmlDoc.LoadHtml(firstData);

            //var htmlBody = htmlDoc.DocumentNode.SelectSingleNode("//*[property=\"og:description\"]");

            // client.SendAsync()

            var gQLRequest = new GQLRequest()
            {
                // operationName = "PlaybackAccessToken_Template",
                query = "query StreamPlayer_Query(\r\n  $login: String!\r\n  $playerType: String!\r\n  $platform: String!\r\n  $skipPlayToken: Boolean!\r\n) {\r\n  ...StreamPlayer_token\r\n}\r\n\r\nfragment StreamPlayer_token on Query {\r\n  user(login: $login) {\r\n    login\r\n    stream @skip(if: $skipPlayToken) {\r\n      playbackAccessToken(params: {platform: $platform, playerType: $playerType}) {\r\n        signature\r\n        value\r\n        expiresAt\r\n        authorization {\r\n          isForbidden\r\n          forbiddenReasonCode\r\n        }\r\n      }\r\n      id\r\n      __typename\r\n    }\r\n    id\r\n    __typename\r\n  }\r\n}\r\n",
                variables = new GQLRequest.Variables
                {
                    //  isLive = true,
                    // isVod = false,
                    login = login,
                    platform = "web",
                    playerType = "pulsar",
                    skipPlayToken = false
                    //   playerType = "site",
                    //   vodID = ""
                }

            };


            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://gql.twitch.tv/gql");
            request.Content = new StringContent(JsonConvert.SerializeObject(gQLRequest));
            request.Headers.Add("Client-Id", clientId);

            using var responce = await client.SendAsync(request);
            var stringResponce = await responce.Content.ReadAsStringAsync();

            if (!responce.IsSuccessStatusCode)
                throw new GQLException(stringResponce);

            GQLResponce? gQLResponce = JsonConvert.DeserializeObject<GQLResponce>(stringResponce);

            //?? throw new GQLException(stringResponce);

            if (gQLResponce?.data?.user?.stream?.playbackAccessToken?.value == null ||
                gQLResponce?.data?.user?.stream?.playbackAccessToken?.signature == null)
            {
                throw new GQLException(stringResponce);
            }

            // билдим запрос


            var urlGet = $"https://usher.ttvnw.net/api/channel/hls/{login}.m3u8?player_type=pulsar&" +
                "player_backend=mediaplayer&" +
                "playlist_include_framerate=true&" +
                "allow_source=true&" +
                "transcode_mode=vbr_v1&" +
                $"token={gQLResponce.data.user.stream.playbackAccessToken.value}&" +
                $"sig={gQLResponce.data.user.stream.playbackAccessToken.signature}&" +
                "cdm=wv&" +
                "player_version=1.19.0";

            return await client.GetStringAsync(urlGet);
        }
    }



    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    internal class GQLRequest
    {
        public string operationName;
        public string query;
        public Variables variables;
        public class Variables
        {
            public bool isLive;
            public string login;
            public bool isVod;
            public string vodID;
            public string playerType;
            public string platform;
            public bool skipPlayToken;
        }
    }
    internal class GQLResponce
    {
        public Data data;
        public Extensions extensions;

        public class Authorization
        {
            public bool isForbidden;
            public string forbiddenReasonCode;
        }

        public class Data
        {
            public User user;
        }

        public class Extensions
        {
            public int durationMilliseconds;
            public string requestID;
        }

        public class PlaybackAccessToken
        {
            public string signature;
            public string value;
            public DateTime expiresAt;
            public Authorization authorization;
        }


        public class Stream
        {
            public PlaybackAccessToken playbackAccessToken;
            public string id;
            public string __typename;
        }

        public class User
        {
            public string login;
            public Stream stream;
            public string id;
            public string __typename;
        }
    }
}
