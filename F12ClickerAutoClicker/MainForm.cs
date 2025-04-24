using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace F12ClickerAutoClicker
{
    public partial class MainForm : Form
    {
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        const int KEYEVENTF_KEYDOWN = 0x0000;
        const int KEYEVENTF_KEYUP = 0x0002;
        const byte VK_F12 = 0x7B;

        private System.Windows.Forms.Timer serverTimeTimer;
        private DateTime serverTime;
        private string selectedServerUrl = "http://worldtimeapi.org/api/timezone/Etc/UTC";

        public MainForm()
        {
            InitializeComponent();
            LoadServerList();
            InitServerTimeUpdater(); // مهم!
            radioManual.CheckedChanged += (s, e) => ToggleInputMode();
            radioFromFile.CheckedChanged += (s, e) => ToggleInputMode();
        }

        private System.Threading.Timer serverTimeThreadingTimer;

        private void InitServerTimeUpdater()
        {
            serverTimeThreadingTimer = new System.Threading.Timer(async _ =>
            {
                try
                {
                    // دریافت زمان از سرور
                    var time = await GetServerTimeFromUrl(selectedServerUrl);
                    if (time.HasValue)
                    {
                        // زمان به درستی دریافت شد، نمایش آن در lblServerTime
                        serverTime = time.Value;
                        Invoke(new Action(() =>
                        {
                            lblServerTime.Text = serverTime.ToString("HH:mm:ss");
                        }));
                    }
                    else
                    {
                        // خطای "Error fetching time" در صورت عدم دریافت زمان
                        Invoke(new Action(() =>
                        {
                            lblServerTime.Text = "خطا در دریافت زمان";
                        }));
                    }
                }
                catch (Exception ex)
                {
                    // در صورت بروز استثنا، نمایش خطا
                    Console.WriteLine("Error fetching time: " + ex.Message);
                    Invoke(new Action(() =>
                    {
                        lblServerTime.Text = "خطا در دریافت زمان";
                    }));
                }
            }, null, 0, 1000); // اجرا هر 1 ثانیه
        }




        private void ToggleInputMode()
        {
            txtManualTime.Enabled = radioManual.Checked;
            btnBrowseConfig.Enabled = radioFromFile.Checked;
        }

        private void btnBrowseConfig_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "Config files|*.json;*.txt" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtConfigPath.Text = ofd.FileName;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            string processName = txtProcess.Text.Trim();
            if (string.IsNullOrEmpty(processName))
            {
                MessageBox.Show("Please enter target process name.");
                return;
            }

            DateTime targetTime;
            if (radioManual.Checked)
            {
                if (!DateTime.TryParseExact(txtManualTime.Text, "HH:mm:ss:fff", null, System.Globalization.DateTimeStyles.None, out targetTime))
                {
                    MessageBox.Show("Invalid time format.");
                    return;
                }
                DateTime now = serverTime;
                targetTime = new DateTime(now.Year, now.Month, now.Day, targetTime.Hour, targetTime.Minute, targetTime.Second, targetTime.Millisecond);
            }
            else if (radioFromFile.Checked)
            {
                if (!File.Exists(txtConfigPath.Text))
                {
                    MessageBox.Show("Config file not found.");
                    return;
                }

                string config = File.ReadAllText(txtConfigPath.Text);
                if (config.Contains("{")) // JSON
                {
                    using JsonDocument doc = JsonDocument.Parse(config);
                    string timeStr = doc.RootElement.GetProperty("time").GetString();
                    targetTime = DateTime.ParseExact(timeStr, "HH:mm:ss:fff", null);
                }
                else // plain text
                {
                    targetTime = DateTime.ParseExact(config.Trim(), "HH:mm:ss:fff", null);
                }

                DateTime now = serverTime;
                targetTime = new DateTime(now.Year, now.Month, now.Day, targetTime.Hour, targetTime.Minute, targetTime.Second, targetTime.Millisecond);
            }
            else
            {
                MessageBox.Show("Select a time input method.");
                return;
            }

            new Thread(() =>
            {
                Process[] procs = Process.GetProcessesByName(processName);
                if (procs.Length == 0)
                {
                    MessageBox.Show("Target process not found.");
                    return;
                }

                IntPtr hWnd = procs[0].MainWindowHandle;

                while (DateTime.Now < targetTime)
                    Thread.Sleep(1);

                SetForegroundWindow(hWnd);
                Thread.Sleep(100);
                keybd_event(VK_F12, 0, KEYEVENTF_KEYDOWN, 0);
                keybd_event(VK_F12, 0, KEYEVENTF_KEYUP, 0);
            }).Start();
        }

        private async Task<DateTime?> GetServerTimeFromUrl(string url)
        {
            try
            {
                using HttpClient client = new HttpClient();

                // دریافت پاسخ از سرور
                var response = await client.GetStringAsync(url);
                Console.WriteLine("Response from server: " + response); // چاپ پاسخ

                if (url.Contains("keybit.ir"))
                {
                    // بررسی اینکه پاسخ به چه شکلی است
                    using JsonDocument doc = JsonDocument.Parse(response);
                    var timeObj = doc.RootElement.GetProperty("time");

                    int hour = timeObj.GetProperty("hour").GetInt32();
                    int minute = timeObj.GetProperty("minute").GetInt32();
                    int second = timeObj.GetProperty("second").GetInt32();

                    return DateTime.Today.Add(new TimeSpan(hour, minute, second));
                }

                // افزودن پشتیبانی برای دیگر سرورها (اگر نیاز است)
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching/parsing time: " + ex.Message);
                return null;
            }
        }



        private void cmbServerList_SelectedIndexChanged(object sender, EventArgs e)
        {
            // چک کردن انتخاب
            Console.WriteLine("Selected Server URL: " + cmbServerList.SelectedItem);
            selectedServerUrl = cmbServerList.SelectedItem.ToString();
        }


        private void LoadServerList()
        {
            List<string> servers = new List<string>
            {
                "https://api.keybit.ir/time/"
            };

            cmbServerList.Items.Clear();
            foreach (var server in servers)
            {
                cmbServerList.Items.Add(server);
            }

            if (cmbServerList.Items.Count > 0)
            {
                cmbServerList.SelectedIndex = 0;
            }
        }

        private async void btnTestTime_Click(object sender, EventArgs e)
        {
            try
            {
                var time = await GetServerTimeFromUrl(cmbServerList.SelectedItem.ToString());
                if (time.HasValue)
                {
                    MessageBox.Show("Received time: " + time.Value.ToString("HH:mm:ss"));
                }
                else
                {
                    MessageBox.Show("Error: Time not received.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}
