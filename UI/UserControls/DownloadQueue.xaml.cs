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

using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Pixeval.Core;
using Pixeval.Data.ViewModel;
using Pixeval.Objects;
using Pixeval.Objects.Primitive;

namespace Pixeval.UI.UserControls
{
    /// <summary>
    ///     Interaction logic for DownloadQueue.xaml
    /// </summary>
    public partial class DownloadQueue
    {
        public DownloadQueue()
        {
            InitializeComponent();
            ((INotifyCollectionChanged) DownloadItemsQueue.Items).CollectionChanged += (sender, args) =>
                EmptyNotifier1.Visibility = DownloadItemsQueue.Items.Count == 0 ? Visibility.Visible : Visibility.Hidden;
            ((INotifyCollectionChanged) DownloadedItemsQueue.Items).CollectionChanged += (sender, args) =>
                EmptyNotifier2.Visibility = DownloadedItemsQueue.Items.Count == 0 ? Visibility.Visible : Visibility.Hidden;
            UiHelper.SetItemsSource(DownloadItemsQueue, DownloadManager.Downloading);
            UiHelper.SetItemsSource(DownloadedItemsQueue, DownloadManager.Downloaded);
        }

        private async void DownloadItemThumbnail_OnLoaded(object sender, RoutedEventArgs e)
        {
            var url = sender.GetDataContext<DownloadableIllustration>().DownloadContent.Thumbnail;
            UiHelper.SetImageSource(sender, await PixivIO.FromUrl(url));
        }

        private void RetryButton_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var model = sender.GetDataContext<DownloadableIllustration>();
            model.Restart();
        }

        private void CancelButton_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var model = sender.GetDataContext<DownloadableIllustration>();
            model.Cancel();
        }

        private void ViewDownloadLocationButton_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var model = sender.GetDataContext<DownloadableIllustration>();
            if (!model.DownloadPath.IsNullOrEmpty() && Path.GetDirectoryName(model.DownloadPath) is var p)
                Process.Start("explorer.exe", p);
            else MainWindow.MessageQueue.Enqueue(StringResources.PathNotExist);
        }

        private void ShowDownloadIllustration(object sender, RoutedEventArgs e)
        {
            MainWindow.Instance.DownloadQueueDialogHost.CloseControl();
            var model = sender.GetDataContext<DownloadableIllustration>();
            MainWindow.Instance.OpenIllustBrowser(model.IsFromManga ? model.DownloadContent.MangaMetadata[0] : model.DownloadContent);
        }

        private void RemoveFromDownloaded(object sender, RoutedEventArgs e)
        {
            DownloadManager.Downloaded.Remove(sender.GetDataContext<DownloadableIllustration>());
        }

        private void RemoveFromDownloading(object sender, RoutedEventArgs e)
        {
            DownloadManager.Downloading.Remove(sender.GetDataContext<DownloadableIllustration>());
        }
    }
}