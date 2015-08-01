﻿using System;
using System.Reflection;
using System.Windows.Forms;
using log4net;
using ScpControl.Rx;
using ScpMonitor.Properties;

namespace ScpMonitor
{
    public partial class SettingsForm : Form
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ScpByteChannel _rootHubChannel;
        private readonly byte[] _mBuffer = new byte[17];

        public SettingsForm(ScpByteChannel protocol)
        {
            _rootHubChannel = protocol;

            InitializeComponent();

            ttSSP.SetToolTip(cbSSP, @"Requires Service Restart");
        }

        public void Reset()
        {
            CenterToScreen();
        }

        public void Response(IScpPacket<byte[]> packet)
        {
            var request = packet.Request;
            var buffer = packet.Payload;

            switch (request)
            {
                case ScpRequest.ConfigRead:
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            tbIdle.Value = buffer[2];
                            cbLX.Checked = buffer[3] == 1;
                            cbLY.Checked = buffer[4] == 1;
                            cbRX.Checked = buffer[5] == 1;
                            cbRY.Checked = buffer[6] == 1;
                            cbLED.Checked = buffer[7] == 1;
                            cbRumble.Checked = buffer[8] == 1;
                            cbTriggers.Checked = buffer[9] == 1;
                            tbLatency.Value = buffer[10];
                            tbLeft.Value = buffer[11];
                            tbRight.Value = buffer[12];
                            cbNative.Checked = buffer[13] == 1;
                            cbSSP.Checked = buffer[14] == 1;
                            tbBrightness.Value = buffer[15];
                            cbForce.Checked = buffer[16] == 1;
                        }));
                    }
                    break;
            }
        }

        public void Request()
        {
            try
            {
                _mBuffer[1] = (byte) ScpRequest.ConfigRead;

                _rootHubChannel.SendAsync(ScpRequest.ConfigRead, _mBuffer);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Unexpected error: {0}", ex);
            }
        }

        private void Form_Load(object sender, EventArgs e)
        {
            Icon = Resources.Scp_All;
        }

        private void Form_Closing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            _mBuffer[1] = (byte) ScpRequest.ConfigWrite;
            _mBuffer[2] = (byte) tbIdle.Value;
            _mBuffer[3] = (byte) (cbLX.Checked ? 0x01 : 0x00);
            _mBuffer[4] = (byte) (cbLY.Checked ? 0x01 : 0x00);
            _mBuffer[5] = (byte) (cbRX.Checked ? 0x01 : 0x00);
            _mBuffer[6] = (byte) (cbRY.Checked ? 0x01 : 0x00);
            _mBuffer[7] = (byte) (cbLED.Checked ? 0x01 : 0x00);
            _mBuffer[8] = (byte) (cbRumble.Checked ? 0x01 : 0x00);
            _mBuffer[9] = (byte) (cbTriggers.Checked ? 0x01 : 0x00);
            _mBuffer[10] = (byte) tbLatency.Value;
            _mBuffer[11] = (byte) tbLeft.Value;
            _mBuffer[12] = (byte) tbRight.Value;
            _mBuffer[13] = (byte) (cbNative.Checked ? 0x01 : 0x00);
            _mBuffer[14] = (byte) (cbSSP.Checked ? 0x01 : 0x00);
            _mBuffer[15] = (byte) tbBrightness.Value;
            _mBuffer[16] = (byte) (cbForce.Checked ? 0x01 : 0x00);

            _rootHubChannel.SendAsync(ScpRequest.ConfigWrite, _mBuffer);

            Hide();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void tbIdle_ValueChanged(object sender, EventArgs e)
        {
            var Value = tbIdle.Value;

            if (Value == 0)
            {
                lblIdle.Text = "Idle Timeout : Disabled";
            }
            else if (Value == 1)
            {
                lblIdle.Text = "Idle Timeout : 1 minute";
            }
            else
            {
                lblIdle.Text = string.Format("Idle Timeout : {0} minutes", Value);
            }
        }

        private void tbLatency_ValueChanged(object sender, EventArgs e)
        {
            var Value = tbLatency.Value << 4;

            lblLatency.Text = string.Format("DS3 Rumble Latency : {0} ms", Value);
        }

        private void tbBrightness_ValueChanged(object sender, EventArgs e)
        {
            var Value = tbBrightness.Value;

            if (Value == 0)
            {
                lblBrightness.Text = string.Format("DS4 Light Bar Brighness : Disabled", Value);
            }
            else
            {
                lblBrightness.Text = string.Format("DS4 Light Bar Brighness : {0}", Value);
            }
        }
    }
}