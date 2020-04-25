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
using Res = ComicWrap.Resources.AppResources;

namespace ComicWrap.Pages
{
    public class ComicDetailPageModel : FreshBasePageModel
    {
        public ComicDetailPageModel(ComicPageTargetType scrollTo)
        {
            scrollToPageTarget = scrollTo;

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
                catch (OperationCanceledException) { }
            });

            OpenPageCommand = new AsyncCommand<ComicPageData>(OpenPage);
        }

        public event Action PagesUpdated;

        private CancellationTokenSource pageCancelTokenSource;
        private bool _isRefreshing;
        private ComicData _comic;
        private ComicPageTargetType scrollToPageTarget;

        public IAsyncCommand OpenOptionsCommand { get; }
        public IAsyncCommand RefreshCommand { get; }
        public IAsyncCommand<ComicPageData> OpenPageCommand { get; }

        public ObservableCollection<ComicPageData> Pages { get; set; }

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

        public ComicPageData ScrollToPage
        {
            get
            {
                switch (scrollToPageTarget)
                {
                    case ComicPageTargetType.None:
                        return null;

                    case ComicPageTargetType.LastRead:
                        return Comic.LastReadPage;

                    case ComicPageTargetType.FirstNew:
                        return Comic.LatestNewPage;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public override void Init(object initData)
        {
            Comic = initData as ComicData;

            Pages = new ObservableCollection<ComicPageData>();
        }

        protected override void ViewIsAppearing(object sender, EventArgs e)
        {
            base.ViewIsAppearing(sender, e);

            pageCancelTokenSource = new CancellationTokenSource();
            UpdatePages(Comic.Pages);
        }

        protected override void ViewIsDisappearing(object sender, EventArgs e)
        {
            base.ViewIsDisappearing(sender, e);

            pageCancelTokenSource.CancelAndDispose();
            IsRefreshing = false;
        }

        private async Task OpenOptions()
        {
            string buttonPressed = await UserDialogs.Instance.ActionSheetAsync(
                title: Res.ComicDetail_Options_Title,
                cancel: Res.ComicDetail_Options_Cancel,
                destructive: Res.ComicDetail_Options_Delete
                );

            if (buttonPressed == Res.ComicDetail_Options_Cancel)
            {
                return;
            }
            else if(buttonPressed == Res.ComicDetail_Options_Delete)
            {
                var comicName = Comic.Name;
                ComicDatabase.Instance.DeleteComic(Comic);
                UserDialogs.Instance.Toast(string.Format(Res.ComicDetail_DeletedComic, comicName));
            }
            else
            {
                throw new NotImplementedException();
            }

            await CoreMethods.PopPageModel();
        }

        private async Task Refresh(CancellationToken cancelToken)
        {
            var newPages = await ComicUpdater.Instance.UpdateComic(Comic, cancelToken: cancelToken);
            UpdatePages(newPages);
        }

        private void UpdatePages(IEnumerable<ComicPageData> newPages)
        {
            // Display new page list
            var reordered = newPages.Reverse();

            // Need to use ObservableCollection methods so UI is updated
            Pages.Clear();
            foreach (var page in reordered)
                Pages.Add(page);

            PagesUpdated?.Invoke();

            // Clear ScrollTo target since page shuld have been scrolled during PagesUpdated event (if desired)
            scrollToPageTarget = ComicPageTargetType.None;
        }

        private async Task OpenPage(ComicPageData pageData)
        {
            await CoreMethods.PushPageModel<ComicReaderPageModel>(pageData);
        }
    }
}
