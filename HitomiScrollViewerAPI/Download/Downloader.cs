using HitomiScrollViewerAPI.Hubs;
using HitomiScrollViewerData;
using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.DTOs;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.SignalR;
using System.Net;
using System.Text.Json;

namespace HitomiScrollViewerAPI.Download {
    public class Downloader : IDisposable {
        private const int GALLERY_JS_EXCLUDE_LENGTH = 18; // length of the string "var galleryinfo = "
        public required int GalleryId { get; init; }
        public required Action<Downloader> RemoveSelf { get; init; }
        public required Func<Downloader, Task> RequestLiveServerInfoUpdate { get; init; }
        public DownloadStatus Status { get; set; } = DownloadStatus.Paused;

        private LiveServerInfo? _liveServerInfo;
        public LiveServerInfo? LiveServerInfo {
            get => _liveServerInfo;
            set {
                _liveServerInfo = value;
                LastLiveServerInfoUpdateTime = DateTime.UtcNow;
            }
        }
        public DateTime LastLiveServerInfoUpdateTime = DateTime.MinValue;

        private readonly IServiceScope _serviceScope;
        private readonly IConfiguration _appConfiguration;
        private readonly ILogger<Downloader> _logger;
        private readonly IHubContext<DownloadHub, IDownloadClient> _hubContext;
        private readonly HttpClient _httpClient;
        private CancellationTokenSource? _cts;
        private Gallery? _gallery;
        private int _progress = 0;
        private bool _liveServerInfoUpdated = false;

        public Downloader(IServiceScope serviceScope) {
            _serviceScope = serviceScope;
            _appConfiguration = _serviceScope.ServiceProvider.GetRequiredService<IConfiguration>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<Downloader>>();
            _hubContext = _serviceScope.ServiceProvider.GetRequiredService<IHubContext<DownloadHub, IDownloadClient>>();
            _httpClient = _serviceScope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
            _httpClient.DefaultRequestHeaders.Referrer = new Uri("https://" + _appConfiguration["HitomiClientDomain"]!);
        }

        private void ChangeStatus(DownloadStatus status, string? message = null) {
            Status = status;
            switch (status) {
                case DownloadStatus.Completed:
                    _hubContext.Clients.All.ReceiveComplete(GalleryId);
                    break;
                case DownloadStatus.Failed:
                    _hubContext.Clients.All.ReceiveFailure(GalleryId, message ?? throw new ArgumentNullException(nameof(message)));
                    break;
                default:
                    _hubContext.Clients.All.ReceiveStatus(GalleryId, status);
                    break;
            }
        }

        public async Task Start() {
            if (Status == DownloadStatus.Downloading) {
                return;
            }
            if (LiveServerInfo == null) {
                _ = RequestLiveServerInfoUpdate(this);
                return;
            }
            _liveServerInfoUpdated = false;
            _cts?.Dispose();
            _cts = new();
            ChangeStatus(DownloadStatus.Downloading);
            _logger.LogInformation("{GalleryId}: Starting download", GalleryId);

            int threadNum = 1;
            using (HitomiContext dbContext = new()) {
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
                    } catch (HttpRequestException) {
                        ChangeStatus(DownloadStatus.Failed, "Failed to get gallery info.");
                        return;
                    } catch (TaskCanceledException) {
                        return;
                    } catch (Exception e) {
                        _logger.LogError(e, "");
                        return;
                    }
                } else {
                    if (_gallery.Images == null) {
                        dbContext.Entry(_gallery).Collection(g => g.Images).Load();
                        if (_gallery.Images == null) {
                            throw new InvalidOperationException("_gallery.GalleryImages is null after loading images");
                        }
                    }
                }
                await _hubContext.Clients.All.ReceiveGalleryAvailable(GalleryId);
                threadNum = dbContext.DownloadConfigurations.First().ThreadNum;
            }
            
