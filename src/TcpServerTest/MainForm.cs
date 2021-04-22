using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TcpServerTest.Core;
using TcpServerTest.Core.Utils;

namespace TcpServerTest
{
    public partial class MainForm : Form
    {
        private List<TcpServerContext> contextList = new List<TcpServerContext>();
        private UnitStringConverting storageUnitStringConverting = UnitStringConverting.StorageUnitStringConverting;

        public MainForm()
        {
            InitializeComponent();
        }

        private void pushStatus(string status)
        {
            Invoke(new Action(() => lblStatus.Text = status));
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            var startPort = Convert.ToInt32(nudPortStart.Value);
            var endPort = Convert.ToInt32(nudPortEnd.Value);
            var recvTimeout = Convert.ToInt32(nudRecvTimeout.Value);

            btnStart.Enabled = false;
            btnStop.Enabled = true;

            contextList.Clear();
            listView1.Items.Clear();
            for (var i = startPort; i <= endPort; i++)
            {
                var lvi = listView1.Items.Add(i.ToString());
                lvi.SubItems.Add("");
                lvi.SubItems.Add("");
                lvi.SubItems.Add("");
                lvi.SubItems.Add("");
                lvi.SubItems.Add("");

                var context = new TcpServerContext(new TcpServerContextOptions()
                {
                    Port = i,
                    RecvTimeout = recvTimeout,
                    ConnectCountHandler = t => Invoke(new Action(() => lvi.SubItems[1].Text = t.ToString())),
                    DisConnectCountHandler = t => Invoke(new Action(() => lvi.SubItems[2].Text = t.ToString())),
                    RecvCountHandler = t => Invoke(new Action(() => lvi.SubItems[3].Text = t.ToString("N0") + " B")),
                    StatusHandler = t => Invoke(new Action(() => lvi.SubItems[4].Text = t))
                });
                contextList.Add(context);
                context.Start();
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = true;
            btnStop.Enabled = false;

            foreach (var context in contextList)
                context.Stop();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (btnStop.Enabled)
                btnStop.PerformClick();
        }

        private void tmrStatus_Tick(object sender, EventArgs e)
        {
            var process = Process.GetCurrentProcess();
            lblStatus.Text = "使用内存：" + storageUnitStringConverting.GetString(process.WorkingSet64, 2, true) + "B";
        }
    }
}
