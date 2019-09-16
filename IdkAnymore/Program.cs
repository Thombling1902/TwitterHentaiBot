using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Models.Entities;
using Tweetinvi.Core.Public.Models.Enum;
using Newtonsoft.Json.Linq;
using Tweetinvi.Parameters;
using Newtonsoft.Json;

using NekosSharp;
using System.Xml;

namespace TwitterHentaiBot
{
    internal class Program
    {
        public class ReturnInfomation
        {
            [JsonProperty("url")]
            public string URL { get; set; }
        }

        public static int i = 0;

        public static int likes = 0;
        public static int retweets = 0;
        public static int followers = 0;
        public static int following = 0;
        public static int tweetCount = 0;
        public static int imageCount = 0;

        public static long[] repliedToMessages;

        public static string[] authDetails = File.ReadAllLines("./twitterLoginDetails.txt");
        public static string[] captions = File.ReadAllLines("./captions.txt");
        public static string[] endpoints = File.ReadAllLines("./endpoints.txt");
        public static string[] accountsToScrape = File.ReadAllLines("./accountstoscrape.txt");
        public static string APIURL = "https://www.nekos.life/api/v2/img/";
        public static string hashtags = "#NSFW #hentai #lewd #cum #neko #tits #pussy #gif #trap #sissy #femboy #porn #anal";

        public static Random rng = new Random();
        public static WebClient client = new WebClient();
        public static IAuthenticatedUser authenticatedUser;

