using MyTunes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Data;
using System.Windows.Input;
using System.Windows.Shapes;

namespace myTunes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> playlistNames = new();
        private List<Song> songList = new();
        private readonly MediaPlayer mediaPlayer;
        private Point startPoint;
        private bool isPlaylistSelected = false;
        private bool isPlayEnabled = false;
        private bool isStopEnabled = false;


        private readonly MusicRepo musicRepo;
        public MainWindow()
        {
            InitializeComponent();

            mediaPlayer = new MediaPlayer();

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
            //Binding List of elements in Playlist[] to playlistListNames
            playlistNames.Add("All Music");
            foreach (var name in musicRepo.Playlists)
            {
                playlistNames.Add(name);
            }
            playlistListBox.ItemsSource = playlistNames;

            //Default selects the list item at 0 ("All Music")
            playlistListBox.SelectedIndex = 0;

            //attaching an Event Handler on SelectionChanged
            playlistListBox.SelectionChanged += playlistSelected_ItemActivate;
        }

        private void playlistSelected_ItemActivate(object sender, EventArgs e)
        {
            string? selectedPlaylist = playlistListBox.SelectedItems[0] as string;

            //When list item is clicked once, display songs inside that playlist
            if (playlistListBox.SelectedItems.Count > 0)
            {
                if (selectedPlaylist != null)
                {
                    if (selectedPlaylist.ToString() != "All Music")
                    {
                        DataView songsTable = musicRepo.SongsForPlaylist(selectedPlaylist.ToString()).DefaultView;
                        displayPlaylistSongs(songsTable, false);
                    }
                    else
                    {
                        DataView songsTable = musicRepo.Songs.DefaultView;
                        displayPlaylistSongs(songsTable, true);
                    }
                }
            }

            //Updates Context menu item 'Remove' for Datagrid
            if (selectedPlaylist != null) UpdateContextMenuItems(selectedPlaylist);
        }

        private void UpdateContextMenuItems(string selectedItem)
        {
            if (selectedItem.ToString() == "All Music")
            {
                RemoveMenuItem.Header = "Remove";
                isPlaylistSelected = false;
            }
            else
            {
                RemoveMenuItem.Header = "Remove from Playlist";
                isPlaylistSelected = true;
            }
        }

        private void displayPlaylistSongs(DataView dataView, bool isDefaultView)
        {
            musicDataGrid.ItemsSource = dataView;

            foreach (DataRowView dataRow in dataView)
            {
                int songId;
                if (isDefaultView)
                {
                    //Casting string to int when dataview = musicRepo.Songs.DefaultView
                    songId = (int)dataRow.Row["id"];
                }
                else {
                    //Casting string to Int32 when dataview = musicRepo.SongsForPlaylist(selectedPlaylist.ToString()).DefaultView
                    songId = Convert.ToInt32((string)dataRow.Row["id"]);
                }

                Song songInfo = musicRepo.GetSong(songId);
                songList.Add(songInfo);
            }
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

        private void addSongButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "",
                DefaultExt = "*.wma;*.wav;*mp3;*.m4a",
                Filter = "Media files|*.mp3;*.m4a;*.wma;*.wav|MP3 (*.mp3)|*.mp3|M4A (*.m4a)|*.m4a|Windows Media Audio (*.wma)|*.wma|Wave files (*.wav)|*.wav|All files|*.*"
            };

            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                // Call the MusicRepo method AddSong() to add the song to the DataSet.
                musicRepo.AddSong(openFileDialog.FileName);

                // Call the MusicRepo method Save() to save the DataSet to the music.xml file.
                musicRepo.Save();

                //TEMPORAL
                mediaPlayer.Open(new Uri(openFileDialog.FileName));
                mediaPlayer.Play();
            }
        }

        private void musicDataGrid_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //stores the mouse position
            startPoint = e.GetPosition(null);
        }

        private void musicDataGrid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Get the current mouse position
            Point mousePos = e.GetPosition(null);
            Vector diff = startPoint - mousePos;

            // Start the drag-drop if mouse has moved far enough
            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                //Determines which row is selected
                DataRowView? rowView = musicDataGrid.SelectedItem as DataRowView;
                if (rowView != null)
                {
                    // Extract the song ID from the selected song
                    int songId = Convert.ToInt32(rowView.Row.ItemArray[0]);

                    // Initiate dragging the text from the textbox
                    DragDrop.DoDragDrop(musicDataGrid, songId, DragDropEffects.Copy);
                }
            }
        }

        private void playlist_Drop(object sender, DragEventArgs e)
        {
            Label listItemText = (Label)sender;
            string? playlist = listItemText.Content.ToString();

            DataRowView? rowView = musicDataGrid.SelectedItem as DataRowView;
            if (rowView != null)
            {
                // Extract the song ID from the selected song
                int songId = Convert.ToInt32(rowView.Row.ItemArray[0]);

                Song song = musicRepo.GetSong(songId);
                if (playlist != null)
                {
                    musicRepo.AddSongToPlaylist(song.Id, playlist);
                    musicRepo.Save();
                }
            }
        }

        private void playlist_DragOver(object sender, DragEventArgs e)
        {
            // By default, don't allow dropping
            e.Effects = DragDropEffects.None;

            if (e.Data != null)
            {
                //If the label content of the listbox item is 'All Music' disable drop
                Label listItemText = (Label)sender;

                if (listItemText.Content.ToString() != "All Music")
                {
                    e.Effects = DragDropEffects.Copy;
                }
            }
        }

        //private void playButton_Click(object sender, RoutedEventArgs e)
        //{
        //    DataRowView? rowView = musicDataGrid.SelectedItem as DataRowView;

        //    if (rowView != null)
        //    {
        //        // Extract the song ID from the selected song
        //        int songId = Convert.ToInt32(rowView.Row.ItemArray[0]);
        //        Song s = musicRepo.GetSong(songId);
        //        mediaPlayer.Open(new Uri(s.Filename));
        //        mediaPlayer.Play();
        //    }
        //}

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
        }

        private void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DataRowView? rowView = musicDataGrid.SelectedItem as DataRowView;
            if (rowView != null)
            {
                int songId = Convert.ToInt32(rowView.Row.ItemArray[0]);
                Song song = musicRepo.GetSong(songId);

                //isPlaylistSelected is false when 'All Music' is selected
                if (!isPlaylistSelected)
                {
                    //Display Confirmation Message Box when trying to remove a song from 'All Music'
                    if (MessageBox.Show("Are you sure you want to remove this song from Library?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        musicRepo.DeleteSong(song.Id);
                    }
                }
                else
                {
                    //No Confirmation Box needed when trying to remove a song from any playlist
                    rowView.Delete();
                }
                musicRepo.Save();
            }
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = isPlayEnabled;
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataRowView? rowView = musicDataGrid.SelectedItem as DataRowView;

            if (rowView != null)
            {
                // Extract the song ID from the selected song
                int songId = Convert.ToInt32(rowView.Row.ItemArray[0]);
                Song s = musicRepo.GetSong(songId);
                mediaPlayer.Open(new Uri(s.Filename));
                mediaPlayer.Play();
                isStopEnabled = true;
            }
        }

        private void CommandBinding_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            mediaPlayer.Stop();
            isStopEnabled = false;
        }

        private void CommandBinding_CanExecute_1(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = isStopEnabled;
        }

        private void musicDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            isPlayEnabled = true;
        }
    }
}