            GalleryImage[] missingGalleryImages = [.. Utils.GalleryFileUtil.GetMissingImages(_gallery.Id, _gallery.Images)];
            _logger.LogInformation("{GalleryId}: Found {ImageCount} missing images", _gallery.Id, missingGalleryImages.Length);
            _progress = _gallery.Images.Count - missingGalleryImages.Length;
            await _hubContext.Clients.All.ReceiveProgress(GalleryId, _progress);
            try {
                await DownloadImages(threadNum, missingGalleryImages, _cts.Token);
            } catch (TaskCanceledException) {
                // if download is canceled not due to user requesting pause, then request LiveServerInfo update
                if (Status != DownloadStatus.Paused) {
                    if (_liveServerInfoUpdated) {
                        ChangeStatus(DownloadStatus.Failed, "Download failed due to an unknown error.");
                    } else {
                        _liveServerInfoUpdated = true;
                        ChangeStatus(DownloadStatus.WaitingLSIUpdate);
                        _ = RequestLiveServerInfoUpdate(this);
                    }
                }
                return;
            } catch (Exception e) {
                _logger.LogError(e, "");
                ChangeStatus(DownloadStatus.Failed, "Download failed due to an unknown error.");
                return;
            }
            missingGalleryImages = [.. Utils.GalleryFileUtil.GetMissingImages(_gallery.Id, _gallery.Images)];
            if (missingGalleryImages.Length > 0) {
                ChangeStatus(DownloadStatus.Failed, $"Failed to download {missingGalleryImages.Length} images.");
            } else {
                ChangeStatus(DownloadStatus.Completed);
                RemoveSelf(this);
            }
        }

        private string? _galleryInfoAddress;
        /**
         * <exception cref="HttpRequestException"></exception>
         * <exception cref="TaskCanceledException"></exception>
        */
        private async Task<string> GetGalleryInfo(CancellationToken ct) {
            _galleryInfoAddress ??= $"https://ltn.{_appConfiguration["HitomiServerDomain"]}/galleries/{GalleryId}.js";
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
                Images = [.. original.Files.Select((f, i) => new GalleryImage() {
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

        private static readonly string[] IMAGE_FILE_EXTS = ["avif", "webp"];
        private async Task DownloadImage(GalleryImage galleryImage, CancellationToken ct) {
            DateTime localLastUpdateTime = LastLiveServerInfoUpdateTime;
            while (true) {
                foreach (string fileExt in IMAGE_FILE_EXTS) {
                    try {
                        HttpResponseMessage response = await _httpClient.GetAsync(GetImageAddress(LiveServerInfo!, galleryImage, fileExt), ct);
                        response.EnsureSuccessStatusCode();
                        byte[] data = await response.Content.ReadAsByteArrayAsync(CancellationToken.None);
                        await Utils.GalleryFileUtil.WriteImageAsync(_gallery!, galleryImage, data, fileExt);
                        await _hubContext.Clients.All.ReceiveProgress(GalleryId, Interlocked.Increment(ref _progress));
                        return;
                    } catch (HttpRequestException e) {
                        if (e.StatusCode == HttpStatusCode.NotFound) {
                            if (localLastUpdateTime < LastLiveServerInfoUpdateTime) {
                                // LiveServerInfo has been updated, so update the localLastUpdateTime and continue
                                localLastUpdateTime = LastLiveServerInfoUpdateTime;
                            } else {
                                // cancel to request LiveServerInfo update
                                _cts?.Cancel();
                                return;
                            }
                        } else {
                            _logger.LogError(e, "Failed to download image at index {Index}", galleryImage.Index);
                            return;
                        }
                    } catch (TaskCanceledException) {
                        throw;
                    } catch (Exception e) {
                        _logger.LogError(e, "Failed to download image at index {Index}", galleryImage.Index);
                        return;
                    }
                }
            }
        }

        public string GetImageAddress(LiveServerInfo liveServerInfo, GalleryImage galleryImage, string fileExt) {
            string hashFragment = Convert.ToInt32(galleryImage.Hash[^1..] + galleryImage.Hash[^3..^1], 16).ToString();
            char subdomainChar2 = liveServerInfo.IsContains ^ liveServerInfo.SubdomainSelectionSet.Contains(hashFragment) ? '1' : '2';
            string subdomain = $"{fileExt[0]}{subdomainChar2}";
            return $"https://{subdomain}.{_appConfiguration["HitomiServerDomain"]}/{liveServerInfo.ServerTime}/{hashFragment}/{galleryImage.Hash}.{fileExt}";
        }

        public void Pause() {
            if (Status == DownloadStatus.Paused) {
                return;
            }
            ChangeStatus(DownloadStatus.Paused);
            _cts?.Cancel();
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
            _serviceScope.Dispose();
            _cts?.Dispose();
        }
    }
}