        private static void Main()
        {
            Auth.SetUserCredentials(authDetails[0], authDetails[1], authDetails[2], authDetails[3]);

            authenticatedUser = User.GetAuthenticatedUser();

            Console.WriteLine($"Logged in as: {authenticatedUser.ScreenName}");
            Console.WriteLine($"Captions: {string.Join(", ", captions)}\n");
            Console.WriteLine($"Accounts to scrape: {string.Join(", ", accountsToScrape)}\n");
            Console.WriteLine($"Endpoints: {string.Join(", ", endpoints)}\n");

            client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla / 5.0(Windows NT 10.0; Win64; x64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 78.0.3879.0 Safari / 537.36 Edg / 78.0.249.1");

            Thread t = new Thread(new ThreadStart(NewForm));
            t.Start();

            CalculateMetrics();
            WriteStats();
            Poster();
        }

        private static void NewForm()
        {
            Application.Run(new Manager());
        }

        private static void WriteStats()
        {
            Console.WriteLine($"Bot stats:\n");
            Console.WriteLine($"Likes: {likes}");
            Console.WriteLine($"Retweets: {retweets}");
            Console.WriteLine($"Followers: {followers}");
            Console.WriteLine($"Following: {following}\n");
            Console.WriteLine($"Amount of tweets: {tweetCount}");
            Console.WriteLine($"Amount of images: {imageCount}\n");
        }

        private static void Poster()
        {
            string[] blockedImages = File.ReadAllLines("./blockedimages.txt");
            string[] captions = File.ReadAllLines("./captions.txt");
            string[] r34endpoints = File.ReadAllLines("./r34endpoints.txt");
            string[] endpoints = File.ReadAllLines("./endpoints.txt");
            string[] accountsToScrape = File.ReadAllLines("./accountstoscrape.txt");

            try
            {
                ITweet tweet;
                string caption = captions[rng.Next(captions.Length)];
                string endpoint = endpoints[rng.Next(endpoints.Length)];

                int rngForR34 = rng.Next(4);

                if (rngForR34 == 3)
                {
                    string newEndpoint = r34endpoints[rng.Next(r34endpoints.Length)];

                    Console.WriteLine(newEndpoint);

                    string url = $"https://rule34.xxx/index.php?page=dapi&s=post&q=index&tags={newEndpoint}";

                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(client.DownloadString(url));

                    XmlNodeList nodes = xmlDocument.DocumentElement.SelectNodes("post");

                    XmlNode node = nodes[rng.Next(nodes.Count)];

                    string fileUrl = node.Attributes["file_url"].Value;

                    string[] URLSplit = fileUrl.Split('/').Last().Split('.');

                    string fileName = URLSplit[0];
                    string fileExtension = URLSplit[1];
                    string fullFileName = $"{fileName}.{fileExtension}";

                    client.DownloadFile(fileUrl, $"./hentai/r34items/{newEndpoint}/{fullFileName}");

                    byte[] bytesToUpload = File.ReadAllBytes($"./hentai/r34items/{newEndpoint}/{fullFileName}");

                    IMedia mediaToUpload;

                    if (fileExtension == "mp4")
                    {
                        mediaToUpload = Upload.UploadBinary(new UploadParameters
                        {
                            Binary = bytesToUpload,
                            MediaType = MediaType.VideoMp4
                        });
                    }
                    else
                    {
                        mediaToUpload = Upload.UploadBinary(new UploadParameters
                        {
                            Binary = bytesToUpload,
                            MediaType = MediaType.Media
                        });
                    }

                    List<IMedia> medias = new List<IMedia> { mediaToUpload };

                    tweet = Tweet.PublishTweet($"{caption}\n\n{hashtags}", new PublishTweetOptionalParameters
                    {
                        Medias = medias
                    });
                }
                else
                {
                    Console.WriteLine($"Selected endpoint: {endpoint}");

                    i++;

                    if (i == 10)
                    {
                        i = 0;
                        WriteStats();
                    }

                    if (endpoint == "scrape")
                    {
                    }
                    else
                    {
                        string jsonString = client.DownloadString(APIURL + endpoint);
                        ReturnInfomation returnInfomation = JsonConvert.DeserializeObject<ReturnInfomation>(jsonString);
                        string URL = returnInfomation.URL;

                        string[] URLSplit = URL.Split('/').Last().Split('.');

                        string fileName = URLSplit[0];
                        string fileExtension = URLSplit[1];
                        string fullFileName = $"{fileName}.{fileExtension}";

                        if (!File.Exists($"./hentai/{endpoint}/{fullFileName}"))
                        {
                            client.DownloadFile(URL, $"./hentai/{endpoint}/{fullFileName}");
                        }

                        foreach (string blockedImage in blockedImages)
                        {
                            if (fullFileName == blockedImage)
                            {
                                Console.WriteLine($"Was going to tweet blocked image: {blockedImage}");
                                Poster();
                                return;
                            }
                        }

                        byte[] bytesToUpload = File.ReadAllBytes($"./hentai/{endpoint}/{fullFileName}");

                        IMedia mediaToUpload;

                        if (fileExtension == "mp4")
                        {
                            mediaToUpload = Upload.UploadBinary(new UploadParameters
                            {
                                Binary = bytesToUpload,
                                MediaType = MediaType.VideoMp4
                            });
                        }
                        else
                        {
                            mediaToUpload = Upload.UploadBinary(new UploadParameters
                            {
                                Binary = bytesToUpload,
                                MediaType = MediaType.Media
                            });
                        }

                        List<IMedia> medias = new List<IMedia> { mediaToUpload };

                        tweet = Tweet.PublishTweet($"{caption}\n\n{hashtags}", new PublishTweetOptionalParameters
                        {
                            Medias = medias
                        });
                    }
                }

                Console.WriteLine("Tweeted");

                Thread thread = new Thread(new ThreadStart(CalculateMetrics));
                thread.Start();
                FollowerInteraction();

                Console.WriteLine("Waiting 1 minute\n");
                Thread.Sleep(TimeSpan.FromMinutes(1));
                Poster();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Poster();
            }
        }

        private static void CalculateMetrics()
        {
            IEnumerable<ITweet> timeline = authenticatedUser.GetUserTimeline(int.MaxValue);

            foreach (ITweet tweet in timeline)
            {
                likes = likes + tweet.FavoriteCount;
                retweets = retweets + tweet.RetweetCount;
            }
            followers = authenticatedUser.GetFollowers(int.MaxValue).Count();
            following = authenticatedUser.GetFriends(int.MaxValue).Count();
            tweetCount = timeline.Count();
            imageCount = Directory.GetFiles("./hentai", "", SearchOption.AllDirectories).Length;
        }

        private static void FollowerInteraction()
        {
            foreach (IUser user in authenticatedUser.GetFollowers(int.MaxValue))
            {
                if (!user.Following)
                {
                    authenticatedUser.FollowUser(user);
                }
            }

            foreach (IMention mention in authenticatedUser.GetMentionsTimeline(int.MaxValue))
            {
                if (!mention.Favorited)
                {
                    mention.Favorite();
                }
            }
        }
    }
}