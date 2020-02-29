﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Acr.UserDialogs;
using FreshMvvm;
using AsyncAwaitBestPractices;
using AsyncAwaitBestPractices.MVVM;

using ComicWrap.Systems;

namespace ComicWrap.Pages
{
    public class ComicDetailPageModel : FreshBasePageModel
    {
        public ComicDetailPageModel()
        {
            OpenOptionsCommand = new AsyncCommand(OpenOptions);

            RefreshCommand = new AsyncCommand(async () =>
            {
                try
                {
                    IsRefreshing = true;

                    var cancelToken = pageCancelTokenSource.Token;

                    cancelToken.ThrowIfCancellationRequested();
                    await Refresh(cancelToken);

                    IsRefreshing = false;
                }
                catch (OperationCanceledException)
                {
                    // Should cancel silently
                }
            });

            OpenPageCommand = new AsyncCommand<ComicPageData>(OpenPage);
        }

        private CancellationTokenSource pageCancelTokenSource;
        private bool _isRefreshing;
        private ComicData _comic;

        public IAsyncCommand OpenOptionsCommand { get; }
        public IAsyncCommand RefreshCommand { get; }
        public IAsyncCommand<ComicPageData> OpenPageCommand { get; }

        public bool IsRefreshing
        {
            get { return _isRefreshing; }
            set
            {
                _isRefreshing = value;
                RaisePropertyChanged();
            }
        }

        public ComicData Comic
        {
            get { return _comic; }
            private set
            {
                _comic = value;

                RaisePropertyChanged();
            }
        }

        public ObservableCollection<ComicPageData> Pages { get; set; }

        public override void Init(object initData)
        {
            Comic = initData as ComicData;

            Pages = new ObservableCollection<ComicPageData>();
        }

        protected override void ViewIsAppearing(object sender, EventArgs e)
        {
            base.ViewIsAppearing(sender, e);

            pageCancelTokenSource = new CancellationTokenSource();
            DisplayPages(Comic.Pages);
        }

        protected override void ViewIsDisappearing(object sender, EventArgs e)
        {
            base.ViewIsDisappearing(sender, e);

            pageCancelTokenSource.Cancel();
            IsRefreshing = false;
        }

        private async Task OpenOptions()
        {
            string buttonPressed = await UserDialogs.Instance.ActionSheetAsync(
                title: "Comic Options",
                cancel: "Cancel",
                destructive: "Delete"
                );

            switch (buttonPressed)
            {
                case "Cancel":
                    return;

                case "Delete":
                    {
                        var comicName = Comic.Name;
                        ComicDatabase.Instance.DeleteComic(Comic);
                        UserDialogs.Instance.Toast($"Deleted Comic: {comicName}");
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            await CoreMethods.PopPageModel();
        }

        private async Task Refresh(CancellationToken cancelToken)
        {
            var newPages = await ComicUpdater.UpdateComic(Comic, cancelToken: cancelToken);
            DisplayPages(newPages);
        }

        private void DisplayPages(IEnumerable<ComicPageData> newPages)
        {
            // Display new page list
            var reordered = newPages.Reverse();

            // Need to use ObservableCollection methods so UI is updated
            Pages.Clear();
            foreach (var page in reordered)
                Pages.Add(page);
        }

        private async Task OpenPage(ComicPageData pageData)
        {
            await CoreMethods.PushPageModel<ComicReaderPageModel>(pageData);
        }
    }
}
