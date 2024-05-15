using Hitomi_Scroll_Viewer.SearchPageComponent;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Hitomi_Scroll_Viewer.ImageWatchingPage;
using static Hitomi_Scroll_Viewer.Utils;

namespace Hitomi_Scroll_Viewer {
    public class Utils {
        public static readonly string ROOT_DIR = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HSV");
        public static readonly string IMAGE_DIR = Path.Combine(ROOT_DIR, "images");
        public static readonly string SETTINGS_PATH = Path.Combine(ROOT_DIR, "settings.json");
        public static readonly string LOGS_PATH = Path.Combine(ROOT_DIR, "logs.txt");

        public static readonly string REFERER = "https://hitomi.la/";
        public static readonly string IMAGE_BASE_DOMAIN = "hitomi.la";
        public static readonly string GALLERY_INFO_DOMAIN = "https://ltn.hitomi.la/galleries/";
        public static readonly string GALLERY_INFO_EXCLUDE_STRING = "var galleryinfo = ";
        public static readonly string SERVER_TIME_ADDRESS = "https://ltn.hitomi.la/gg.js";
        public static readonly string SERVER_TIME_EXCLUDE_STRING = "0123456789/'\r\n};";
        public static readonly string[] POSSIBLE_IMAGE_SUBDOMAINS = [ "https://aa.", "https://ba." ];
        public static readonly JsonSerializerOptions serializerOptions = new() { IncludeFields = true, WriteIndented = true };

        public static readonly string[] NEW_LINE_SEPS = [Environment.NewLine, "\r"];
        public static readonly StringSplitOptions STR_SPLIT_OPTION = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

        public struct Settings {
            public ViewMode viewMode;
            public ViewDirection viewDirection;
            public double scrollSpeed;
            public int numOfPages;
            public double pageTurnDelay;
            public bool isLooping;

            public Settings() { }
            
            public Settings(
                ViewMode viewMode,
                ViewDirection viewDirection,
                double scrollSpeed,
                int numOfPages,
                double pageTurnDelay,
                bool isLooping
            ) {
                this.viewMode = viewMode;
                this.viewDirection = viewDirection;
                this.scrollSpeed = scrollSpeed;
                this.numOfPages = numOfPages;
                this.pageTurnDelay = pageTurnDelay;
                this.isLooping = isLooping;
            }
        }

        public struct DownloadInfo {
            public HttpClient httpClient;
            public string id;
            public int concurrentTaskNum;
            public ProgressBar progressBar;
            public BookmarkItem bmItem;
            public CancellationToken ct;
        }

        /**
         * <exception cref="HttpRequestException"></exception>
         * <exception cref="TaskCanceledException"></exception>
        */
        public static async Task<string> GetGalleryInfo(HttpClient httpClient, string id, CancellationToken ct) {
            string address = GALLERY_INFO_DOMAIN + id + ".js";
            HttpRequestMessage galleryInfoRequest = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(address)
            };
            HttpResponseMessage response;
            response = await httpClient.SendAsync(galleryInfoRequest, ct);
            response.EnsureSuccessStatusCode();
            string responseString = await response.Content.ReadAsStringAsync(ct);
            return responseString[GALLERY_INFO_EXCLUDE_STRING.Length..];
        }

