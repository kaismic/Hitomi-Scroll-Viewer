using Hitomi_Scroll_Viewer.SearchPageComponent;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Hitomi_Scroll_Viewer {
    public static class Utils {
        public static readonly string ROOT_DIR = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HSV");
        public static readonly string IMAGE_DIR = Path.Combine(ROOT_DIR, "images");
        public static readonly string SETTINGS_PATH = Path.Combine(ROOT_DIR, "settings.json");
        public static readonly string LOGS_PATH = Path.Combine(ROOT_DIR, "logs.txt");

        public static readonly string REFERER = "https://hitomi.la/";
        public static readonly string BASE_DOMAIN = "hitomi.la";
        public static readonly string GALLERY_INFO_DOMAIN = "https://ltn.hitomi.la/galleries/";
        public static readonly string GALLERY_INFO_EXCLUDE_STRING = "var galleryinfo = ";
        public static readonly string GG_JS_ADDRESS = "https://ltn.hitomi.la/gg.js";
        public static readonly string SERVER_TIME_EXCLUDE_STRING = "0123456789/'\r\n};";

        /*
         * apparently I can't just use Environment.NewLine as separator
         * because of this TextBox bug which somehow converts \r\n to \r and it's still not fixed...
         * https://github.com/microsoft/microsoft-ui-xaml/issues/1826
         * https://stackoverflow.com/questions/35138047/uwp-textbox-selectedtext-changes-r-n-to-r
        */
        public static readonly string[] NEW_LINE_SEPS = [Environment.NewLine, "\r"];
        public static readonly JsonSerializerOptions serializerOptions = new() { IncludeFields = true, WriteIndented = true };
        public static readonly StringSplitOptions STR_SPLIT_OPTION = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

        public struct DownloadInfo {
            public HttpClient httpClient;
            public string id;
            public int concurrentTaskNum;
            public ProgressBar progressBar;
            public BookmarkItem bmItem;
            public CancellationToken ct;
            public HashSet<string> subdomainSelectorSet;
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
            HttpResponseMessage response = await httpClient.SendAsync(galleryInfoRequest, ct);
            response.EnsureSuccessStatusCode();
            string responseString = await response.Content.ReadAsStringAsync(ct);
            return responseString[GALLERY_INFO_EXCLUDE_STRING.Length..];
        }

        /**
         * <exception cref="HttpRequestException"></exception>
         * <exception cref="TaskCanceledException"></exception>
        */
        public static async Task<string> GetggjsFile(HttpClient httpClient, CancellationToken ct) {
            HttpRequestMessage req = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(GG_JS_ADDRESS)
            };
            HttpResponseMessage response = await httpClient.SendAsync(req, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }

        public static HashSet<string> ExtractSubdomainSelectionSet(string ggjs) {
            string pat = @"case (\d+)";
            MatchCollection matches = Regex.Matches(ggjs, pat);
            return matches.Select(match => match.Groups[1].Value).ToHashSet();
        }

        public static (string notContains, string contains) ExtractSubdomainOrder(string ggjs) {
            string pat = @"var o = (\d);";
            Match match = Regex.Match(ggjs, pat);
            return match.Groups[1].Value == "0" ? ("aa", "ba") : ("ba", "aa");
        }

        public static string[] GetImageAddresses(ImageInfo[] imageInfos, string[] imgFormats, string ggjs) {
            string serverTime = ggjs.Substring(ggjs.Length - SERVER_TIME_EXCLUDE_STRING.Length, 10);
            HashSet<string> subdomainFilterSet = ExtractSubdomainSelectionSet(ggjs);
            (string notContains, string contains) = ExtractSubdomainOrder(ggjs);

            string[] result = new string[imageInfos.Length];
            for (int i = 0; i < imageInfos.Length; i++) {
                string hash = imageInfos[i].hash;
                string subdomainAndAddressValue = Convert.ToInt32(hash[^1..] + hash[^3..^1], 16).ToString();
                string subdomain = subdomainFilterSet.Contains(subdomainAndAddressValue) ? contains : notContains;
                result[i] = $"https://{subdomain}.{BASE_DOMAIN}/{imgFormats[i]}/{serverTime}/{subdomainAndAddressValue}/{hash}.{imgFormats[i]}";
            }
            return result;
        }

        public static string[] GetImageFormats(ImageInfo[] imageInfos) {
            string[] imgFormats = new string[imageInfos.Length];
            for (int i = 0; i < imgFormats.Length; i++) {
                if (imageInfos[i].haswebp == 1) {
                    imgFormats[i] = "webp";
                } else if (imageInfos[i].hasavif == 1) {
                    imgFormats[i] = "avif";
                } else if (imageInfos[i].hasjxl == 1) {
                    imgFormats[i] = "jxl";
                }
            }
            return imgFormats;
        }

        /**
         * <exception cref="TaskCanceledException"></exception>
         */
        public static async Task FetchImage(DownloadInfo di, string imgAddress, string imgFormat, int idx) {
            try {
                HttpResponseMessage response = null;
                try {
                    response = await di.httpClient.GetAsync(imgAddress, di.ct);
                    response.EnsureSuccessStatusCode();
                } catch (HttpRequestException e) {
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine("Status Code: " + e.StatusCode);
                    return;
                }
                try {
                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync(di.ct);
                    await File.WriteAllBytesAsync(Path.Combine(IMAGE_DIR, di.id, idx.ToString()) + '.' + imgFormat, imageBytes, di.ct);
                } catch (DirectoryNotFoundException) {
                    return;
                } catch (IOException) {
                    return;
                }
            } catch (TaskCanceledException) {
                throw;
            }
        }

        /**
         * <exception cref="TaskCanceledException"></exception>
        */
        public static Task DownloadImages(DownloadInfo di, string[] imgAddresses, string[] imgFormats, List<int> missingIndexes) {
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
                        int i = thisStartIdx + j;
                        await FetchImage(di, imgAddresses[i], imgFormats[i], missingIndexes[i])
                        .ContinueWith(
                            (task) => {
                                if (task.IsCompletedSuccessfully) {
                                    di.bmItem.DispatcherQueue.TryEnqueue(() => { di.bmItem.UpdateSingleImage(missingIndexes[i]); });
                                    di.progressBar.DispatcherQueue.TryEnqueue(() => {
                                        lock (di.progressBar) {
                                            di.progressBar.Value++;
                                        }
                                    });
                                }
                            },
                            di.ct
                        );
                    }
                }, di.ct);
                startIdx += thisJMax;
            }
            return Task.WhenAll(tasks);
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
