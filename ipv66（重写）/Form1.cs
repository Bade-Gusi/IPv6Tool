using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
        // 状态区
        private Label lblStatus = null!;
        // 大按钮
        private Button btnEnableBig = null!, btnDisableBig = null!;
        // 访问检测结果
        private Label lblOutbound = null!, lblDns = null!, lblPublicAddr = null!, lblInbound = null!;
        // 工具栏
        private Button btnDetect = null!, btnDns = null!, btnNetReset = null!;
        private Button btnAdapters = null!, btnCopy = null!, btnExport = null!;
        // 底部
        private Button btnGitHub = null!, btnUpdate = null!;
        private CheckBox chkAutoStart = null!;
        private Label lblVersion = null!, lblAuthor = null!;
        // 日志
        private RichTextBox txtLog = null!;

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
            Size = new Size(700, 720);
            MinimumSize = Size;
            MaximumSize = Size;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(245, 245, 250);

            int x = 16;

            // ================================================================
            // 顶部: 状态大标题
            // ================================================================
            var panelTop = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(700, 70),
                BackColor = Color.FromArgb(30, 30, 45)
            };
            Controls.Add(panelTop);

            lblStatus = new Label
            {
                Location = new Point(20, 0),
                Size = new Size(660, 70),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "正在检测 IPv6 状态..."
            };
            panelTop.Controls.Add(lblStatus);

            int yy = 85;

            // ================================================================
            // 大按钮: 开启 / 禁用
            // ================================================================
            btnEnableBig = new Button
            {
                Location = new Point(x, yy),
                Size = new Size(200, 42),
                Text = "开启 IPv6",
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 180, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnEnableBig.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 210, 120);
            btnEnableBig.Click += (_, _) => SetIPv6(0, "开启");
            Controls.Add(btnEnableBig);

            btnDisableBig = new Button
            {
                Location = new Point(x + 212, yy),
                Size = new Size(200, 42),
                Text = "禁用 IPv6",
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 70, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnDisableBig.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 90, 80);
            btnDisableBig.Click += (_, _) => SetIPv6(0xFF, "禁用");
            Controls.Add(btnDisableBig);

            // 检测按钮
            btnDetect = new Button
            {
                Location = new Point(x + 424, yy + 6),
                Size = new Size(110, 30),
                Text = "刷新检测",
                Cursor = Cursors.Hand
            };
            btnDetect.Click += (_, _) => { DetectIPv6(); FullAccessTest(); };
            Controls.Add(btnDetect);

            // ================================================================
            // IPv6 访问能力检测
            // ================================================================
            yy = 140;
            var grpAccess = new GroupBox
            {
                Location = new Point(x, yy),
                Size = new Size(668, 140),
                Text = " IPv6 访问能力检测 ",
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                BackColor = Color.White
            };
            Controls.Add(grpAccess);

            int gy = 28, gl = 22, gg = 6;
            lblOutbound   = MakeResultLabel(24, gy, grpAccess);
            lblDns        = MakeResultLabel(24, gy + gl + gg, grpAccess);
            lblPublicAddr = MakeResultLabel(340, gy, grpAccess);
            lblInbound    = MakeResultLabel(340, gy + gl + gg, grpAccess);

            gy += 2 * (gl + gg) + 4;
            var btnTestAll = new Button
            {
                Location = new Point(24, gy),
                Size = new Size(200, 28),
                Text = "开始全面检测",
                Cursor = Cursors.Hand,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            btnTestAll.Click += (_, _) => FullAccessTest();
            grpAccess.Controls.Add(btnTestAll);

            var lblHint = new Label
            {
                Location = new Point(236, gy + 4),
                Size = new Size(400, 24),
                Text = "检测 IPv6 外网连通性、DNS 解析、公网地址和入站访问",
                ForeColor = Color.Gray
            };
            grpAccess.Controls.Add(lblHint);

            // ================================================================
            // 工具按钮区
            // ================================================================
            yy = 295;
            var grpTools = new GroupBox
            {
                Location = new Point(x, yy),
                Size = new Size(668, 90),
                Text = " 工具箱 ",
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                BackColor = Color.White
            };
            Controls.Add(grpTools);

            int tx = 16, ty = 28, tw = 120;
            btnDns      = MakeToolBtn("刷新 DNS", tx, ty, grpTools);
            btnDns.Click += (_, _) => RunCmd("ipconfig", "/flushdns");

            btnNetReset = MakeToolBtn("重置网络", tx += tw + 8, ty, grpTools);
            btnNetReset.Click += (_, _) => RunCmd("netsh", "int ip reset");

            btnAdapters = MakeToolBtn("适配器列表", tx += tw + 8, ty, grpTools);
            btnAdapters.Click += (_, _) => ListAdapters();

            btnCopy     = MakeToolBtn("复制信息", tx += tw + 8, ty, grpTools);
            btnCopy.Click += (_, _) => CopyLog();

            btnExport   = MakeToolBtn("导出日志", tx += tw + 8, ty, grpTools);
            btnExport.Click += (_, _) => ExportLog();

            // ================================================================
            // 底部信息栏
            // ================================================================
            yy = 395;
            var panelFooter = new Panel
            {
                Location = new Point(x, yy),
                Size = new Size(668, 70),
                BackColor = Color.FromArgb(240, 240, 245),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(panelFooter);

            lblAuthor = new Label
            {
                Location = new Point(12, 8),
                Size = new Size(250, 22),
                Text = "作者: BadeGusi",
                ForeColor = Color.FromArgb(80, 80, 100),
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            panelFooter.Controls.Add(lblAuthor);

            btnGitHub = new Button
            {
                Location = new Point(12, 34),
                Size = new Size(100, 28),
                Text = "GitHub",
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(36, 41, 46),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
            btnGitHub.Click += (_, _) => OpenUrl(GitHubUrl);
            panelFooter.Controls.Add(btnGitHub);

            btnUpdate = new Button
            {
                Location = new Point(120, 34),
                Size = new Size(90, 28),
                Text = "检查更新",
                Cursor = Cursors.Hand
            };
            btnUpdate.Click += (_, _) => CheckUpdate();
            panelFooter.Controls.Add(btnUpdate);

            chkAutoStart = new CheckBox
            {
                Location = new Point(340, 14),
                Size = new Size(130, 24),
                Text = "开机自启动",
                TextAlign = ContentAlignment.MiddleLeft
            };
            chkAutoStart.CheckedChanged += (_, _) => ToggleAutoStart();
            panelFooter.Controls.Add(chkAutoStart);

            lblVersion = new Label
            {
                Location = new Point(340, 40),
                Size = new Size(120, 22),
                Text = $"v{AppVersion}",
                ForeColor = Color.Gray
            };
            panelFooter.Controls.Add(lblVersion);

            var lblOpenSource = new Label
            {
                Location = new Point(480, 14),
                Size = new Size(170, 48),
                Text = "开源项目 · 免费使用",
                ForeColor = Color.FromArgb(140, 140, 160),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            panelFooter.Controls.Add(lblOpenSource);

            // ================================================================
            // 日志
            // ================================================================
            yy = 475;
            var lblLogTitle = new Label
            {
                Location = new Point(x, yy),
                Size = new Size(100, 22),
                Text = "运行日志",
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
            };
            Controls.Add(lblLogTitle);

            txtLog = new RichTextBox
            {
                Location = new Point(x, yy + 26),
                Size = new Size(668, 195),
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(0, 220, 0),
                Font = new Font("Consolas", 9F),
                WordWrap = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(txtLog);
        }

        private static Label MakeResultLabel(int x, int y, Control parent)
        {
            var lb = new Label
            {
                Location = new Point(x, y),
                Size = new Size(300, 22),
                Text = "待检测",
                ForeColor = Color.Gray,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            parent.Controls.Add(lb);
            return lb;
        }

        private static Button MakeToolBtn(string text, int x, int y, Control parent)
        {
            var btn = new Button
            {
                Location = new Point(x, y),
                Size = new Size(120, 28),
                Text = text,
                Cursor = Cursors.Hand,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            parent.Controls.Add(btn);
            return btn;
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
                    SetStatus("IPv6 已启用", true);
                    btnEnableBig.Enabled = false;
                    btnDisableBig.Enabled = true;
                    Log("IPv6 当前为启用状态。");
                }
                else
                {
                    SetStatus("IPv6 已禁用", false);
                    btnEnableBig.Enabled = true;
                    btnDisableBig.Enabled = false;
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

        // ==================== 全面访问检测 ====================

        private async void FullAccessTest()
        {
            Log("开始 IPv6 全面访问检测...");
            SetStatus("检测中...", null);

            lblOutbound.Text   = "外网访问: 检测中...";
            lblDns.Text        = "DNS 解析: 检测中...";
            lblPublicAddr.Text = "公网地址: 检测中...";
            lblInbound.Text    = "入站访问: 检测中...";

            // 1. 外网连通性 (Ping 多个 IPv6 目标)
            bool pingOk = await PingIPv6("2400:3200::1", "阿里云 DNS")
                       || await PingIPv6("2001:4860:4860::8888", "Google DNS")
                       || await PingIPv6("240c::6666", "百度 IPv6");

            lblOutbound.Text = pingOk
                ? "外网访问: 可访问 IPv6 互联网"
                : "外网访问: 无法访问 IPv6 互联网";
            lblOutbound.ForeColor = pingOk ? Color.Green : Color.Red;

            // 2. 检测本机是否有公网 IPv6 地址
            bool hasPublicAddr = HasPublicIPv6();
            lblPublicAddr.Text = hasPublicAddr
                ? "公网地址: 有公网 IPv6 地址"
                : "公网地址: 无公网 IPv6 地址（可能仅链路本地）";
            lblPublicAddr.ForeColor = hasPublicAddr ? Color.Green : Color.Orange;

            // 3. DNS 解析测试
            bool dnsOk = await TestDnsIPv6();
            lblDns.Text = dnsOk
                ? "DNS 解析: IPv6 DNS 解析正常"
                : "DNS 解析: IPv6 DNS 解析异常";
            lblDns.ForeColor = dnsOk ? Color.Green : Color.Red;

            // 4. 入站访问检测 (监听 IPv6 端口?)
            bool inboundOk = CheckIPv6Listening();
            lblInbound.Text = inboundOk
                ? "入站访问: 有服务监听 IPv6 端口"
                : "入站访问: 未检测到 IPv6 监听服务";
            lblInbound.ForeColor = inboundOk ? Color.Green : Color.Orange;

            bool overall = pingOk && dnsOk;
            SetStatus(overall ? "IPv6 运行正常" : "IPv6 存在问题", overall);
            Log("全面检测完成。");
        }

        private async Task<bool> PingIPv6(string address, string name)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(address, 3000);
                if (reply.Status == IPStatus.Success)
                {
                    Log($"Ping {name} ({address}) 成功, {reply.RoundtripTime}ms");
                    return true;
                }
                Log($"Ping {name} ({address}) 失败: {reply.Status}");
                return false;
            }
            catch
            {
                Log($"Ping {name} ({address}) 超时/错误");
                return false;
            }
        }

        private static bool HasPublicIPv6()
        {
            try
            {
                var nis = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up);
                foreach (var ni in nis)
                {
                    var ips = ni.GetIPProperties().UnicastAddresses;
                    foreach (var ip in ips)
                    {
                        // Global unicast = 公网 IPv6 (非链路本地、非唯一本地)
                        if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6 &&
                            !ip.Address.IsIPv6LinkLocal &&
                            !ip.Address.IsIPv6SiteLocal &&
                            ip.Address.GetAddressBytes()[0] >= 0x20)
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private static async Task<bool> TestDnsIPv6()
        {
            try
            {
                var entries = await System.Net.Dns.GetHostEntryAsync("ipv6.baidu.com");
                bool ok = entries.AddressList.Any(a => a.AddressFamily == AddressFamily.InterNetworkV6);
                return ok;
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckIPv6Listening()
        {
            try
            {
                var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                return listeners.Any(ep => ep.AddressFamily == AddressFamily.InterNetworkV6 &&
                                          ep.Port != 0);
            }
            catch { return false; }
        }

        // ==================== 启用 / 禁用 IPv6 ====================

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

                var ok = value == 0;
                SetStatus($"IPv6 已{label}（重启后生效）", ok);
                btnEnableBig.Enabled  = !ok;
                btnDisableBig.Enabled = ok;

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

        // ==================== 适配器列表 ====================

        private void ListAdapters()
        {
            Log("正在读取网络适配器信息...");
            try
            {
                var adapters = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in adapters)
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                        continue;

                    var ipProps = ni.GetIPProperties();
                    bool hasIPv6 = ipProps.UnicastAddresses.Any(a =>
                        a.Address.AddressFamily == AddressFamily.InterNetworkV6 &&
                        !a.Address.IsIPv6LinkLocal);

                    string status = hasIPv6 ? "OK" : "--";
                    string note  = hasIPv6 ? "IPv6 正常" : "无 IPv6 地址";
                    Log($"  [{status}] {ni.Name}  ({ni.NetworkInterfaceType}) - {note}");
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

        private void SetStatus(string text, bool? ok)
        {
            lblStatus.Text = text;
            if (ok == true)   lblStatus.ForeColor = Color.LightGreen;
            else if (ok == false) lblStatus.ForeColor = Color.FromArgb(255, 150, 150);
            else              lblStatus.ForeColor = Color.White;

            // 顶部背景颜色微调
            if (Parent is Panel p)
                p.BackColor = ok == true ? Color.FromArgb(30, 60, 40)
                           : ok == false ? Color.FromArgb(60, 30, 30)
                           : Color.FromArgb(30, 30, 45);
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
                Log("建议: 右键程序 → 以管理员身份运行");
            }
        }
    }
}
