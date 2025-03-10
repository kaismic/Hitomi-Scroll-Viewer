using ConsoleUtilities;
using HitomiScrollViewerAPI.Hubs;
using HitomiScrollViewerData;
using HitomiScrollViewerData.DbContexts;
using HitomiScrollViewerData.Entities;
using Microsoft.AspNetCore.SignalR;

namespace HitomiScrollViewerAPI {
    public class DatabaseInitializer(IHubContext<DbStatusHub, IStatusClient> hubContext) {
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
        private readonly IHubContext<DbStatusHub, IStatusClient> _hubContext = hubContext;

        public void Start() {
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
            CompleteInitialization(flagExists);
        }

        private void CompleteInitialization(bool fileExists) {
            if (!fileExists) {
                File.WriteAllText(DB_INIT_FLAG_PATH, "Delete this file to re-initialize database.");
            }
            IsInitialized = true;
            _hubContext.Clients.All.ReceiveStatus(InitStatus.Complete, -1);
        }

        private const int MAX_DESC_TEXT_LENGTH = 40;
        private static readonly ProgressBar _progressBar = new(10);
        private static readonly int _totalLeftAlignment = MAX_DESC_TEXT_LENGTH + _progressBar.TotalLength;

        private void AddDefaultDataAsync(HitomiContext context) {
            _hubContext.Clients.All.ReceiveStatus(InitStatus.InProgress, 0);
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
            _hubContext.Clients.All.ReceiveStatus(InitStatus.InProgress, 1);
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


            // add query configurations
            Console.Write("{0,-" + _totalLeftAlignment + "}", "Adding query configurations... ");
            _hubContext.Clients.All.ReceiveStatus(InitStatus.InProgress, 2);
            context.SearchQueryConfigurations.Add(new() {
                GalleryLanguage = context.GalleryLanguages.First(gl => gl.IsAll),
                GalleryType = context.GalleryTypes.First(gt => gt.IsAll)
            });
            context.BrowseQueryConfigurations.Add(new() {
                GalleryLanguage = context.GalleryLanguages.First(gl => gl.IsAll),
                GalleryType = context.GalleryTypes.First(gt => gt.IsAll)
            });
            context.SaveChanges();
            Console.WriteLine("  Complete");

            // add gallery sorts
            Console.Write("{0,-" + _totalLeftAlignment + "}", "Adding gallery sorts... ");
            _hubContext.Clients.All.ReceiveStatus(InitStatus.InProgress, 3);
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
            _hubContext.Clients.All.ReceiveStatus(InitStatus.InProgress, 4);
            Console.Write("{0,-" + _totalLeftAlignment + "}", "Adding example tag filters... ");
            IQueryable<Tag> tags = context.Tags;
            context.TagFilters.AddRange(
                new() {
                    Name = Resources.ExampleTagFilterNames.ExampleTagFilterName_1,
                    Tags = [
                        Utils.GetTag(tags, "full color", TagCategory.Tag)!,
                        Utils.GetTag(tags, "very long hair", TagCategory.Female)!,
                    ]
                },
                new() {
                    Name = Resources.ExampleTagFilterNames.ExampleTagFilterName_2,
                    Tags = [
                        Utils.GetTag(tags, "glasses", TagCategory.Female)!,
                        Utils.GetTag(tags, "sole male", TagCategory.Male)!,
                    ]
                },
                new() {
                    Name = Resources.ExampleTagFilterNames.ExampleTagFilterName_3,
                    Tags = [
                        Utils.GetTag(tags, "naruto", TagCategory.Series)!,
                        Utils.GetTag(tags, "big breasts", TagCategory.Female)!,
                    ]
                },
                new() {
                    Name = Resources.ExampleTagFilterNames.ExampleTagFilterName_4,
                    Tags = [
                        Utils.GetTag(tags, "non-h imageset", TagCategory.Tag)!
                    ]
                }
            );
            context.SaveChanges();
            Console.WriteLine("  Complete");
        }
    }
}
