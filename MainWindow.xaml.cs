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
            PopulateSongs_Datagrid();        
        }

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
                MenuItem_RemovePlaylist.IsEnabled = false;
                MenuItem_RenamePlaylist.IsEnabled = false;
            }
            else
            {
                RemoveMenuItem.Header = "Remove from Playlist";
                isPlaylistSelected = true;
                MenuItem_RemovePlaylist.IsEnabled = true;
                MenuItem_RenamePlaylist.IsEnabled = true;
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
                        playlistListBox.Items.Refresh();    //https://learn.microsoft.com/en-us/answers/questions/343765/how-to-refresh-a-listbox.html
                        musicRepo.Save();
                    }
                    else
                    {
                        MessageBox.Show("There is already a playlist with that name. Please enter a new playlist name.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            DataRowView? rowView = musicDataGrid.SelectedItem as DataRowView;

            if (rowView != null)
            {
                // Extract the song ID from the selected song
                int songId = Convert.ToInt32(rowView.Row.ItemArray[0]);
                Song s = musicRepo.GetSong(songId);
                mediaPlayer.Open(new Uri(s.Filename));
                mediaPlayer.Play();
            }
        }

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
                    if (MessageBox.Show($"Are you sure you want to remove song: \"{song.Title}\" from Library?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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

        private void PlayMenuItem_Click(object sender, RoutedEventArgs e)
        {
            playButton_Click(sender, e);
        }

        private void MenuItem_DeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            string? selectedPlaylist = playlistListBox.SelectedItems[0] as string;

            if (selectedPlaylist != null)
            {
                //Display Confirmation Message Box when trying to remove a song from 'All Music'
                //String.Format source: https://stackoverflow.com/questions/13033581/insert-variable-values-in-the-middle-of-a-string
                if (MessageBox.Show($"Are you sure you want to delete \"{selectedPlaylist}\" playlist?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (musicRepo.DeletePlaylist(selectedPlaylist))
                    {
                        playlistNames.Remove(selectedPlaylist);
                        playlistListBox.SelectedIndex = 0;
                        playlistListBox.Items.Refresh();
                        musicRepo.Save();
                    }
                }
            }
        }

        private void MenuItem_RenamePlaylist_Click(object sender, RoutedEventArgs e)
        {
            string? selectedPlaylist = playlistListBox.SelectedItems[0] as string;
            RenamePlaylist renamePlaylist = new RenamePlaylist();
            bool? result = renamePlaylist.ShowDialog();

            if (result == true)
            {
                //Checks that input entered onto textbox is not empty/null.
                if (!String.IsNullOrEmpty(renamePlaylist.playlistNewName_Textbox.Text) && selectedPlaylist!= null)
                {
                    //Tries adding the playlist name; returns true if successful, false if not added (playllist already existed)
                    if (musicRepo.RenamePlaylist(selectedPlaylist, renamePlaylist.playlistNewName_Textbox.Text))
                    {
                        //replace list item; source: https://stackoverflow.com/questions/17188966/how-to-replace-list-item-in-best-way
                        int index = playlistNames.FindIndex(s => s == selectedPlaylist);
                        if (index != -1) playlistNames[index] = renamePlaylist.playlistNewName_Textbox.Text;
                        playlistListBox.SelectedIndex = index;
                        playlistListBox.Items.Refresh();
                        musicRepo.Save();
                    }
                    else
                    {
                        MessageBox.Show("There is already a playlist with that name. Please enter a new name.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Please enter a new playlist name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
