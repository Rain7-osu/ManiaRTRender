﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace RenderClient
{
    public partial class RenderForm : Form
    {
        private RenderClient renderClient;
        private System.Timers.Timer fpsTimer;
        private int id = -1;
        private SynchronizationContext syncContext = SynchronizationContext.Current;
        private long fpsCallbackCount = 0;

        public RenderForm(int id)
        {
            InitializeComponent();
            syncContext = SynchronizationContext.Current;
            this.FormBorderStyle = FormBorderStyle.None; // no borders
            this.DoubleBuffered = true;
            this.BackColor = Color.Black;
            this.SetStyle(ControlStyles.ResizeRedraw, true); // this is to avoid visual artifacts
            this.id = id;

            renderClient = new RenderClient(glControl, this, id);

            SetupCallback(glControl);

            fpsTimer = new System.Timers.Timer(250);
            fpsTimer.Enabled = true;
            fpsTimer.Elapsed += new ElapsedEventHandler(CalculateFPS);
            fpsTimer.AutoReset = true;

            controlLabel.Hide();
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            //string id_string = id >= 0 ? $"({id}) " : "";
            Text = $"ManiaRTRender ({id})";
        }

        private void SetupCallback(Control control)
        {
            control.MouseMove += GLMouseMove;
            control.MouseDown += GLMouseDown;
            control.MouseEnter += GLMouseEnter;
            control.MouseLeave += GLMouseLeave;
        }

        private void CalculateFPS(object sender, ElapsedEventArgs e)
        {
            if (fpsCallbackCount % 4 == 0)
            {
                syncContext.Post(UpdateControlLabel, null);
            }

            fpsCallbackCount += 1;
            // search for sync.exe every 10 seconds
            if (fpsCallbackCount % 10 == 0)
            {

                bool hasSync = false;
                
                System.Diagnostics.Process[] processList = System.Diagnostics.Process.GetProcesses();
                foreach (System.Diagnostics.Process process in processList)
                {
                    if (process.Id == Program.ParentId)
                    {
                        hasSync = true;
                        break;
                    }
                }

                if (!hasSync) Close();
            }
        }

        private void UpdateControlLabel(object obj)
        {
            controlLabel.Text = $"{renderClient.PlayerName} (FPS: {renderClient.GetRenderCountAndClear()})";
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            renderClient.Load(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            renderClient.Close(e);
            base.OnClosing(e);
        }

        #region resize window
        private const int
            HTCAPTION = 2,
            HTLEFT = 10,
            HTRIGHT = 11,
            HTTOP = 12,
            HTTOPLEFT = 13,
            HTTOPRIGHT = 14,
            HTBOTTOM = 15,
            HTBOTTOMLEFT = 16,
            HTBOTTOMRIGHT = 17;

        const int _ = 8;

        Rectangle Top { get { return new Rectangle(0, 0, this.ClientSize.Width, _); } }
        Rectangle Left { get { return new Rectangle(0, 0, _, this.ClientSize.Height); } }
        Rectangle Bottom { get { return new Rectangle(0, this.ClientSize.Height - _, this.ClientSize.Width, _); } }
        Rectangle Right { get { return new Rectangle(this.ClientSize.Width - _, 0, _, this.ClientSize.Height); } }

        Rectangle TopLeft { get { return new Rectangle(0, 0, _, _); } }
        Rectangle TopRight { get { return new Rectangle(this.ClientSize.Width - _, 0, _, _); } }

        private void hideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            hideToolStripMenuItem.Checked = !hideToolStripMenuItem.Checked;
            renderClient.SetHideInIdle(hideToolStripMenuItem.Checked);
        }

        private void topMostToolStripMenuItem_Click(object sender, EventArgs e)
        {
            topMostToolStripMenuItem.Checked = !topMostToolStripMenuItem.Checked;
            TopMost = topMostToolStripMenuItem.Checked;
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        Rectangle BottomLeft { get { return new Rectangle(0, this.ClientSize.Height - _, _, _); } }
        Rectangle BottomRight { get { return new Rectangle(this.ClientSize.Width - _, this.ClientSize.Height - _, _, _); } }

        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);

            if (message.Msg == 0x84) // WM_NCHITTEST
            {
                var cursor = this.PointToClient(Cursor.Position);

                if (TopLeft.Contains(cursor)) message.Result = (IntPtr)HTTOPLEFT;
                else if (TopRight.Contains(cursor)) message.Result = (IntPtr)HTTOPRIGHT;
                else if (BottomLeft.Contains(cursor)) message.Result = (IntPtr)HTBOTTOMLEFT;
                else if (BottomRight.Contains(cursor)) message.Result = (IntPtr)HTBOTTOMRIGHT;

                else if (Top.Contains(cursor)) message.Result = (IntPtr)HTTOP;
                else if (Left.Contains(cursor)) message.Result = (IntPtr)HTLEFT;
                else if (Right.Contains(cursor)) message.Result = (IntPtr)HTRIGHT;
                else if (Bottom.Contains(cursor)) message.Result = (IntPtr)HTBOTTOM;
                else message.Result = (IntPtr)HTCAPTION;
            }
        }

        #endregion

        #region shift window

        Point downPoint;
        private void GLMouseDown(object sender, MouseEventArgs e)
        {
            downPoint = new Point(e.X, e.Y);
        }

        private void GLMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Location = new Point(this.Location.X + e.X - downPoint.X,
                    this.Location.Y + e.Y - downPoint.Y);
            }
        }

        private void GLMouseEnter(object sender, EventArgs e)
        {
            controlLabel.Show();
            controlLabel.BringToFront();
            BackColor = SystemColors.Highlight;
        }

        private void GLMouseLeave(object sender, EventArgs e)
        {
            controlLabel.Hide();
            BackColor = Color.Black;
        }

        #endregion
    }
}
