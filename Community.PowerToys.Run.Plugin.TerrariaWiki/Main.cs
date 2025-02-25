using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Web;
using Wox.Plugin;
using BrowserInfo = Wox.Plugin.Common.DefaultBrowserInfo;

namespace Community.PowerToys.Run.Plugin.TerrariaWiki
{
    /// <summary>
    /// Main class of this plugin that implement all used interfaces.
    /// </summary>
    public class Main : IPlugin, IContextMenu, IDisposable
    {
        /// <summary>
        /// ID of the plugin.
        /// </summary>
        public static string PluginID => "C07E4E15D18048B7A98E6B4DF72466E5";

        /// <summary>
        /// Name of the plugin.
        /// </summary>
        public string Name => "TerrariaWiki";

        /// <summary>
        /// Description of the plugin.
        /// </summary>
        public string Description => "Search the Terraria wiki.gg wiki page";

        private string IconPath { get; set; }

        private bool Disposed { get; set; }

        private HttpClientHandler clientHandler = null;
        private HttpClient httpClient = null;

        /// <summary>
        /// Return a filtered list, based on the given query.
        /// </summary>
        /// <param name="query">The query to filter the list.</param>
        /// <returns>A filtered list, can be empty when nothing was found.</returns>
        public List<Result> Query(Query query)
        {
            Task<WikiSearchResults> searchResultsTask = Task.Run(() =>
            {
                return httpClient.GetFromJsonAsync<WikiSearchResults>($"https://terraria.wiki.gg/api.php?action=query&list=search&srsearch={HttpUtility.HtmlEncode(query.Search)}&format=json");
            });
            WikiSearchResults searchResults = searchResultsTask.GetAwaiter().GetResult();

            List<Result> results = [];

            if (searchResults?.Query?.Search != null)
            {
                foreach (var result in searchResults.Query.Search)
                {
                    string snippet = result.Snippet;
                    // Remove <span class="searchmatch">...</span>
                    // This isn't the best way to do it but it works alright for this wiki
                    snippet = snippet.Replace("<span class=\"searchmatch\">", "");
                    snippet = snippet.Replace("</span>", "");

                    // The snippet also contains html characters (ex. &#39;)
                    snippet = HttpUtility.HtmlDecode(snippet);

                    // TODO: Check whether we're at a limit and this snippet continues or not, using elipsis always is bad
                    snippet += "...";
                    
                    results.Add(new Result
                    {
                        QueryTextDisplay = result.Title,
                        IcoPath = IconPath,
                        Title = result.Title,
                        SubTitle = snippet ?? "Terraria Wiki Search Result",
                        ToolTipData = new ToolTipData(result.Title, snippet ?? "Search Result from Terraria Wiki"),
                        Action = _ =>
                        {
                            string url = $"https://terraria.wiki.gg/wiki/{System.Uri.EscapeDataString(result.Title)}";
                            return Wox.Infrastructure.Helper.OpenCommandInShell(BrowserInfo.Path, BrowserInfo.ArgumentsPattern, url);
                        },
                    });
                }
            }
            else
            {
                results.Add(new Result
                {
                    QueryTextDisplay = "",
                    IcoPath = IconPath,
                    Title = "No results found",
                    SubTitle = "Try a different search term",
                    ToolTipData = new ToolTipData("No results", "No articles found on Terraria Wiki for your search term."),
                    Action = _ => { return false; }
                });
            }

            return results;
        }

        /// <summary>
        /// Initialize the plugin with the given <see cref="PluginInitContext"/>.
        /// </summary>
        /// <param name="context">The <see cref="PluginInitContext"/> for this plugin.</param>
        public void Init(PluginInitContext context)
        {
            IconPath = "Images/favicon.png";

            clientHandler = new()
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };
            httpClient = new(clientHandler);

            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("PowerToys/Community.PowerToys.Run.Plugin.TerrariaWiki");
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
        }

        /// <summary>
        /// Return a list context menu entries for a given <see cref="Result"/> (shown at the right side of the result).
        /// </summary>
        /// <param name="selectedResult">The <see cref="Result"/> for the list with context menu entries.</param>
        /// <returns>A list context menu entries.</returns>
        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            return [];
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Wrapper method for <see cref="Dispose()"/> that dispose additional objects and events form the plugin itself.
        /// </summary>
        /// <param name="disposing">Indicate that the plugin is disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed || !disposing)
            {
                return;
            }

            Disposed = true;
        }
    }

    // Types from wikimedia
    public class WikiSearchResults
    {
        public string Batchcomplete { get; set; }

        //[JsonProperty("continue")]
        //public ContinueInfo ContinueInfo { get; set; }

        public QueryInfo Query { get; set; }
    }

    public class ContinueInfo
    {
        public int Sroffset { get; set; }
        public string Continue { get; set; }
    }

    public class QueryInfo
    {
        public SearchInfo Searchinfo { get; set; }
        public List<SearchItem> Search { get; set; }
    }

    public class SearchInfo
    {
        public int Totalhits { get; set; }
    }

    public class SearchItem
    {
        public int Ns { get; set; }
        public string Title { get; set; }
        public int Pageid { get; set; }
        public int Size { get; set; }
        public int Wordcount { get; set; }
        public string Snippet { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
