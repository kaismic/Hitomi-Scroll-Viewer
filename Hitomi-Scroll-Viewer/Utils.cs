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

        // TODO
        // downloading items
        // remove bookmark limit (bookmarkfull)
        // handle window closing if something downloading or bookmarking ask the user to wait or cancel downloading
        // when everything is clear then close window

        public static void DeleteGallery(Gallery removingGallery) {
            string path = Path.Combine(IMAGE_DIR, removingGallery.id);
            if (Directory.Exists(path)) Directory.Delete(path, true);
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
            catch (TaskCanceledException e) {
                throw e;
            }
            return await response.Content.ReadAsByteArrayAsync(ct);
        }

        /**
         * <exception cref="TaskCanceledException"></exception>
         */
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
                byte[] imageBytes;
                try {
                    imageBytes = await GetImageBytesFromWeb(httpClient, subdomain + imgAddress, ct);
                } catch (TaskCanceledException e) {
                    throw e;
                }
                if (imageBytes != null) {
                    try {
                        await File.WriteAllBytesAsync(Path.Combine(IMAGE_DIR, id, idx.ToString()) + '.' + imgFormat, imageBytes, ct);
                        progressBar.DispatcherQueue.TryEnqueue(() => {
                            lock (progressBar) {
                                progressBar.Value++;
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

        /**
         * <returns>The image indexes <c>int[]</c> if the image directory exists.</returns>
         * <exception cref="DirectoryNotFoundException"></exception>
         */
        public static int[] GetMissingIndexes(Gallery gallery) {
            string imageDir = Path.Combine(IMAGE_DIR, gallery.id);
            int[] missingIndexes = new int[gallery.files.Length];
            int missingCount = 0;
            for (int i = 0; i < missingIndexes.Length; i++) {
                string[] file = Directory.GetFiles(imageDir, i.ToString() + ".*");
                if (file.Length == 0) {
                    missingIndexes[missingCount] = i;
                    missingCount++;
                }
            }
            return missingIndexes[..missingCount];
        }
    }
}
