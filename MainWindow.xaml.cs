using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace myTunes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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

            PopulateListBox();          
        }

        private void PopulateListBox()
        {
            //Binding List of elements in Playlist[] to playlistListbox
            List<string> playlistNames = new List<string>();
            foreach (var name in musicRepo.Playlists)
            {
                playlistNames.Add(name);
            }
            playlistListBox.ItemsSource = playlistNames;
        }
    }
}
