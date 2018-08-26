﻿using Digimezzo.Utilities.Log;
using Digimezzo.Utilities.Utils;
using Dopamine.Core.Base;
using Dopamine.Data;
using Dopamine.Services.Dialog;
using Dopamine.Services.Entities;
using Prism.Commands;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Dopamine.ViewModels.Common.Base
{
    public abstract class PlaylistsViewModelBase : TracksViewModelBaseWithTrackArt
    {
        private ObservableCollection<PlaylistViewModel> playlists;
        private PlaylistViewModel selectedPlaylist;
        private IDialogService dialogService;

        public DelegateCommand NewPlaylistCommand { get; set; }

        public DelegateCommand ImportPlaylistsCommand { get; set; }

        public DelegateCommand DeleteSelectedPlaylistCommand { get; set; }

        public DelegateCommand<PlaylistViewModel> DeletePlaylistCommand { get; set; }

        public PlaylistsViewModelBase(IContainerProvider container, IDialogService dialogService) : base(container)
        {
            this.dialogService = dialogService;

            // Commands
            this.DeletePlaylistCommand = new DelegateCommand<PlaylistViewModel>(async (playlist) => await this.ConfirmDeletePlaylistAsync(playlist));

            this.DeleteSelectedPlaylistCommand = new DelegateCommand(async () =>
            {
                if (this.IsPlaylistSelected)
                {
                    await this.ConfirmDeletePlaylistAsync(this.SelectedPlaylist);
                }
            });
        }

        public string PlaylistsTarget => "ListBoxPlaylists";

        public string TracksTarget => "ListBoxTracks";

        public bool IsPlaylistSelected => this.selectedPlaylist != null;

        public long PlaylistsCount => this.playlists == null ? 0 : this.playlists.Count;

        public ObservableCollection<PlaylistViewModel> Playlists
        {
            get { return this.playlists; }
            set { SetProperty<ObservableCollection<PlaylistViewModel>>(ref this.playlists, value); }
        }

        public PlaylistViewModel SelectedPlaylist
        {
            get { return this.selectedPlaylist; }
            set
            {
                SetProperty<PlaylistViewModel>(ref this.selectedPlaylist, value);

                if (value != null)
                {
                    this.GetTracksAsync();
                }
                else
                {
                    this.ClearTracks();
                }
            }
        }

        public string SelectedPlaylistName
        {
            get
            {
                if (this.SelectedPlaylist != null && !string.IsNullOrEmpty(this.SelectedPlaylist.Name))
                {
                    return this.SelectedPlaylist.Name;
                }

                return null;
            }
        }

        protected abstract Task GetTracksAsync();

        protected abstract Task GetPlaylistsAsync();

        private async Task ClearTracks()
        {
            await this.GetTracksCommonAsync(new List<TrackViewModel>(), TrackOrder.None);
        }

        protected void TrySelectFirstPlaylist()
        {
            try
            {
                if (this.Playlists.Count > 0)
                {
                    this.SelectedPlaylist = this.Playlists[0];
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("An error occurred while selecting the playlist. Exception: {0}", ex.Message);
            }
        }

        protected override async Task FillListsAsync()
        {
            await this.GetPlaylistsAsync();
            await this.GetTracksAsync();
        }

        protected override async Task LoadedCommandAsync()
        {
            if (!this.IsFirstLoad())
            {
                return;
            }

            await Task.Delay(Constants.CommonListLoadDelay); // Wait for the UI to slide in
            await this.FillListsAsync(); // Fill all the lists
        }

        protected void PlaylistAddedHandler(PlaylistViewModel addedPlaylist)
        {
            this.Playlists.Add(addedPlaylist);

            // If there is only 1 playlist, automatically select it.
            if (this.Playlists != null && this.Playlists.Count == 1)
            {
                this.TrySelectFirstPlaylist();
            }

            // Notify that the count has changed
            this.RaisePropertyChanged(nameof(this.PlaylistsCount));
        }

        protected void PlaylistDeletedHandler(PlaylistViewModel deletedPlaylist)
        {
            this.Playlists.Remove(deletedPlaylist);

            // If the selected playlist was deleted, select the first playlist.
            if (this.SelectedPlaylist == null)
            {
                this.TrySelectFirstPlaylist();
            }

            // Notify that the count has changed
            this.RaisePropertyChanged(nameof(this.PlaylistsCount));
        }

        protected abstract Task DeletePlaylistAsync(PlaylistViewModel playlist);

        private async Task ConfirmDeletePlaylistAsync(PlaylistViewModel playlist)
        {
            if (this.dialogService.ShowConfirmation(
                0xe11b,
                16,
                ResourceUtils.GetString("Language_Delete"),
                ResourceUtils.GetString("Language_Are_You_Sure_To_Delete_Playlist").Replace("{playlistname}", playlist.Name),
                ResourceUtils.GetString("Language_Yes"),
                ResourceUtils.GetString("Language_No")))
            {
                await this.DeletePlaylistAsync(playlist);
            }
        }
    }
}