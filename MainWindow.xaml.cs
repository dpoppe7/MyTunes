using MyTunes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Data;

namespace myTunes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> playlistNames = new();
        private List<Song> songList = new();

        private readonly MusicRepo musicRepo;
        public MainWindow()
        {
            InitializeComponent();

            try
            {
                musicRepo = new MusicRepo();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error loading file: " + e.Message, "MyTunes", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            PopulatePlaylists_Listbox();
            PopulateSongs_Datagrid();        }

        private void PopulateSongs_Datagrid()
        {
            if (musicRepo != null)
            {
                DataView dataView = musicRepo.Songs.DefaultView;
                musicDataGrid.ItemsSource = dataView;

                foreach (DataRowView dataRow in dataView)
                {
                    int id = (int)dataRow.Row["id"];

                    Song songInfo = musicRepo.GetSong(id);
                    songList.Add(songInfo); 
                }
            }
        }

        private void PopulatePlaylists_Listbox()
        {
            //Binding List of elements in Playlist[] to playlistListboxplaylistNames = new List<string>();
            playlistNames.Add("All Music");
            foreach (var name in musicRepo.Playlists)
            {
                playlistNames.Add(name);
            }
            playlistListBox.ItemsSource = playlistNames;

            //Default selects the list item at 0 ("All Music")
            playlistListBox.SelectedIndex = 0;
        }

        //source (Display context menu on right click): https://stackoverflow.com/questions/43547647/how-to-make-right-click-button-context-menu-in-wpf
        private void playlistListBox_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ContextMenu = (ContextMenu)Resources["listboxItemContextMenu"];
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.ShowDialog();
        }

        private void NewPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            NewPlaylist newPlaylist = new NewPlaylist();
            bool? result = newPlaylist.ShowDialog();

            if (result == true)
            {
                //Checks that input entered onto newPlaylist textbox is not empty/null.
                if (!String.IsNullOrEmpty(newPlaylist.newPlaylistName_Textbox.Text))
                {
                    //Tries adding the playlist name; returns true if successful, false if not added (playllist already existed)
                    if (musicRepo.AddPlaylist(newPlaylist.newPlaylistName_Textbox.Text))
                    {
                        playlistNames.Add(newPlaylist.newPlaylistName_Textbox.Text);

                        playlistListBox.Items.Refresh();
                    }
                    else
                    {
                        MessageBox.Show("There is already a playlist with that name.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Please enter a playlist name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
