﻿using ConsoleUtilities;
using HitomiScrollViewerAPI.Hubs;
using HitomiScrollViewerData;
using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.SignalR;

namespace HitomiScrollViewerAPI.Services {
    public class DbInitializeService(IHubContext<DbInitializeHub, IDbStatusClient> hubContext) : BackgroundService {
        private const string DB_INIT_FLAG_PATH = "db-init-flag.txt";
        private static readonly string[] ALPHABETS_WITH_123 =
            ["123", .. Enumerable.Range('a', 26).Select(intValue => Convert.ToChar(intValue).ToString())];

        private const string DB_RES_ROOT_DIR = "DatabaseResources";
        private static readonly string DELIMITER_FILE_PATH = Path.Combine(
            DB_RES_ROOT_DIR,
            "delimiter.txt"
        );
        private static readonly string LANGUAGES_FILE_PATH = Path.Combine(
            DB_RES_ROOT_DIR,
            "languages.txt"
        );
        private static readonly string TYPES_FILE_PATH = Path.Combine(
            DB_RES_ROOT_DIR,
            "types.txt"
        );
        private static readonly Dictionary<TagCategory, string> CATEGORY_DIR_DICT = new() {
            { TagCategory.Tag, "Tags" },
            { TagCategory.Male, "Males" },
            { TagCategory.Female, "Females" },
            { TagCategory.Artist, "Artists" },
            { TagCategory.Group, "Groups" },
            { TagCategory.Character, "Characters" },
            { TagCategory.Series, "Series" }
        };

        public static bool IsInitialized { get; private set; } = false;

        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            bool flagExists = File.Exists(DB_INIT_FLAG_PATH);
            if (!flagExists) {
                using HitomiContext context = new();
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
                Console.WriteLine("\n--- Starting database initialization ---\n");
                AddDefaultDataAsync(context);
                AddExampleTagFilters(context);
                Console.WriteLine("\n--- Database initialization complete ---\n");
            }
            return CompleteInitialization(flagExists);
        }

        private async Task CompleteInitialization(bool fileExists) {
            if (!fileExists) {
                await File.WriteAllTextAsync(DB_INIT_FLAG_PATH, "Delete this file to re-initialize database.");
            }
            IsInitialized = true;
            await hubContext.Clients.All.ReceiveStatus(DbInitStatus.Complete, -1);
        }

        private const int MAX_DESC_TEXT_LENGTH = 40;
        private static readonly ProgressBar _progressBar = new(10);
        private static readonly int _totalLeftAlignment = MAX_DESC_TEXT_LENGTH + _progressBar.TotalLength;

