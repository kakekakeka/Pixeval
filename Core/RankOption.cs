// Pixeval - A Strong, Fast and Flexible Pixiv Client
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
using Pixeval.Objects;

namespace Pixeval.Core
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ForR18Only : Attribute { }

    public enum RankOption
    {
        /// <summary>
        ///     日榜
        /// </summary>
        [EnumAlias("day")]
        [EnumName(StringResources.RankOptionDay)]
        Day,

        /// <summary>
        ///     周榜
        /// </summary>
        [EnumAlias("week")]
        [EnumName(StringResources.RankOptionWeek)]
        Week,

        /// <summary>
        ///     月榜
        /// </summary>
        [EnumAlias("month")]
        [EnumName(StringResources.RankOptionMonth)]
        Month,

        /// <summary>
        ///     男性向日榜
        /// </summary>
        [EnumAlias("day_male")]
        [EnumName(StringResources.RankOptionDayMale)]
        DayMale,

        /// <summary>
        ///     女性向日榜
        /// </summary>
        [EnumAlias("day_female")]
        [EnumName(StringResources.RankOptionDayFemale)]
        DayFemale,

        /// <summary>
        ///     多图日榜
        /// </summary>
        [EnumAlias("day_manga")]
        [EnumName(StringResources.RankOptionDayManga)]
        DayManga,

        /// <summary>
        ///     多图周榜
        /// </summary>
        [EnumAlias("week_manga")]
        [EnumName(StringResources.RankOptionWeekManga)]
        WeekManga,

        /// <summary>
        ///     原创
        /// </summary>
        [EnumAlias("week_original")]
        [EnumName(StringResources.RankOptionWeekOriginal)]
        WeekOriginal,

        /// <summary>
        ///     新人
        /// </summary>
        [EnumAlias("week_rookie")]
        [EnumName(StringResources.RankOptionWeekRookie)]
        WeekRookie,

        /// <summary>
        ///     R18日榜
        /// </summary>
        [ForR18Only]
        [EnumAlias("day_r18")]
        [EnumName(StringResources.RankOptionDayR18)]
        DayR18,

        /// <summary>
        ///     男性向R18日榜
        /// </summary>
        [ForR18Only]
        [EnumAlias("day_male_r18")]
        [EnumName(StringResources.RankOptionDayMaleR18)]
        DayMaleR18,

        /// <summary>
        ///     女性向R18日榜
        /// </summary>
        [ForR18Only]
        [EnumAlias("day_female_r18")]
        [EnumName(StringResources.RankOptionDayFemaleR18)]
        DayFemaleR18,

        /// <summary>
        ///     R18周榜
        /// </summary>
        [ForR18Only]
        [EnumAlias("week_r18")]
        [EnumName(StringResources.RankOptionWeekR18)]
        WeekR18,

        /// <summary>
        ///     R18G周榜
        /// </summary>
        [ForR18Only]
        [EnumAlias("week_r18g")]
        [EnumName(StringResources.RankOptionWeekR18G)]
        WeekR18G
    }
}