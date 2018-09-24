using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace SoundMatchGame
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private ObservableCollection<Song> Songs;
        private StorageFolder folder = KnownFolders.MusicLibrary;
        private ObservableCollection<StorageFile> allsongs;
        private List<StorageFile> random_songs;
        private bool IsAddSong;
        private int num = 1;
        private int score;
        private bool IsGameStart = false;
        private int total_score = 0;
        private bool IsClick = false;
        private Uri uri;
        public MainPage()
        {
            this.InitializeComponent();
            Songs = new ObservableCollection<Song>();
        }

        private void main_storyboard_Completed(object sender, object e)
        {
            if (IsGameStart)
            {
                num++;
                main_mediaElement.Stop();
                if (IsClick == false)
                {
                    score = -100;
                    SampleResultMethod();
                }
            }
            else
            {
                StartGame();
                GetPlaySong();
                IsClick = false;
            }
        }
        private async void SampleResultMethod()
        {
            total_score += score;
            try
            {
                score_textblock.Text = $"本回合 {score} 分，已开始{ num - 1} 回合，总分 {total_score}";
                var correct_song = Songs.FirstOrDefault(p => p.Selected == true);
                title_textblok.Text = $"正确歌曲歌名: {correct_song.Title}";
                artist_textblock.Text = $"正确歌曲歌手: {correct_song.Artist}";
                album_textblok.Text = $"正确歌曲专辑名: {correct_song.Album}";
                correct_song.Used = true;
                correct_song.Selected = false;
                if (num < 6)
                {
                    WaitingStart();
                }
                else
                {
                    help_textblock.Text = $"游戏结束，总分为:{total_score}";
                    main_storyboard.Stop();
                    refresh_button.Visibility = Visibility.Visible;
                }
            }
            catch
            {

                ContentDialog content_dialog = new ContentDialog
                {
                    Title = "错误提示",
                    Content = "该局游戏已结束，点击重新开始按钮可以重新开始游戏",
                    IsPrimaryButtonEnabled = true,
                    PrimaryButtonText = "OK",


                };
                ContentDialogResult result = await content_dialog.ShowAsync();
            }

        }
        private async Task GetAllSong(ObservableCollection<StorageFile> allsongs, StorageFolder folder)
        {
            foreach (var song in await folder.GetFilesAsync())
            {
                if (song.FileType == ".mp3")
                {
                    allsongs.Add(song);
                }
            }
            foreach (var item in await folder.GetFoldersAsync())
            {
                await GetAllSong(allsongs, item);
            }
        }
        private async Task<List<StorageFile>> GetRandomSong(ObservableCollection<StorageFile> allsongs)
        {
            Random rd = new Random();
            var allsongs_count = allsongs.Count();
            random_songs = new List<StorageFile>();
            while (random_songs.Count() < 10)
            {
                var index = rd.Next(allsongs_count);
                var random_song = allsongs[index];
                IsAddSong = false;
                MusicProperties set_randomsong = await random_song.Properties.GetMusicPropertiesAsync();
                foreach (var song in random_songs)
                {
                    MusicProperties get_randomsong = await song.Properties.GetMusicPropertiesAsync();
                    if (String.IsNullOrEmpty(set_randomsong.Album) || set_randomsong.Album == get_randomsong.Album)
                    {
                        IsAddSong = true;
                    }
                }
                if (IsAddSong == false)
                {
                    random_songs.Add(random_song);
                }
            }
            return random_songs;
        }
        private async Task GetRancomSongFile(List<StorageFile> random_songs)
        {
            foreach (var song in random_songs)
            {
                StorageItemThumbnail thumb = await song.GetThumbnailAsync(
                    ThumbnailMode.MusicView,
                    200,
                    ThumbnailOptions.UseCurrentScale);
                BitmapImage bitmap = new BitmapImage();
                bitmap.SetSource(thumb);
                Song songs = new Song();
                MusicProperties properties = await song.Properties.GetMusicPropertiesAsync();
                songs.Title = properties.Title;
                songs.Artist = properties.Artist;
                songs.Album = properties.Album;
                songs.songFile = song;
                songs.AlbumCover = bitmap;
                songs.Used = false;
                Songs.Add(songs);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //refresh_button.Visibility = Visibility.Collapsed;
            //help_textblock.Text = "获取音乐文件中...";
            main_progressbar.Visibility = Visibility.Collapsed;
            refresh_button.Content = "开始游戏";
            help_textblock.Text = "歌曲搭配游戏";
            //GetSongs();

        }
        private async void GetSongs()
        {
            Songs.Clear();
            main_progressing.IsActive = true;
            allsongs = new ObservableCollection<StorageFile>();
            await GetAllSong(allsongs, folder);
            var random_songs = await GetRandomSong(allsongs);
            await GetRancomSongFile(random_songs);
            main_progressing.IsActive = false;
            WaitingStart();
            //refresh_button.Visibility = Visibility.Visible;
        }
        private async void main_gridview_ItemClick(object sender, ItemClickEventArgs e)
        {
            IsClick = true;

            if (IsGameStart == false) return;
            main_storyboard.Pause();
            main_mediaElement.Stop();
            var click_song = (Song)e.ClickedItem;
            var correct_song = Songs.FirstOrDefault(p => p.Selected == true);

            if (click_song.Selected)
            {
                uri = new Uri("ms-appx:///Assets/correct.png");
                score = (int)main_progressbar.Value;
            }
            else
            {
                uri = new Uri("ms-appx:///Assets/incorrect.png");
                score = (int)main_progressbar.Value * -1;
            }
            if (num < 6)
            {
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(uri);
                var png_file = await file.OpenAsync(FileAccessMode.Read);
                await click_song.AlbumCover.SetSourceAsync(png_file);
            }
            num++;
            click_song.Used = true;
            SampleResultMethod();
        }

        private void refresh_button_Click(object sender, RoutedEventArgs e)
        {
            refresh_button.Visibility = Visibility.Collapsed;
            refresh_button.Content = "重新开始";
            num = 1;
            main_progressbar.Visibility = Visibility.Collapsed;
            help_textblock.Text = "获取音乐文件中...";
            help_textblock.Foreground = new SolidColorBrush(Colors.Black);
            total_score = 0;
            score_textblock.Text = "";
            title_textblok.Text = "";
            artist_textblock.Text = "";
            album_textblok.Text = "";
            GetSongs();
        }
        private void WaitingStart()
        {
            main_progressbar.Visibility = Visibility.Visible;
            SolidColorBrush skyBlue = new SolidColorBrush(Colors.SkyBlue);
            main_progressbar.Foreground = skyBlue;
            help_textblock.Foreground = skyBlue;
            help_textblock.Text = $"准备开始游戏，第{num}回合";
            IsGameStart = false;
            main_storyboard.Begin();
        }
        private void StartGame()
        {
            main_storyboard.Begin();
            SolidColorBrush red = new SolidColorBrush(Colors.DarkRed);
            main_progressbar.Foreground = red;
            help_textblock.Foreground = red;
            help_textblock.Text = "游戏中...";
            IsGameStart = true;
        }
        private async void GetPlaySong()
        {
            Random newrd = new Random();
            var unUsedSongs = Songs.Where(p => p.Used == false);
            var playsong_index = newrd.Next(unUsedSongs.Count());
            var playsong = unUsedSongs.ElementAt(playsong_index);
            main_mediaElement.SetSource(await playsong.songFile.OpenAsync(
                FileAccessMode.Read), playsong.songFile.ContentType);
            main_mediaElement.Play();
            playsong.Used = true;
            playsong.Selected = true;
        }
    }
}
