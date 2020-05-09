﻿// Pixeval - A Strong, Fast and Flexible Pixiv Client
// Copyright (C) 2019-2020 Dylech30th
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Pixeval.Core;
using Pixeval.Objects.Primitive;
using PropertyChanged;

namespace Pixeval.Data.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class SpotlightArticle
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("pure_title")]
        public string PureTitle { get; set; }

        [JsonProperty("thumbnail")]
        public string Thumbnail { get; set; }

        [JsonProperty("article_url")]
        public string ArticleUrl { get; set; }

        [JsonProperty("publish_date")]
        public DateTimeOffset PublishDate { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("subcategory_label")]
        public string SubcategoryLabel { get; set; }

        public async void Download()
        {
            var result = await Tasks<string, Illustration>.Of(await PixivClient.Instance.GetArticleWorks(Id.ToString()))
                .Mapping(async i =>
                {
                    var res = await PixivHelper.IllustrationInfo(i);
                    res.SpotlightTitle = Title;
                    res.FromSpotlight = true;
                    return res;
                })
                .Construct()
                .WhenAll();

            foreach (var illustration in result) DownloadManager.EnqueueDownloadItem(illustration);
        }

        public string GetCover()
        {
            var match = Regex.Match(Thumbnail, "/(?<illust_id>\\d+)_p\\d+_master1200\\.jpg|png");
            if (match.Success)
            {
                var url = Regex.Replace(Thumbnail, "/c/\\d+x\\d+_\\d+/img-master/", "/img-original/").Replace("_master1200", string.Empty);
                return url;
            }

            return Thumbnail;
        }
    }
}