using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using PosPrinterApp.Services;
using PosPrinterApp.Models;

namespace PosPrinterApp
{
    public partial class MainForm : Form
    {
        private PosPrinterService _printerService;
        private CashDrawerService _cashDrawerService;
        private HttpServerService _httpServerService;
        private JobPollerService? _jobPollerService;
        private ComboBox _printerComboBox;
        private TextBox _customTextTextBox;
        private Button _btnPrintTest;
        private Button _btnPrintCustom;
        private Button _btnOpenDrawer;
        private Button _btnOpenDrawerPin1;
        private Button _btnOpenDrawerPin2;
        private Label _lblPrinter;
        private Label _lblCustomText;
        private GroupBox _grpServer;
        private Label _lblServerPort;
        private TextBox _txtServerPort;
        private Button _btnServerStart;
        private Button _btnServerStop;
        private Label _lblServerStatus;
        private TextBox _txtServerLog;
        private GroupBox _grpPolling;
        private TextBox _txtBaseUrl;
        private ComboBox _cmbCompany;
        private ComboBox _cmbUser;
        private Button _btnStartPolling;
        private Button _btnStopPolling;
        private Label _lblPollingStatus;
        private Dictionary<string, List<string>> _companyUsers = new Dictionary<string, List<string>>();
        private TabControl _tabControl;
        private string _sessionCookie = ""; // PHPSESSID dari WebView setelah login
        private bool _companySwitchDetected = false; // true jika user sempat ke /site/company
        private TabPage _tabPrinter;
        private TabPage _tabWebView;
        private WebView2 _webView;
        private TextBox _txtWebViewUrl;
        private Button _btnWebViewGo;
        private Button _btnWebViewRefresh;
        private Button _btnZoomIn;
        private Button _btnZoomOut;
        private Button _btnZoomReset;
        private Button _btnFitToWidth;
        private Label _lblZoom;
        private Button _btnBack;
        private Button _btnForward;
        private double _currentZoomFactor = 1.0;
        private int _screenWidth;
        private int _screenHeight;
        private float _dpiScaleX;
        private float _dpiScaleY;

