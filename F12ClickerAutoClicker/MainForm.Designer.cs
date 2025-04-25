namespace F12ClickerAutoClicker
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        //private System.Windows.Forms.Button btnTestTime;




        private void InitializeComponent()
        {
            lblServerTime = new System.Windows.Forms.Label();
            cmbServerList = new System.Windows.Forms.ComboBox();
            radioManual = new System.Windows.Forms.RadioButton();
            radioFromFile = new System.Windows.Forms.RadioButton();
            txtManualTime = new System.Windows.Forms.TextBox();
            txtConfigPath = new System.Windows.Forms.TextBox();
            txtProcess = new System.Windows.Forms.TextBox();
            btnBrowseConfig = new System.Windows.Forms.Button();
            btnStart = new System.Windows.Forms.Button();
            SuspendLayout();

            // 
            // lblServerTime
            // 
            lblServerTime.AutoSize = true;
            lblServerTime.Location = new System.Drawing.Point(152, 313);
            lblServerTime.Name = "lblServerTime";
            lblServerTime.Size = new System.Drawing.Size(127, 30);
            lblServerTime.TabIndex = 10;
            lblServerTime.Text = "00:00:00.000";
            // 
            // cmbServerList
            // 
            cmbServerList.Items.Clear(); // پاک کردن آیتم‌های قبلی (در صورت نیاز)
            cmbServerList.Items.Add("https://api.keybit.ir/time/");

            // در صورت نیاز به اضافه کردن سرورهای بیشتر
            cmbServerList.Items.Add("https://worldtimeapi.org/api/timezone/Etc/UTC");
            cmbServerList.Items.Add("https://time.ir");

            // تنظیم آیتم انتخاب‌شده به اولین آیتم
            cmbServerList.SelectedIndex = 0;

            // به روز رسانی سایر ویژگی‌ها
            cmbServerList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbServerList.FormattingEnabled = true;
            cmbServerList.Location = new System.Drawing.Point(120, 20);
            cmbServerList.Name = "cmbServerList";
            cmbServerList.Size = new System.Drawing.Size(200, 38);
            cmbServerList.TabIndex = 8;
            cmbServerList.SelectedIndexChanged += cmbServerList_SelectedIndexChanged;
            // 
            // radioManual
            // 
            radioManual.AutoSize = true;
            radioManual.Location = new System.Drawing.Point(120, 80);
            radioManual.Name = "radioManual";
            radioManual.Size = new System.Drawing.Size(159, 34);
            radioManual.TabIndex = 11;
            radioManual.TabStop = true;
            radioManual.Text = "Manual Time";
            radioManual.UseVisualStyleBackColor = true;
            // 
            // radioFromFile
            // 
            radioFromFile.AutoSize = true;
            radioFromFile.Location = new System.Drawing.Point(120, 110);
            radioFromFile.Name = "radioFromFile";
            radioFromFile.Size = new System.Drawing.Size(189, 34);
            radioFromFile.TabIndex = 12;
            radioFromFile.TabStop = true;
            radioFromFile.Text = "From Config File";
            radioFromFile.UseVisualStyleBackColor = true;
            // 
            // txtManualTime
            // 
            txtManualTime.Enabled = false;
            txtManualTime.Location = new System.Drawing.Point(120, 140);
            txtManualTime.Name = "txtManualTime";
            txtManualTime.Size = new System.Drawing.Size(200, 35);
            txtManualTime.TabIndex = 13;
            // 
            // txtConfigPath
            // 
            txtConfigPath.Enabled = false;
            txtConfigPath.Location = new System.Drawing.Point(120, 170);
            txtConfigPath.Name = "txtConfigPath";
            txtConfigPath.Size = new System.Drawing.Size(200, 35);
            txtConfigPath.TabIndex = 14;
            // 
            // txtProcess
            // 
            txtProcess.Location = new System.Drawing.Point(0, 0);
            txtProcess.Name = "txtProcess";
            txtProcess.Size = new System.Drawing.Size(100, 35);
            txtProcess.TabIndex = 0;
            // 
            // btnBrowseConfig
            // 
            btnBrowseConfig.Enabled = false;
            btnBrowseConfig.Location = new System.Drawing.Point(120, 224);
            btnBrowseConfig.Name = "btnBrowseConfig";
            btnBrowseConfig.Size = new System.Drawing.Size(75, 23);
            btnBrowseConfig.TabIndex = 15;
            btnBrowseConfig.Text = "Browse";
            btnBrowseConfig.UseVisualStyleBackColor = true;
            btnBrowseConfig.Click += btnBrowseConfig_Click;
            // 
            // btnStart
            // 
            btnStart.Location = new System.Drawing.Point(120, 270);
            btnStart.Name = "btnStart";
            btnStart.Size = new System.Drawing.Size(75, 23);
            btnStart.TabIndex = 16;
            btnStart.Text = "Start";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // MainForm
            // 
            ClientSize = new System.Drawing.Size(410, 394);
            Controls.Add(btnStart);
            Controls.Add(btnBrowseConfig);
            Controls.Add(txtConfigPath);
            Controls.Add(txtManualTime);
            Controls.Add(radioFromFile);
            Controls.Add(radioManual);
            Controls.Add(cmbServerList);
            Controls.Add(lblServerTime);
            Name = "MainForm";
            Text = "F12 Auto Clicker";
            ResumeLayout(false);
            PerformLayout();

            //btnTestTime = new System.Windows.Forms.Button();
            //btnTestTime.Location = new System.Drawing.Point(200, 270);
            //btnTestTime.Name = "btnTestTime";
            //btnTestTime.Size = new System.Drawing.Size(100, 30);
            //btnTestTime.TabIndex = 17;
            //btnTestTime.Text = "Test Time API";
            //btnTestTime.UseVisualStyleBackColor = true;
            //btnTestTime.Click += btnTestTime_Click;
            //Controls.Add(btnTestTime);
        }

        private void UpdateServerTimeLabel()
        {
            // تغییر ظاهر lblServerTime برای نمایش میلی‌ثانیه
            lblServerTime.AutoSize = true;
            lblServerTime.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            lblServerTime.Size = new System.Drawing.Size(150, 34);
            lblServerTime.Text = "00:00:00.000";
        }

        private System.Windows.Forms.RadioButton radioManual;
        private System.Windows.Forms.RadioButton radioFromFile;
        private System.Windows.Forms.TextBox txtManualTime;
        private System.Windows.Forms.Button btnBrowseConfig;
        private System.Windows.Forms.TextBox txtConfigPath;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.TextBox txtProcess;
        private System.Windows.Forms.Label lblServerTime;
        private System.Windows.Forms.ComboBox cmbServerList;
    }
}