        private void AddDefaultDataAsync(HitomiContext context) {
            hubContext.Clients.All.ReceiveStatus(DbInitStatus.InProgress, 0);
            string delimiter = File.ReadAllText(DELIMITER_FILE_PATH);
            foreach (TagCategory category in Tag.TAG_CATEGORIES) {
                Console.Write("{0,-" + MAX_DESC_TEXT_LENGTH + "}", $"Adding {category} tags... ");
                int progress = 0;
                string categoryStr = CATEGORY_DIR_DICT[category];
                string dir = Path.Combine(DB_RES_ROOT_DIR, categoryStr);
                foreach (string alphanumStr in ALPHABETS_WITH_123) {
                    string path = Path.Combine(dir, $"{categoryStr.ToLower()}-{alphanumStr}.txt");
                    string[] tagInfoStrs = File.ReadAllLines(path);
                    context.Tags.AddRange(tagInfoStrs.Select(
                        tagInfoStr => {
                            string[] tagInfoArr = tagInfoStr.Split(delimiter);
                            return new Tag() {
                                Category = category,
                                Value = tagInfoArr[0],
                                GalleryCount = int.Parse(tagInfoArr[1])
                            };
                        }
                    ));
                    _progressBar.Report((double)++progress / ALPHABETS_WITH_123.Length);
                }
                _progressBar.Reset();
                Console.WriteLine("  Complete");
            }

            // add gallery languages and its local names
            hubContext.Clients.All.ReceiveStatus(DbInitStatus.InProgress, 1);
            Console.Write("{0,-" + _totalLeftAlignment + "}", "Adding gallery languages and types... ");
            string[][] languages = [.. File.ReadAllLines(LANGUAGES_FILE_PATH).Select(pair => pair.Split(delimiter))];
            context.GalleryLanguages.Add(new GalleryLanguage() {
                IsAll = true,
                EnglishName = "All",
                LocalName = "All" // TODO localize?
            });
            context.GalleryLanguages.AddRange(languages.Select(
                pair => {
                    return new GalleryLanguage() {
                        IsAll = false,
                        EnglishName = pair[0],
                        LocalName = pair[1]
                    };
                }
            ));
            // add gallery types
            string[] types = [.. File.ReadAllLines(TYPES_FILE_PATH)];
            context.GalleryTypes.Add(new GalleryType() {
                IsAll = true,
                Value = "All" // TODO localize?
            });
            context.GalleryTypes.AddRange(types.Select(t => new GalleryType() { IsAll = false, Value = t }));
            context.SaveChanges();
            Console.WriteLine("  Complete");


            // add configurations
            Console.Write("{0,-" + _totalLeftAlignment + "}", "Adding configurations... ");
            hubContext.Clients.All.ReceiveStatus(DbInitStatus.InProgress, 2);
            context.SearchConfigurations.Add(new() {
                IsAutoSaveEnabled = true,
                SelectedLanguage = context.GalleryLanguages.First(gl => gl.IsAll),
                SelectedType = context.GalleryTypes.First(gt => gt.IsAll)
            });
            context.BrowseConfigurations.Add(new() {
                SelectedLanguage = context.GalleryLanguages.First(gl => gl.IsAll),
                SelectedType = context.GalleryTypes.First(gt => gt.IsAll),
                ItemsPerPage = 8
            });
            context.DownloadConfigurations.Add(new() { ThreadNum = 1 });
            context.ViewConfigurations.Add(new() {
                ImagesPerPage = 2,
                Loop = true,
                ImageLayoutMode = ImageLayoutMode.Automatic,
                ViewDirection = ViewDirection.RTL,
                AutoPageFlipInterval = 8,
                AutoScrollMode = AutoScrollMode.Continuous,
                AutoScrollSpeed = 20,
                AutoScrollDistance = 80,
                AutoScrollInterval = 4
            });
            context.SaveChanges();
            Console.WriteLine("  Complete");

            // add gallery sorts
            Console.Write("{0,-" + _totalLeftAlignment + "}", "Adding gallery sorts... ");
            hubContext.Clients.All.ReceiveStatus(DbInitStatus.InProgress, 3);
            GallerySort[] sorts =
                [.. Enumerable.Range(0, Enum.GetNames<GalleryProperty>().Length)
                    .Select(i => new GallerySort() {
                        Property = (GalleryProperty)i,
                        SortDirection = SortDirection.Ascending,
                        IsActive = false
                    })
                ];
            // default GallerySort as LastDownloadTime Descending
            GallerySort lastDownloadTimeSort = sorts.First(s => s.Property == GalleryProperty.LastDownloadTime);
            lastDownloadTimeSort.IsActive = true;
            lastDownloadTimeSort.SortDirection = SortDirection.Descending;
            context.GallerySorts.AddRange(sorts);
            context.SaveChanges();
            Console.WriteLine("  Complete");
        }

        private void AddExampleTagFilters(HitomiContext context) {
            hubContext.Clients.All.ReceiveStatus(DbInitStatus.InProgress, 4);
            Console.Write("{0,-" + _totalLeftAlignment + "}", "Adding example tag filters... ");
            SearchConfiguration searchConfig = context.SearchConfigurations.First();
            context.Entry(searchConfig).Collection(c => c.TagFilters).Load();
            IQueryable<Tag> tags = context.Tags;
            searchConfig.TagFilters.AddRange(
                new() {
                    Name = Resources.ExampleTagFilterNames.ExampleTagFilterName_1,
                    Tags = [
                        TagUtils.GetTag(tags, "full color", TagCategory.Tag)!,
                        TagUtils.GetTag(tags, "very long hair", TagCategory.Female)!,
                    ]
                },
                new() {
                    Name = Resources.ExampleTagFilterNames.ExampleTagFilterName_2,
                    Tags = [
                        TagUtils.GetTag(tags, "glasses", TagCategory.Female)!,
                        TagUtils.GetTag(tags, "sole male", TagCategory.Male)!,
                    ]
                },
                new() {
                    Name = Resources.ExampleTagFilterNames.ExampleTagFilterName_3,
                    Tags = [
                        TagUtils.GetTag(tags, "naruto", TagCategory.Series)!,
                        TagUtils.GetTag(tags, "big breasts", TagCategory.Female)!,
                    ]
                },
                new() {
                    Name = Resources.ExampleTagFilterNames.ExampleTagFilterName_4,
                    Tags = [
                        TagUtils.GetTag(tags, "non-h imageset", TagCategory.Tag)!
                    ]
                }
            );
            context.SaveChanges();
            Console.WriteLine("  Complete");
        }
    }
}