        public MainForm()
        {
            // Set working directory ke application directory untuk ensure DLLs bisa ditemukan
            try
            {
                string appDir = Path.GetDirectoryName(Application.ExecutablePath);
                if (!string.IsNullOrEmpty(appDir) && Directory.Exists(appDir))
                {
                    Directory.SetCurrentDirectory(appDir);
                    System.Diagnostics.Debug.WriteLine($"Working directory set to: {appDir}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting working directory: {ex.Message}");
            }
            
            // Get screen resolution and DPI before initializing components
            GetScreenResolution();
            InitializeComponent();
            InitializeServices();
            LoadPrinters();
        }

        private void GetScreenResolution()
        {
            // Get primary screen bounds (physical resolution)
            var screen = Screen.PrimaryScreen;
            _screenWidth = screen.Bounds.Width;
            _screenHeight = screen.Bounds.Height;
            
            // Get DPI scaling - use a temporary form to get DPI
            try
            {
                using (var tempForm = new Form())
                {
                    tempForm.CreateControl();
                    using (var g = tempForm.CreateGraphics())
                    {
                        _dpiScaleX = g.DpiX / 96f; // 96 is standard DPI
                        _dpiScaleY = g.DpiY / 96f;
                    }
                }
            }
            catch
            {
                // Fallback to default DPI if can't get it
                _dpiScaleX = 1.0f;
                _dpiScaleY = 1.0f;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "DXN POS";
            
            // Set application icon dari PNG
            try
            {
                var icon = IconHelper.LoadIconFromPng("logo_pos.png");
                if (icon != null)
                {
                    this.Icon = icon;
                }
            }
            catch (Exception ex)
            {
                // Ignore icon loading errors, use default icon
                System.Diagnostics.Debug.WriteLine($"Error loading icon: {ex.Message}");
            }
            
            // Calculate form size based on screen resolution (50% of screen)
            int formWidth = (int)(_screenWidth * 0.5);
            int formHeight = (int)(_screenHeight * 0.5);
            
            // Ensure minimum size (lebih kecil lagi)
            formWidth = Math.Max(formWidth, 600);
            formHeight = Math.Max(formHeight, 400);
            
            // Ensure maximum size doesn't exceed screen
            formWidth = Math.Min(formWidth, _screenWidth - 40);
            formHeight = Math.Min(formHeight, _screenHeight - 40);
            
            this.Size = new Size(formWidth, formHeight);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimumSize = new Size(600, 400);
            this.WindowState = FormWindowState.Maximized; // Maximize saat startup
            this.FormClosing += MainForm_FormClosing;
            this.Shown += MainForm_Shown; // Event untuk auto-fit setelah form shown

            // TabControl - akan mengisi seluruh form
            _tabControl = new TabControl
            {
                Location = new Point(0, 0),
                Dock = DockStyle.Fill
            };
            this.Controls.Add(_tabControl);

            // Tab 1: WebView (di kiri)
            _tabWebView = new TabPage("Web POS");
            _tabControl.TabPages.Add(_tabWebView);

            // Tab 2: Printer Control (di kanan)
            _tabPrinter = new TabPage("Kontrol Aplikasi");
            _tabControl.TabPages.Add(_tabPrinter);

            InitializeWebViewTab();
            InitializePrinterTab();
        }

        private void InitializePrinterTab()
        {
            // Header dengan logo DXN
            Panel headerPanel = new Panel
            {
                Location = new Point(0, 0),
                Height = 60,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(0, 120, 215) // DXN Blue
            };
            _tabPrinter.Controls.Add(headerPanel);
            
            // Label logo DXN
            Label lblDxnLogo = new Label
            {
                Text = "DXN POS SYSTEM",
                Location = new Point(10, 10),
                Size = new Size(300, 40),
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerPanel.Controls.Add(lblDxnLogo);
            
            // Browser-like toolbar di bawah header
            Panel toolbar = new Panel
            {
                Location = new Point(0, 60),
                Height = 40,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(240, 240, 240)
            };
            _tabPrinter.Controls.Add(toolbar);

            // Back Button
            _btnBack = new Button
            {
                Text = "←",
                Location = new Point(5, 5),
                Size = new Size(35, 30),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnBack.FlatAppearance.BorderSize = 1;
            _btnBack.FlatAppearance.BorderColor = Color.Gray;
            _btnBack.Click += BtnBack_Click;
            toolbar.Controls.Add(_btnBack);

            // Forward Button
            _btnForward = new Button
            {
                Text = "→",
                Location = new Point(45, 5),
                Size = new Size(35, 30),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnForward.FlatAppearance.BorderSize = 1;
            _btnForward.FlatAppearance.BorderColor = Color.Gray;
            _btnForward.Click += BtnForward_Click;
            toolbar.Controls.Add(_btnForward);

            // Refresh Button
            _btnWebViewRefresh = new Button
            {
                Text = "⟳",
                Location = new Point(85, 5),
                Size = new Size(35, 30),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnWebViewRefresh.FlatAppearance.BorderSize = 1;
            _btnWebViewRefresh.FlatAppearance.BorderColor = Color.Gray;
            _btnWebViewRefresh.Click += BtnWebViewRefresh_Click;
            toolbar.Controls.Add(_btnWebViewRefresh);

            // Panel untuk button di kanan (dibuat dulu agar address bar bisa anchor dengan benar)
            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 280,
                Height = 40
            };
            toolbar.Controls.Add(rightPanel);

            // Address Bar (URL Input) - akan resize dengan toolbar
            _txtWebViewUrl = new TextBox
            {
                Location = new Point(125, 7),
                Height = 26,
                Text = "https://dxnpos-train.dxn2u.com",
                Font = new Font("Segoe UI", 9F),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _txtWebViewUrl.KeyDown += TxtWebViewUrl_KeyDown;
            toolbar.Controls.Add(_txtWebViewUrl);

            // Zoom In Button (paling kanan)
            _btnZoomIn = new Button
            {
                Text = "+",
                Location = new Point(245, 5),
                Size = new Size(30, 30),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            _btnZoomIn.FlatAppearance.BorderSize = 1;
            _btnZoomIn.FlatAppearance.BorderColor = Color.Gray;
            _btnZoomIn.Click += BtnZoomIn_Click;
            rightPanel.Controls.Add(_btnZoomIn);

            // Zoom Out Button
            _btnZoomOut = new Button
            {
                Text = "-",
                Location = new Point(210, 5),
                Size = new Size(30, 30),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            _btnZoomOut.FlatAppearance.BorderSize = 1;
            _btnZoomOut.FlatAppearance.BorderColor = Color.Gray;
            _btnZoomOut.Click += BtnZoomOut_Click;
            rightPanel.Controls.Add(_btnZoomOut);

            // Zoom Label
            _lblZoom = new Label
            {
                Text = "100%",
                Location = new Point(165, 12),
                Size = new Size(40, 20),
                Font = new Font("Segoe UI", 9F),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter
            };
            rightPanel.Controls.Add(_lblZoom);

            // Fit to Width Button
            _btnFitToWidth = new Button
            {
                Text = "Fit",
                Location = new Point(115, 5),
                Size = new Size(45, 30),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            _btnFitToWidth.FlatAppearance.BorderSize = 0;
            _btnFitToWidth.Click += BtnFitToWidth_Click;
            rightPanel.Controls.Add(_btnFitToWidth);

            // Go Button
            _btnWebViewGo = new Button
            {
                Text = "Go",
                Location = new Point(65, 5),
                Size = new Size(45, 30),
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            _btnWebViewGo.FlatAppearance.BorderSize = 0;
            _btnWebViewGo.Click += BtnWebViewGo_Click;
            rightPanel.Controls.Add(_btnWebViewGo);

            // Label Printer (offset 100px dari atas karena ada header + toolbar)
            _lblPrinter = new Label
            {
                Text = "Printer:",
                Location = new Point(5, 105),
                Size = new Size(80, 20),
                Font = new Font("Segoe UI", 9F)
            };
            _tabPrinter.Controls.Add(_lblPrinter);

            // ComboBox Printer
            _printerComboBox = new ComboBox
            {
                Location = new Point(5, 125),
                Size = new Size(300, 22),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            _printerComboBox.SelectedIndexChanged += PrinterComboBox_SelectedIndexChanged;
            _tabPrinter.Controls.Add(_printerComboBox);

            // Label Custom Text
            _lblCustomText = new Label
            {
                Text = "Teks:",
                Location = new Point(5, 152),
                Size = new Size(80, 20),
                Font = new Font("Segoe UI", 9F)
            };
            _tabPrinter.Controls.Add(_lblCustomText);

            // TextBox Custom Text
            _customTextTextBox = new TextBox
            {
                Location = new Point(5, 172),
                Size = new Size(300, 45),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Courier New", 8F),
                Text = "Masukkan teks yang ingin dicetak di sini..."
            };
            _tabPrinter.Controls.Add(_customTextTextBox);

            // Button Print Test
            _btnPrintTest = new Button
            {
                Text = "Test",
                Location = new Point(5, 224),
                Size = new Size(70, 26),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnPrintTest.FlatAppearance.BorderSize = 0;
            _btnPrintTest.Click += BtnPrintTest_Click;
            _tabPrinter.Controls.Add(_btnPrintTest);

            // Button Print Custom
            _btnPrintCustom = new Button
            {
                Text = "Custom",
                Location = new Point(80, 224),
                Size = new Size(70, 26),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnPrintCustom.FlatAppearance.BorderSize = 0;
            _btnPrintCustom.Click += BtnPrintCustom_Click;
            _tabPrinter.Controls.Add(_btnPrintCustom);

            // Button Open Drawer
            _btnOpenDrawer = new Button
            {
                Text = "Drawer",
                Location = new Point(155, 224),
                Size = new Size(70, 26),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnOpenDrawer.FlatAppearance.BorderSize = 0;
            _btnOpenDrawer.Click += BtnOpenDrawer_Click;
            _tabPrinter.Controls.Add(_btnOpenDrawer);

            // Button Open Drawer Pin 1
            _btnOpenDrawerPin1 = new Button
            {
                Text = "Pin 1",
                Location = new Point(230, 224),
                Size = new Size(70, 26),
                Font = new Font("Segoe UI", 8F),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnOpenDrawerPin1.FlatAppearance.BorderSize = 0;
            _btnOpenDrawerPin1.Click += BtnOpenDrawerPin1_Click;
            _tabPrinter.Controls.Add(_btnOpenDrawerPin1);

            // Button Open Drawer Pin 2
            _btnOpenDrawerPin2 = new Button
            {
                Text = "Pin 2",
                Location = new Point(5, 255),
                Size = new Size(70, 26),
                Font = new Font("Segoe UI", 8F),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnOpenDrawerPin2.FlatAppearance.BorderSize = 0;
            _btnOpenDrawerPin2.Click += BtnOpenDrawerPin2_Click;
            _tabPrinter.Controls.Add(_btnOpenDrawerPin2);

            // GroupBox Server
            _grpServer = new GroupBox
            {
                Text = "Server",
                Location = new Point(5, 288),
                Size = new Size(300, 150),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            _tabPrinter.Controls.Add(_grpServer);

            // Label Port
            _lblServerPort = new Label
            {
                Text = "Port:",
                Location = new Point(5, 22),
                Size = new Size(35, 20),
                Font = new Font("Segoe UI", 8F)
            };
            _grpServer.Controls.Add(_lblServerPort);

            // TextBox Port
            _txtServerPort = new TextBox
            {
                Location = new Point(45, 20),
                Size = new Size(55, 22),
                Text = "7080",
                Font = new Font("Segoe UI", 8F)
            };
            _grpServer.Controls.Add(_txtServerPort);

            // Button Start Server
            _btnServerStart = new Button
            {
                Text = "Start",
                Location = new Point(105, 19),
                Size = new Size(60, 24),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnServerStart.FlatAppearance.BorderSize = 0;
            _btnServerStart.Click += BtnServerStart_Click;
            _grpServer.Controls.Add(_btnServerStart);

            // Button Stop Server
            _btnServerStop = new Button
            {
                Text = "Stop",
                Location = new Point(170, 19),
                Size = new Size(60, 24),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _btnServerStop.FlatAppearance.BorderSize = 0;
            _btnServerStop.Click += BtnServerStop_Click;
            _grpServer.Controls.Add(_btnServerStop);

            // Label Status
            _lblServerStatus = new Label
            {
                Text = "Status: Tidak Aktif",
                Location = new Point(5, 48),
                Size = new Size(290, 20),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Red
            };
            _grpServer.Controls.Add(_lblServerStatus);

            // TextBox Log
            _txtServerLog = new TextBox
            {
                Location = new Point(5, 70),
                Size = new Size(290, 72),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 7F),
                BackColor = Color.Black,
                ForeColor = Color.LimeGreen
            };
            _grpServer.Controls.Add(_txtServerLog);

            // GroupBox Polling
            _grpPolling = new GroupBox
            {
                Text = "Auto Print Polling",
                Location = new Point(5, 445),
                Size = new Size(300, 200),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            _tabPrinter.Controls.Add(_grpPolling);

            // Label Base URL
            Label lblBaseUrl = new Label
            {
                Text = "Base URL:",
                Location = new Point(5, 22),
                Size = new Size(60, 20),
                Font = new Font("Segoe UI", 8F)
            };
            _grpPolling.Controls.Add(lblBaseUrl);

            // TextBox Base URL
            _txtBaseUrl = new TextBox
            {
                Location = new Point(70, 20),
                Size = new Size(220, 22),
                Font = new Font("Segoe UI", 8F),
                Text = "https://dxnpos-train.dxn2u.com"
            };
            _grpPolling.Controls.Add(_txtBaseUrl);

            // Label Company
            Label lblCompany = new Label
            {
                Text = "Company:",
                Location = new Point(5, 48),
                Size = new Size(60, 20),
                Font = new Font("Segoe UI", 8F)
            };
            _grpPolling.Controls.Add(lblCompany);

            // ComboBox Company
            _cmbCompany = new ComboBox
            {
                Location = new Point(70, 46),
                Size = new Size(220, 22),
                Font = new Font("Segoe UI", 8F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbCompany.SelectedIndexChanged += CmbCompany_SelectedIndexChanged;
            _grpPolling.Controls.Add(_cmbCompany);

            // Label User
            Label lblUser = new Label
            {
                Text = "User:",
                Location = new Point(5, 74),
                Size = new Size(60, 20),
                Font = new Font("Segoe UI", 8F)
            };
            _grpPolling.Controls.Add(lblUser);

            // ComboBox User
            _cmbUser = new ComboBox
            {
                Location = new Point(70, 72),
                Size = new Size(220, 22),
                Font = new Font("Segoe UI", 8F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _grpPolling.Controls.Add(_cmbUser);

            // Button Start Polling
            _btnStartPolling = new Button
            {
                Text = "Start Polling",
                Location = new Point(5, 100),
                Size = new Size(140, 30),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnStartPolling.FlatAppearance.BorderSize = 0;
            _btnStartPolling.Click += BtnStartPolling_Click;
            _grpPolling.Controls.Add(_btnStartPolling);

            // Button Stop Polling
            _btnStopPolling = new Button
            {
                Text = "Stop Polling",
                Location = new Point(150, 100),
                Size = new Size(140, 30),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _btnStopPolling.FlatAppearance.BorderSize = 0;
            _btnStopPolling.Click += BtnStopPolling_Click;
            _grpPolling.Controls.Add(_btnStopPolling);

            // Label Status
            _lblPollingStatus = new Label
            {
                Text = "Status: Tidak Aktif",
                Location = new Point(5, 180),
                Size = new Size(290, 20),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Red
            };
            _grpPolling.Controls.Add(_lblPollingStatus);
        }

        private void InitializeWebViewTab()
        {
            // WebView2 - Full size (tanpa toolbar, toolbar ada di tab Kontrol Aplikasi)
            _webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            _tabWebView.Controls.Add(_webView);

            // Initialize WebView2
            InitializeWebView2();
        }

        // Helper function untuk log ke file
        private void LogToFile(string message)
        {
            try
            {
                string logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DXN_POS_Printer",
                    "Logs");
                
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                
                string logFile = Path.Combine(logDir, $"webview2_{DateTime.Now:yyyyMMdd}.log");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\r\n";
                File.AppendAllText(logFile, logEntry);
            }
            catch
            {
                // Ignore logging errors
            }
        }

        private async void InitializeWebView2()
        {
            try
            {
                LogToFile("=== Starting WebView2 Initialization ===");
                
                // Check if WebView2 Runtime is available
                string version = null;
                try
                {
                    version = Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
                    LogToFile($"WebView2 Runtime Version: {version}");
                    System.Diagnostics.Debug.WriteLine($"WebView2 Runtime Version: {version}");
                }
                catch (Exception ex)
                {
                    // If GetAvailableBrowserVersionString throws, WebView2 is not installed
                    LogToFile($"Error getting WebView2 version: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Error getting WebView2 version: {ex.Message}");
                    version = null;
                }
                
                if (string.IsNullOrEmpty(version))
                {
                    string errorMsg = "Microsoft Edge WebView2 Runtime tidak ditemukan.\n\n" +
                        "Aplikasi memerlukan WebView2 Runtime untuk berjalan.\n\n" +
                        "Silakan download dan install dari:\n" +
                        "https://developer.microsoft.com/microsoft-edge/webview2/\n\n" +
                        "Setelah install, restart aplikasi.";
                    
                    LogToFile("ERROR: WebView2 Runtime not found");
                    MessageBox.Show(
                        errorMsg,
                        "WebView2 Runtime Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // Initialize WebView2 dengan default location (biarkan WebView2 pilih sendiri)
                // Ini biasanya lebih reliable daripada specify custom folder
                LogToFile("Initializing WebView2 with default location...");
                LogToFile($"WebView2 Control Handle: {_webView.Handle}");
                LogToFile($"WebView2 Control IsDisposed: {_webView.IsDisposed}");
                LogToFile($"Current Directory: {Directory.GetCurrentDirectory()}");
                LogToFile($"Application Executable Path: {Application.ExecutablePath}");
                
                System.Diagnostics.Debug.WriteLine("Initializing WebView2 with default location...");
                System.Diagnostics.Debug.WriteLine($"WebView2 Control Handle: {_webView.Handle}");
                System.Diagnostics.Debug.WriteLine($"WebView2 Control IsDisposed: {_webView.IsDisposed}");
                
                try
                {
                    LogToFile("Calling EnsureCoreWebView2Async...");
                    await _webView.EnsureCoreWebView2Async(null);
                    LogToFile("EnsureCoreWebView2Async completed");
                    System.Diagnostics.Debug.WriteLine("WebView2 EnsureCoreWebView2Async completed");
                    
                    if (_webView.CoreWebView2 == null)
                    {
                        string errorMsg = "CoreWebView2 is null after EnsureCoreWebView2Async";
                        LogToFile($"ERROR: {errorMsg}");
                        throw new Exception(errorMsg);
                    }
                    
                    LogToFile($"WebView2 CoreWebView2 created: {_webView.CoreWebView2 != null}");
                    LogToFile("WebView2 initialized successfully");
                    System.Diagnostics.Debug.WriteLine($"WebView2 CoreWebView2 created: {_webView.CoreWebView2 != null}");
                    System.Diagnostics.Debug.WriteLine("WebView2 initialized successfully");
                    
                    // Disable zoom control dan set zoom tetap berdasarkan DPI
                    _webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                    _webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                    
                    // Set zoom factor tetap berdasarkan DPI scaling
                    // Jika DPI scale > 1, kita perlu adjust zoom agar konten tidak terlalu kecil
                    double baseZoom = 1.0 / Math.Max(_dpiScaleX, _dpiScaleY);
                    _currentZoomFactor = Math.Max(0.5, Math.Min(baseZoom, 2.0)); // Batasi antara 50% - 200%
                    
                    LogToFile($"DPI Scale X: {_dpiScaleX}, Y: {_dpiScaleY}, Base Zoom: {baseZoom}, Final Zoom: {_currentZoomFactor}");
                }
                catch (Exception initEx)
                {
                    string errorDetails = $"Error in EnsureCoreWebView2Async: {initEx.Message}\r\nStack trace: {initEx.StackTrace}";
                    LogToFile($"ERROR: {errorDetails}");
                    System.Diagnostics.Debug.WriteLine($"Error in EnsureCoreWebView2Async: {initEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {initEx.StackTrace}");
                    throw; // Re-throw untuk ditangani di catch block utama
                }
                
                // Setup WebMessageReceived handler
                _webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;
                
                // Update navigation buttons state
                _webView.CoreWebView2.HistoryChanged += (sender, e) =>
                {
                    UpdateNavigationButtons();
                };
                
                // Handle WebView resize untuk update viewport (dengan zoom tetap)
                _webView.SizeChanged += async (sender, e) =>
                {
                    if (_webView?.CoreWebView2 != null)
                    {
                        // Adjust dimensions based on DPI scaling
                        int adjustedWidth = (int)(_webView.Width / _dpiScaleX);
                        int adjustedHeight = (int)(_webView.Height / _dpiScaleY);
                        
                        // Trigger auto-fit in JavaScript when WebView2 control resizes
                        // Zoom factor tetap, hanya adjust fit
                        _ = _webView.CoreWebView2.ExecuteScriptAsync($@"
                            (function() {{
                                if (window.posPrinterAutoFit) {{
                                    window.posPrinterAutoFit({adjustedWidth}, {adjustedHeight});
                                }}
                                // Pastikan zoom tetap
                                var zoomStyle = document.getElementById(""pos-printer-zoom-fixed"");
                                if (!zoomStyle) {{
                                    zoomStyle = document.createElement(""style"");
                                    zoomStyle.id = ""pos-printer-zoom-fixed"";
                                    document.head.appendChild(zoomStyle);
                                }}
                                var scale = {_currentZoomFactor.ToString(System.Globalization.CultureInfo.InvariantCulture)};
                                var widthPercent = (100 / scale);
                                zoomStyle.textContent = ""html, body {{ transform: scale("" + scale + "") !important; transform-origin: top left !important; width: "" + widthPercent + ""% !important; }} "";
                            }})();
                        ");
                    }
                };
                
                // Inject JavaScript bridge setelah page loaded dan update address bar
                _webView.CoreWebView2.NavigationCompleted += async (sender, e) =>
                {
                    if (e.IsSuccess)
                    {
                        try
                        {
                            // Deteksi login/logout berdasarkan URL
                            string currentUrl = _webView.CoreWebView2.Source ?? "";
                            if (!string.IsNullOrWhiteSpace(currentUrl))
                            {
                                if (currentUrl.Contains("/site/login") || currentUrl.Contains("/site/logout"))
                                {
                                    // User logout atau di login page - hapus session cookie
                                    System.Diagnostics.Debug.WriteLine($"Detected logout or login page: {currentUrl}, clearing session cookie");
                                    
                                    // Hapus session cookie dari memory
                                    _sessionCookie = "";
                                    
                                    // Hapus session cookie dari file
                                    SaveSessionCookie("");
                                    
                                    // Hapus session cookie dari WebView juga
                                    if (_webView?.CoreWebView2?.CookieManager != null)
                                    {
                                        try
                                        {
                                            var cookies = await _webView.CoreWebView2.CookieManager.GetCookiesAsync(_webView.CoreWebView2.Source);
                                            foreach (var cookie in cookies)
                                            {
                                                if (cookie.Name.Equals("PHPSESSID", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    _webView.CoreWebView2.CookieManager.DeleteCookie(cookie);
                                                    System.Diagnostics.Debug.WriteLine("Deleted PHPSESSID cookie from WebView");
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Error deleting cookies: {ex.Message}");
                                        }
                                    }
                                    
                                    // Stop polling jika sedang berjalan
                                    _jobPollerService?.Stop();
                                    
                                    // Update UI
                                    if (_lblPollingStatus != null)
                                    {
                                        _lblPollingStatus.Text = "Status: Logged out";
                                        _lblPollingStatus.ForeColor = Color.Orange;
                                    }
                                    if (_btnStartPolling != null) _btnStartPolling.Enabled = true;
                                    if (_btnStopPolling != null) _btnStopPolling.Enabled = false;
                                    
                                    System.Diagnostics.Debug.WriteLine("Session cookie cleared and polling stopped");
                                }
                                else if (currentUrl.Contains("/site/company"))
                                {
                                    // User sedang memilih / ganti company.
                                    // Cookie biasanya tetap sama, jadi pakai flag untuk force refresh polling
                                    // setelah pindah kembali ke halaman non-company.
                                    _companySwitchDetected = true;
                                    OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] Company page terdeteksi, polling akan di-refresh setelah company dipilih.");
                                }
                                else if (!currentUrl.Contains("/site/login"))
                                {
                                    // User sudah login - ambil cookie PHPSESSID dari WebView
                                    // Cek beberapa kali dengan delay karena cookie mungkin belum tersedia langsung
                                    _ = Task.Run(async () =>
                                    {
                                        // Cek apakah polling sudah berjalan - jika sudah, jangan restart
                                        bool pollingWasRunning = _jobPollerService != null;
                                        
                                        for (int i = 0; i < 5; i++)
                                        {
                                            await Task.Delay(500 * (i + 1)); // Delay: 500ms, 1000ms, 1500ms, 2000ms, 2500ms
                                            if (InvokeRequired)
                                            {
                                                Invoke(new Action(async () => await CheckAndSaveSessionCookie()));
                                            }
                                            else
                                            {
                                                await CheckAndSaveSessionCookie();
                                            }
                                            
                                            // Jika sudah menemukan session cookie, stop checking
                                            if (!string.IsNullOrWhiteSpace(_sessionCookie))
                                            {
                                                OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] Session cookie ditemukan pada attempt ke-{i + 1}");
                                                
                                                // Jika polling sudah berjalan sebelumnya, jangan restart
                                                // Hanya restart jika polling tidak berjalan atau terhenti
                                                if (_companySwitchDetected || !pollingWasRunning || _jobPollerService == null)
                                                {
                                                    if (_companySwitchDetected)
                                                    {
                                                        OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] Company switch terdeteksi, force auto-fetch polling...");
                                                    }
                                                    OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] Polling tidak berjalan, memulai auto-fetch...");
                                                    await Task.Delay(1000); // Delay untuk memastikan cookie tersimpan
                                                    if (InvokeRequired)
                                                    {
                                                        Invoke(new Action(async () => await AutoFetchAndStartPollingAsync()));
                                                    }
                                                    else
                                                    {
                                                        await AutoFetchAndStartPollingAsync();
                                                    }
                                                }
                                                else
                                                {
                                                    OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] Polling sudah berjalan, tidak perlu restart");
                                                }
                                                break;
                                            }
                                        }
                                    });
                                }
                            }

                            // Get WebView dimensions untuk pass ke JavaScript (actual pixel dimensions)
                            int webViewWidth = _webView.Width;
                            int webViewHeight = _webView.Height;
                            
                            // Inject viewport meta tag untuk responsive website
                            string viewportScript = $@"
                                (function() {{
                                    // Get actual container dimensions from WebView
                                    var containerWidth = {webViewWidth};
                                    var containerHeight = {webViewHeight};
                                    
                                    // Inject atau update viewport meta tag - gunakan device-width untuk responsive
                                    var viewportMeta = document.querySelector(""meta[name='viewport']"");
                                    if (!viewportMeta) {{
                                        viewportMeta = document.createElement(""meta"");
                                        viewportMeta.name = ""viewport"";
                                        document.getElementsByTagName(""head"")[0].appendChild(viewportMeta);
                                    }}
                                    // Set viewport untuk responsive - gunakan device-width bukan fixed width
                                    viewportMeta.content = 'width=device-width, initial-scale=1.0, maximum-scale=5.0, user-scalable=yes';
                                    
                                    // Inject CSS minimal untuk memastikan tidak ada overflow horizontal
                                    var style = document.createElement(""style"");
                                    style.textContent = ""html, body {{ margin: 0 !important; padding: 0 !important; overflow-x: hidden !important; }} * {{ box-sizing: border-box !important; }}"";
                                    if (!document.getElementById(""pos-printer-style"")) {{
                                        style.id = ""pos-printer-style"";
                                        document.head.appendChild(style);
                                    }}
                                    
                                    // Auto-fit function - hanya untuk website yang TIDAK responsive
                                    function autoFit(providedWidth, providedHeight) {{
                                        // Use provided dimensions or fallback to window size
                                        var containerWidth = providedWidth || (window.innerWidth || document.documentElement.clientWidth);
                                        var containerHeight = providedHeight || (window.innerHeight || document.documentElement.clientHeight);
                                        
                                        if (containerWidth <= 0 || containerHeight <= 0) {{
                                            return;
                                        }}
                                        
                                        // Cek apakah website sudah responsive
                                        // Jika scrollWidth <= containerWidth + 50px (toleransi), berarti sudah responsive
                                        var body = document.body;
                                        var html = document.documentElement;
                                        void body.offsetWidth; // Force reflow
                                        
                                        var contentWidth = Math.max(
                                            body.scrollWidth || 0,
                                            html.scrollWidth || 0
                                        );
                                        
                                        // Jika website sudah responsive (tidak ada horizontal overflow), jangan lakukan scaling
                                        if (contentWidth <= containerWidth + 50) {{
                                            // Remove any existing transform
                                            var zoomStyle = document.getElementById(""pos-printer-zoom-auto"");
                                            if (zoomStyle) {{
                                                zoomStyle.textContent = """";
                                            }}
                                            return;
                                        }}
                                        
                                        // Hanya lakukan scaling jika website TIDAK responsive (ada overflow)
                                        // Remove all existing transforms first
                                        var zoomStyle = document.getElementById(""pos-printer-zoom-auto"");
                                        if (zoomStyle) {{
                                            zoomStyle.textContent = """";
                                        }}
                                        
                                        // If content width is 0, try again later
                                        if (contentWidth <= 0 || containerWidth <= 0) {{
                                            return;
                                        }}
                                        
                                        // Calculate scale - hanya scale down jika content lebih besar dari container
                                        var scale = containerWidth / contentWidth;
                                        
                                        // Jika content lebih besar dari container, scale down
                                        if (contentWidth > containerWidth) {{
                                            // Scale down untuk fit
                                            scale = Math.max(0.25, Math.min(scale, 1.0));
                                        }} else {{
                                            // Tidak perlu scale, tetap 1.0
                                            scale = 1.0;
                                        }}
                                        
                                        if (!zoomStyle) {{
                                            zoomStyle = document.createElement(""style"");
                                            zoomStyle.id = ""pos-printer-zoom-auto"";
                                            document.head.appendChild(zoomStyle);
                                        }}
                                        
                                        var widthPercent = (100 / scale);
                                        var heightPercent = (containerHeight / scale);
                                        
                                        // Apply transform dengan !important untuk override semua style
                                        var cssText = ""html, body {{ transform: scale("" + scale + "") !important; transform-origin: top left !important; width: "" + widthPercent + ""% !important; }} html {{ height: "" + heightPercent + ""px !important; overflow-x: hidden !important; }} body {{ height: auto !important; overflow-x: hidden !important; }}"";
                                        zoomStyle.textContent = cssText;
                                        
                                        // Update document dimensions
                                        document.documentElement.style.width = widthPercent + ""%"";
                                        document.documentElement.style.height = heightPercent + ""px"";
                                        document.body.style.width = widthPercent + ""%"";
                                        document.body.style.height = ""auto"";
                                    }}
                                    
                                    // Expose autoFit function globally
                                    window.posPrinterAutoFit = autoFit;
                                    
                                    // Helper function untuk auto-fit dengan ukuran window
                                    function autoFitWithSize() {{
                                        autoFit();
                                    }}
                                    
                                    // Try multiple times dengan ukuran WebView yang sebenarnya
                                    var webViewWidth = {webViewWidth};
                                    var webViewHeight = {webViewHeight};
                                    
                                    // Jangan update viewport meta tag lagi - biarkan device-width untuk responsive
                                    
                                    // Auto-fit dengan delay untuk memastikan website sudah fully loaded
                                    setTimeout(function() {{ autoFit(webViewWidth, webViewHeight); }}, 100);
                                    setTimeout(function() {{ autoFit(webViewWidth, webViewHeight); }}, 500);
                                    setTimeout(function() {{ autoFit(webViewWidth, webViewHeight); }}, 1000);
                                    setTimeout(function() {{ autoFit(webViewWidth, webViewHeight); }}, 2000);
                                    setTimeout(function() {{ autoFit(webViewWidth, webViewHeight); }}, 4000);
                                    
                                    // Auto-fit saat window resize dengan debounce
                                    var resizeTimeout;
                                    window.addEventListener('resize', function() {{
                                        clearTimeout(resizeTimeout);
                                        var newWidth = window.innerWidth || document.documentElement.clientWidth;
                                        var newHeight = window.innerHeight || document.documentElement.clientHeight;
                                        // Jangan update viewport meta tag - biarkan device-width untuk responsive
                                        resizeTimeout = setTimeout(function() {{
                                            autoFit(newWidth, newHeight);
                                        }}, 300);
                                    }});
                                }})();
                            ";
                            await _webView.CoreWebView2.ExecuteScriptAsync(viewportScript);
                            
                            // Set zoom tetap setelah script injection (tidak berubah-ubah)
                            await SetFixedZoomLevel(_currentZoomFactor);
                            
                            // Auto-fit setelah delay untuk memastikan konten sudah loaded
                            await Task.Delay(500);
                            if (_webView?.CoreWebView2 != null)
                            {
                                int adjustedWidth = (int)(_webView.Width / _dpiScaleX);
                                int adjustedHeight = (int)(_webView.Height / _dpiScaleY);
                                await _webView.CoreWebView2.ExecuteScriptAsync($"if (window.posPrinterAutoFit) window.posPrinterAutoFit({adjustedWidth}, {adjustedHeight});");
                            }
                            
                            // Inject JavaScript bridge dengan intercept window.print()
                            string script = @"
                                (function() {
                                    if (window.posPrinter) return;
                                    
                                    // Helper function untuk extract text dari HTML element
                                    function extractTextFromElement(element) {
                                        if (!element) return '';
                                        
                                        var text = '';
                                        var nodes = element.childNodes;
                                        
                                        for (var i = 0; i < nodes.length; i++) {
                                            var node = nodes[i];
                                            
                                            if (node.nodeType === 3) { // Text node
                                                text += node.textContent.trim() + '\n';
                                            } else if (node.nodeType === 1) { // Element node
                                                var tagName = node.tagName ? node.tagName.toLowerCase() : '';
                                                
                                                // Handle table rows
                                                if (tagName === 'tr') {
                                                    var cells = node.querySelectorAll('td, th');
                                                    var rowText = '';
                                                    for (var j = 0; j < cells.length; j++) {
                                                        var cellText = cells[j].textContent.trim();
                                                        if (cellText) {
                                                            rowText += cellText + '  ';
                                                        }
                                                    }
                                                    if (rowText) {
                                                        text += rowText.trim() + '\n';
                                                    }
                                                } else {
                                                    // Recursive untuk nested elements
                                                    var childText = extractTextFromElement(node);
                                                    if (childText) {
                                                        text += childText;
                                                    }
                                                }
                                            }
                                        }
                                        
                                        return text;
                                    }
                                    
                                    // Helper function untuk format text untuk POS printer
                                    function formatForPosPrinter(text) {
                                        if (!text) return '';
                                        
                                        // Clean up multiple newlines
                                        text = text.replace(/\n{3,}/g, '\n\n');
                                        
                                        // Remove leading/trailing whitespace
                                        text = text.trim();
                                        
                                        return text;
                                    }
                                    
                                    // Intercept window.print() untuk redirect ke POS printer
                                    var originalPrint = window.print;
                                    window.print = function() {
                                        // Cek apakah ada PrintContent element
                                        var printContent = document.getElementById('PrintContent');
                                        
                                        if (printContent && window.posPrinter) {
                                            // Extract text dari PrintContent
                                            var text = extractTextFromElement(printContent);
                                            text = formatForPosPrinter(text);
                                            
                                            if (text) {
                                                // Print ke POS printer
                                                window.posPrinter.print(text, true);
                                                console.log('Print ke POS printer via intercept');
                                                
                                                // Cek apakah ada atribut data-auto-cash-drawer untuk auto-open cash drawer
                                                var autoCashDrawer = printContent.getAttribute('data-auto-cash-drawer');
                                                if (autoCashDrawer !== null && autoCashDrawer !== 'false') {
                                                    // Parse pin dari atribut (default: 2)
                                                    var pin = parseInt(autoCashDrawer) || 2;
                                                    setTimeout(function() {
                                                        window.posPrinter.openCashDrawer(pin);
                                                    }, 500); // Delay 500ms untuk memastikan print selesai
                                                }
                                                
                                                return;
                                            }
                                        }
                                        
                                        // Fallback ke original print jika tidak ada PrintContent atau posPrinter
                                        if (originalPrint) {
                                            originalPrint.call(window);
                                        }
                                    };
                                    
                                    window.posPrinter = {
                                        print: function(content, cutPaper) {
                                            if (typeof content !== ""string"") {{
                                                console.error(""POS Printer: content must be a string"");
                                                return;
                                            }}
                                            window.chrome.webview.postMessage(JSON.stringify({{
                                                type: ""print"",
                                                content: content,
                                                cutPaper: cutPaper !== false
                                            }}));
                                        }},
                                        openCashDrawer: function(pin) {{
                                            window.chrome.webview.postMessage(JSON.stringify({{
                                                type: ""cashDrawer"",
                                                pin: pin || null
                                            }}));
                                        }},
                                        // Helper function untuk extract dan print dari element
                                        printFromElement: function(selector, cutPaper) {{
                                            var element = typeof selector === 'string' 
                                                ? document.querySelector(selector) 
                                                : selector;
                                            
                                            if (!element) {{
                                                console.error('POS Printer: Element not found');
                                                return;
                                            }}
                                            
                                            var text = extractTextFromElement(element);
                                            text = formatForPosPrinter(text);
                                            
                                            if (text) {{
                                                window.posPrinter.print(text, cutPaper !== false);
                                            }} else {{
                                                console.error('POS Printer: No text content found');
                                            }}
                                        }}
                                    }};
                                    console.log(""POS Printer bridge initialized with window.print() intercept"");
                                })();
                            ";
                            await _webView.CoreWebView2.ExecuteScriptAsync(script);
                        }
                        catch (Exception ex)
                        {
                            // Ignore script injection errors
                            System.Diagnostics.Debug.WriteLine($"Script injection error: {ex.Message}");
                        }
                    }
                };
                
                // Load default URL
                _webView.CoreWebView2.Navigate(_txtWebViewUrl.Text);
            }
            catch (System.UnauthorizedAccessException ex)
            {
                string errorDetails = $"Access Denied Error: {ex.Message}\r\nStack trace: {ex.StackTrace}";
                LogToFile($"ERROR: {errorDetails}");
                
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DXN_POS_Printer",
                    "Logs",
                    $"webview2_{DateTime.Now:yyyyMMdd}.log");
                
                MessageBox.Show(
                    $"Access Denied Error: {ex.Message}\n\n" +
                    "Kemungkinan penyebab:\n" +
                    "1. WebView2 Runtime tidak terinstall atau tidak dapat diakses\n" +
                    "2. Permission tidak cukup (coba jalankan sebagai Administrator)\n" +
                    "3. Antivirus atau security software memblokir akses\n\n" +
                    "Solusi:\n" +
                    "1. Install WebView2 Runtime dari:\n" +
                    "   https://developer.microsoft.com/microsoft-edge/webview2/\n" +
                    "2. Restart aplikasi sebagai Administrator\n" +
                    "3. Cek antivirus/security software settings\n" +
                    "4. Restart komputer setelah install WebView2 Runtime\n\n" +
                    $"Log file: {logPath}",
                    "WebView2 Access Denied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                string errorDetails = $"Error initializing WebView2: {ex.Message}\r\nStack trace: {ex.StackTrace}";
                LogToFile($"ERROR: {errorDetails}");
                
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DXN_POS_Printer",
                    "Logs",
                    $"webview2_{DateTime.Now:yyyyMMdd}.log");
                
                string errorMessage = $"Error initializing WebView2: {ex.Message}\n\n";
                
                // Check specific error types
                if (ex.Message.Contains("access", StringComparison.OrdinalIgnoreCase) || 
                    ex.Message.Contains("denied", StringComparison.OrdinalIgnoreCase) || 
                    ex.Message.Contains("permission", StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage += "Access Denied Error:\n";
                    errorMessage += "1. Pastikan WebView2 Runtime sudah terinstall\n";
                    errorMessage += "2. Coba jalankan aplikasi sebagai Administrator\n";
                    errorMessage += "3. Cek antivirus/security software settings\n";
                }
                else
                {
                    errorMessage += "Pastikan WebView2 Runtime sudah terinstall.\n";
                }
                
                errorMessage += "\nDownload WebView2 Runtime dari:\n";
                errorMessage += "https://developer.microsoft.com/microsoft-edge/webview2/\n\n";
                errorMessage += $"Log file: {logPath}";
                
                MessageBox.Show(
                    errorMessage,
                    "WebView2 Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string message = e.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(message))
                    return;
                    
                var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(message);
                
                string type = json.GetProperty("type").GetString();
                
                if (type == "print")
                {
                    string content = json.GetProperty("content").GetString();
                    bool cutPaper = json.TryGetProperty("cutPaper", out var cutPaperProp) ? cutPaperProp.GetBoolean() : true;
                    
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => {
                            bool success = _printerService.PrintCustomText(content, cutPaper);
                            SendWebViewResponse("print", success, success ? "Print berhasil" : "Print gagal");
                        }));
                    }
                    else
                    {
                        bool success = _printerService.PrintCustomText(content, cutPaper);
                        SendWebViewResponse("print", success, success ? "Print berhasil" : "Print gagal");
                    }
                }
                else if (type == "cashDrawer")
                {
                    int? pin = null;
                    if (json.TryGetProperty("pin", out var pinProp))
                    {
                        if (pinProp.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            pin = pinProp.GetInt32();
                        }
                    }
                    
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => {
                            bool success = false;
                            if (pin == 1)
                                success = _cashDrawerService.OpenCashDrawerPin1(showMessage: false);
                            else if (pin == 2)
                                success = _cashDrawerService.OpenCashDrawerPin2(showMessage: false);
                            else
                                success = _cashDrawerService.OpenCashDrawer(showMessage: false);
                            
                            SendWebViewResponse("cashDrawer", success, success ? "Cash drawer berhasil dibuka" : "Cash drawer gagal dibuka");
                        }));
                    }
                    else
                    {
                        bool success = false;
                        if (pin == 1)
                            success = _cashDrawerService.OpenCashDrawerPin1(showMessage: false);
                        else if (pin == 2)
                            success = _cashDrawerService.OpenCashDrawerPin2(showMessage: false);
                        else
                            success = _cashDrawerService.OpenCashDrawer(showMessage: false);
                        
                        SendWebViewResponse("cashDrawer", success, success ? "Cash drawer berhasil dibuka" : "Cash drawer gagal dibuka");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing WebView message: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void SendWebViewResponse(string type, bool success, string message)
        {
            try
            {
                if (_webView?.CoreWebView2 != null)
                {
                    string script = $@"
                        if (window.onPosPrinterResponse) {{
                            window.onPosPrinterResponse({{
                                type: '{type}',
                                success: {success.ToString().ToLower()},
                                message: '{message}'
                            }});
                        }}
                    ";
                    await _webView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                // Ignore errors in response
            }
        }

        private void BtnWebViewGo_Click(object sender, EventArgs e)
        {
            try
            {
                string url = _txtWebViewUrl.Text.Trim();
                if (string.IsNullOrEmpty(url))
                {
                    MessageBox.Show("Masukkan URL yang valid", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                    _txtWebViewUrl.Text = url;
                }

                _webView?.CoreWebView2?.Navigate(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading URL: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            try
            {
                if (_webView?.CoreWebView2?.CanGoBack == true)
                {
                    _webView.CoreWebView2.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error going back: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnForward_Click(object sender, EventArgs e)
        {
            try
            {
                if (_webView?.CoreWebView2?.CanGoForward == true)
                {
                    _webView.CoreWebView2.GoForward();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error going forward: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateNavigationButtons()
        {
            if (_btnBack != null && _btnForward != null && _webView?.CoreWebView2 != null)
            {
                _btnBack.Enabled = _webView.CoreWebView2.CanGoBack;
                _btnForward.Enabled = _webView.CoreWebView2.CanGoForward;
            }
        }

        private void TxtWebViewUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                BtnWebViewGo_Click(sender, e);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void BtnWebViewRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                _webView?.CoreWebView2?.Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnZoomIn_Click(object sender, EventArgs e)
        {
            // Zoom manual dinonaktifkan - zoom tetap berdasarkan ukuran layar
            // Method ini tetap ada untuk kompatibilitas tapi tidak mengubah zoom
            try
            {
                if (_webView?.CoreWebView2 != null)
                {
                    // Kembalikan ke zoom tetap
                    await SetFixedZoomLevel(_currentZoomFactor);
                    UpdateZoomLabel();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in zoom in: {ex.Message}");
            }
        }

        private async void BtnZoomOut_Click(object sender, EventArgs e)
        {
            // Zoom manual dinonaktifkan - zoom tetap berdasarkan ukuran layar
            // Method ini tetap ada untuk kompatibilitas tapi tidak mengubah zoom
            try
            {
                if (_webView?.CoreWebView2 != null)
                {
                    // Kembalikan ke zoom tetap
                    await SetFixedZoomLevel(_currentZoomFactor);
                    UpdateZoomLabel();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in zoom out: {ex.Message}");
            }
        }

        private async void BtnZoomReset_Click(object sender, EventArgs e)
        {
            // Reset ke zoom tetap berdasarkan DPI
            try
            {
                if (_webView?.CoreWebView2 != null)
                {
                    // Kembalikan ke zoom tetap berdasarkan DPI
                    double baseZoom = 1.0 / Math.Max(_dpiScaleX, _dpiScaleY);
                    _currentZoomFactor = Math.Max(0.5, Math.Min(baseZoom, 2.0));
                    await SetFixedZoomLevel(_currentZoomFactor);
                    UpdateZoomLabel();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reset zoom: {ex.Message}");
            }
        }

        private async Task SetZoomLevel(double zoomFactor)
        {
            try
            {
                if (_webView?.CoreWebView2 != null)
                {
                    // Gunakan style dengan ID khusus untuk zoom tetap
                    string script = $@"
                        (function() {{
                            // Hapus zoom style yang lama (jika ada)
                            var oldZoomStyle = document.getElementById(""pos-printer-zoom-auto"");
                            if (oldZoomStyle) {{
                                oldZoomStyle.remove();
                            }}
                            
                            // Buat atau update zoom style tetap
                            var zoomStyle = document.getElementById(""pos-printer-zoom-fixed"");
                            if (!zoomStyle) {{
                                zoomStyle = document.createElement(""style"");
                                zoomStyle.id = ""pos-printer-zoom-fixed"";
                                document.head.appendChild(zoomStyle);
                            }}
                            
                            var scale = {zoomFactor.ToString(System.Globalization.CultureInfo.InvariantCulture)};
                            var widthPercent = (100 / scale);
                            
                            // Apply zoom dengan !important untuk mencegah perubahan
                            zoomStyle.textContent = ""html, body {{ transform: scale("" + scale + "") !important; transform-origin: top left !important; width: "" + widthPercent + ""% !important; }} "";
                            
                            // Update viewport height
                            var viewportHeight = (window.innerHeight || document.documentElement.clientHeight) / scale;
                            document.documentElement.style.height = viewportHeight + ""px"";
                            document.body.style.height = ""auto"";
                        }})();
                    ";
                    await _webView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting zoom: {ex.Message}");
            }
        }

        /// <summary>
        /// Set zoom level yang tetap (tidak berubah-ubah) berdasarkan ukuran layar dan DPI
        /// </summary>
        private async Task SetFixedZoomLevel(double zoomFactor)
        {
            try
            {
                if (_webView?.CoreWebView2 != null)
                {
                    // Gunakan style dengan ID khusus untuk zoom tetap
                    string script = $@"
                        (function() {{
                            // Hapus zoom style yang lama (jika ada)
                            var oldZoomStyle = document.getElementById(""pos-printer-zoom-auto"");
                            if (oldZoomStyle) {{
                                oldZoomStyle.remove();
                            }}
                            
                            // Buat atau update zoom style tetap
                            var zoomStyle = document.getElementById(""pos-printer-zoom-fixed"");
                            if (!zoomStyle) {{
                                zoomStyle = document.createElement(""style"");
                                zoomStyle.id = ""pos-printer-zoom-fixed"";
                                document.head.appendChild(zoomStyle);
                            }}
                            
                            var scale = {zoomFactor.ToString(System.Globalization.CultureInfo.InvariantCulture)};
                            var widthPercent = (100 / scale);
                            
                            // Apply zoom dengan !important untuk mencegah perubahan
                            zoomStyle.textContent = ""html, body {{ transform: scale("" + scale + "") !important; transform-origin: top left !important; width: "" + widthPercent + ""% !important; }} "";
                            
                            // Update viewport height
                            var viewportHeight = (window.innerHeight || document.documentElement.clientHeight) / scale;
                            document.documentElement.style.height = viewportHeight + ""px"";
                            document.body.style.height = ""auto"";
                        }})();
                    ";
                    await _webView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting fixed zoom: {ex.Message}");
            }
        }

        private void UpdateZoomLabel()
        {
            if (_lblZoom != null)
            {
                _lblZoom.Text = $"{(int)(_currentZoomFactor * 100)}%";
            }
        }

        private async void BtnFitToWidth_Click(object sender, EventArgs e)
        {
            try
            {
                if (_webView?.CoreWebView2 != null)
                {
                    // Adjust dimensions based on DPI scaling
                    int adjustedWidth = (int)(_webView.Width / _dpiScaleX);
                    int adjustedHeight = (int)(_webView.Height / _dpiScaleY);
                    
                    // Trigger auto-fit JavaScript dengan adjusted WebView dimensions
                    // Zoom tetap, hanya adjust fit
                    string fitScript = $@"
                        (function() {{
                            var autoFit = window.posPrinterAutoFit;
                            if (autoFit) {{
                                autoFit({adjustedWidth}, {adjustedHeight});
                            }}
                            
                            // Pastikan zoom tetap tidak berubah
                            var zoomStyle = document.getElementById(""pos-printer-zoom-fixed"");
                            if (zoomStyle) {{
                                var scale = {_currentZoomFactor.ToString(System.Globalization.CultureInfo.InvariantCulture)};
                                var widthPercent = (100 / scale);
                                zoomStyle.textContent = ""html, body {{ transform: scale("" + scale + "") !important; transform-origin: top left !important; width: "" + widthPercent + ""% !important; }} "";
                            }}
                            
                            return {_currentZoomFactor.ToString(System.Globalization.CultureInfo.InvariantCulture)};
                        }})();
                    ";
                    
                    string scaleResult = await _webView.CoreWebView2.ExecuteScriptAsync(fitScript);
                    // Remove quotes and parse
                    scaleResult = scaleResult.Trim('"');
                    if (double.TryParse(scaleResult, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double scale))
                    {
                        // Zoom tetap, tidak update _currentZoomFactor
                        UpdateZoomLabel();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fit to width: {ex.Message}");
            }
        }

        private void InitializeServices()
        {
            _printerService = new PosPrinterService();
            _cashDrawerService = new CashDrawerService();
            _httpServerService = new HttpServerService(_printerService, _cashDrawerService);
            _httpServerService.SetLogCallback(OnServerLog);
        }

        private void OnServerLog(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(OnServerLog), message);
                return;
            }

            _txtServerLog.AppendText(message + Environment.NewLine);
            _txtServerLog.SelectionStart = _txtServerLog.Text.Length;
            _txtServerLog.ScrollToCaret();
        }

        /// <summary>
        /// Extract error message dari HTML atau JSON response, filter HTML tags
        /// </summary>
        private string ExtractErrorMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return "";

            // Jika response adalah JSON, coba parse
            if (body.TrimStart().StartsWith("{") || body.TrimStart().StartsWith("["))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("message", out var msgEl))
                    {
                        return msgEl.GetString() ?? "";
                    }
                    if (doc.RootElement.TryGetProperty("error", out var errEl))
                    {
                        return errEl.GetString() ?? "";
                    }
                }
                catch { /* Bukan JSON valid, lanjut ke HTML parsing */ }
            }

            // Jika response adalah HTML, extract pesan error
            if (body.Contains("<html") || body.Contains("<!DOCTYPE"))
            {
                // Extract dari tag <h2>, <h3>, atau <title>
                var patterns = new[]
                {
                    @"<h2[^>]*>(.*?)</h2>",
                    @"<h3[^>]*>(.*?)</h3>",
                    @"<title[^>]*>(.*?)</title>",
                    @"Unauthorized",
                    @"the page you requested was protected"
                };

                foreach (var pattern in patterns)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(body, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
                    if (match.Success)
                    {
                        string extracted = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                        // Remove HTML tags
                        extracted = System.Text.RegularExpressions.Regex.Replace(extracted, "<[^>]+>", "").Trim();
                        if (!string.IsNullOrWhiteSpace(extracted))
                        {
                            return extracted;
                        }
                    }
                }

                // Fallback: cari status code di HTML
                var statusMatch = System.Text.RegularExpressions.Regex.Match(body, @"(\d{3})");
                if (statusMatch.Success)
                {
                    return $"HTTP {statusMatch.Value}";
                }

                return "Error (HTML response)";
            }

            // Jika bukan HTML atau JSON, return as-is (tapi limit length)
            return body.Length > 200 ? body.Substring(0, 200) + "..." : body;
        }

        private void LoadPrinters()
        {
            try
            {
                var printers = PrinterSettings.InstalledPrinters.Cast<string>().ToList();
                _printerComboBox.Items.Clear();
                
                foreach (var printer in printers)
                {
                    _printerComboBox.Items.Add(printer);
                }

                if (_printerComboBox.Items.Count > 0)
                {
                    // Set default printer
                    var defaultPrinter = _printerService.GetDefaultPrinter();
                    int index = _printerComboBox.Items.IndexOf(defaultPrinter);
                    if (index >= 0)
                    {
                        _printerComboBox.SelectedIndex = index;
                    }
                    else
                    {
                        _printerComboBox.SelectedIndex = 0;
                    }
                    
                    // Auto-set printer ke service saat load
                    if (_printerComboBox.SelectedItem != null)
                    {
                        string selectedPrinter = _printerComboBox.SelectedItem.ToString();
                        _printerService.SetPrinter(selectedPrinter);
                        _cashDrawerService.SetPrinter(selectedPrinter);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error memuat daftar printer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void MainForm_Shown(object sender, EventArgs e)
        {
            // Auto-fit WebView setelah form fully loaded
            try
            {
                // Delay sedikit untuk memastikan WebView sudah ready
                Task.Delay(500).ContinueWith(_ =>
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => AutoFitWebView()));
                    }
                    else
                    {
                        AutoFitWebView();
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error auto-fit: {ex.Message}");
            }

            // Auto-start HTTP server saat aplikasi dibuka (default port: 7080)
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(300); // beri waktu UI selesai render
                    if (IsDisposed) return;

                    if (InvokeRequired)
                    {
                        Invoke(new Action(async () => await AutoStartHttpServerAsync()));
                    }
                    else
                    {
                        await AutoStartHttpServerAsync();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error auto-start server: {ex.Message}");
                }
            });

            // Load session cookie dari file jika ada (setelah controls dibuat)
            // Tidak perlu API key manual, cukup session cookie dari login
            _sessionCookie = LoadSessionCookie();
            if (!string.IsNullOrWhiteSpace(_sessionCookie))
            {
                System.Diagnostics.Debug.WriteLine($"Loaded session cookie from file: {_sessionCookie.Substring(0, Math.Min(20, _sessionCookie.Length))}...");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No session cookie found. User needs to login via WebView first.");
            }

            // Auto-fetch settings dan start polling jika API key ada
            System.Diagnostics.Debug.WriteLine("MainForm_Shown: Scheduling auto-fetch task...");
            _ = Task.Run(async () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("Auto-fetch task: Waiting 1500ms for UI to render...");
                    await Task.Delay(1500); // Beri waktu UI selesai render (diperpanjang)
                    if (IsDisposed)
                    {
                        System.Diagnostics.Debug.WriteLine("Auto-fetch task: Form is disposed, aborting");
                        return;
                    }

                    System.Diagnostics.Debug.WriteLine("Auto-fetch task: Starting after delay...");
                    System.Diagnostics.Debug.WriteLine($"Auto-fetch task: InvokeRequired={InvokeRequired}, IsDisposed={IsDisposed}");
                    
                    // Pastikan kita di UI thread untuk akses controls
                    if (InvokeRequired)
                    {
                        System.Diagnostics.Debug.WriteLine("Auto-fetch: InvokeRequired=true, using BeginInvoke");
                        // Gunakan BeginInvoke untuk async operation
                        BeginInvoke(new Action(async () =>
                        {
                            try
                            {
                                System.Diagnostics.Debug.WriteLine("Auto-fetch: Executing on UI thread via BeginInvoke");
                                await AutoFetchAndStartPollingAsync();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error in AutoFetchAndStartPollingAsync: {ex.Message}\n{ex.StackTrace}");
                            }
                        }));
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Auto-fetch: Already on UI thread, calling directly");
                        await AutoFetchAndStartPollingAsync();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error auto-fetch polling: {ex.Message}\n{ex.StackTrace}");
                }
            });
            System.Diagnostics.Debug.WriteLine("MainForm_Shown: Auto-fetch task scheduled");
        }

        private static bool IsTcpPortAvailable(int port)
        {
            try
            {
                var listener = new TcpListener(System.Net.IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task AutoStartHttpServerAsync()
        {
            if (_httpServerService == null || _httpServerService.IsRunning)
                return;

            int port = 7080;
            if (_txtServerPort != null && int.TryParse(_txtServerPort.Text, out int parsed) && parsed > 0 && parsed < 65536)
            {
                port = parsed;
            }
            else if (_txtServerPort != null)
            {
                _txtServerPort.Text = "7080";
            }

            if (!IsTcpPortAvailable(port))
            {
                _lblServerStatus.Text = $"Status: Gagal auto-start (port {port} sudah digunakan)";
                _lblServerStatus.ForeColor = Color.OrangeRed;
                OnServerLog($"[{DateTime.Now:HH:mm:ss}] Auto-start dibatalkan: port {port} sudah digunakan");
                return;
            }

            try
            {
                _httpServerService.Port = port;
                await _httpServerService.StartAsync();

                _btnServerStart.Enabled = false;
                _btnServerStop.Enabled = true;
                _txtServerPort.Enabled = false;
                _lblServerStatus.Text = $"Status: Aktif di http://localhost:{port}/";
                _lblServerStatus.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                _lblServerStatus.Text = $"Status: Gagal auto-start ({ex.Message})";
                _lblServerStatus.ForeColor = Color.OrangeRed;
                OnServerLog($"[{DateTime.Now:HH:mm:ss}] Error auto-start server: {ex.Message}");
            }
        }
        
        private async void AutoFitWebView()
        {
            try
            {
                // Auto-fit to width jika WebView sudah loaded
                if (_webView?.CoreWebView2 != null)
                {
                    // Set zoom tetap berdasarkan DPI (tidak reset ke 1.0)
                    await SetFixedZoomLevel(_currentZoomFactor);
                    UpdateZoomLabel();
                    
                    // Fit to width setelah delay untuk memastikan website sudah loaded
                    await Task.Delay(1500);
                    
                    if (_webView?.CoreWebView2 != null)
                    {
                        BtnFitToWidth_Click(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error auto-fit WebView: {ex.Message}");
            }
        }

        private void PrinterComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_printerComboBox.SelectedItem != null)
            {
                string selectedPrinter = _printerComboBox.SelectedItem.ToString();
                _printerService.SetPrinter(selectedPrinter);
                _cashDrawerService.SetPrinter(selectedPrinter);
            }
        }

        private void BtnPrintTest_Click(object sender, EventArgs e)
        {
            try
            {
                bool success = _printerService.PrintTestReceipt();
                if (success)
                {
                    MessageBox.Show("Test receipt berhasil dicetak!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPrintCustom_Click(object sender, EventArgs e)
        {
            try
            {
                string text = _customTextTextBox.Text;
                if (string.IsNullOrWhiteSpace(text))
                {
                    MessageBox.Show("Masukkan teks yang ingin dicetak!", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bool success = _printerService.PrintCustomText(text);
                if (success)
                {
                    MessageBox.Show("Teks berhasil dicetak!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnOpenDrawer_Click(object sender, EventArgs e)
        {
            try
            {
                _cashDrawerService.OpenCashDrawer();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnOpenDrawerPin1_Click(object sender, EventArgs e)
        {
            try
            {
                bool success = _cashDrawerService.OpenCashDrawerPin1(showMessage: true);
                if (success)
                {
                    MessageBox.Show("Cash drawer (Pin 1) berhasil dibuka!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnOpenDrawerPin2_Click(object sender, EventArgs e)
        {
            try
            {
                bool success = _cashDrawerService.OpenCashDrawerPin2(showMessage: true);
                if (success)
                {
                    MessageBox.Show("Cash drawer (Pin 2) berhasil dibuka!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnServerStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (int.TryParse(_txtServerPort.Text, out int port) && port > 0 && port < 65536)
                {
                    _httpServerService.Port = port;
                    await _httpServerService.StartAsync();
                    
                    _btnServerStart.Enabled = false;
                    _btnServerStop.Enabled = true;
                    _txtServerPort.Enabled = false;
                    _lblServerStatus.Text = $"Status: Aktif di http://localhost:{port}/";
                    _lblServerStatus.ForeColor = Color.Green;
                }
                else
                {
                    MessageBox.Show("Port harus berupa angka antara 1-65535", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting server: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnServerStop_Click(object sender, EventArgs e)
        {
            try
            {
                _httpServerService.Stop();
                
                _btnServerStart.Enabled = true;
                _btnServerStop.Enabled = false;
                _txtServerPort.Enabled = true;
                _lblServerStatus.Text = "Status: Tidak Aktif";
                _lblServerStatus.ForeColor = Color.Red;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping server: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_httpServerService.IsRunning)
            {
                _httpServerService.Stop();
            }
            _jobPollerService?.Stop();
            _jobPollerService?.Dispose();

            // Clear dropdown company dan user saat aplikasi ditutup
            if (_cmbCompany != null)
            {
                _cmbCompany.Items.Clear();
                _cmbCompany.SelectedItem = null;
            }
            if (_cmbUser != null)
            {
                _cmbUser.Items.Clear();
                _cmbUser.SelectedItem = null;
            }
            _companyUsers.Clear();
        }

        // Method BtnFetchSettings_Click removed - Fetch is now automatic after login

        private void UpdateUserDropdown()
        {
            if (_cmbCompany.SelectedItem == null) return;

            string selectedCompany = _cmbCompany.SelectedItem.ToString() ?? "";
            _cmbUser.Items.Clear();

            if (_companyUsers.ContainsKey(selectedCompany))
            {
                foreach (var userId in _companyUsers[selectedCompany])
                {
                    _cmbUser.Items.Add(userId);
                }
                if (_cmbUser.Items.Count > 0)
                {
                    _cmbUser.SelectedIndex = 0;
                    System.Diagnostics.Debug.WriteLine($"UpdateUserDropdown: Selected user={_cmbUser.SelectedItem}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateUserDropdown: No users found for company={selectedCompany}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"UpdateUserDropdown: Company {selectedCompany} not found in _companyUsers");
            }
        }

        private void CmbCompany_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateUserDropdown();
        }

        private void BtnStartPolling_Click(object sender, EventArgs e)
        {
            string sessionCookie = _sessionCookie ?? LoadSessionCookie() ?? "";
            string baseUrl = _txtBaseUrl.Text.Trim();

            if (string.IsNullOrWhiteSpace(sessionCookie))
            {
                MessageBox.Show("Session cookie tidak ditemukan. Silakan login terlebih dahulu via WebView.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_cmbCompany.SelectedItem == null || _cmbUser.SelectedItem == null)
            {
                MessageBox.Show("Pilih Company dan User terlebih dahulu", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string companyId = _cmbCompany.SelectedItem?.ToString() ?? "";
            string userId = _cmbUser.SelectedItem?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(userId))
            {
                MessageBox.Show("Company ID atau User ID tidak valid", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Save pilihan terakhir
            SaveLastCompany(companyId);
            SaveLastUser(userId);

            // Stop polling yang lama jika ada
            _jobPollerService?.Stop();
            _jobPollerService?.Dispose();

            // Buat settings baru
            var settings = new PosPrinterPollSettings
            {
                Enabled = true,
                BaseUrl = baseUrl,
                CompanyId = companyId,
                UserId = userId,
                ApiKey = sessionCookie, // Simpan session cookie sebagai "ApiKey" (untuk kompatibilitas)
                IntervalMs = 3000,
                MaxJobs = 5
            };

            // Fetch settings dari API untuk update interval/max_jobs
            _jobPollerService = new JobPollerService(settings, _printerService, _cashDrawerService, OnServerLog);
            _ = Task.Run(async () =>
            {
                bool fetched = await _jobPollerService.FetchSettingsFromApiAsync();
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        if (fetched)
                        {
                            _jobPollerService.Start();
                            _btnStartPolling.Enabled = false;
                            _btnStopPolling.Enabled = true;
                            _lblPollingStatus.Text = "Status: Aktif";
                            _lblPollingStatus.ForeColor = Color.Green;
                        }
                        else
                        {
                            MessageBox.Show("Gagal fetch settings dari API, polling tidak dimulai", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }));
                }
            });
        }

        private void BtnStopPolling_Click(object sender, EventArgs e)
        {
            _jobPollerService?.Stop();
            _btnStartPolling.Enabled = true;
            _btnStopPolling.Enabled = false;
            _lblPollingStatus.Text = "Status: Tidak Aktif";
            _lblPollingStatus.ForeColor = Color.Red;
        }

        private async Task AutoFetchAndStartPollingAsync()
        {
            try
            {
                OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-FETCH] Memulai auto-fetch settings...");
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine("AutoFetchAndStartPollingAsync: Starting...");
                System.Diagnostics.Debug.WriteLine($"Thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

                // Cek apakah controls sudah diinisialisasi
                System.Diagnostics.Debug.WriteLine($"Controls check: _txtBaseUrl={_txtBaseUrl != null}, _cmbCompany={_cmbCompany != null}, _cmbUser={_cmbUser != null}");
                if (_txtBaseUrl == null || _cmbCompany == null || _cmbUser == null)
                {
                    OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-FETCH] Dibatalkan: controls belum diinisialisasi");
                    System.Diagnostics.Debug.WriteLine("Auto-fetch skipped: controls not initialized");
                    return;
                }

                // Cek apakah session cookie ada (prioritas utama, tidak perlu API key manual)
                string sessionCookie = _sessionCookie ?? "";
                System.Diagnostics.Debug.WriteLine($"Auto-fetch: Initial sessionCookie check - _sessionCookie={(_sessionCookie != null && !string.IsNullOrWhiteSpace(_sessionCookie) ? "exists" : "null/empty")}");
                
                if (string.IsNullOrWhiteSpace(sessionCookie))
                {
                    // Coba load session cookie dari file
                    OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-FETCH] Session cookie tidak ditemukan, mencoba load dari file...");
                    sessionCookie = LoadSessionCookie();
                    System.Diagnostics.Debug.WriteLine($"Auto-fetch: LoadSessionCookie() returned {(string.IsNullOrWhiteSpace(sessionCookie) ? "empty" : $"{sessionCookie.Length} chars")}");
                    
                    if (!string.IsNullOrWhiteSpace(sessionCookie))
                    {
                        _sessionCookie = sessionCookie;
                        OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-FETCH] Session cookie berhasil di-load dari file ({sessionCookie.Length} chars)");
                        System.Diagnostics.Debug.WriteLine("Auto-fetch: Loaded session cookie from file");
                    }
                    else
                    {
                        OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-FETCH] Dibatalkan: Session cookie tidak ditemukan. Silakan login via WebView terlebih dahulu.");
                        System.Diagnostics.Debug.WriteLine("Auto-fetch skipped: No session cookie found. Please login via WebView first.");
                        if (_lblPollingStatus != null)
                        {
                            _lblPollingStatus.Text = "Status: Belum login";
                            _lblPollingStatus.ForeColor = Color.Orange;
                        }
                        return; // Tidak ada session cookie, skip auto-start (user harus login dulu)
                    }
                }
                else
                {
                    OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-FETCH] Menggunakan session cookie yang ada ({sessionCookie.Length} chars)");
                    System.Diagnostics.Debug.WriteLine($"Auto-fetch: Using existing sessionCookie (length: {sessionCookie.Length})");
                }

                string baseUrl = _txtBaseUrl.Text?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    // Set default Base URL jika kosong
                    baseUrl = "https://dxnpos-train.dxn2u.com";
                    if (_txtBaseUrl != null)
                    {
                        _txtBaseUrl.Text = baseUrl;
                    }
                    OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-FETCH] Menggunakan default Base URL: {baseUrl}");
                    System.Diagnostics.Debug.WriteLine($"Auto-fetch: Using default Base URL: {baseUrl}");
                }

                // Cek apakah sessionCookie adalah session cookie atau API key
                // Session cookie biasanya format: "CookieName=CookieValue" dan mengandung "sess" atau "session"
                bool isSessionCookie = sessionCookie.Contains("=") && (sessionCookie.ToLower().Contains("sess") || sessionCookie.ToLower().Contains("session"));
                string cookieName = isSessionCookie ? sessionCookie.Split('=')[0] : "";
                var authType = isSessionCookie ? $"Cookie({cookieName})" : "API Key";
                var url = baseUrl.TrimEnd('/') + "/pos-printer-api/get-settings";
                OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-FETCH] Mengirim request ke: {url} (Auth: {authType})");
                System.Diagnostics.Debug.WriteLine($"Auto-fetch: Using baseUrl={baseUrl}, authType={authType}");

                // Fetch settings dari API menggunakan session cookie atau API key
                using var http = new System.Net.Http.HttpClient();
                http.Timeout = TimeSpan.FromSeconds(10);
                using var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                System.Diagnostics.Debug.WriteLine($"Auto-fetch: Request URL={url}");
                
                if (isSessionCookie)
                {
                    // Session cookie - gunakan header Cookie
                    req.Headers.Add("Cookie", sessionCookie);
                    System.Diagnostics.Debug.WriteLine($"Auto-fetch: Added header Cookie: {cookieName}=***");
                }
                else
                {
                    // API key - gunakan header X-PosPrinter-Key
                    req.Headers.Add("X-PosPrinter-Key", sessionCookie);
                    System.Diagnostics.Debug.WriteLine($"Auto-fetch: Added header X-PosPrinter-Key (length={sessionCookie.Length})");
                }

                System.Diagnostics.Debug.WriteLine("Auto-fetch: Sending HTTP request...");
                using var resp = await http.SendAsync(req);
                var body = await resp.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Auto-fetch: HTTP status={(int)resp.StatusCode} {resp.StatusCode}, bodyLength={(body?.Length ?? 0)}");
                
                if (!resp.IsSuccessStatusCode)
                {
                    string errorMsg = ExtractErrorMessage(body);
                    if (string.IsNullOrWhiteSpace(errorMsg))
                    {
                        errorMsg = $"{resp.ReasonPhrase}";
                    }
                    OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-FETCH] Gagal: HTTP {(int)resp.StatusCode} - {errorMsg}");
                    System.Diagnostics.Debug.WriteLine($"Auto-fetch settings failed: HTTP {(int)resp.StatusCode}, Body: {body}");
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            if (_lblPollingStatus != null)
                            {
                                _lblPollingStatus.Text = $"Status: Error HTTP {(int)resp.StatusCode}";
                                _lblPollingStatus.ForeColor = Color.Red;
                            }
                        }));
                    }
                    return;
                }

                using var doc = System.Text.Json.JsonDocument.Parse(body);
                if (!doc.RootElement.TryGetProperty("success", out var successEl) || !successEl.GetBoolean())
                {
                    string errorMsg = "API returned success=false";
                    if (doc.RootElement.TryGetProperty("message", out var msgEl))
                    {
                        errorMsg = msgEl.GetString() ?? errorMsg;
                    }
                    OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-FETCH] Gagal: {errorMsg}");
                    System.Diagnostics.Debug.WriteLine($"Auto-fetch settings failed: {errorMsg}");
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() =>
                        {
                            if (_lblPollingStatus != null)
                            {
                                _lblPollingStatus.Text = $"Status: Error - {errorMsg}";
                                _lblPollingStatus.ForeColor = Color.Red;
                            }
                        }));
                    }
                    return;
                }

                OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-FETCH] Berhasil mendapatkan settings dari API");
                System.Diagnostics.Debug.WriteLine("Auto-fetch settings: Success, parsing response...");

                // Parse settings (harus di UI thread) - gunakan await untuk memastikan selesai
                if (InvokeRequired)
                {
                    // Gunakan BeginInvoke untuk async operation
                    var taskCompletionSource = new TaskCompletionSource<bool>();
                    BeginInvoke(new Action(async () =>
                    {
                        try
                        {
                            await ParseAndPopulateSettingsAsync(doc.RootElement, baseUrl, sessionCookie);
                            taskCompletionSource.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error in ParseAndPopulateSettingsAsync: {ex.Message}");
                            taskCompletionSource.SetException(ex);
                        }
                    }));
                    await taskCompletionSource.Task;
                }
                else
                {
                    await ParseAndPopulateSettingsAsync(doc.RootElement, baseUrl, sessionCookie);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error auto-fetch and start polling: {ex.Message}");
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        if (_lblPollingStatus != null)
                        {
                            _lblPollingStatus.Text = $"Status: Error - {ex.Message}";
                            _lblPollingStatus.ForeColor = Color.Red;
                        }
                    }));
                }
            }
        }

        private async Task ParseAndPopulateSettingsAsync(System.Text.Json.JsonElement rootElement, string baseUrl, string sessionCookie)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine("ParseAndPopulateSettingsAsync: Starting...");
                System.Diagnostics.Debug.WriteLine($"ParseAndPopulateSettingsAsync: baseUrl={baseUrl}, sessionCookie length={sessionCookie?.Length ?? 0}");
                
                // Extract interval dan max_jobs dari API response
                int intervalMs = 3000; // Default
                int maxJobs = 5; // Default
                if (rootElement.TryGetProperty("default_interval_ms", out var intervalEl))
                {
                    intervalMs = intervalEl.GetInt32();
                    System.Diagnostics.Debug.WriteLine($"ParseAndPopulateSettingsAsync: Using interval from API: {intervalMs}ms");
                }
                if (rootElement.TryGetProperty("default_max_jobs", out var maxJobsEl))
                {
                    maxJobs = maxJobsEl.GetInt32();
                    System.Diagnostics.Debug.WriteLine($"ParseAndPopulateSettingsAsync: Using max_jobs from API: {maxJobs}");
                }
                
                // Clear dropdowns
                _cmbCompany.Items.Clear();
                _cmbUser.Items.Clear();
                _companyUsers.Clear();
                System.Diagnostics.Debug.WriteLine("ParseAndPopulateSettingsAsync: Cleared dropdowns");

                if (rootElement.TryGetProperty("settings", out var settingsEl) && settingsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    System.Diagnostics.Debug.WriteLine($"ParseAndPopulateSettingsAsync: Found settings array with {settingsEl.GetArrayLength()} item(s)");
                    foreach (var comp in settingsEl.EnumerateArray())
                    {
                        string compId = comp.GetProperty("company_id").GetString() ?? "";
                        _cmbCompany.Items.Add(compId);
                        System.Diagnostics.Debug.WriteLine($"ParseAndPopulateSettingsAsync: Added company: {compId}");

                        List<string> users = new List<string>();
                        if (comp.TryGetProperty("users", out var usersEl) && usersEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var user in usersEl.EnumerateArray())
                            {
                                string userId = user.GetProperty("user_id").GetString() ?? "";
                                users.Add(userId);
                            }
                        }
                        _companyUsers[compId] = users;
                    }
                }

                if (_cmbCompany.Items.Count == 0)
                {
                    OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-FETCH] Tidak ada company ditemukan, auto-start dibatalkan");
                    System.Diagnostics.Debug.WriteLine("ParseAndPopulateSettings: No companies found in settings, skipping auto-start");
                    if (_lblPollingStatus != null)
                    {
                        _lblPollingStatus.Text = "Status: No companies found";
                        _lblPollingStatus.ForeColor = Color.OrangeRed;
                    }
                    return; // Tidak ada company, skip auto-start
                }
                
                OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-FETCH] Ditemukan {_cmbCompany.Items.Count} company");
                System.Diagnostics.Debug.WriteLine($"ParseAndPopulateSettings: Found {_cmbCompany.Items.Count} company(ies)");

                // Load pilihan terakhir atau pilih yang pertama
                string lastCompany = LoadLastCompany();
                string lastUser = LoadLastUser();

                if (!string.IsNullOrWhiteSpace(lastCompany) && _cmbCompany.Items.Contains(lastCompany))
                {
                    _cmbCompany.SelectedItem = lastCompany;
                    UpdateUserDropdown();

                    // Pastikan user dipilih setelah UpdateUserDropdown
                    if (!string.IsNullOrWhiteSpace(lastUser) && _cmbUser.Items.Contains(lastUser))
                    {
                        _cmbUser.SelectedItem = lastUser;
                    }
                    else if (_cmbUser.Items.Count > 0)
                    {
                        // Jika lastUser tidak ditemukan, pilih yang pertama
                        _cmbUser.SelectedIndex = 0;
                    }
                }
                else
                {
                    // Pilih yang pertama
                    _cmbCompany.SelectedIndex = 0;
                    UpdateUserDropdown();
                    
                    // Pastikan user dipilih setelah UpdateUserDropdown
                    if (_cmbUser.Items.Count > 0)
                    {
                        _cmbUser.SelectedIndex = 0;
                    }
                }

                // Double check: pastikan user terpilih
                if (_cmbUser.Items.Count > 0 && _cmbUser.SelectedItem == null)
                {
                    _cmbUser.SelectedIndex = 0;
                    System.Diagnostics.Debug.WriteLine($"ParseAndPopulateSettings: Force-selected first user (double check)");
                }

                System.Diagnostics.Debug.WriteLine($"ParseAndPopulateSettings: Company selected={_cmbCompany.SelectedItem}, User selected={_cmbUser.SelectedItem}, User count={_cmbUser.Items.Count}");

                // Auto-start polling jika company dan user sudah dipilih
                // Pastikan user terpilih sebelum cek
                if (_cmbUser.Items.Count > 0 && _cmbUser.SelectedItem == null)
                {
                    _cmbUser.SelectedIndex = 0;
                    System.Diagnostics.Debug.WriteLine($"ParseAndPopulateSettings: Force-selected first user before auto-start check");
                }

                if (_cmbCompany.SelectedItem != null && _cmbUser.SelectedItem != null)
                {
                    string companyId = _cmbCompany.SelectedItem.ToString() ?? "";
                    string userId = _cmbUser.SelectedItem.ToString() ?? "";

                    OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-START] Company: {companyId}, User: {userId}");
                    System.Diagnostics.Debug.WriteLine($"Auto-start polling: Company={companyId}, User={userId}");

                    if (!string.IsNullOrWhiteSpace(companyId) && !string.IsNullOrWhiteSpace(userId))
                    {
                        // Save pilihan terakhir
                        SaveLastCompany(companyId);
                        SaveLastUser(userId);

                        // Stop polling yang lama jika ada
                        _jobPollerService?.Stop();
                        _jobPollerService?.Dispose();

                        // intervalMs dan maxJobs sudah di-extract di awal method (scope method)
                        // Buat settings baru dengan interval dari API
                        var settings = new PosPrinterPollSettings
                        {
                            Enabled = true,
                            BaseUrl = baseUrl,
                            CompanyId = companyId,
                            UserId = userId,
                            ApiKey = sessionCookie, // Simpan session cookie sebagai "ApiKey" (untuk kompatibilitas)
                            IntervalMs = intervalMs, // Gunakan interval dari API (dari awal method)
                            MaxJobs = maxJobs // Gunakan max_jobs dari API (dari awal method)
                        };

                        OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-START] Membuat JobPollerService (interval={intervalMs}ms, max_jobs={maxJobs})");
                        System.Diagnostics.Debug.WriteLine($"Auto-start polling: Creating JobPollerService for company={companyId}, user={userId}");
                        System.Diagnostics.Debug.WriteLine($"Auto-start polling: Settings - Enabled={settings.Enabled}, BaseUrl={settings.BaseUrl}, IntervalMs={settings.IntervalMs}, MaxJobs={settings.MaxJobs}, ApiKey length={settings.ApiKey?.Length ?? 0}");

                        // Buat JobPollerService dan langsung start (interval sudah di-set dari API)
                        _jobPollerService = new JobPollerService(settings, _printerService, _cashDrawerService, OnServerLog);
                        
                        // Start polling langsung (tidak perlu fetch lagi karena interval sudah di-set)
                        System.Diagnostics.Debug.WriteLine("Auto-start polling: Starting poller with interval from API...");
                        System.Diagnostics.Debug.WriteLine($"Auto-start polling: Final Settings - Enabled={settings.Enabled}, CompanyId={settings.CompanyId}, UserId={settings.UserId}, IntervalMs={settings.IntervalMs}, MaxJobs={settings.MaxJobs}");
                        
                        // Pastikan settings masih valid sebelum start
                        if (settings.Enabled && !string.IsNullOrWhiteSpace(settings.CompanyId) && !string.IsNullOrWhiteSpace(settings.UserId))
                        {
                            OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-START] Memulai polling untuk company={companyId}, user={userId}...");
                            System.Diagnostics.Debug.WriteLine("Auto-start polling: Calling Start()...");
                            _jobPollerService.Start();
                            System.Diagnostics.Debug.WriteLine("Auto-start polling: Start() method called");
                            
                            // Verifikasi bahwa poller benar-benar started
                            await Task.Delay(200); // Beri waktu sedikit untuk timer di-start
                            
                            // Update UI
                            if (_btnStartPolling != null) _btnStartPolling.Enabled = false;
                            if (_btnStopPolling != null) _btnStopPolling.Enabled = true;
                            if (_lblPollingStatus != null)
                            {
                                _lblPollingStatus.Text = $"Status: Aktif (Auto-start, interval={intervalMs}ms)";
                                _lblPollingStatus.ForeColor = Color.Green;
                            }
                            OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-START] Polling berhasil dimulai (interval={intervalMs}ms)");
                            System.Diagnostics.Debug.WriteLine($"Auto-start polling: SUCCESS for company={companyId}, user={userId}, interval={intervalMs}ms");
                        }
                        else
                        {
                            OnServerLog($"[{DateTime.Now:HH:mm:ss}] [AUTO-START] Gagal: Settings tidak valid");
                            System.Diagnostics.Debug.WriteLine($"Auto-start polling: Settings invalid - Enabled={settings.Enabled}, CompanyId={settings.CompanyId}, UserId={settings.UserId}");
                            if (_lblPollingStatus != null)
                            {
                                _lblPollingStatus.Text = "Status: Settings invalid";
                                _lblPollingStatus.ForeColor = Color.Red;
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Auto-start polling: CompanyId or UserId is empty - CompanyId={companyId}, UserId={userId}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Auto-start polling: Company or User not selected - Company={_cmbCompany.SelectedItem}, User={_cmbUser.SelectedItem}, UserCount={_cmbUser.Items.Count}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ParseAndPopulateSettings: {ex.Message}\n{ex.StackTrace}");
                if (_lblPollingStatus != null)
                {
                    _lblPollingStatus.Text = $"Status: Error - {ex.Message}";
                    _lblPollingStatus.ForeColor = Color.Red;
                }
            }
        }

        private static string LastSelectionFilePath => Path.Combine(AppContext.BaseDirectory, "posprinter_last_selection.txt");

        private static void SaveLastCompany(string companyId)
        {
            try
            {
                string content = $"company={companyId}\n";
                if (File.Exists(LastSelectionFilePath))
                {
                    string existing = File.ReadAllText(LastSelectionFilePath);
                    var lines = existing.Split('\n').ToList();
                    var newLines = new List<string> { content.Trim() };
                    foreach (var line in lines)
                    {
                        if (!line.StartsWith("company=") && !string.IsNullOrWhiteSpace(line))
                        {
                            newLines.Add(line);
                        }
                    }
                    File.WriteAllText(LastSelectionFilePath, string.Join("\n", newLines));
                }
                else
                {
                    File.WriteAllText(LastSelectionFilePath, content);
                }
            }
            catch { }
        }

        private static void SaveLastUser(string userId)
        {
            try
            {
                string content = $"user={userId}\n";
                if (File.Exists(LastSelectionFilePath))
                {
                    string existing = File.ReadAllText(LastSelectionFilePath);
                    var lines = existing.Split('\n').ToList();
                    var newLines = new List<string>();
                    bool userFound = false;
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("user="))
                        {
                            newLines.Add(content.Trim());
                            userFound = true;
                        }
                        else if (!string.IsNullOrWhiteSpace(line))
                        {
                            newLines.Add(line);
                        }
                    }
                    if (!userFound)
                    {
                        newLines.Add(content.Trim());
                    }
                    File.WriteAllText(LastSelectionFilePath, string.Join("\n", newLines));
                }
                else
                {
                    File.WriteAllText(LastSelectionFilePath, content);
                }
            }
            catch { }
        }

        private static string LoadLastCompany()
        {
            try
            {
                if (File.Exists(LastSelectionFilePath))
                {
                    foreach (var line in File.ReadAllLines(LastSelectionFilePath))
                    {
                        if (line.StartsWith("company="))
                        {
                            return line.Substring(8).Trim();
                        }
                    }
                }
            }
            catch { }
            return "";
        }

        private static string LoadLastUser()
        {
            try
            {
                if (File.Exists(LastSelectionFilePath))
                {
                    foreach (var line in File.ReadAllLines(LastSelectionFilePath))
                    {
                        if (line.StartsWith("user="))
                        {
                            return line.Substring(5).Trim();
                        }
                    }
                }
            }
            catch { }
            return "";
        }

        private static string SessionCookieFilePath => Path.Combine(AppContext.BaseDirectory, "posprinter_session_cookie.txt");

        private static void SaveSessionCookie(string cookie)
        {
            try
            {
                File.WriteAllText(SessionCookieFilePath, cookie.Trim());
            }
            catch { }
        }

        private static string LoadSessionCookie()
        {
            try
            {
                if (File.Exists(SessionCookieFilePath))
                {
                    return File.ReadAllText(SessionCookieFilePath).Trim();
                }
            }
            catch { }
            return "";
        }

        private async Task CheckAndSaveSessionCookie()
        {
            try
            {
                if (_webView?.CoreWebView2?.CookieManager == null)
                {
                    OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] WebView atau CookieManager tidak tersedia");
                    return;
                }

                // Ambil semua cookies - gunakan base URL dari WebView source
                string sourceUrl = _webView.CoreWebView2.Source ?? "";
                OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] Mengecek cookies dari URL: {sourceUrl}");
                
                // Coba ambil cookies dengan beberapa cara:
                // 1. Dari source URL
                var cookies = await _webView.CoreWebView2.CookieManager.GetCookiesAsync(sourceUrl);
                OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] Ditemukan {cookies.Count} cookie(s) dari source URL");
                
                // 2. Jika tidak ada, coba dari base domain
                if (cookies.Count == 0 && !string.IsNullOrEmpty(sourceUrl))
                {
                    try
                    {
                        var uri = new Uri(sourceUrl);
                        string baseDomain = $"{uri.Scheme}://{uri.Host}";
                        cookies = await _webView.CoreWebView2.CookieManager.GetCookiesAsync(baseDomain);
                        OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] Ditemukan {cookies.Count} cookie(s) dari base domain: {baseDomain}");
                    }
                    catch (Exception ex)
                    {
                        OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] Error parsing URL: {ex.Message}");
                    }
                }
                
                // Cari cookie session (prioritas: dxnpostest_sess_id, kemudian PHPSESSID, kemudian cookie lain yang kemungkinan session)
                bool found = false;
                string sessionCookieName = null;
                string sessionCookieValue = null;
                
                foreach (var cookie in cookies)
                {
                    OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] Cookie ditemukan: {cookie.Name} = {cookie.Value.Substring(0, Math.Min(20, cookie.Value.Length))}...");
                    
                    // Prioritas 1: dxnpostest_sess_id (custom session name Yii2)
                    if (cookie.Name.Equals("dxnpostest_sess_id", StringComparison.OrdinalIgnoreCase))
                    {
                        sessionCookieName = cookie.Name;
                        sessionCookieValue = cookie.Value;
                        found = true;
                        break;
                    }
                    // Prioritas 2: PHPSESSID (default PHP session)
                    else if (cookie.Name.Equals("PHPSESSID", StringComparison.OrdinalIgnoreCase) && !found)
                    {
                        sessionCookieName = cookie.Name;
                        sessionCookieValue = cookie.Value;
                        found = true;
                        // Jangan break, cari yang lebih spesifik dulu
                    }
                    // Prioritas 3: Cookie lain yang mengandung "sess" atau "session"
                    else if (!found && (cookie.Name.ToLower().Contains("sess") || cookie.Name.ToLower().Contains("session")))
                    {
                        sessionCookieName = cookie.Name;
                        sessionCookieValue = cookie.Value;
                        found = true;
                        // Jangan break, cari yang lebih spesifik dulu
                    }
                }
                
                if (found && !string.IsNullOrEmpty(sessionCookieName) && !string.IsNullOrEmpty(sessionCookieValue))
                {
                    string cookieValue = $"{sessionCookieName}={sessionCookieValue}";
                    bool isNewCookie = _sessionCookie != cookieValue; // Cek apakah cookie berubah
                    _sessionCookie = cookieValue;
                    SaveSessionCookie(cookieValue);
                    OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] Session cookie ditemukan dan disimpan: {sessionCookieName}={sessionCookieValue.Substring(0, Math.Min(30, sessionCookieValue.Length))}...");
                    System.Diagnostics.Debug.WriteLine($"Session cookie saved: {cookieValue.Substring(0, Math.Min(20, cookieValue.Length))}...");

                    // Auto-fetch settings setelah login atau jika polling tidak berjalan
                    // Restart polling jika: (1) cookie baru ditemukan, atau (2) polling tidak berjalan
                    // Setelah navigation (misalnya setelah confirm order), selalu cek dan restart polling jika perlu
                    bool shouldRestartPolling = isNewCookie || _jobPollerService == null || _companySwitchDetected;
                    
                    // Setelah navigation, selalu cek apakah polling masih berjalan
                    // Jika polling service ada tapi mungkin terhenti, restart polling
                    if (!shouldRestartPolling && _jobPollerService != null)
                    {
                        // Cek apakah polling masih aktif dengan melihat status label
                        // Jika status tidak "Aktif", berarti polling mungkin terhenti
                        if (_lblPollingStatus != null)
                        {
                            string currentStatus = _lblPollingStatus.Text ?? "";
                            if (!currentStatus.Contains("Aktif") && !currentStatus.Contains("aktif"))
                            {
                                OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] Polling service ada tapi status tidak aktif ({currentStatus}), akan restart...");
                                shouldRestartPolling = true;
                            }
                        }
                    }
                    
                    if (shouldRestartPolling)
                    {
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(1000); // Delay lebih lama untuk memastikan cookie tersimpan
                            _companySwitchDetected = false; // reset setelah trigger restart
                            OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] Trigger auto-fetch setelah login/navigation...");
                            if (InvokeRequired)
                            {
                                Invoke(new Action(async () => await AutoFetchAndStartPollingAsync()));
                            }
                            else
                            {
                                await AutoFetchAndStartPollingAsync();
                            }
                        });
                    }
                    else
                    {
                        OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] Session cookie sudah ada dan polling berjalan, tidak perlu restart");
                    }
                }
                else
                {
                    OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] Session cookie tidak ditemukan di {cookies.Count} cookie(s)");
                    
                    // Jika session cookie tidak ditemukan tapi polling masih berjalan, stop polling
                    if (_jobPollerService != null)
                    {
                        OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] Session cookie hilang, menghentikan polling...");
                        _jobPollerService?.Stop();
                        _jobPollerService?.Dispose();
                        _jobPollerService = null;
                        if (_lblPollingStatus != null)
                        {
                            _lblPollingStatus.Text = "Status: Session cookie hilang";
                            _lblPollingStatus.ForeColor = Color.Orange;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnServerLog($"[{DateTime.Now:HH:mm:ss}] [SESSION] Error checking session cookie: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Error checking session cookie: {ex.Message}");
            }
        }
    }
}

