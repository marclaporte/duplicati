using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Wizard;

namespace Duplicati.Wizard_pages
{
    public partial class SelectBackend : UserControl, IWizardControl
    {
        public enum Provider
        {
            Unknown,
            File,
            FTP,
            SSH,
            WebDAV,
            S3
        }

        public SelectBackend()
        {
            InitializeComponent();
        }

        #region IWizardControl Members

        Control IWizardControl.Control
        {
            get { return this; }
        }

        string IWizardControl.Title
        {
            get { return "Select a place to store the backups"; }
        }

        string IWizardControl.HelpText
        {
            get { return "On this page you can select the type of device or service that store the backups. You may need information from the service provider when you continue."; }
        }

        Image IWizardControl.Image
        {
            get { return null; }
        }

        bool IWizardControl.FullSize
        {
            get { return false; }
        }

        void IWizardControl.Enter(IWizardForm owner)
        {
        }

        void IWizardControl.Leave(IWizardForm owner, ref bool cancel)
        {
            if (!(File.Checked || FTP.Checked || SSH.Checked || WebDAV.Checked || S3.Checked))
            {
                MessageBox.Show(this, "You must enter the storage method before you can continue.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                cancel = true;
                return;
            }

            if (WebDAV.Checked)
            {
                MessageBox.Show(this, "WebDAV is not implemented yet.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                cancel = true;
                return;
            }
        }

        #endregion


        public Provider SelectedProvider
        {
            get
            {
                if (File.Checked)
                    return Provider.File;
                else if (FTP.Checked)
                    return Provider.FTP;
                else if (SSH.Checked)
                    return Provider.SSH;
                else if (WebDAV.Checked)
                    return Provider.WebDAV;
                else if (S3.Checked)
                    return Provider.S3;
                else
                    return Provider.Unknown;
            }
        }
    }
}