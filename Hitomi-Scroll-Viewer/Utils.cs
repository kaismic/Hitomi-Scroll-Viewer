using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hitomi_Scroll_Viewer
{
    public class Utils {
        public static readonly string ROOT_DIR = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HSV");
        public static readonly string IMAGE_DIR = Path.Combine(ROOT_DIR, "images");
        public static readonly string[] IMAGE_FORMATS = ["webp", "avif", "jxl"];

        public static readonly string REFERER = "https://hitomi.la/";
        public static readonly string IMAGE_BASE_DOMAIN = "hitomi.la";
        public static readonly string GALLERY_INFO_DOMAIN = "https://ltn.hitomi.la/galleries/";
        public static readonly string GALLERY_INFO_EXCLUDE_STRING = "var galleryinfo = ";
        public static readonly string SERVER_TIME_ADDRESS = "https://ltn.hitomi.la/gg.js";
        public static readonly string SERVER_TIME_EXCLUDE_STRING = "0123456789/'\r\n};";
        public static readonly string[] POSSIBLE_IMAGE_SUBDOMAINS = { "https://aa.", "https://ba." };
        public static readonly JsonSerializerOptions serializerOptions = new() { IncludeFields = true, WriteIndented = true };

        public static readonly int MAX_CONCURRENT_REQUEST = 4;

        // TODO
        // downloading items
        // image format for loop
        // remove bookmark limit (bookmarkfull)

        /**
         * <exception cref="HttpRequestException"></exception>
        */
        public static async Task<string> GetGalleryInfo(HttpClient httpClient, string id, CancellationToken ct) {
            string address = GALLERY_INFO_DOMAIN + id + ".js";
            HttpRequestMessage galleryInfoRequest = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(address)
            };
            HttpResponseMessage response;
            try {
                response = await httpClient.SendAsync(galleryInfoRequest, ct);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e) {
                throw e;
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested) {
                return null;
            }
            string responseString = await response.Content.ReadAsStringAsync(ct);
            return responseString[GALLERY_INFO_EXCLUDE_STRING.Length..];
        }

        /**
         * <exception cref="HttpRequestException"></exception>
        */
        public static async Task<string> GetServerTime(HttpClient httpClient, CancellationToken ct) {
            HttpRequestMessage serverTimeRequest = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(SERVER_TIME_ADDRESS)
            };
            HttpResponseMessage response;
            try {
                response = await httpClient.SendAsync(serverTimeRequest, ct);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e) {
                throw e;
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested) {
                return null;
            }
            string responseString = await response.Content.ReadAsStringAsync(ct);
            // get numbers between ' and /'
            return responseString.Substring(responseString.Length - SERVER_TIME_EXCLUDE_STRING.Length, 10);
        }

        public static string[] GetImageAddresses(string[] imgHashArr, string[] imgFormats, string serverTime) {
            string[] result = new string[imgHashArr.Length];
            for (int i = 0; i < imgHashArr.Length; i++) {
                string hash = imgHashArr[i];
                string format = imgFormats[i];
                string oneTwoCharInt = Convert.ToInt32(hash[^1..] + hash[^3..^1], 16).ToString();
                result[i] = $"{IMAGE_BASE_DOMAIN}/{format}/{serverTime}/{oneTwoCharInt}/{hash}.{format}";
            }
            return result;
        }

        /**
         * <returns>The image <c>byte[]</c> if the given address is valid, otherwise <c>null</c>.</returns>
         */
        public static async Task<byte[]> GetImageBytesFromWeb(HttpClient httpClient, string address, CancellationToken ct) {
            HttpRequestMessage request = new() {
                Method = HttpMethod.Get,
                RequestUri = new Uri(address),
            };
            HttpResponseMessage response;
            try {
                response = await httpClient.SendAsync(request, ct);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e) {
                if (e.StatusCode != HttpStatusCode.NotFound) {
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine("Status Code: " + e.StatusCode);
                }
                return null;
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested) {
                return null;
            }
            return await response.Content.ReadAsByteArrayAsync(ct);
        }

        public static async Task TryGetImageBytesFromWeb(
            HttpClient httpClient,
            string id,
            string imgAddress,
            string imgFormat,
            int idx,
            ProgressBar progressBar,
            CancellationToken ct
            ) {
            foreach (string subdomain in POSSIBLE_IMAGE_SUBDOMAINS) {
                if (ct.IsCancellationRequested) {
                    break;
                }
                byte[] imageBytes = await GetImageBytesFromWeb(httpClient, subdomain + imgAddress, ct);
                if (imageBytes != null) {
                    try {
                        await File.WriteAllBytesAsync(Path.Combine(IMAGE_DIR, id, idx.ToString()) + '.' + imgFormat, imageBytes, ct);
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
            progressBar.DispatcherQueue.TryEnqueue(() => {
                lock (progressBar) {
                    progressBar.Value++;
                }
            });
        }

        public static Task[] DownloadImages(
            HttpClient httpClient,
            string id,
            string[] imgAddresses,
            string[] imgFormats,
            int[] indexes,
            int concurrentTaskNum,
            ProgressBar progressBar,
            CancellationToken ct
            ) {

            Directory.CreateDirectory(Path.Combine(IMAGE_DIR, id));

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
            int quotient = imgAddresses.Length / concurrentTaskNum;
            int remainder = imgAddresses.Length % concurrentTaskNum;
            Task[] tasks = new Task[concurrentTaskNum];

            int startIdx = 0;
            for (int i = 0; i < concurrentTaskNum; i++) {
                int thisStartIdx = startIdx;
                int thisJMax = quotient + (i < remainder ? 1 : 0);
                tasks[i] = Task.Run(async () => {
                    for (int j = 0; j < thisJMax; j++) {
                        int idx = thisStartIdx + j;
                        await TryGetImageBytesFromWeb(httpClient, id, imgAddresses[idx], imgFormats[idx], indexes[idx], progressBar, ct);
                    }
                }, ct);
                startIdx += thisJMax;
            }
            return tasks;
        }
    }
}
