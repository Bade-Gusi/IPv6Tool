using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ipv66_重写_
{
    public partial class Form1 : Form
    {
        // ===== GitHub 配置 =====
        private const string GitHubRepo = "Bade-Gusi/IPv6Tool";
        private const string GitHubApi  = "https://api.github.com/repos/Bade-Gusi/IPv6Tool/releases/latest";
        private const string GitHubUrl  = "https://github.com/Bade-Gusi/IPv6Tool";
        private readonly string AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

        // ===== 控件 =====
        private Label     lblStatus = null!;
        private Button    btnDetect = null!, btnEnable = null!, btnDisable = null!;
        private Button    btnDns    = null!, btnNetReset = null!, btnPing = null!;
        private Button    btnAdapters = null!, btnCopy = null!, btnExport = null!;
        private Button    btnGitHub = null!, btnUpdate = null!;
        private CheckBox  chkAutoStart = null!;
        private RichTextBox txtLog = null!;
        private Label     lblVersion = null!;

        public Form1()
        {
            InitializeComponent();
            SetupUI();
            CheckAutoStart();
            DetectIPv6();
        }

        // ==================== UI ====================

        private void SetupUI()
        {
            Text = "IPv6 检测工具";
            Size = new Size(620, 580);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Microsoft YaHei UI", 9F);

            int y = 12, col1 = 12, colW = 130, colGap = 10;

            // ===== 状态大标题 =====
            lblStatus = new Label
            {
                Location    = new Point(col1, y),
                Size        = new Size(580, 40),
                TextAlign   = ContentAlignment.MiddleCenter,
                Font        = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold),
                Text        = "正在检测..."
            };
            Controls.Add(lblStatus);

            // ===== 第一行：检测 / 启用 / 禁用 =====
            y = 60;
            btnDetect  = MakeBtn("检测 IPv6", col1, y, colW);
            btnDetect.Click += (_, _) => DetectIPv6();

            btnEnable  = MakeBtn("启用 IPv6", col1 + colW + colGap, y, colW);
            btnEnable.Enabled = false;
            btnEnable.Click += (_, _) => EnableIPv6();

            btnDisable = MakeBtn("禁用 IPv6", col1 + 2 * (colW + colGap), y, colW);
            btnDisable.Click += (_, _) => DisableIPv6();

            // ===== 第二行：刷新 DNS / 重置网络 / 连通测试 =====
            y = 100;
            btnDns      = MakeBtn("刷新 DNS",   col1, y, colW);
            btnDns.Click += (_, _) => RunCmd("ipconfig", "/flushdns");

            btnNetReset = MakeBtn("重置网络",    col1 + colW + colGap, y, colW);
            btnNetReset.Click += (_, _) => RunCmd("netsh", "int ip reset");

            btnPing     = MakeBtn("连通测试",    col1 + 2 * (colW + colGap), y, colW);
            btnPing.Click += (_, _) => PingTest();

            // ===== 第三行：适配器列表 / 复制 / 导出 =====
            y = 140;
            btnAdapters = MakeBtn("适配器列表",  col1, y, colW);
            btnAdapters.Click += (_, _) => ListAdapters();

            btnCopy     = MakeBtn("复制信息",    col1 + colW + colGap, y, colW);
            btnCopy.Click += (_, _) => CopyLog();

            btnExport   = MakeBtn("导出日志",    col1 + 2 * (colW + colGap), y, colW);
            btnExport.Click += (_, _) => ExportLog();

            // ===== 第四行：GitHub / 更新 / 自启 =====
            y = 180;
            btnGitHub = MakeBtn("GitHub",  col1, y, colW);
            btnGitHub.Click += (_, _) => OpenUrl(GitHubUrl);

            btnUpdate = MakeBtn("检查更新", col1 + colW + colGap, y, colW);
            btnUpdate.Click += (_, _) => CheckUpdate();

            chkAutoStart = new CheckBox
            {
                Location    = new Point(col1 + 2 * (colW + colGap), y + 4),
                Size        = new Size(120, 24),
                Text        = "开机自启动",
                TextAlign   = ContentAlignment.MiddleLeft
            };
            chkAutoStart.CheckedChanged += (_, _) => ToggleAutoStart();
            Controls.Add(chkAutoStart);

            lblVersion = new Label
            {
                Location    = new Point(col1 + 2 * (colW + colGap) + 120, y + 4),
                Size        = new Size(100, 24),
                TextAlign   = ContentAlignment.MiddleLeft,
                Text        = $"v{AppVersion}",
                ForeColor   = Color.Gray
            };
            Controls.Add(lblVersion);

            // ===== 日志 =====
            txtLog = new RichTextBox
            {
                Location    = new Point(12, 220),
                Size        = new Size(580, 310),
                ReadOnly    = true,
                BackColor   = Color.FromArgb(30, 30, 30),
                ForeColor   = Color.FromArgb(0, 255, 0),
                Font        = new Font("Consolas", 9F),
                WordWrap    = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(txtLog);
        }

        private static Button MakeBtn(string text, int x, int y, int w)
        {
            return new Button { Location = new Point(x, y), Size = new Size(w, 30), Text = text };
        }

        private void Log(string msg)
        {
            if (txtLog.IsDisposed) return;
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            txtLog.ScrollToCaret();
        }

        // ==================== IPv6 检测 ====================

        private void DetectIPv6()
        {
            Log("正在检测 IPv6 状态...");
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters");
                int disabled = key != null ? (int)(key.GetValue("DisabledComponents", 0) ?? 0) : 0;

                if (disabled == 0)
                {
                    SetStatus("IPv6 已启用", Color.Green);
                    btnEnable.Enabled  = false;
                    btnDisable.Enabled = true;
                    Log("IPv6 当前为启用状态。");
                }
                else
                {
                    SetStatus("IPv6 已禁用", Color.Red);
                    btnEnable.Enabled  = true;
                    btnDisable.Enabled = false;
                    Log($"IPv6 当前为禁用状态 {disabled switch
                    {
                        0xFF => "（全部接口禁用）",
                        0x20 => "（优先 IPv4）",
                        0x21 => "（全部禁用 + 优先 IPv4）",
                        _   => $"(DisabledComponents = 0x{disabled:X2})"
                    }}");
                }
            }
            catch (Exception ex)
            {
                Log($"检测失败: {ex.Message}");
            }
        }

        // ==================== 启用 / 禁用 IPv6 ====================

        private void EnableIPv6()  => SetIPv6(0,    "启用");
        private void DisableIPv6() => SetIPv6(0xFF, "禁用");

        private void SetIPv6(int value, string label)
        {
            if (!IsAdmin())
            {
                MessageBox.Show("需要管理员权限。\n请右键以管理员身份运行本程序。", "权限不足",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log("错误: 需要管理员权限。");
                return;
            }

            var r = MessageBox.Show($"确定要{label} IPv6 吗？\n修改注册表后需要重启才能完全生效。",
                label + " IPv6", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) return;

            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", writable: true)
                    ?? throw new Exception("无法打开注册表键");

                key.SetValue("DisabledComponents", value, RegistryValueKind.DWord);
                Log($"已设置 DisabledComponents = 0x{value:X2}，IPv6 已{label}。");

                var ok = value == 0 ? "已启用" : "已禁用";
                SetStatus($"IPv6 {ok}（重启后生效）", Color.Orange);
                btnEnable.Enabled  = value != 0;
                btnDisable.Enabled = value == 0;

                if (MessageBox.Show("设置成功，需要重启才能完全生效。\n立即重启？",
                        "需要重启", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    Process.Start("shutdown", "/r /t 5");
            }
            catch (Exception ex)
            {
                Log($"操作失败: {ex.Message}");
            }
        }

        // ==================== 系统命令 ====================

        private void RunCmd(string program, string args)
        {
            if (!IsAdmin())
            {
                Log("此操作需要管理员权限。");
                MessageBox.Show("需要管理员权限。请右键以管理员身份运行。", "权限不足",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Log($"执行: {program} {args}");
            try
            {
                var psi = new ProcessStartInfo(program, args)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };
                var p = Process.Start(psi)!;
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(15000);

                if (!string.IsNullOrWhiteSpace(stdout)) Log(stdout.Trim());
                if (!string.IsNullOrWhiteSpace(stderr)) Log("错误: " + stderr.Trim());
                Log($"命令执行完毕，退出码: {p.ExitCode}");
            }
            catch (Exception ex)
            {
                Log($"执行失败: {ex.Message}");
            }
        }

        // ==================== 连通测试 ====================

        private async void PingTest()
        {
            const string target = "2400:3200::1";
            Log($"正在 Ping IPv6 地址 {target}...");
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(target, 3000);
                if (reply.Status == IPStatus.Success)
                {
                    Log($"Ping 成功！延迟: {reply.RoundtripTime}ms");
                    SetStatus("IPv6 连通正常", Color.Green);
                }
                else
                {
                    Log($"Ping 失败: {reply.Status}");
                    SetStatus("IPv6 无法连通", Color.Red);
                }
            }
            catch (Exception ex)
            {
                Log($"Ping 测试失败: {ex.Message}");
                Log("提示: 可能是不支持 IPv6 的网络环境或被防火墙阻止。");
            }
        }

        // ==================== 适配器列表 ====================

        private void ListAdapters()
        {
            Log("正在读取网络适配器信息...");
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters");
                int globalDisabled = key != null ? (int)(key.GetValue("DisabledComponents", 0) ?? 0) : 0;

                var adapters = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in adapters)
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                        continue;

                    var ipProps = ni.GetIPProperties();
                    bool hasIPv6 = ipProps.UnicastAddresses.Any(a =>
                        a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 &&
                        !a.Address.IsIPv6LinkLocal);

                    string status = hasIPv6 ? "OK" : globalDisabled != 0 ? "DIS" : "--";
                    string note  = hasIPv6 ? "IPv6 正常" : globalDisabled != 0 ? "已禁用" : "无 IPv6 地址";
                    Log($"  [{status}] {ni.Name}  ({ni.NetworkInterfaceType}) — {note}");
                }
                Log($"共扫描 {adapters.Length} 个适配器。");
            }
            catch (Exception ex)
            {
                Log($"读取适配器失败: {ex.Message}");
            }
        }

        // ==================== 复制 / 导出 ====================

        private void CopyLog()
        {
            if (string.IsNullOrEmpty(txtLog.Text))
            {
                Log("没有可复制的内容。");
                return;
            }
            Clipboard.SetText(txtLog.Text);
            Log("日志已复制到剪贴板。");
        }

        private void ExportLog()
        {
            using var dlg = new SaveFileDialog
            {
                Filter   = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                FileName = $"IPv6日志_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(dlg.FileName, txtLog.Text, Encoding.UTF8);
                Log($"日志已导出至: {dlg.FileName}");
            }
        }

        // ==================== 开机自启 ====================

        private void CheckAutoStart()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            chkAutoStart.Checked = key?.GetValue("IPv6Tool") != null;
        }

        private void ToggleAutoStart()
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
            if (key == null) return;

            if (chkAutoStart.Checked)
            {
                key.SetValue("IPv6Tool", $"\"{Application.ExecutablePath}\"");
                Log("已添加开机自启动。");
            }
            else
            {
                key.DeleteValue("IPv6Tool", false);
                Log("已移除开机自启动。");
            }
        }

        // ==================== GitHub / 更新 ====================

        private static void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }

        private async void CheckUpdate()
        {
            btnUpdate.Enabled = false;
            btnUpdate.Text = "检查中...";
            Log("正在检查更新...");

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "IPv6Tool-App");
                client.Timeout = TimeSpan.FromSeconds(10);

                var json = await client.GetStringAsync(GitHubApi);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var tag   = root.GetProperty("tag_name").GetString() ?? "";
                var url   = root.GetProperty("html_url").GetString() ?? "";
                var latestVer = tag.TrimStart('v');

                Log($"最新版本: {tag}");

                if (CompareVer(latestVer, AppVersion) > 0)
                {
                    var r = MessageBox.Show($"发现新版本 {tag}！\n当前: v{AppVersion}\n\n前往下载？",
                        "发现更新", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (r == DialogResult.Yes) OpenUrl(url);
                }
                else
                {
                    Log("当前已是最新版本。");
                    MessageBox.Show($"当前已是最新版本 v{AppVersion}", "检查更新",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Log($"检查更新失败: {ex.Message}");
            }
            finally
            {
                btnUpdate.Enabled = true;
                btnUpdate.Text = "检查更新";
            }
        }

        private static int CompareVer(string a, string b)
        {
            var pa = a.Split('.'); var pb = b.Split('.');
            int len = Math.Max(pa.Length, pb.Length);
            for (int i = 0; i < len; i++)
            {
                int va = i < pa.Length && int.TryParse(pa[i], out var x) ? x : 0;
                int vb = i < pb.Length && int.TryParse(pb[i], out var y) ? y : 0;
                if (va != vb) return va.CompareTo(vb);
            }
            return 0;
        }

        // ==================== 工具 ====================

        private void SetStatus(string text, Color color)
        {
            lblStatus.Text   = text;
            lblStatus.ForeColor = color;
        }

        private static bool IsAdmin()
        {
            using var id = System.Security.Principal.WindowsIdentity.GetCurrent();
            return new System.Security.Principal.WindowsPrincipal(id)
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!IsAdmin())
            {
                Log("未以管理员身份运行，启用/禁用 IPv6 和系统命令将受限。");
                Log("   建议: 右键程序 → 以管理员身份运行");
            }
        }
    }
}
