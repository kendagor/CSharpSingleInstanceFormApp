/*
  CSharpSingleInstanceFormApp - A single instance C# sample app

 Copyright (c) 2016 Elisha Kendagor kosistudio@live.com

 Permission is hereby granted, free of charge, to any person obtaining
 a copy of this software and associated documentation files (the
 "Software"), to deal in the Software without restriction, including
 without limitation the rights to use, copy, modify, merge, publish,
 distribute, sublicense, and/or sell copies of the Software, and to
 permit persons to whom the Software is furnished to do so, subject to
 the following conditions:

 The above copyright notice and this permission notice shall be
 included in all copies or substantial portions of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

*/

using System;
using System.Windows.Forms;

namespace CSharpSingleInstanceFormApp
{
    public partial class SingleInstanceFormMain : Form
    {
        public void ShowForm()
        {
            WindowState = FormWindowState.Normal;
            Show();
            ShowInTaskbar = true;
            notifyIcon1.Visible = false;
        }

        public void HandleCmdlineParams(string extCmdline = null)
        {
            // Handle
        }

        public SingleInstanceFormMain()
        {
            InitializeComponent();
            InstanceManager.MainForm = this;
        }

        private void SingleInstanceFormMain_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                notifyIcon1.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
                notifyIcon1.BalloonTipText = "CSharpSingleInstanceFormApp is running in the background";
                notifyIcon1.BalloonTipTitle = "CSharpSingleInstanceFormApp";
                notifyIcon1.Icon = AppResources.app_icon;

                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(3000);

                notifyIcon1.Text = "CSharpSingleInstanceFormApp";

                ShowInTaskbar = false;
            }
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            ShowForm();
        }

        private void SingleInstanceFormMain_Load(object sender, EventArgs e)
        {
            HandleCmdlineParams();
        }
    }
}
