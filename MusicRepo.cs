// MusicRepo by Frank McCown

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Data;
using System.Net;
using System.IO;
using System.Xml;
using System.Threading.Tasks;

namespace myTunes
{
    public class MusicRepo
    {
        private readonly DataSet musicDataSet;

        public const string XML_MUSICFILE = "music.xml";
        public const string XSD_MUSICFILE = "music.xsd";


        public string[] Playlists
        {
            get
            {
                var items = from row in musicDataSet.Tables["playlist"].AsEnumerable()
                            orderby row["name"]
                            select row["name"].ToString();
                return items.ToArray();
            }
        }

        public DataTable Songs
        {
            get
            {
                return musicDataSet.Tables["song"];
            }
        }

        public MusicRepo()
        {
            musicDataSet = new DataSet();
            musicDataSet.ReadXmlSchema(XSD_MUSICFILE);
            musicDataSet.ReadXml(XML_MUSICFILE);
        }

        /// <summary>
        /// Adds a song to the music library and returns the song's ID. The Song's ID
        /// is also updated to reflect the song's auto-assigned ID.
        /// </summary>
        /// <param name="s">Song to add</param>
        /// <returns>The song's ID</returns>
        public int AddSong(Song s)
        {
            DataTable table = musicDataSet.Tables["song"];
            DataRow row = table.NewRow();

            row["title"] = s.Title;
            row["artist"] = s.Artist;
            row["album"] = s.Album;
            row["filename"] = s.Filename;
            row["length"] = s.Length;
            row["genre"] = s.Genre;
            table.Rows.Add(row);

            // Update this song's ID
            s.Id = Convert.ToInt32(row["id"]);

            return s.Id;
        }

        /// <summary>
        /// Add a song pointed to by the filename.  Returns the newly added song.
        /// </summary>
        /// <param name="filename">MP3 filename</param>
        /// <returns>Song created from the MP3</returns>
        public Song AddSong(string filename)
        {
            // PM> Install-Package taglib
            // http://stackoverflow.com/questions/1750464/how-to-read-and-write-id3-tags-to-an-mp3-in-c
            TagLib.File file = TagLib.File.Create(filename);

            Song s = new Song
            {
                Title = file.Tag.Title,
                Artist = file.Tag.AlbumArtists.Length > 0 ? file.Tag.AlbumArtists[0] : "",
                Album = file.Tag.Album,
                Genre = file.Tag.Genres.Length > 0 ? file.Tag.Genres[0] : "",
                Length = file.Properties.Duration.Minutes + ":" + file.Properties.Duration.Seconds,
                Filename = filename
            };

            AddSong(s);
            return s;
        }

        /// <summary>
        /// Return a Song for the given song ID. Returns null if the song was not found.
        /// </summary>
        /// <param name="songId">ID of song to search for</param>
        /// <returns>The song matching the songId or null if the song wasn't found</returns>
        public Song GetSong(int songId)
        {
            DataTable table = musicDataSet.Tables["song"];

            // Only one row should be selected
            foreach (DataRow row in table.Select("id=" + songId))
            {
                Song song = new Song();
                song.Id = songId;
                song.Title = row["title"].ToString();
                song.Artist = row["artist"].ToString();
                song.Album = row["album"].ToString();
                song.Genre = row["genre"].ToString();
                song.Length = row["length"].ToString();
                song.Filename = row["filename"].ToString();

                return song;
            }

            // Must not have found this song ID
            return null;
        }

        //     
        // 
        /// <summary>
        /// Update the given song with the given song ID. Returns true if the song was 
        /// updated, false if it could not because the song ID was not found.
        /// </summary>
        /// <param name="songId">Song ID to search for</param>
        /// <param name="song">Song data that has possibly changed</param>
        /// <returns>true if the song with the song ID was found, false otherwise</returns>
        public bool UpdateSong(int songId, Song song)
        {
            DataTable table = musicDataSet.Tables["song"];

            // Only one row should be selected
            foreach (DataRow row in table.Select("id=" + songId))
            {
                row["title"] = song.Title;
                row["artist"] = song.Artist;
                row["album"] = song.Album;
                row["genre"] = song.Genre;
                row["length"] = song.Length;
                row["filename"] = song.Filename;

                return true;
            }

            // Must not have found the song ID
            return false;
        }

