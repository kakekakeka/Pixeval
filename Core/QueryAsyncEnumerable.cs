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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Pixeval.Data.ViewModel;
using Pixeval.Data.Web;
using Pixeval.Data.Web.Delegation;
using Pixeval.Data.Web.Response;
using Pixeval.Objects;
using Pixeval.Objects.Exceptions;
using Pixeval.Objects.Primitive;
using Pixeval.Persisting;

namespace Pixeval.Core
{
    public abstract class AbstractQueryAsyncEnumerable : AbstractPixivAsyncEnumerable<Illustration>
    {
        private readonly SearchTagMatchOption matchOption;
        private readonly int start;
        private readonly string tag;

        protected AbstractQueryAsyncEnumerable(string tag, SearchTagMatchOption matchOption, int start = 1)
        {
            this.start = start < 1 ? 1 : start;
            this.tag = tag;
            this.matchOption = matchOption;
        }

        public override int RequestedPages { get; protected set; }

        public abstract override void InsertionPolicy(Illustration item, IList<Illustration> collection);

        public override bool VerifyRational(Illustration item, IList<Illustration> collection)
        {
            return item != null && collection.All(t => t.Id != item.Id) && PixivHelper.VerifyIllustRational(Settings.Global.ExcludeTag, Settings.Global.IncludeTag, Settings.Global.MinBookmark, item);
        }

        public override IAsyncEnumerator<Illustration> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new QueryAsyncEnumerator(this, tag, matchOption, start);
        }

        private class QueryAsyncEnumerator : AbstractPixivAsyncEnumerator<Illustration>
        {
            private readonly int current;
            private readonly string keyword;
            private readonly SearchTagMatchOption matchOption;

            private QueryWorksResponse entity;

            private IEnumerator<Illustration> illustrationsEnumerator;

            public QueryAsyncEnumerator(IPixivAsyncEnumerable<Illustration> enumerable, string keyword, SearchTagMatchOption matchOption, int current) : base(enumerable)
            {
                this.keyword = keyword;
                this.matchOption = matchOption;
                this.current = current;
            }

            public override Illustration Current => illustrationsEnumerator.Current;

            protected override void UpdateEnumerator()
            {
                illustrationsEnumerator = entity.Illusts.NonNull().Select(_ => _.Parse()).GetEnumerator();
            }

            public override async ValueTask<bool> MoveNextAsync()
            {
                if (entity == null)
                {
                    if (await TryGetResponse($"/v1/search/illust?search_target={matchOption.GetEnumAttribute<EnumAlias>().AliasAs}&sort=date_desc&word={keyword}&filter=for_android&offset={(current - 1) * 30}") is (true, var model))
                    {
                        entity = model;
                        UpdateEnumerator();
                    }
                    else
                    {
                        throw new QueryNotRespondingException();
                    }

                    Enumerable.ReportRequestedPages();
                }

                if (illustrationsEnumerator.MoveNext()) return true;

                if (int.Parse(entity.NextUrl[(entity.NextUrl.LastIndexOf('=') + 1)..]) >= 5000) return false;

                if (await TryGetResponse(entity.NextUrl) is (true, var res))
                {
                    entity = res;
                    UpdateEnumerator();
                    Enumerable.ReportRequestedPages();
                    return true;
                }

                return false;
            }

            private static async Task<HttpResponse<QueryWorksResponse>> TryGetResponse(string url)
            {
                var res = (await HttpClientFactory.AppApiHttpClient().Apply(h => h.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "zh-cn")).GetStringAsync(url)).FromJson<QueryWorksResponse>();
                if (res is { } response && !response.Illusts.IsNullOrEmpty()) return HttpResponse<QueryWorksResponse>.Wrap(true, response);

                return HttpResponse<QueryWorksResponse>.Wrap(false);
            }
        }
    }

    public class PopularityQueryAsyncEnumerable : AbstractQueryAsyncEnumerable
    {
        public PopularityQueryAsyncEnumerable(string tag, SearchTagMatchOption matchOption, int start = 1) : base(tag, matchOption, start) { }

        public override void InsertionPolicy(Illustration item, IList<Illustration> collection)
        {
            if (item != null) collection.AddSorted(item, IllustrationPopularityComparator.Instance);
        }
    }

    public class PublishDateQueryAsyncEnumerable : AbstractQueryAsyncEnumerable
    {
        public PublishDateQueryAsyncEnumerable(string tag, SearchTagMatchOption matchOption, int start = 1) : base(tag, matchOption, start) { }

        public override void InsertionPolicy(Illustration item, IList<Illustration> collection)
        {
            if (item != null) collection.AddSorted(item, IllustrationPublishDateComparator.Instance);
        }
    }
}