using HitomiScrollViewerAPI.Hubs;
using HitomiScrollViewerData;
using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace HitomiScrollViewerAPI.Download {
    public class Downloader(IServiceScope serviceScope) : IDisposable {
        private const int GALLERY_JS_EXCLUDE_LENGTH = 18; // length of the string "var galleryinfo = "
        public required IHubContext<DownloadHub, IDownloadClient> DownloadHubContext { get; init; }
        public string? ConnectionId { get; set; }
        public required int GalleryId { get; init; }
        public required Action<Downloader> DownloadCompleted { get; init; }
        public required Func<Downloader, Task> RequestGgjsFetch { get; init; }
        public DownloadStatus Status { get; set; } = DownloadStatus.Pending;

        private LiveServerInfo? _liveServerInfo;
        public LiveServerInfo? LiveServerInfo {
            get => _liveServerInfo;
            set {
                _liveServerInfo = value;
                LastLiveServerInfoUpdateTime = DateTime.UtcNow;
            }
        }
        public DateTime LastLiveServerInfoUpdateTime = DateTime.MinValue;

        private readonly HttpClient _httpClient = serviceScope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
        private readonly IConfiguration _appConfiguration = serviceScope.ServiceProvider.GetRequiredService<IConfiguration>();
        private readonly ILogger<Downloader> _logger = serviceScope.ServiceProvider.GetRequiredService<ILogger<Downloader>>();
        private CancellationTokenSource? _cts;
        private Gallery? _gallery;

        private void ChangeStatus(DownloadStatus status, string message) {
            Status = status;
            DownloadHubContext.Clients.Client(ConnectionId!).ReceiveStatus(status, message);
        }
        
        public async Task Start() {
            _logger.LogInformation("{GalleryId}: Attempting to start download", GalleryId);
            if (Status == DownloadStatus.Downloading) {
                return;
            }
            if (LiveServerInfo == null) {
                _ = RequestGgjsFetch(this);
                return;
            }
            if (_cts != null && !_cts.IsCancellationRequested) {
                return;
            }
            _cts?.Dispose();
            _cts = new();
            ChangeStatus(DownloadStatus.Downloading, "");

            using HitomiContext dbContext = new();
            _gallery ??= dbContext.Galleries.Find(GalleryId);
            if (_gallery == null) {
                string galleryInfoResponse;
                try {
                    galleryInfoResponse = await GetGalleryInfo(_cts.Token);
                    OriginalGalleryInfoDTO? ogi = JsonSerializer.Deserialize<OriginalGalleryInfoDTO>(galleryInfoResponse, OriginalGalleryInfoDTO.SERIALIZER_OPTIONS);
                    if (ogi == null) {
                        ChangeStatus(DownloadStatus.Failed, "Failed to parse gallery info.");
                        return;
                    }
                    _gallery = CreateGallery(ogi, dbContext);
                    if (_gallery == null) {
                        return;
                    }
                    await DownloadHubContext.Clients.Client(ConnectionId!).ReceiveGalleryCreated();
                } catch (HttpRequestException) {
                    ChangeStatus(DownloadStatus.Failed, "Failed to get gallery info.");
                    return;
                } catch (TaskCanceledException) {
                    ChangeStatus(DownloadStatus.Paused, "");
                    return;
                } catch (Exception e) {
                    _logger.LogError(e, "");
                    return;
                }
            } else {
                dbContext.Entry(_gallery).Collection(g => g.GalleryImages).Load();
            }
            int threadNum = dbContext.DownloadConfigurations.First().ThreadNum;
            GalleryImage[] missingGalleryImages = [.. Utils.GalleryFileUtil.GetMissingFiles(_gallery.Id, _gallery.GalleryImages)];
            _logger.LogInformation("{GalleryId}: Found {ImageCount} missing images", _gallery.Id, missingGalleryImages.Length);
            await DownloadHubContext.Clients.Client(ConnectionId!).ReceiveProgress(_gallery.GalleryImages.Count - missingGalleryImages.Length);
            await DownloadImages(threadNum, missingGalleryImages, _cts.Token);
            ChangeStatus(DownloadStatus.Completed, "");
            DownloadCompleted(this);
        }

        private string? _galleryInfoAddress;
        /**
         * <exception cref="HttpRequestException"></exception>
         * <exception cref="TaskCanceledException"></exception>
        */
        private async Task<string> GetGalleryInfo(CancellationToken ct) {
            _galleryInfoAddress ??= $"https://{_appConfiguration["HitomiServerInfoDomain"]}/galleries/{GalleryId}.js";
            HttpResponseMessage response = await _httpClient.GetAsync(_galleryInfoAddress, ct);
            response.EnsureSuccessStatusCode();
            string responseString = await response.Content.ReadAsStringAsync(ct);
            return responseString[GALLERY_JS_EXCLUDE_LENGTH..];
        }

        public Gallery? CreateGallery(OriginalGalleryInfoDTO original, HitomiContext dbContext) {
            GalleryLanguage? language = dbContext.GalleryLanguages.FirstOrDefault(l => l.EnglishName == original.Language);
            if (language == null) {
                ChangeStatus(DownloadStatus.Failed, $"Language {original.Language} not found");
                return null;
            }
            GalleryType? type = dbContext.GalleryTypes.FirstOrDefault(t => t.Value == original.Type);
            if (type == null) {
                ChangeStatus(DownloadStatus.Failed, $"Type {original.Type} not found");
                return null;
            }

            // add artist, group, character, parody (series) tags
            IEnumerable<Tag> GetTagsFromDictionary(Dictionary<string, string>[]? originalDictArr, TagCategory category) {
                if (originalDictArr == null) {
                    return [];
                }
                return originalDictArr.Select(dict => {
                    string tagValue = dict[OriginalGalleryInfoDTO.CATEGORY_PROP_KEY_DICT[category]];
                    return dbContext.Tags.FirstOrDefault(t => t.Category == category && t.Value == tagValue);
                }).Where(t => t != null).Cast<Tag>();
                // TODO maybe log and request database update if tag is null?
            }
            IEnumerable<Tag> tags =
                GetTagsFromDictionary(original.Artists, TagCategory.Artist)
                .Concat(GetTagsFromDictionary(original.Groups, TagCategory.Group))
                .Concat(GetTagsFromDictionary(original.Characters, TagCategory.Character))
                .Concat(GetTagsFromDictionary(original.Parodys, TagCategory.Series));
            // add male, female, and tag tags
            tags = tags.Concat(original.Tags.Select(compositeTag => {
                TagCategory category = compositeTag.Male == 1 ? TagCategory.Male : compositeTag.Female == 1 ? TagCategory.Female : TagCategory.Tag;
                return dbContext.Tags.FirstOrDefault(t => t.Category == category && t.Value == compositeTag.Tag);
            }).Where(t => t != null).Cast<Tag>());

            Gallery gallery = new() {
                Id = original.Id,
                Title = original.Title,
                JapaneseTitle = original.JapaneseTitle,
                Date = original.Date,
                SceneIndexes = original.SceneIndexes,
                Related = original.Related,
                LastDownloadTime = DateTime.UtcNow,
                Language = language,
                Type = type,
                GalleryImages = [.. original.Files.Select((f, i) => new GalleryImage() {
                    Index = i + 1,
                    FileName = (i + 1).ToString("D" + Math.Floor(Math.Log10(original.Files.Count) + 1)),
                    Hash = f.Hash,
                    Width = f.Width,
                    Height = f.Height,
                    Hasavif = f.Hasavif,
                    Haswebp = f.Haswebp,
                    Hasjxl = f.Hasjxl
                })],
                Tags = [.. tags]
            };
            dbContext.Galleries.Add(gallery);
            dbContext.SaveChanges();
            return gallery;
        }


        /**
         * <exception cref="TaskCanceledException"></exception>
        */
        private Task DownloadImages(int threadNum, GalleryImage[] galleryImages, CancellationToken ct) {
            /*
                example:
                totalCount = 8, indexes = [0,1,4,5,7,9,10,11,14,15,17], threadNum = 3
                11 / 3 = 3 r 2
                -----------------
                |3+1 | 3+1 |  3 |
                 0      7    14
                 1      9    15
                 4     10    17
                 5     11
            */
            int quotient = galleryImages.Length / threadNum;
            int remainder = galleryImages.Length % threadNum;
            Task[] tasks = new Task[threadNum];
            int startIdx = 0;
            for (int i = 0; i < threadNum; i++) {
                int localStartIdx = startIdx;
                int localJMax = quotient + (i < remainder ? 1 : 0);
                tasks[i] = Task.Run
                (
                    async () =>
                    {
                        for (int j = 0; j < localJMax; j++) {
                            int k = localStartIdx + j;
                            await DownloadImage(galleryImages[k], ct);
                        }
                    },
                    ct
                );
                startIdx += localJMax;
            }
            return Task.WhenAll(tasks);
        }

        private async Task DownloadImage(GalleryImage galleryImage, CancellationToken ct) {
            DateTime localLastUpdateTime = LastLiveServerInfoUpdateTime;
            while (localLastUpdateTime <= LastLiveServerInfoUpdateTime) {
                try {
                    HttpResponseMessage response = await _httpClient.GetAsync(GetImageAddress(LiveServerInfo!, galleryImage), ct);
                    response.EnsureSuccessStatusCode();
                    byte[] data = await response.Content.ReadAsByteArrayAsync(ct);
                    await Utils.GalleryFileUtil.WriteImageAsync(_gallery!, galleryImage, data);
                    return;
                } catch (HttpRequestException e) {
                    if (e.StatusCode == HttpStatusCode.NotFound) {
                        if (localLastUpdateTime < LastLiveServerInfoUpdateTime) {
                            localLastUpdateTime = LastLiveServerInfoUpdateTime;
                        } else {
                            _cts?.Cancel();
                            _ = RequestGgjsFetch(this);
                        }
                    } else {
                        Debug.WriteLine(e.Message);
                        Debug.WriteLine($"Fetching image at Index {galleryImage.Index} of {_gallery!.Id} failed. Status Code: {e.StatusCode}");
                        return;
                    }
                } catch (IOException) {
                    return;
                } catch (TaskCanceledException) {
                    return;
                }
            }
        }

        public string GetImageAddress(LiveServerInfo liveServerInfo, GalleryImage galleryImage) {
            string hashFragment = Convert.ToInt32(galleryImage.Hash[^1..] + galleryImage.Hash[^3..^1], 16).ToString();
            string subdomain = liveServerInfo.IsAAContains ^ liveServerInfo.SubdomainSelectionSet.Contains(hashFragment) ? "ba" : "aa";
            string fileExt = galleryImage.FileExt;
            return $"https://{subdomain}.{_appConfiguration["HitomiMainDomain"]}/{fileExt}/{liveServerInfo.ServerTime}/{hashFragment}/{galleryImage.Hash}.{fileExt}";
        }

        public void Pause() {
            _cts?.Cancel();
            DownloadHubContext.Clients.Client(ConnectionId!).ReceiveStatus(DownloadStatus.Paused, "Download paused");
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
            serviceScope.Dispose();
            _cts?.Dispose();
        }
    }
}
