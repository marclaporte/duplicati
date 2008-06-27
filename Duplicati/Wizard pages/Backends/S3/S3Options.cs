using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Wizard;
using Duplicati.Datamodel;

namespace Duplicati.Wizard_pages.Backends.S3
{
    public partial class S3Options : UserControl, IWizardControl, Wizard_pages.Interfaces.ITaskBased
    {
        private const string LOGIN_PAGE = "http://aws.amazon.com/s3";
        private Duplicati.Datamodel.Backends.S3 m_s3;
       
        public S3Options()
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
            get { return "Backup storage options"; }
        }

        string IWizardControl.HelpText
        {
            get { return "On this page you can select where to store the backup data."; }
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
            AWS_ID.Text = m_s3.AccessID;
            AWS_KEY.Text = m_s3.AccessKey;
            BucketName.Text = m_s3.BucketName;
            UseEuroBuckets.Checked = m_s3.UseEuroBucket;
        }

        void IWizardControl.Leave(IWizardForm owner, ref bool cancel)
        {
            if (AWS_ID.Text.Trim().Length <= 0)
            {
                MessageBox.Show(this, "You must enter your AWS Access ID.\nYou may click the link to the right\nto open the AWS login page, and retrieve it.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                cancel = true;
                return;
            }

            if (AWS_KEY.Text.Trim().Length <= 0)
            {
                MessageBox.Show(this, "You must enter your AWS Access ID.\nYou may click the link to the right\nto open the AWS login page, and retrieve it.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                cancel = true;
                return;
            }

            if (BucketName.Text.Trim().Length <= 0)
            {
                MessageBox.Show(this, "You must enter a name for the bucket.\nYou must use a unique name for each backup.\nYou may enter any name you like.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                cancel = true;
                return;
            }

            m_s3.AccessID = AWS_ID.Text;
            m_s3.AccessKey = AWS_KEY.Text;
            m_s3.BucketName = BucketName.Text;
            m_s3.ServerUrl = null;
            m_s3.Prefix = null;
            m_s3.UseEuroBucket = UseEuroBuckets.Checked;

        }

        #endregion

        private void SignUpLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            UrlUtillity.OpenUrl(LOGIN_PAGE);
        }

        #region ITaskBased Members

        public void Setup(Task task)
        {
            m_s3 = new Duplicati.Datamodel.Backends.S3(task);
        }

        #endregion
    }
}