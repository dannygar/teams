using System;
using System.Collections.Generic;
using TeamsMessagingExtensionsSearchAuthConfig.Models;

namespace TeamsMessagingExtensionsSearchAuthConfig.Extensions
{
    public static class SearchResultConverter
    {
        public static CustomSearchModel ToCustomSearchResult(WebPage webPage)
        {
            var article = new CustomSearchModel
            {
                Id = Guid.NewGuid().ToString(),
                Url = webPage.url,
                ThumbnailUrl = "https://studentcommunity.ansys.com/Content/Images/admin-icon.png",
                Name = webPage.name,
                Description = webPage.snippet,
                DatePublished = webPage.dateLastCrawled
            };



            return article;
        }

        public static List<CustomSearchModel> ToWebArticleList(BingCustomSearchResponse response)
        {
            var articles = new List<CustomSearchModel>();

            foreach (var webPage in response.webPages.value)
            {
                articles.Add(ToCustomSearchResult(webPage));
            }

            return articles;
        }
    }
}