        /// <summary>
        /// Delete a song given the song's ID. Return true if the song was
        /// successfully deleted, false otherwise.
        /// </summary>
        /// <param name="songId">Song ID to delete</param>
        /// <returns>true if the song ID was found and deleted, false otherwise</returns>
        public bool DeleteSong(int songId)
        {
            // Search the primary key for the selected song and delete it from 
            // the song table
            DataTable table = musicDataSet.Tables["song"];
            DataRow songRow = table.Rows.Find(songId);
            if (songRow == null)
                return false;

            table.Rows.Remove(songRow);

            // Remove from playlist_song every occurance of songId.
            // Add rows to a separate list before deleting because we'll get an exception
            // if we try to delete more than one row while looping through table.Rows

            List<DataRow> rows = new List<DataRow>();
            table = musicDataSet.Tables["playlist_song"];
            foreach (DataRow row in table.Rows)
                if (row["song_id"].ToString() == songId.ToString())
                    rows.Add(row);

            foreach (DataRow row in rows)
                RemoveSongFromPlaylist(Convert.ToInt32(row["position"]),
                        Convert.ToInt32(row["song_id"]), row["playlist_name"].ToString());

            return true;
        }

        /// <summary>
        /// Save the song database to file.
        /// </summary>
        public void Save()
        {
            // Save music.xml 
            Console.WriteLine("Saving " + XML_MUSICFILE);
            musicDataSet.WriteXml(XML_MUSICFILE);
        }

