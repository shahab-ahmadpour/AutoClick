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
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                client.Timeout = TimeSpan.FromSeconds(10);

                // دریافت پاسخ از سرور
                HttpResponseMessage response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"API returned status code: {response.StatusCode}");
                    return null;
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response from {url}: {responseContent}");

                // کد پارس کردن بر اساس نوع سرور
                if (url.Contains("keybit.ir"))
                {
                    return ParseKeybitResponse(responseContent);
                }
                else if (url.Contains("worldtimeapi.org"))
                {
                    return ParseWorldTimeApiResponse(responseContent);
                }
                else if (url.Contains("timeapi.io"))
                {
                    return ParseTimeApiIoResponse(responseContent);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching time from {url}: {ex.Message}");
                return null;
            }
        }

        private DateTime? ParseKeybitResponse(string responseContent)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(responseContent);

                if (doc.RootElement.TryGetProperty("date", out var dateElement) &&
                    dateElement.TryGetProperty("time", out var timeElement))
                {
                    // تلاش برای خواندن زمان کامل
                    if (timeElement.TryGetProperty("full", out var fullElement))
                    {
                        string timeStr = fullElement.GetString();
                        if (TimeSpan.TryParse(timeStr, out TimeSpan timeOfDay))
                        {
                            DateTime now = DateTime.Now;
                            return new DateTime(now.Year, now.Month, now.Day,
                                timeOfDay.Hours, timeOfDay.Minutes, timeOfDay.Seconds);
                        }
                    }
                    // تلاش برای خواندن ساعت/دقیقه/ثانیه
                    else if (timeElement.TryGetProperty("hour", out var hourElement) &&
                            timeElement.TryGetProperty("minute", out var minuteElement) &&
                            timeElement.TryGetProperty("second", out var secondElement))
                    {
                        int hour = hourElement.GetInt32();
                        int minute = minuteElement.GetInt32();
                        int second = secondElement.GetInt32();

                        DateTime now = DateTime.Now;
                        return new DateTime(now.Year, now.Month, now.Day, hour, minute, second);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing keybit response: {ex.Message}");
            }

            return null;
        }

        private DateTime? ParseWorldTimeApiResponse(string responseContent)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(responseContent);

                if (doc.RootElement.TryGetProperty("datetime", out var datetimeElement))
                {
                    string datetimeStr = datetimeElement.GetString();
                    if (DateTime.TryParse(datetimeStr, out DateTime result))
                    {
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing worldtimeapi response: {ex.Message}");
            }

            return null;
        }

        private DateTime? ParseTimeApiIoResponse(string responseContent)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(responseContent);

                if (doc.RootElement.TryGetProperty("dateTime", out var datetimeElement))
                {
                    string datetimeStr = datetimeElement.GetString();
                    if (DateTime.TryParse(datetimeStr, out DateTime result))
                    {
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing timeapi.io response: {ex.Message}");
            }

            return null;
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
        "https://api.keybit.ir/time/",
        "https://worldtimeapi.org/api/timezone/Asia/Tehran",
        "https://time.ir"
        // می‌توانید سرور‌های دیگر مانند api.time.ir نیز اضافه کنید
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


        private async Task<DateTime?> GetTimeWithRetry()
        {
            foreach (string server in cmbServerList.Items)
            {
                var time = await GetServerTimeFromUrl(server);
                if (time.HasValue)
                    return time;
            }
            return null;
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
            _updateTimer.Interval = 10; // هر 10 میلی‌ثانیه به‌روزرسانی (برای نمایش دقیق‌تر)
            _updateTimer.Tick += async (s, e) =>
            {
                // نمایش آنی زمان محلی بهینه شده با میلی‌ثانیه
                DateTime localTime = DateTime.Now;
                lblServerTime.Text = localTime.ToString("HH:mm:ss.fff");

                // هر 5 ثانیه یکبار با سرور همگام‌سازی می‌کنیم (برای کاهش فشار شبکه)
                if (localTime.Second % 5 == 0 && localTime.Millisecond < 50)
                {
                    try
                    {
                        var serverTimeResult = await GetServerTimeWithHighPrecision();
                        if (serverTimeResult.HasValue)
                        {
                            // ذخیره اختلاف زمانی بین سرور و سیستم محلی
                            TimeSpan timeDifference = serverTimeResult.Value - DateTime.Now;
                            _serverTimeDifference = timeDifference;

                            // به‌روزرسانی متغیر زمان سرور
                            serverTime = serverTimeResult.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Server time sync error: " + ex.Message);
                    }
                }

                // تنظیم زمان نمایشی بر اساس زمان محلی + اختلاف با سرور
                DateTime adjustedTime = DateTime.Now.Add(_serverTimeDifference);
                lblServerTime.Text = adjustedTime.ToString("HH:mm:ss.fff");
            };

            _updateTimer.Start();

            // همگام‌سازی اولیه با سرور
            SyncWithServerAsync();
        }
        // متغیر جدید برای نگهداری اختلاف زمانی
        private TimeSpan _serverTimeDifference = TimeSpan.Zero;

        // متد جدید برای همگام‌سازی اولیه
        private async void SyncWithServerAsync()
        {
            try
            {
                var serverTimeResult = await GetServerTimeWithHighPrecision();
                if (serverTimeResult.HasValue)
                {
                    _serverTimeDifference = serverTimeResult.Value - DateTime.Now;
                    serverTime = serverTimeResult.Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Initial server time sync error: " + ex.Message);
            }
        }

        // متد دریافت زمان با دقت بالا
        private async Task<DateTime?> GetServerTimeWithHighPrecision()
        {
            Stopwatch stopwatch = new Stopwatch();
            DateTime requestStartTime = DateTime.Now;
            stopwatch.Start();

            try
            {
                using HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(3); // کاهش timeout برای دقت بیشتر

                // ارسال درخواست به سرور زمان
                string url = selectedServerUrl;
                HttpResponseMessage response = await client.GetAsync(url);

                // زمان دریافت پاسخ
                stopwatch.Stop();
                TimeSpan roundTripTime = stopwatch.Elapsed;

                // محاسبه تقریبی زمان one-way delay
                TimeSpan networkDelay = TimeSpan.FromMilliseconds(roundTripTime.TotalMilliseconds / 2);

                string responseContent = await response.Content.ReadAsStringAsync();

                // بسته به سرور انتخابی، پردازش پاسخ متفاوت خواهد بود
                if (url.Contains("keybit.ir"))
                {
                    using JsonDocument doc = JsonDocument.Parse(responseContent);

                    // پردازش API کی‌بیت
                    if (doc.RootElement.TryGetProperty("date", out var dateElement) &&
                        dateElement.TryGetProperty("time", out var timeElement) &&
                        timeElement.TryGetProperty("full", out var fullElement))
                    {
                        string timeStr = fullElement.GetString();

                        // دریافت تاریخ کامل
                        if (doc.RootElement.TryGetProperty("date", out var dateObj) &&
                            dateObj.TryGetProperty("gregorian", out var gregObj) &&
                            gregObj.TryGetProperty("full", out var fullDateObj))
                        {
                            string dateStr = fullDateObj.GetString();

                            // ترکیب تاریخ و زمان
                            if (DateTime.TryParse($"{dateStr} {timeStr}", out DateTime serverTime))
                            {
                                // تنظیم با در نظر گرفتن تأخیر شبکه
                                return serverTime.Add(networkDelay);
                            }
                        }
                    }
                }
                else if (url.Contains("worldtimeapi.org"))
                {
                    // پردازش API جهانی و تبدیل به زمان تهران
                    using JsonDocument doc = JsonDocument.Parse(responseContent);
                    if (doc.RootElement.TryGetProperty("utc_datetime", out var utcElement))
                    {
                        string utcTimeStr = utcElement.GetString();
                        if (DateTime.TryParse(utcTimeStr, out DateTime utcTime))
                        {
                            // تبدیل به زمان تهران (+3:30)
                            TimeZoneInfo tehranZone = TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");
                            DateTime tehranTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, tehranZone);

                            // تنظیم با در نظر گرفتن تأخیر شبکه
                            return tehranTime.Add(networkDelay);
                        }
                    }
                }

                // در صورت ناموفق بودن، بازگشت زمان محلی
                return DateTime.Now;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetServerTimeWithHighPrecision: " + ex.ToString());
                return null;
            }
        }


    }
}
