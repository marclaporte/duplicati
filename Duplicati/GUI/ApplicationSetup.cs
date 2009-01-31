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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Data.LightDatamodel;
using Duplicati.Datamodel;

namespace Duplicati.GUI
{
    public partial class ApplicationSetup : Form
    {
        private IDataFetcherCached m_connection;
        private ApplicationSettings m_settings;
        private bool m_isUpdating = false;

        public ApplicationSetup()
        {
            InitializeComponent();
            m_connection = new DataFetcherNested(Program.DataConnection);
            m_settings = new ApplicationSettings(m_connection);

            try
            {
                m_isUpdating = true;
                RecentDuration.Text = m_settings.RecentBackupDuration;
                PGPPath.Text = m_settings.PGPPath;
                PythonPath.Text = m_settings.PythonPath;
                DuplicityPath.Text = m_settings.DuplicityPath;
                NcFTPPath.Text = m_settings.NcFTPPath;
            }
            finally
            {
                m_isUpdating = false;
            }
        }

        private bool TestForFiles(string folder, params string[] files)
        {
            try
            {
                foreach(string file in files)
                    if (!System.IO.File.Exists(System.IO.Path.Combine(folder, file)))
                        if (MessageBox.Show(this, "The folder selected does not contain the file: " + file + ".\r\nDo you want to use that folder anyway?", Application.ProductName, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button3) != DialogResult.Yes)
                            return false;
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(this, "An exception occured while examining the folder: "+ ex.Message + ".\r\nDo you want to use that folder anyway?", Application.ProductName, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button3) != DialogResult.Yes)
                    return false;
            }

            return true;
        }

        private void BrowsePGP_Click(object sender, EventArgs e)
        {
            if (PGPBrowser.ShowDialog(this) == DialogResult.OK)
                if (TestForFiles(PGPBrowser.SelectedPath, "pgp.exe")) 
                    PGPPath.Text = PGPBrowser.SelectedPath;
        }

        private void BrowsePython_Click(object sender, EventArgs e)
        {
            if (PythonFileDialog.ShowDialog(this) == DialogResult.OK)
                if (TestForFiles(System.IO.Path.GetDirectoryName(PythonFileDialog.FileName), "python.exe"))
                    PythonPath.Text = PythonFileDialog.FileName;
        }

        private void BrowseDuplicity_Click(object sender, EventArgs e)
        {
            if (DuplicityFileDialog.ShowDialog(this) == DialogResult.OK)
                if (TestForFiles(System.IO.Path.GetDirectoryName(DuplicityFileDialog.FileName), "duplicity.py"))
                    DuplicityPath.Text = DuplicityFileDialog.FileName;
        }

        private void RecentDuration_TextChanged(object sender, EventArgs e)
        {
            if (m_isUpdating)
                return;
            m_settings.RecentBackupDuration = RecentDuration.Text;
        }

        private void PGPPath_TextChanged(object sender, EventArgs e)
        {
            if (m_isUpdating)
                return;
            m_settings.PGPPath = PGPPath.Text;
        }

        private void PythonPath_TextChanged(object sender, EventArgs e)
        {
            if (m_isUpdating)
                return;
            m_settings.PythonPath = PythonPath.Text;
        }

        private void DuplicityPath_TextChanged(object sender, EventArgs e)
        {
            if (m_isUpdating)
                return;
            m_settings.DuplicityPath = DuplicityPath.Text;
        }

        private void OKBtn_Click(object sender, EventArgs e)
        {
            //TODO: Fix once CommitRecursive is done
            m_connection.CommitAll();
            Program.DataConnection.CommitAll();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BrowseNcFTP_Click(object sender, EventArgs e)
        {
            if (NcFTPBrowser.ShowDialog(this) == DialogResult.OK)
                if (TestForFiles(NcFTPBrowser.SelectedPath, "ncftp.exe", "ncftpbatch.exe", "ncftpget.exe", "ncftpls.exe", "ncftpput.exe"))
                    NcFTPPath.Text = NcFTPBrowser.SelectedPath;
       }

        private void NcFTPPath_TextChanged(object sender, EventArgs e)
        {
            if (m_isUpdating)
                return;
            m_settings.NcFTPPath  = NcFTPPath.Text;
        }

        private void BrowsePutty_Click(object sender, EventArgs e)
        {
            if (PuttyBrowser.ShowDialog(this) == DialogResult.OK)
                if (TestForFiles(PuttyBrowser.SelectedPath, "pscp.exe", "psftp.exe"))
                    PuttyPath.Text = PuttyBrowser.SelectedPath;
        }

        private void PuttyPath_TextChanged(object sender, EventArgs e)
        {
            if (m_isUpdating)
                return;
            m_settings.PuttyPath = PuttyPath.Text;
        }
    }
}