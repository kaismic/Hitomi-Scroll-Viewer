using HitomiScrollViewerAPI.Hubs;
using HitomiScrollViewerAPI.Services;
using HitomiScrollViewerData;
using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace HitomiScrollViewerAPI.Download {
    public class Downloader : IDisposable {
        private const int GALLERY_JS_EXCLUDE_LENGTH = 18; // length of the string "var galleryinfo = "
        public required DownloadItemDTO DownloadItem { get; init; }
        public required IHubContext<DownloadHub, IDownloadClient> DownloadHubContext { get; init; }
        public required HitomiUrlService HitomiUrlService { get; init; }
        public required string ConnectionId { get; init; }
        public required HttpClient HttpClient { get; init; }
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

        private CancellationTokenSource? _cts;
        private Gallery? _gallery;
        
        public async Task Start() {
            if (Status == DownloadStatus.Downloading) {
                return;
            }
            Status = DownloadStatus.Downloading;
            if (LiveServerInfo == null) {
                await RequestGgjsFetch(this);
            }
            if (_cts != null && !_cts.IsCancellationRequested) {
                return;
            }
            _cts?.Dispose();
            _cts = new();

            using HitomiContext dbContext = new();
            _gallery ??= dbContext.Galleries.Find(DownloadItem.GalleryId);
            if (_gallery == null) {
                string galleryInfoResponse;
                try {
                    galleryInfoResponse = await GetGalleryInfo(_cts.Token);
                    OriginalGalleryInfoDTO? ogi = JsonSerializer.Deserialize<OriginalGalleryInfoDTO>(galleryInfoResponse, OriginalGalleryInfoDTO.SERIALIZER_OPTIONS);
                    if (ogi == null) {
                        Status = DownloadStatus.Failed;
                        DownloadHubContext.Clients.Client(ConnectionId).ReceiveStatus(DownloadStatus.Failed, "Failed to parse gallery info.");
                        return;
                    }
                    _gallery = CreateGallery(ogi, dbContext);
                    if (_gallery == null) {
                        Status = DownloadStatus.Failed;
                        return;
                    }
                } catch (HttpRequestException) {
                    Status = DownloadStatus.Failed;
                    DownloadHubContext.Clients.Client(ConnectionId).ReceiveStatus(DownloadStatus.Failed, "Failed to get gallery info.");
                    return;
                } catch (TaskCanceledException) {
                    Status = DownloadStatus.Paused;
                    return;
                }
            } else {
                dbContext.Entry(_gallery).Collection(g => g.GalleryImages).Load();
            }
            int threadNum = dbContext.DownloadConfigurations.First().ThreadNum;
            GalleryImage[] missingGalleryImages = [.. GalleryFileService.GetMissingFiles(_gallery.Id, _gallery.GalleryImages)];
            DownloadHubContext.Clients.Client(ConnectionId).ReceiveProgress(_gallery.GalleryImages.Count - missingGalleryImages.Length);
            await DownloadImages(threadNum, missingGalleryImages, _cts.Token);
            Status = DownloadStatus.Completed;
            DownloadHubContext.Clients.Client(ConnectionId).ReceiveStatus(DownloadStatus.Completed, "Download completed");
            DownloadCompleted(this);
        }

        /**
         * <exception cref="HttpRequestException"></exception>
         * <exception cref="TaskCanceledException"></exception>
        */
        private async Task<string> GetGalleryInfo(CancellationToken ct) {
            HttpResponseMessage response = await HttpClient.GetAsync(HitomiUrlService.GetHitomiGalleryInfoAddress(DownloadItem.GalleryId), ct);
            response.EnsureSuccessStatusCode();
            string responseString = await response.Content.ReadAsStringAsync(ct);
            return responseString[GALLERY_JS_EXCLUDE_LENGTH..];
        }

        public Gallery? CreateGallery(OriginalGalleryInfoDTO original, HitomiContext dbContext) {
            Gallery? gallery = dbContext.Galleries.Find(original.Id);
            if (gallery != null) {
                DownloadHubContext.Clients.Client(ConnectionId).ReceiveStatus(DownloadStatus.Failed, $"Gallery with id {original.Id} already exists");
                return null;
            }
            GalleryLanguage? language = dbContext.GalleryLanguages.FirstOrDefault(l => l.EnglishName == original.Language);
            if (language == null) {
                DownloadHubContext.Clients.Client(ConnectionId).ReceiveStatus(DownloadStatus.Failed, $"Language {original.Language} not found");
                return null;
            }
            GalleryType? type = dbContext.GalleryTypes.FirstOrDefault(t => t.Value == original.Type);
            if (type == null) {
                DownloadHubContext.Clients.Client(ConnectionId).ReceiveStatus(DownloadStatus.Failed, $"Type {original.Type} not found");
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

            gallery = new() {
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
            while (localLastUpdateTime < LastLiveServerInfoUpdateTime) {
                try {
                    HttpResponseMessage response = await HttpClient.GetAsync(GetImageAddress(LiveServerInfo!, galleryImage), ct);
                    response.EnsureSuccessStatusCode();
                    byte[] data = await response.Content.ReadAsByteArrayAsync(ct);
                    await GalleryFileService.WriteImageAsync(_gallery!, galleryImage, data);
                    return;
                } catch (HttpRequestException e) {
                    if (e.StatusCode == HttpStatusCode.NotFound) {
                        if (localLastUpdateTime < LastLiveServerInfoUpdateTime) {
                            localLastUpdateTime = LastLiveServerInfoUpdateTime;
                        } else {
                            _cts?.Cancel();
                            Status = DownloadStatus.Pending;
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
            return $"https://{subdomain}.{HitomiUrlService.HitomiMainDomain}/{fileExt}/{liveServerInfo.ServerTime}/{hashFragment}/{galleryImage.Hash}.{fileExt}";
        }

        public void Pause() {
            _cts?.Cancel();
            DownloadHubContext.Clients.Client(ConnectionId).ReceiveStatus(DownloadStatus.Paused, "Download paused");
        }

        public void Remove() {
            _cts?.Cancel();
            DownloadHubContext.Clients.Client(ConnectionId).ReceiveStatus(DownloadStatus.Removed, "Download removed");
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
            _cts?.Dispose();
        }
    }
}