        /**
         * <exception cref="HttpRequestException"></exception>
         * <exception cref="TaskCanceledException"></exception>
        */
        public static async Task<string> GetServerTime(HttpClient httpClient, CancellationToken ct) {
            HttpRequestMessage serverTimeRequest = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(SERVER_TIME_ADDRESS)
            };
            HttpResponseMessage response;
            response = await httpClient.SendAsync(serverTimeRequest, ct);
            response.EnsureSuccessStatusCode();
            string responseString = await response.Content.ReadAsStringAsync(ct);
            // get numbers between ' and /'
            return responseString.Substring(responseString.Length - SERVER_TIME_EXCLUDE_STRING.Length, 10);
        }

        public static string[] GetImageFormats(ImageInfo[] imageInfos) {
            string[] imgFormats = new string[imageInfos.Length];
            for (int i = 0; i < imgFormats.Length; i++) {
                if (imageInfos[i].haswebp == 1) {
                    imgFormats[i] = "webp";
                }
                else if (imageInfos[i].hasavif == 1) {
                    imgFormats[i] = "avif";
                }
                else if (imageInfos[i].hasjxl == 1) {
                    imgFormats[i] = "jxl";
                }
            }
            return imgFormats;
        }

        public static string[] GetImageAddresses(ImageInfo[] imageInfos, string[] imgFormats, string serverTime) {
            string[] result = new string[imageInfos.Length];
            for (int i = 0; i < imageInfos.Length; i++) {
                string hash = imageInfos[i].hash;
                string oneTwoCharInt = Convert.ToInt32(hash[^1..] + hash[^3..^1], 16).ToString();
                result[i] = $"{IMAGE_BASE_DOMAIN}/{imgFormats[i]}/{serverTime}/{oneTwoCharInt}/{hash}.{imgFormats[i]}";
            }
            return result;
        }

        /**
         * <returns>The image <c>byte[]</c> if the given address is valid, otherwise <c>null</c>.</returns>
         * <exception cref="TaskCanceledException"></exception>
         */
        public static async Task<byte[]> GetImageBytesFromWeb(HttpClient httpClient, string address, CancellationToken ct) {
            HttpResponseMessage response;
            try {
                response = await httpClient.GetAsync(address, ct);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e) {
                if (e.StatusCode != HttpStatusCode.NotFound) {
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine("Status Code: " + e.StatusCode);
                }
                return null;
            }
            catch (TaskCanceledException) {
                throw;
            }
            return await response.Content.ReadAsByteArrayAsync(ct);
        }

        /**
         * <exception cref="TaskCanceledException"></exception>
         */
        public static async Task TryGetImageBytesFromWeb(DownloadInfo di, string imgAddress, string imgFormat, int idx) {
            foreach (string subdomain in POSSIBLE_IMAGE_SUBDOMAINS) {
                if (di.ct.IsCancellationRequested) {
                    break;
                }
                byte[] imageBytes;
                try {
                    imageBytes = await GetImageBytesFromWeb(di.httpClient, subdomain + imgAddress, di.ct);
                } catch (TaskCanceledException) {
                    throw;
                }
                if (imageBytes != null) {
                    try {
                        await File.WriteAllBytesAsync(Path.Combine(IMAGE_DIR, di.id, idx.ToString()) + '.' + imgFormat, imageBytes, di.ct);
                        di.bmItem.DispatcherQueue.TryEnqueue(() => { di.bmItem.UpdateSingleImage(idx); });
                        di.progressBar.DispatcherQueue.TryEnqueue(() => {
                            lock (di.progressBar) {
                                di.progressBar.Value++;
                            }
                        });
                    }
                    catch (DirectoryNotFoundException) {
                        break;
                    }
                    catch (IOException) {
                        break;
                    }
                    break;
                }
            }
        }

        /**
         * <exception cref="TaskCanceledException"></exception>
        */
        public static Task[] DownloadImages(DownloadInfo di, string[] imgAddresses, string[] imgFormats, List<int> indexes) {
            Directory.CreateDirectory(Path.Combine(IMAGE_DIR, di.id));

            /*
                example:
                imgAddresses.Length = 8, indexes = [0,1,4,5,7,9,10,11,14,15,17], concurrentTaskNum = 3
                11 / 3 = 3 r 2
                -----------------
                |3+1 | 3+1 |  3 |
                 0      7    14
                 1      9    15
                 4     10    17
                 5     11
            */
            int quotient = imgAddresses.Length / di.concurrentTaskNum;
            int remainder = imgAddresses.Length % di.concurrentTaskNum;
            Task[] tasks = new Task[di.concurrentTaskNum];

            int startIdx = 0;
            for (int i = 0; i < di.concurrentTaskNum; i++) {
                int thisStartIdx = startIdx;
                int thisJMax = quotient + (i < remainder ? 1 : 0);
                tasks[i] = Task.Run(async () => {
                    for (int j = 0; j < thisJMax; j++) {
                        int idx = thisStartIdx + j;
                        await TryGetImageBytesFromWeb(di, imgAddresses[idx], imgFormats[idx], indexes[idx]);
                    }
                }, di.ct);
                startIdx += thisJMax;
            }
            return tasks;
        }

        /**
         * <returns>The image indexes if the image directory exists, otherwise, throws <c>DirectoryNotFoundException</c></returns>
         * <exception cref="DirectoryNotFoundException"></exception>
         */
        public static List<int> GetMissingIndexes(Gallery gallery) {
            string imageDir = Path.Combine(IMAGE_DIR, gallery.id);
            List<int> missingIndexes = [];
            for (int i = 0; i < gallery.files.Length; i++) {
                string[] file = Directory.GetFiles(imageDir, i.ToString() + ".*");
                if (file.Length == 0) {
                    missingIndexes.Add(i);
                }
            }
            return missingIndexes;
        }

        public static string GetExceptionDetails(Exception e) {
            string output = "";
            string stacktrace = e.StackTrace ?? "";
            output += $"  {e.GetType().Name}: {e.Message}," + Environment.NewLine;
            while (e.InnerException != null) {
                e = e.InnerException;
                output += $"  {e.GetType().Name}: {e.Message}," + Environment.NewLine + ",";
            }
            output += stacktrace;

            return output;
        }

        public static void WriteObjectToJson(string path, object obj) {
            File.WriteAllText(path, JsonSerializer.Serialize(obj, serializerOptions));
        }
    }
}