        /// <summary>
        /// Debug information displaying all the table data in the console.
        /// </summary>
        public void PrintAllTables()
        {
            foreach (DataTable table in musicDataSet.Tables)
            {
                Console.WriteLine("Table name = " + table.TableName);
                foreach (DataRow row in table.Rows)
                {
                    Console.WriteLine("Row:");
                    int i = 0;
                    foreach (Object item in row.ItemArray)
                    {
                        Console.WriteLine(" " + table.Columns[i].Caption + "=" + item);
                        i++;
                    }
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Adds the playlist and returns true if successful, false if not.
        /// The only reason this function would return false is if the playlist
        /// already existed.
        /// </summary>
        /// <param name="playlist">Name of the playlist</param>
        /// <returns>True if the playlist was successfully added</returns>
        public bool AddPlaylist(string playlist)
        {
            Console.WriteLine("AddPlaylist: " + playlist);
            DataTable table = musicDataSet.Tables["playlist"];
            DataRow row = table.NewRow();
            row["name"] = playlist;

            try
            {
                table.Rows.Add(row);
            }
            catch (Exception)
            {
                // Probably a playlist with the same name was being added
                return false;
            }

            return true;
        }


        /// <summary>
        /// Changes an existing playlist's name. Returns true if playlist name
        /// was changed successfully, false if the oldPlaylistName does not exist
        /// or if the newPlaylistName already exists.
        /// </summary>
        /// <param name="oldPlaylistName">Name of playlist to change</param>
        /// <param name="newPlaylistName">New name for playlist</param>
        /// <returns>True if the playlist's name was successfully changed</returns>
        public bool RenamePlaylist(string oldPlaylistName, string newPlaylistName)
        {
            Console.WriteLine("RenamePlaylist: " + oldPlaylistName + " to " + newPlaylistName);

            newPlaylistName = newPlaylistName.Trim();
            if (newPlaylistName == "" || oldPlaylistName == newPlaylistName)
                return false;

            // Update playlist name in playlist and playlist_song tables

            DataTable table = musicDataSet.Tables["playlist"];
            DataRow row = table.Rows.Find(oldPlaylistName);
            if (row == null || PlaylistExists(newPlaylistName))
                return false;

            row["name"] = newPlaylistName;

            table = musicDataSet.Tables["playlist_song"];
            foreach (DataRow r in table.Rows)
                if ((string)r["playlist_name"] == oldPlaylistName)
                    r["playlist_name"] = newPlaylistName;

            return true;
        }

        /// <summary>
        /// Returns true if the given playlist exists, false otherwise.
        /// </summary>
        /// <param name="playlist">Playlist to search for</param>
        /// <returns>True if the playlist exists</returns>
        public bool PlaylistExists(string playlist)
        {
            DataTable table = musicDataSet.Tables["playlist"];
            DataRow row = table.Rows.Find(playlist);
            return row != null;
        }

        /// <summary>
        /// Removes an existing playlist.  Returns true if successful, false if
        /// the playlist doesn't exist.
        /// </summary>
        /// <param name="playlist">Playlist to delete</param>
        /// <returns>True if the playlist was found and deleted</returns>
        public bool DeletePlaylist(string playlist)
        {
            // Search the primary key for this playlist and delete it from the playlist table
            DataTable table = musicDataSet.Tables["playlist"];
            DataRow row = table.Rows.Find(playlist);
            if (row == null)
                return false;

            table.Rows.Remove(row);

            // Remove from playlist_song every occurance of this playlist
            List<DataRow> rows = new List<DataRow>();
            table = musicDataSet.Tables["playlist_song"];
            foreach (DataRow r in table.Rows)
                if ((string)r["playlist_name"] == playlist)
                    rows.Add(r);

            foreach (DataRow r in rows)
                r.Delete();

            return true;
        }

        /// <summary>
        /// Add a song to the last position of the playlist.
        /// </summary>
        /// <param name="songId"></param>
        /// <param name="playlist"></param>
        public void AddSongToPlaylist(int songId, string playlist)
        {
            // Find the last position in the playlist
            int pos = GetLastPosition(playlist);

            DataTable table = musicDataSet.Tables["playlist_song"];
            DataRow row = table.NewRow();

            row["song_id"] = songId;
            row["playlist_name"] = playlist;
            row["position"] = pos + 1;
            table.Rows.Add(row);

            Console.WriteLine("Adding " + songId + " to " + playlist + " at pos " + (pos + 1));
        }

        /// <summary>
        /// Returns the highest position in the given playlist.
        /// </summary>
        /// <param name="playlist"></param>
        /// <returns></returns>
        private int GetLastPosition(string playlist)
        {
            Console.WriteLine("playlist=" + playlist);
            DataTable table = musicDataSet.Tables["playlist_song"];
            var positions = from row in table.AsEnumerable()
                            where (string)row["playlist_name"] == playlist
                            select row["position"];
            return Convert.ToInt32(positions.Max());
        }

        /// <summary>
        /// Remove an existing song at the given position from the playlist.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="songId"></param>
        /// <param name="playlist"></param>
        public void RemoveSongFromPlaylist(int position, int songId, string playlist)
        {
            Console.WriteLine("RemoveSongFromPlaylist: id=" + songId + ", pos=" + position +
                ", playlist=" + playlist);

            // Search the primary key for this playlist and delete it from the playlist table
            DataTable table = musicDataSet.Tables["playlist_song"];
            ArrayList primaryKeys = new ArrayList();

            // Order of keys must match column order
            primaryKeys.Add(songId);
            primaryKeys.Add(playlist);
            primaryKeys.Add(position);
            table.Rows.Remove(table.Rows.Find(primaryKeys.ToArray()));

            // Decrement position by 1 for each song in this playlist that is positioned after
            // this one
            table = musicDataSet.Tables["playlist_song"];
            foreach (DataRow r in table.Rows)
            {
                int pos = Convert.ToInt32(r["position"]);
                if ((string)r["playlist_name"] == playlist && pos > position)
                    r["position"] = pos - 1;
            }

        }

        /// <summary>
        /// Returns a song DataTable for the given playlist or an empty table if the
        /// playlist could not be found.
        /// </summary>
        /// <param name="playlist"></param>
        /// <returns></returns>
        public DataTable SongsForPlaylist(string playlist)
        {
            // Create a table with song attributes and position
            DataTable table = new DataTable();
            table.Columns.Add("id");
            table.Columns.Add("position");
            table.Columns.Add("title");
            table.Columns.Add("artist");
            table.Columns.Add("album");
            table.Columns.Add("genre");

            // Join on the song ID to create a single table
            var songs = from r1 in musicDataSet.Tables["playlist_song"].AsEnumerable()
                        join r2 in musicDataSet.Tables["song"].AsEnumerable()
                             on r1["song_id"] equals r2["id"]
                        where (string)r1["playlist_name"] == playlist
                        orderby r1["position"]
                        select new
                        {
                            Id = r2["id"],
                            Position = r1["position"],
                            Title = r2["title"],
                            Artist = r2["artist"],
                            Album = r2["album"],
                            Genre = r2["genre"]
                        };

            Console.WriteLine("Songs for playlist " + playlist + ":");
            foreach (var s in songs)
            {
                Console.WriteLine(s.ToString());
                DataRow newRow = table.NewRow();
                newRow["id"] = s.Id;
                newRow["position"] = s.Position;
                newRow["title"] = s.Title;
                newRow["artist"] = s.Artist;
                newRow["album"] = s.Album;
                newRow["genre"] = s.Genre;
                table.Rows.Add(newRow);
            }

            return table;
        }
    }

}
