#region Disclaimer / License
// Copyright (C) 2008, Kenneth Skovhede
// http://www.hexad.dk, opensource@hexad.dk
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// 
#endregion
using System;
using System.Collections.Generic;
using System.Text;

namespace Duplicati.Library.Backend
{
    public class File : IStreamingBackend
    {
        private string m_path;
        private string m_username;
        private string m_password;
        Dictionary<string, string> m_options;

        public File()
        {
        }

        public File(string url, Dictionary<string, string> options)
        {
            m_options = options;
            m_path = url.Substring("file://".Length);

            if (m_path.IndexOf("@") > 0)
            {
                m_username = m_path.Substring(0, m_path.IndexOf("@"));
                m_path = m_path.Substring(m_path.IndexOf("@") + 1);

                if (m_username.IndexOf(":") > 0)
                {
                    m_password = m_username.Substring(0, m_username.IndexOf(":"));
                    m_username = m_username.Substring(m_username.IndexOf(":") + 1);
                }
                else
                {
                    if (options.ContainsKey("ftp_password"))
                        m_password = options["ftp_password"];
                }
            }

            if (!System.IO.Path.IsPathRooted(url))
                m_path = System.IO.Path.GetFullPath(m_path);

        }

        #region IBackendInterface Members

        public string DisplayName
        {
            get { return "File based"; }
        }

        public string ProtocolKey
        {
            get { return "file"; }
        }

        public bool SupportsStreaming
        {
            get { return true; }
        }

        public List<FileEntry> List()
        {
            string path = m_path;
            List<FileEntry> ls = new List<FileEntry>();

            //Attempt to apply credentials
            if (!string.IsNullOrEmpty(m_username) && m_password != null)
                Win32.PreAuthenticate(m_path, m_username, m_password);

            foreach (string s in System.IO.Directory.GetFiles(path))
            {
                System.IO.FileInfo fi = new System.IO.FileInfo(s);
                ls.Add(new FileEntry(fi.Name, fi.Length, fi.LastAccessTime, fi.LastWriteTime));
            }

            foreach (string s in System.IO.Directory.GetDirectories(path))
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(s);
                FileEntry fe = new FileEntry(di.Name,0, di.LastAccessTime, di.LastWriteTime);
                fe.IsFolder = true;
                ls.Add(fe);
            }

            return ls;
        }

        public void Put(string remotename, System.IO.Stream stream)
        {
            string path = System.IO.Path.Combine(m_path, remotename);
            using (System.IO.FileStream writestream = System.IO.File.Open(path, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
                Core.Utility.CopyStream(stream, writestream);
        }

        public void Put(string remotename, string filename)
        {
            using (System.IO.FileStream readstream = System.IO.File.Open(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                Put(remotename, filename);
        }

        public void Get(string remotename, System.IO.Stream stream)
        {
            string path = System.IO.Path.Combine(m_path, remotename);
            using (System.IO.FileStream readstream = System.IO.File.Open(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                Core.Utility.CopyStream(readstream, stream);
        }

        public void Get(string remotename, string filename)
        {
            using (System.IO.FileStream writestream = System.IO.File.Open(filename, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
                Get(remotename, writestream);
        }

        public void Delete(string remotename)
        {
            string path = System.IO.Path.Combine(m_path, remotename);
            System.IO.File.Delete(path);
        }

        #endregion

        #region IDisposable Members
        
        public void Dispose()
        {
            if (m_options != null)
                m_options = null;
            if (m_username != null)
                m_username = null;
            if (m_password != null)
                m_password = null;
        }

        #endregion


        public static bool PreAuthenticate(string path, string username, string password)
        {
            return Win32.PreAuthenticate(path, username, password);
        }

    }
}