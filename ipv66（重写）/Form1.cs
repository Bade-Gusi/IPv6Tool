using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        // ===== GitHub =====
        private const string GitHubRepo = "Bade-Gusi/IPv6Tool";
        private const string GitHubApi  = "https://api.github.com/repos/Bade-Gusi/IPv6Tool/releases/latest";
        private const string GitHubUrl  = "https://github.com/Bade-Gusi/IPv6Tool";
        private readonly string AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

        // ===== 设计令牌 =====
        private static readonly Color
            BgHeader     = Color.FromArgb(26, 26, 46),
            BgHeaderOk   = Color.FromArgb(26, 58, 40),
            BgHeaderErr  = Color.FromArgb(58, 26, 26),
            BgBody       = Color.FromArgb(245, 245, 250),
            BgCard       = Color.White,
            BgFooter     = Color.FromArgb(240, 240, 245),
            BtnEnable    = Color.FromArgb(40, 160, 92),
            BtnEnableHov = Color.FromArgb(52, 196, 118),
            BtnDisable   = Color.FromArgb(192, 57, 43),
            BtnDisableHov= Color.FromArgb(224, 72, 58),
            BtnGitHub    = Color.FromArgb(36, 41, 46),
            TxtSecondary = Color.FromArgb(127, 140, 141),
            LogBg        = Color.FromArgb(30, 30, 30),
            LogFg        = Color.FromArgb(0, 220, 0);

        // ===== 状态 =====
        private readonly bool isAutoStart;
        private bool ipv6Enabled;
        private bool ipv6Detected;

        // ===== 控件 =====
        private Label     lblStatus = null!;
        private Panel     panelTop = null!;
        private Button    btnEnableBig = null!, btnDisableBig = null!;
        private Label     lblOutbound = null!, lblDns = null!, lblPublicAddr = null!, lblInbound = null!;
        private Label     lblOutboundIcon = null!, lblDnsIcon = null!, lblPublicAddrIcon = null!, lblInboundIcon = null!;
        private Button    btnDetect = null!, btnDns = null!, btnNetReset = null!;
        private Button    btnAdapters = null!, btnCopy = null!, btnExport = null!;
        private Button    btnGitHub = null!, btnUpdate = null!;
        private CheckBox  chkAutoStart = null!;
        private Label     lblVersion = null!, lblAuthor = null!;
        private RichTextBox txtLog = null!;
        private ToolTip   tooltip = null!;
        private System.Windows.Forms.Timer statusTimer = null!;
        private int       dotCount;

        public Form1(bool isAutoStart = false)
        {
            this.isAutoStart = isAutoStart;
            InitializeComponent();
            SetupUI();
            CheckAutoStart();
            DetectIPv6();
        }

        // ==================== UI 构建 ====================

        private void SetupUI()
        {
            Icon = MakeIcon();
            Text = "IPv6 检测工具";
            AutoScaleMode = AutoScaleMode.Dpi;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(660, 560);
            Size = new Size(700, 720);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = BgBody;

            tooltip = new ToolTip { AutoPopDelay = 5000, InitialDelay = 500, ReshowDelay = 200 };
            statusTimer = new System.Windows.Forms.Timer { Interval = 500 };
            statusTimer.Tick += (_, _) => AnimateStatusDots();

            // ================================================================
            // 顶部状态栏 (Dock=Top)
            // ================================================================
            panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = BgHeader
            };
            Controls.Add(panelTop);

            lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "正在检测 IPv6 状态..."
            };
            panelTop.Controls.Add(lblStatus);

            // ================================================================
            // 中间面板 (Dock=Fill)
            // ================================================================
            var panelMid = new Panel { Dock = DockStyle.Fill };
            Controls.Add(panelMid);

            int x = 16;

            // ================================================================
            // 大按钮区 (Dock=Top)
            // ================================================================
            var panelActions = new Panel { Dock = DockStyle.Top, Height = 55 };
            panelMid.Controls.Add(panelActions);

            btnEnableBig = MakeActionBtn("开启 IPv6", x, 8, 200, 40,
                BtnEnable, BtnEnableHov, false);
            btnEnableBig.Click += (_, _) => SetIPv6(0, "开启");
            panelActions.Controls.Add(btnEnableBig);

            btnDisableBig = MakeActionBtn("禁用 IPv6", x + 212, 8, 200, 40,
                BtnDisable, BtnDisableHov, false);
            btnDisableBig.Click += (_, _) => SetIPv6(0xFF, "禁用");
            panelActions.Controls.Add(btnDisableBig);

            btnDetect = new Button
            {
                Location = new Point(x + 424, 14),
                Size = new Size(120, 28),
                Text = "刷新检测",
                TabIndex = 2,
                Cursor = Cursors.Hand
            };
            btnDetect.Click += (_, _) => { DetectIPv6(); FullAccessTest(); };
            panelActions.Controls.Add(btnDetect);
            tooltip.SetToolTip(btnDetect, "重新检测 IPv6 状态并执行全面访问测试");

            // ================================================================
            // IPv6 访问能力检测 (Dock=Top)
            // ================================================================
            var grpAccess = new GroupBox
            {
                Dock = DockStyle.Top,
                Height = 155,
                Text = " IPv6 访问能力检测 ",
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                BackColor = BgCard
            };
            panelMid.Controls.Add(grpAccess);

            int gy = 28, gl = 22, gg = 6;
            MakeResultRow(grpAccess, 24, gy, out lblOutboundIcon, out lblOutbound);
            MakeResultRow(grpAccess, 24, gy + gl + gg, out lblDnsIcon, out lblDns);
            MakeResultRow(grpAccess, 340, gy, out lblPublicAddrIcon, out lblPublicAddr);
            MakeResultRow(grpAccess, 340, gy + gl + gg, out lblInboundIcon, out lblInbound);

            gy += 2 * (gl + gg) + 4;
            var btnTestAll = new Button
            {
                Location = new Point(24, gy),
                Size = new Size(200, 28),
                Text = "开始全面检测",
                TabIndex = 3,
                Cursor = Cursors.Hand
            };
            btnTestAll.Click += (_, _) => FullAccessTest();
            grpAccess.Controls.Add(btnTestAll);
            tooltip.SetToolTip(btnTestAll, "测试 IPv6 外网连通性、DNS 解析、公网地址和入站访问");

            var lblHint = new Label
            {
                Location = new Point(236, gy + 4),
                Size = new Size(400, 24),
                Text = "检测 IPv6 外网连通性、DNS 解析、公网地址和入站访问",
                ForeColor = TxtSecondary
            };
            grpAccess.Controls.Add(lblHint);

            // ================================================================
            // 工具箱 (Dock=Top)
            // ================================================================
            var grpTools = new GroupBox
            {
                Dock = DockStyle.Top,
                Height = 100,
                Text = " 工具箱 ",
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                BackColor = BgCard
            };
            panelMid.Controls.Add(grpTools);

            int tx = 16, ty = 28, tw = 120, ti = 4;
            btnDns      = MakeToolBtn("刷新 DNS", tx, ty, tw, grpTools, ti++);
            btnDns.Click += (_, _) => RunCmd("ipconfig", "/flushdns");
            tooltip.SetToolTip(btnDns, "执行 ipconfig /flushdns 清除 DNS 缓存");

            btnNetReset = MakeToolBtn("重置网络", tx += tw + 8, ty, tw, grpTools, ti++);
            btnNetReset.Click += (_, _) => RunCmd("netsh", "int ip reset");
            tooltip.SetToolTip(btnNetReset, "执行 netsh int ip reset 重置 TCP/IP 堆栈");

            btnAdapters = MakeToolBtn("适配器列表", tx += tw + 8, ty, tw, grpTools, ti++);
            btnAdapters.Click += (_, _) => ListAdapters();
            tooltip.SetToolTip(btnAdapters, "列出所有网络适配器及其 IPv6 状态");

            btnCopy     = MakeToolBtn("复制信息", tx += tw + 8, ty, tw, grpTools, ti++);
            btnCopy.Click += (_, _) => CopyLog();
            tooltip.SetToolTip(btnCopy, "将运行日志复制到剪贴板");

            btnExport   = MakeToolBtn("导出日志", tx += tw + 8, ty, tw, grpTools, ti++);
            btnExport.Click += (_, _) => ExportLog();
            tooltip.SetToolTip(btnExport, "将运行日志保存为文本文件");

            // ================================================================
            // 底部信息栏 (Dock=Top)
            // ================================================================
            var panelFooter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 75,
                BackColor = BgFooter,
                BorderStyle = BorderStyle.FixedSingle
            };
            panelMid.Controls.Add(panelFooter);

            lblAuthor = new Label
            {
                Location = new Point(12, 8),
                Size = new Size(250, 22),
                Text = "作者: BadeGusi",
                ForeColor = Color.FromArgb(80, 80, 100)
            };
            panelFooter.Controls.Add(lblAuthor);

            btnGitHub = new Button
            {
                Location = new Point(12, 34),
                Size = new Size(100, 28),
                Text = "   GitHub",
                TabIndex = 10,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                BackColor = BtnGitHub,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(60, 65, 70) }
            };
            btnGitHub.Click += (_, _) => OpenUrl(GitHubUrl);
            panelFooter.Controls.Add(btnGitHub);
            tooltip.SetToolTip(btnGitHub, GitHubUrl);

            btnUpdate = new Button
            {
                Location = new Point(120, 34),
                Size = new Size(100, 28),
                Text = "检查更新",
                TabIndex = 11,
                Cursor = Cursors.Hand
            };
            btnUpdate.Click += (_, _) => CheckUpdate();
            panelFooter.Controls.Add(btnUpdate);
            tooltip.SetToolTip(btnUpdate, "检查 GitHub 上是否有新版本");

            chkAutoStart = new CheckBox
            {
                Location = new Point(340, 14),
                Size = new Size(130, 24),
                Text = "开机自启动",
                TabIndex = 12,
                TextAlign = ContentAlignment.MiddleLeft
            };
            chkAutoStart.CheckedChanged += (_, _) => ToggleAutoStart();
            panelFooter.Controls.Add(chkAutoStart);
            tooltip.SetToolTip(chkAutoStart, "开机时自动运行本程序，检测到 IPv6 已开启则自动退出");

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
                Location = new Point(480, 10),
                Size = new Size(170, 50),
                Text = "开源项目\n免费使用",
                ForeColor = Color.FromArgb(160, 160, 180),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 8F)
            };
            panelFooter.Controls.Add(lblOpenSource);

            // ================================================================
            // 日志 (Dock=Fill — 占满剩余空间)
            // ================================================================
            var lblLogTitle = new Label
            {
                Location = new Point(16, 0),
                Size = new Size(100, 24),
                Text = "运行日志",
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
            };
            panelMid.Controls.Add(lblLogTitle);

            txtLog = new RichTextBox
            {
                Location = new Point(16, 24),
                Width = 0,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                BackColor = LogBg,
                ForeColor = LogFg,
                Font = new Font("Consolas", 9F),
                WordWrap = true,
                BorderStyle = BorderStyle.FixedSingle,
                TabIndex = 20
            };
            // 修正宽度: 减去两边 padding
            txtLog.Width = panelMid.ClientSize.Width - 32;
            txtLog.Height = panelMid.ClientSize.Height - txtLog.Top - 16;
            panelMid.Controls.Add(txtLog);
            // 窗口尺寸变化时同步调整日志宽度和高度
            panelMid.Resize += (_, _) =>
            {
                txtLog.Width  = panelMid.ClientSize.Width - 32;
                txtLog.Height = panelMid.ClientSize.Height - txtLog.Top - 16;
            };
        }

        // ==================== UI 辅助 ====================

        private static Button MakeActionBtn(string text, int x, int y, int w, int h,
            Color color, Color hover, bool enabled)
        {
            return new Button
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                Text = text,
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = hover },
                Enabled = enabled,
                Cursor = Cursors.Hand
            };
        }

        private static Button MakeToolBtn(string text, int x, int y, int w, Control parent, int tab)
        {
            var btn = new Button
            {
                Location = new Point(x, y),
                Size = new Size(w, 28),
                Text = text,
                TabIndex = tab,
                Cursor = Cursors.Hand
            };
            parent.Controls.Add(btn);
            return btn;
        }

        private static void MakeResultRow(Control parent, int x, int y,
            out Label icon, out Label text)
        {
            icon = new Label
            {
                Location = new Point(x, y),
                Size = new Size(16, 22),
                Text = "",
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = TxtSecondary,
                TextAlign = ContentAlignment.MiddleCenter
            };
            parent.Controls.Add(icon);

            text = new Label
            {
                Location = new Point(x + 18, y),
                Size = new Size(280, 22),
                Text = "待检测",
                ForeColor = TxtSecondary,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            parent.Controls.Add(text);
        }

        private void Log(string msg)
        {
            if (txtLog.IsDisposed) return;
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            txtLog.ScrollToCaret();
        }

        // ==================== 程序图标 ====================

        private static Icon MakeIcon()
        {
            var bmp = new Bitmap(32, 32);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var pen = new Pen(Color.DodgerBlue, 2.5f);
            g.DrawEllipse(pen, 4, 4, 24, 24);
            g.DrawEllipse(pen, 10, 10, 12, 12);

            using var brush = new SolidBrush(Color.DodgerBlue);
            g.FillEllipse(brush, 13, 13, 6, 6);

            using var pen2 = new Pen(Color.DodgerBlue, 2);
            g.DrawLine(pen2, 6, 6, 26, 26);
            g.DrawLine(pen2, 26, 6, 6, 26);

            return Icon.FromHandle(bmp.GetHicon());
        }

        // ==================== 状态动画 ====================

        private void AnimateStatusDots()
        {
            dotCount = (dotCount + 1) % 4;
            var baseText = lblStatus.Text.Contains("检测") ? "检测中" : "运行中";
            lblStatus.Text = baseText + new string('.', dotCount);
        }

        private void StartLoadingAnim()
        {
            dotCount = 0; lblStatus.Text = "检测中"; statusTimer.Start();
        }

        private void StopLoadingAnim() => statusTimer.Stop();

        // ==================== IPv6 检测 ====================

        private void DetectIPv6()
        {
            Log("正在检测 IPv6 状态...");
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters");
                int disabled = key != null ? (int)(key.GetValue("DisabledComponents", 0) ?? 0) : 0;

                ipv6Enabled = disabled == 0;
                ipv6Detected = true;

                if (ipv6Enabled)
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
            StartLoadingAnim();

            SetResult(lblOutboundIcon, lblOutbound, "外网访问", null, "检测中...");
            SetResult(lblDnsIcon, lblDns, "DNS 解析", null, "检测中...");
            SetResult(lblPublicAddrIcon, lblPublicAddr, "公网地址", null, "检测中...");
            SetResult(lblInboundIcon, lblInbound, "入站访问", null, "检测中...");

            bool pingOk = await PingIPv6("2400:3200::1", "阿里云 DNS")
                       || await PingIPv6("2001:4860:4860::8888", "Google DNS")
                       || await PingIPv6("240c::6666", "百度 IPv6");

            SetResult(lblOutboundIcon, lblOutbound, "外网访问", pingOk,
                pingOk ? "可访问 IPv6 互联网" : "无法访问 IPv6 互联网");

            bool hasPublicAddr = HasPublicIPv6();
            SetResult(lblPublicAddrIcon, lblPublicAddr, "公网地址", hasPublicAddr,
                hasPublicAddr ? "有公网 IPv6 地址" : "无公网 IPv6 地址");

            bool dnsOk = await TestDnsIPv6();
            SetResult(lblDnsIcon, lblDns, "DNS 解析", dnsOk,
                dnsOk ? "IPv6 DNS 解析正常" : "IPv6 DNS 解析异常");

            bool inboundOk = CheckIPv6Listening();
            SetResult(lblInboundIcon, lblInbound, "入站访问", inboundOk,
                inboundOk ? "有服务监听 IPv6 端口" : "未检测到 IPv6 监听服务");

            bool overall = pingOk && dnsOk;
            StopLoadingAnim();
            SetStatus(overall ? "IPv6 运行正常" : "IPv6 存在问题", overall);
            Log("全面检测完成。");
        }

        private static void SetResult(Label icon, Label text, string label, bool? ok, string msg)
        {
            if (ok == true)
            {
                icon.Text = "\u2713"; icon.ForeColor = Color.Green;
                text.Text = $"{label}: {msg}"; text.ForeColor = Color.Green;
            }
            else if (ok == false)
            {
                icon.Text = "\u2717"; icon.ForeColor = Color.Red;
                text.Text = $"{label}: {msg}"; text.ForeColor = Color.Red;
            }
            else
            {
                icon.Text = "";
                text.Text = $"{label}: {msg}"; text.ForeColor = TxtSecondary;
            }
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
                    foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6 &&
                            !ip.Address.IsIPv6LinkLocal &&
                            !ip.Address.IsIPv6SiteLocal &&
                            ip.Address.GetAddressBytes()[0] >= 0x20)
                            return true;
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
                return entries.AddressList.Any(a => a.AddressFamily == AddressFamily.InterNetworkV6);
            }
            catch { return false; }
        }

        private static bool CheckIPv6Listening()
        {
            try
            {
                var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                return listeners.Any(ep => ep.AddressFamily == AddressFamily.InterNetworkV6 && ep.Port != 0);
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

                    Log($"  [{(hasIPv6 ? "OK" : "--")}] {ni.Name}  ({ni.NetworkInterfaceType}) - {(hasIPv6 ? "IPv6 正常" : "无 IPv6 地址")}");
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
                // 注册自启动时附带 --autostart 参数，以便启动时区分场景
                key.SetValue("IPv6Tool", $"\"{Application.ExecutablePath}\" --autostart");
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
            StopLoadingAnim();
            lblStatus.Text = text;
            lblStatus.ForeColor = ok == true ? Color.LightGreen
                : ok == false ? Color.FromArgb(255, 150, 150)
                : Color.White;

            panelTop.BackColor = ok == true ? BgHeaderOk
                : ok == false ? BgHeaderErr
                : BgHeader;
        }

        private static bool IsAdmin()
        {
            using var id = System.Security.Principal.WindowsIdentity.GetCurrent();
            return new System.Security.Principal.WindowsPrincipal(id)
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        // ==================== 生命周期 ====================

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!IsAdmin())
                MessageBox.Show("当前未以管理员身份运行。\n启用/禁用 IPv6 以及刷新DNS、重置网络等操作需要管理员权限。\n\n建议关闭程序后右键 → 以管理员身份运行。",
                    "权限提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!isAutoStart) return;
            if (!ipv6Detected) return;

            if (ipv6Enabled)
            {
                BeginInvoke(() => ShowAutoStartCountdown());
            }
        }

        // ==================== 开机自启动倒计时 ====================

        private void ShowAutoStartCountdown()
        {
            using var dlg = new Form
            {
                Text = "IPv6 检测工具",
                Size = new Size(430, 170),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                ControlBox = false,
                BackColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            int seconds = 10;
            var lbl = new Label
            {
                Location = new Point(20, 20),
                Size = new Size(390, 50),
                Text = $"检测到 IPv6 已开启，程序将在 {seconds} 秒后自动关闭",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80)
            };
            dlg.Controls.Add(lbl);

            var btnExit = new Button
            {
                Location = new Point(90, 90),
                Size = new Size(110, 32),
                Text = "立即退出",
                Cursor = Cursors.Hand,
                BackColor = BtnDisable,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
            btnExit.Click += (_, _) => { dlg.DialogResult = DialogResult.Yes; dlg.Close(); };
            dlg.Controls.Add(btnExit);

            var btnStay = new Button
            {
                Location = new Point(220, 90),
                Size = new Size(120, 32),
                Text = "取消并留在程序",
                Cursor = Cursors.Hand,
                BackColor = BtnEnable,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
            btnStay.Click += (_, _) => { dlg.DialogResult = DialogResult.No; dlg.Close(); };
            dlg.Controls.Add(btnStay);

            var timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += (_, _) =>
            {
                seconds--;
                lbl.Text = $"检测到 IPv6 已开启，程序将在 {seconds} 秒后自动关闭";
                if (seconds <= 0)
                {
                    timer.Stop();
                    dlg.DialogResult = DialogResult.Yes;
                    dlg.Close();
                }
            };
            timer.Start();

            dlg.Shown += (_, _) => timer.Start();
            dlg.FormClosing += (_, _) => timer.Stop();

            dlg.ShowDialog(this);
            if (dlg.DialogResult == DialogResult.Yes)
                Close();
        }
    }
}
