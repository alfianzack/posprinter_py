using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using PosPrinterApp.Services;

namespace PosPrinterApp
{
    public partial class MainForm : Form
    {
        private PosPrinterService _printerService;
        private CashDrawerService _cashDrawerService;
        private HttpServerService _httpServerService;
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
        private TabControl _tabControl;
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
            this.FormClosing += MainForm_FormClosing;

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
                Text = "8080",
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

        private async void InitializeWebView2()
        {
            try
            {
                await _webView.EnsureCoreWebView2Async(null);
                
                // Setup WebMessageReceived handler
                _webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;
                
                // Update navigation buttons state
                _webView.CoreWebView2.HistoryChanged += (sender, e) =>
                {
                    UpdateNavigationButtons();
                };
                
                // Handle WebView resize untuk update viewport
                _webView.SizeChanged += async (sender, e) =>
                {
                    if (_webView?.CoreWebView2 != null)
                    {
                        // Adjust dimensions based on DPI scaling
                        int adjustedWidth = (int)(_webView.Width / _dpiScaleX);
                        int adjustedHeight = (int)(_webView.Height / _dpiScaleY);
                        
                        // Trigger auto-fit in JavaScript when WebView2 control resizes
                        _ = _webView.CoreWebView2.ExecuteScriptAsync($"window.posPrinterAutoFit({adjustedWidth}, {adjustedHeight});");
                    }
                };
                
                // Inject JavaScript bridge setelah page loaded dan update address bar
                _webView.CoreWebView2.NavigationCompleted += async (sender, e) =>
                {
                    if (e.IsSuccess)
                    {
                        try
                        {
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
                                        var cssText = ""html, body {{ transform: scale("" + scale + "") !important; transform-origin: top left !important; width: "" + widthPercent + ""% !important; min-width: "" + widthPercent + ""% !important; max-width: "" + widthPercent + ""% !important; }} html {{ height: "" + heightPercent + ""px !important; overflow-x: hidden !important; }} body {{ height: auto !important; overflow-x: hidden !important; }}"";
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
                            
                            // Set zoom default setelah script injection
                            await SetZoomLevel(_currentZoomFactor);
                            
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
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing WebView2: {ex.Message}\n\nPastikan WebView2 Runtime sudah terinstall.\n\nDownload dari: https://developer.microsoft.com/microsoft-edge/webview2/", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            try
            {
                if (_webView?.CoreWebView2 != null)
                {
                    _currentZoomFactor = Math.Min(_currentZoomFactor + 0.1, 3.0); // Max 300%
                    await SetZoomLevel(_currentZoomFactor);
                    UpdateZoomLabel();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error zoom in: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnZoomOut_Click(object sender, EventArgs e)
        {
            try
            {
                if (_webView?.CoreWebView2 != null)
                {
                    _currentZoomFactor = Math.Max(_currentZoomFactor - 0.1, 0.25); // Min 25%
                    await SetZoomLevel(_currentZoomFactor);
                    UpdateZoomLabel();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error zoom out: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnZoomReset_Click(object sender, EventArgs e)
        {
            try
            {
                if (_webView?.CoreWebView2 != null)
                {
                    _currentZoomFactor = 1.0; // Reset to 100%
                    await SetZoomLevel(_currentZoomFactor);
                    UpdateZoomLabel();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reset zoom: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SetZoomLevel(double zoomFactor)
        {
            try
            {
                if (_webView?.CoreWebView2 != null)
                {
                    // Gunakan JavaScript untuk zoom dengan style yang konsisten
                    string script = $@"
                        (function() {{
                            var zoomStyle = document.getElementById(""pos-printer-zoom-auto"");
                            if (!zoomStyle) {{
                                zoomStyle = document.createElement(""style"");
                                zoomStyle.id = ""pos-printer-zoom-auto"";
                                document.head.appendChild(zoomStyle);
                            }}
                            
                            var scale = {zoomFactor.ToString(System.Globalization.CultureInfo.InvariantCulture)};
                            var widthPercent = (100 / scale);
                            
                            zoomStyle.textContent = ""html, body {{ transform: scale("" + scale + ""); transform-origin: top left; width: "" + widthPercent + ""% !important; min-width: "" + widthPercent + ""% !important; }}"";
                            
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
                    string fitScript = $@"
                        (function() {{
                            var autoFit = window.posPrinterAutoFit;
                            if (autoFit) {{
                                autoFit({adjustedWidth}, {adjustedHeight});
                                
                                // Get current scale untuk update label
                                var zoomStyle = document.getElementById(""pos-printer-zoom-auto"");
                                if (zoomStyle && zoomStyle.textContent) {{
                                    var match = zoomStyle.textContent.match(/scale\(([0-9.]+)\)/);
                                    if (match && match[1]) {{
                                        return parseFloat(match[1]);
                                    }}
                                }}
                            }}
                            return 1.0;
                        }})();
                    ";
                    
                    string scaleResult = await _webView.CoreWebView2.ExecuteScriptAsync(fitScript);
                    // Remove quotes and parse
                    scaleResult = scaleResult.Trim('"');
                    if (double.TryParse(scaleResult, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double scale))
                    {
                        _currentZoomFactor = scale;
                        UpdateZoomLabel();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fit to width: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error memuat daftar printer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        }
    }
}

