using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace UPSPingTool
{
    public partial class MainForm : Form
    {
        private string _targetIP;
        private bool _monitoring;

        private PropertyFile propertyFile;

        public MainForm()
        {
            InitializeComponent();

            toolStripStatusLabel1.Text = "Idle";
            toolStripStatusLabel2.Text = "OK";

            propertyFile = new PropertyFile("config.ini");

            string pattern = @"\b(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";

            string ip = propertyFile.get("TargetIP", "");
            if (Regex.IsMatch(ip, pattern))
            {
                textBox1.Text = ip;
                StartMonitoring();
            }
        }

        public void StartMonitoring()
        {
            ResetStats();
            timer1.Enabled = true;
            button1.Text = "Stop Monitoring";
            toolStripStatusLabel1.Text = "Monitoring: " + _targetIP;
            textBox1.Enabled = false;
            _monitoring = true;

            LogMessageToFile("Monitoring Started");
        }
               
        public void StopMonitoring()
        {
            timer1.Enabled = false;
            ResetStats();
            button1.Text = "Start Monitoring";
            toolStripStatusLabel1.Text = "Idle";
            toolStripStatusLabel2.Text = "OK";
            textBox1.Enabled = true;
            _monitoring = false;

            LogMessageToFile("Monitoring Stopped");
        }

        public void SaveConfig()
        {
            propertyFile.set("TargetIP", _targetIP);
            propertyFile.Save();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_monitoring)
            {
                StopMonitoring();
            }
            else
            {
                StartMonitoring();
                SaveConfig();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string pattern = @"\b(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";

            
            if (Regex.IsMatch(textBox1.Text, pattern))
            {
                _targetIP = textBox1.Text;
                button1.Enabled = true;
            }
            else
            {
                button1.Enabled = false;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "UPS Monitoring Service PING DATA";
            byte[] buffer = Encoding.ASCII.GetBytes (data);
            int timeout = 1000;
            PingReply reply = pingSender.Send (_targetIP, timeout, buffer, options);
            if (reply.Status != IPStatus.Success)
            {
                _noReplyPings++;
            }

            UpdateStats();
        }
        public void UpdateStats()
        {
            if (_noReplyPings > 0)
            {
                toolStripStatusLabel2.Text = "NoReply: " + _noReplyPings;

                if (_noReplyPings > 12)
                {
                    // 60 seconds of no reply, we will commence Shutdown
                    LogMessageToFile("Initiated Shutdown");
                    Process.Start("shutdown", "/s /t 0");
                }
            }
            else {
                toolStripStatusLabel2.Text = "Target OK";
            }
        }
        private int _noReplyPings = 0;
        public void ResetStats()
        {
            _noReplyPings = 0;
            toolStripStatusLabel2.Text = "OK";
        }

        private void LogMessageToFile(string msg)
        {
            System.IO.StreamWriter sw = System.IO.File.AppendText("UPS_Ping_Tool.log");
            try
            {
                string logLine = System.String.Format(
                    "{0:G}: {1}.", System.DateTime.Now, msg);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }
    }
}
