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
        private string selectedServerUrl = "https://api.keybit.ir/time/";

        public MainForm()
        {
            InitializeComponent();
            LoadServerList();

            // InitServerTimeUpdater(); // متد قبلی را کامنت کنید
            SetupSimpleTimeUpdater(); // متد جدید را فراخوانی کنید

            radioManual.CheckedChanged += (s, e) => ToggleInputMode();
            radioFromFile.CheckedChanged += (s, e) => ToggleInputMode();
        }

        private System.Threading.Timer serverTimeThreadingTimer;

        private void InitServerTimeUpdater()
        {
            // اضافه کردن پشتیبان ساعت محلی در صورت خطا
            System.Windows.Forms.Timer localTimeTimer = new System.Windows.Forms.Timer();
            localTimeTimer.Interval = 1000;
            localTimeTimer.Tick += (s, e) =>
            {
                // اگر lblServerTime هنوز نمایش دهنده خطاست
                if (lblServerTime.Text.Contains("خطا"))
                {
                    lblServerTime.Text = DateTime.Now.ToString("HH:mm:ss");
                }
            };
            localTimeTimer.Start();

            // کد اصلی دریافت زمان از سرور
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
                client.Timeout = TimeSpan.FromSeconds(10);

                // دریافت پاسخ از سرور
                var response = await client.GetStringAsync(url);
                Console.WriteLine("Raw API Response: " + response);

                if (url.Contains("keybit.ir"))
                {
                    // پارس کردن با محافظت کامل
                    try
                    {
                        using JsonDocument doc = JsonDocument.Parse(response);

                        // ساختار date.time.full
                        if (doc.RootElement.TryGetProperty("date", out var dateElement))
                        {
                            if (dateElement.TryGetProperty("time", out var timeElement) &&
                                timeElement.TryGetProperty("full", out var fullElement))
                            {
                                var timeStr = fullElement.GetString();
                                Console.WriteLine("Found time string: " + timeStr);
                                if (DateTime.TryParse(timeStr, out DateTime parsedTime))
                                {
                                    return parsedTime;
                                }
                            }
                        }

                        // جستجوی ساختار time.hour/minute/second
                        if (doc.RootElement.TryGetProperty("time", out var directTimeElement))
                        {
                            Console.WriteLine("Found time object");
                            if (directTimeElement.ValueKind == JsonValueKind.Object)
                            {
                                if (directTimeElement.TryGetProperty("hour", out var hourElement) &&
                                    directTimeElement.TryGetProperty("minute", out var minuteElement) &&
                                    directTimeElement.TryGetProperty("second", out var secondElement))
                                {
                                    int hour = hourElement.GetInt32();
                                    int minute = minuteElement.GetInt32();
                                    int second = secondElement.GetInt32();
                                    Console.WriteLine($"Found time components: {hour}:{minute}:{second}");
                                    return DateTime.Today.Add(new TimeSpan(hour, minute, second));
                                }
                            }
                            else if (directTimeElement.ValueKind == JsonValueKind.String)
                            {
                                var timeStr = directTimeElement.GetString();
                                if (DateTime.TryParse(timeStr, out DateTime parsedTime))
                                {
                                    return parsedTime;
                                }
                            }
                        }

                        Console.WriteLine("Could not find expected time structure in JSON");
                    }
                    catch (JsonException jex)
                    {
                        Console.WriteLine("JSON parsing error: " + jex.Message);
                    }
                }

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

        //private async void btnTestTime_Click(object sender, EventArgs e)
        //{
        //    btnTestTime.Enabled = false;
        //    btnTestTime.Text = "در حال تست...";

        //    try
        //    {
        //        var time = await GetServerTimeFromKeybit();
        //        if (time.HasValue)
        //        {
        //            MessageBox.Show($"زمان دریافتی از سرور: {time.Value.ToString("HH:mm:ss")}",
        //                "موفقیت", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //        }
        //        else
        //        {
        //            MessageBox.Show("دریافت زمان از سرور با خطا مواجه شد. لطفاً لاگ کنسول را بررسی کنید.",
        //                "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"خطا: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }

        //    btnTestTime.Text = "Test Time API";
        //    btnTestTime.Enabled = true;
        //}

        private async Task<DateTime?> GetServerTimeFromKeybit()
        {
            try
            {
                using HttpClient client = new HttpClient();
                string url = "https://api.keybit.ir/time/";

                // دریافت پاسخ از سرور
                string response = await client.GetStringAsync(url);

                // لاگ کردن پاسخ برای دیباگ
                Console.WriteLine("API Response: " + response);

                // پارس کردن پاسخ JSON
                using JsonDocument doc = JsonDocument.Parse(response);

                // بررسی ساختار date.time.full
                if (doc.RootElement.TryGetProperty("date", out var dateElement) &&
                    dateElement.TryGetProperty("time", out var timeElement) &&
                    timeElement.TryGetProperty("full", out var fullElement))
                {
                    string timeStr = fullElement.GetString();
                    DateTime currentTime = DateTime.Now;

                    // تبدیل ساعت:دقیقه:ثانیه به DateTime
                    if (TimeSpan.TryParse(timeStr, out TimeSpan timeOfDay))
                    {
                        return new DateTime(currentTime.Year, currentTime.Month, currentTime.Day,
                            timeOfDay.Hours, timeOfDay.Minutes, timeOfDay.Seconds);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetServerTimeFromKeybit: " + ex.ToString());
                return null;
            }
        }

        private System.Windows.Forms.Timer _updateTimer;

        private void SetupSimpleTimeUpdater()
        {
            _updateTimer = new System.Windows.Forms.Timer();
            _updateTimer.Interval = 1000; // هر 5 ثانیه
            _updateTimer.Tick += async (s, e) =>
            {
                _updateTimer.Stop(); // توقف تایمر برای جلوگیری از فراخوانی همزمان

                try
                {
                    var time = await GetServerTimeFromKeybit();
                    if (time.HasValue)
                    {
                        serverTime = time.Value;
                        lblServerTime.Text = serverTime.ToString("HH:mm:ss");
                    }
                    else
                    {
                        lblServerTime.Text = DateTime.Now.ToString("HH:mm:ss");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Update timer error: " + ex.Message);
                    lblServerTime.Text = DateTime.Now.ToString("HH:mm:ss");
                }

                _updateTimer.Start(); // شروع مجدد تایمر
            };

            _updateTimer.Start();
        }
    }
}
