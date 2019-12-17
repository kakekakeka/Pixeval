﻿// Pixeval
// Copyright (C) 2019 Dylech30th <decem0730@gmail.com>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Net;

namespace Pixeval.Data.Web.Delegation
{
    public class PixivApiDnsResolver : DnsResolver
    {
        public static DnsResolver Instance = new PixivApiDnsResolver();

        protected override void UseDefaultDns()
        {
            IpList.Add(IPAddress.Parse("210.140.131.219"));
            IpList.Add(IPAddress.Parse("210.140.131.223"));
            IpList.Add(IPAddress.Parse("210.140.131.226"));
        }
    }
}