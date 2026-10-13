// Keyboard Pad - turns keyboard + mouse into a virtual Xbox 360 controller.
// Built by Start.bat with the C# compiler that ships with Windows (no install needed).
// Needs: ViGEmBus driver (Install-Driver.bat) and ViGEmClient.dll (included).
// Keep to C# 5 syntax: the Windows built-in compiler is old.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace KeyboardPad
{
    // ------------------------------------------------------------------
    // Settings: which key/mouse button drives which controller button
    // ------------------------------------------------------------------
    static class Config
    {
        // { setting name, label shown in the window, default keys }
        public static readonly string[][] Rows = new string[][]
        {
            new string[] { "MoveUp",    "Move up  (left stick)",       "W" },
            new string[] { "MoveDown",  "Move down  (left stick)",     "S" },
            new string[] { "MoveLeft",  "Move left  (left stick)",     "A" },
            new string[] { "MoveRight", "Move right  (left stick)",    "D" },
            new string[] { "CamUp",     "Camera up  (right stick)",    "Up" },
            new string[] { "CamDown",   "Camera down  (right stick)",  "Down" },
            new string[] { "CamLeft",   "Camera left  (right stick)",  "Left" },
            new string[] { "CamRight",  "Camera right  (right stick)", "Right" },
            new string[] { "A",         "A button",                    "Space" },
            new string[] { "B",         "B button",                    "L,MButton" },
            new string[] { "X",         "X button",                    "J,LButton" },
            new string[] { "Y",         "Y button",                    "K,RButton" },
            new string[] { "LB",        "LB  (left bumper)",           "Q" },
            new string[] { "RB",        "RB  (right bumper)",          "E" },
            new string[] { "LT",        "LT  (left trigger)",          "ShiftKey" },
            new string[] { "RT",        "RT  (right trigger)",         "ControlKey" },
            new string[] { "L3",        "L3  (click left stick)",      "Z" },
            new string[] { "R3",        "R3  (click right stick)",     "X" },
            new string[] { "Start",     "Start",                       "Return" },
            new string[] { "Back",      "Back / Select",               "Tab" },
            new string[] { "DpadUp",    "D-pad up",                    "T" },
            new string[] { "DpadDown",  "D-pad down",                  "G" },
            new string[] { "DpadLeft",  "D-pad left",                  "F" },
            new string[] { "DpadRight", "D-pad right",                 "H" }
        };

        // Not shown in the list; editable in keymap.txt
        public static readonly string[][] Hidden = new string[][]
        {
            new string[] { "PauseKey", "F9" },
            new string[] { "ExitKey",  "F12" }
        };

        public static string FilePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "keymap.txt"); }
        }

        public static int[] ParseKeys(string text)
        {
            List<int> codes = new List<int>();
            if (text == null) return codes.ToArray();
            string[] parts = text.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string k = parts[i].Trim();
                if (k.Length == 0) continue;
                try
                {
                    object o = Enum.Parse(typeof(Keys), k, true);
                    int vk = ((int)(Keys)o) & 0xFFFF;
                    if (vk > 0 && !codes.Contains(vk)) codes.Add(vk);
                }
                catch (Exception) { }
            }
            return codes.ToArray();
        }

        public static string KeyName(int vk)
        {
            return ((Keys)vk).ToString();
        }

        public static string KeyLabel(int vk)
        {
            switch (vk)
            {
                case 1: return "Mouse left";
                case 2: return "Mouse right";
                case 4: return "Mouse middle";
                case 5: return "Mouse side 1";
                case 6: return "Mouse side 2";
                case 8: return "Backspace";
                case 13: return "Enter";
                case 16: return "Shift";
                case 17: return "Ctrl";
                case 18: return "Alt";
                case 32: return "Space";
                case 37: return "Left arrow";
                case 38: return "Up arrow";
                case 39: return "Right arrow";
                case 40: return "Down arrow";
            }
            string s = ((Keys)vk).ToString();
            if (s.Length == 2 && s[0] == 'D' && char.IsDigit(s[1])) return s.Substring(1);
            return s;
        }

        public static string Describe(int[] codes)
        {
            if (codes == null || codes.Length == 0) return "(not set)";
            string[] names = new string[codes.Length];
            for (int i = 0; i < codes.Length; i++) names[i] = KeyLabel(codes[i]);
            return string.Join("  /  ", names);
        }

        public static Dictionary<string, int[]> Defaults()
        {
            Dictionary<string, int[]> m = new Dictionary<string, int[]>();
            for (int i = 0; i < Rows.Length; i++) m[Rows[i][0]] = ParseKeys(Rows[i][2]);
            for (int i = 0; i < Hidden.Length; i++) m[Hidden[i][0]] = ParseKeys(Hidden[i][1]);
            return m;
        }

        public static Dictionary<string, int[]> Load(out string game, out bool onlyInGame)
        {
            Dictionary<string, int[]> m = Defaults();
            game = "";
            bool onlySet = false;
            onlyInGame = false;
            try
            {
                if (File.Exists(FilePath))
                {
                    foreach (string line in File.ReadAllLines(FilePath))
                    {
                        string t = line.Trim();
                        if (t.Length == 0 || t.StartsWith("#")) continue;
                        int eq = t.IndexOf('=');
                        if (eq < 1) continue;
                        string name = t.Substring(0, eq).Trim();
                        string val = t.Substring(eq + 1).Trim();
                        if (name == "GameProcess")
                        {
                            if (val.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                val = val.Substring(0, val.Length - 4);
                            game = val;
                        }
                        else if (name == "OnlyInGame")
                        {
                            onlyInGame = (val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase));
                            onlySet = true;
                        }
                        else if (m.ContainsKey(name))
                        {
                            m[name] = ParseKeys(val);
                        }
                    }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            if (!onlySet) onlyInGame = game.Length > 0;
            return m;
        }

        public static void Save(Dictionary<string, int[]> m, string game, bool onlyInGame)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Keyboard Pad key bindings. The app rewrites this file when you change keys.");
            sb.AppendLine("# Format:  Name = Key, Key   (several keys can trigger the same button)");
            sb.AppendLine("# Key names: A-Z, D0-D9, Space, Return, Tab, Back, Up, Down, Left, Right,");
            sb.AppendLine("#   ShiftKey, ControlKey, Menu (Alt), F1-F12, LButton, RButton, MButton, XButton1, XButton2");
            sb.AppendLine("# PauseKey / ExitKey are global hotkeys that only exist in this file.");
            sb.AppendLine();
            for (int i = 0; i < Rows.Length; i++) sb.AppendLine(Line(m, Rows[i][0]));
            for (int i = 0; i < Hidden.Length; i++) sb.AppendLine(Line(m, Hidden[i][0]));
            sb.AppendLine();
            sb.AppendLine("OnlyInGame = " + (onlyInGame ? "1" : "0"));
            sb.AppendLine("GameProcess = " + (game ?? ""));
            try { File.WriteAllText(FilePath, sb.ToString()); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        static string Line(Dictionary<string, int[]> m, string name)
        {
            int[] codes;
            if (!m.TryGetValue(name, out codes)) codes = new int[0];
            string[] names = new string[codes.Length];
            for (int i = 0; i < codes.Length; i++) names[i] = KeyName(codes[i]);
            return name + " = " + string.Join(", ", names);
        }
    }

    // ------------------------------------------------------------------
    // Engine: reads the keyboard/mouse and feeds the virtual controller
    // ------------------------------------------------------------------
    sealed class Engine
    {
        [StructLayout(LayoutKind.Sequential)]
        struct Report
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr LoadLibrary(string path);
        [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
        [DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint ms);
        [DllImport("winmm.dll")] static extern uint timeEndPeriod(uint ms);

        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern IntPtr vigem_alloc();
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern void vigem_free(IntPtr client);
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern uint vigem_connect(IntPtr client);
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern void vigem_disconnect(IntPtr client);
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern IntPtr vigem_target_x360_alloc();
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern void vigem_target_free(IntPtr target);
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern uint vigem_target_add(IntPtr client, IntPtr target);
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern uint vigem_target_remove(IntPtr client, IntPtr target);
        [DllImport("ViGEmClient.dll", CallingConvention = CallingConvention.Cdecl)] static extern uint vigem_target_x360_update(IntPtr client, IntPtr target, Report report);

        const uint VIGEM_OK = 0x20000000;

        volatile Dictionary<string, int[]> map = new Dictionary<string, int[]>();
        public volatile bool Running;
        public volatile bool Paused;
        public volatile string GameProcess = "";

        IntPtr client = IntPtr.Zero;
        IntPtr pad = IntPtr.Zero;
        bool added;
        bool timerRaised;
        Thread worker;

        public void SetMap(Dictionary<string, int[]> m)
        {
            map = m;
        }

        public bool Down(string name)
        {
            Dictionary<string, int[]> m = map;
            int[] codes;
            if (!m.TryGetValue(name, out codes)) return false;
            for (int i = 0; i < codes.Length; i++)
            {
                if ((GetAsyncKeyState(codes[i]) & 0x8000) != 0) return true;
            }
            return false;
        }

        // Returns null on success, otherwise a message for the user.
        public string Start(string dllPath)
        {
            if (Running) return null;
            try
            {
                if (LoadLibrary(dllPath) == IntPtr.Zero)
                    return "Could not load ViGEmClient.dll. Make sure it is in the same folder as this program (and that Windows is 64-bit).";

                client = vigem_alloc();
                if (client == IntPtr.Zero) return "The controller library failed to start.";

                uint err = vigem_connect(client);
                if (err != VIGEM_OK)
                {
                    Cleanup();
                    return "The virtual controller driver (ViGEmBus) is not installed or not running.\n\nClick \"Install driver\" in the window, approve the Windows prompt, and try again. If it asks for a restart, restart Windows first.";
                }

                pad = vigem_target_x360_alloc();
                err = vigem_target_add(client, pad);
                if (err != VIGEM_OK)
                {
                    Cleanup();
                    return "Windows would not create the virtual controller (code 0x" + err.ToString("X") + ").";
                }
                added = true;
            }
            catch (Exception e)
            {
                Cleanup();
                return "Could not start: " + e.Message;
            }

            timeBeginPeriod(1);
            timerRaised = true;
            Paused = false;
            Running = true;
            worker = new Thread(Loop);
            worker.IsBackground = true;
            worker.Name = "KeyboardPadLoop";
            worker.Start();
            return null;
        }

        public void Stop()
        {
            Running = false;
            Thread w = worker;
            worker = null;
            if (w != null && w.IsAlive && w != Thread.CurrentThread) w.Join(1000);
            Cleanup();
            if (timerRaised)
            {
                timeEndPeriod(1);
                timerRaised = false;
            }
        }

        void Cleanup()
        {
            if (client != IntPtr.Zero)
            {
                try
                {
                    if (added) vigem_target_remove(client, pad);
                    if (pad != IntPtr.Zero) vigem_target_free(pad);
                    vigem_disconnect(client);
                    vigem_free(client);
                }
                catch (Exception) { }
            }
            added = false;
            pad = IntPtr.Zero;
            client = IntPtr.Zero;
        }

        static short Axis(bool positive, bool negative, bool diagonal)
        {
            int dir = (positive ? 1 : 0) - (negative ? 1 : 0);
            double scale = diagonal ? 0.7071 : 1.0;
            return (short)(dir * 32767 * scale);
        }

        static bool GameActive(string name)
        {
            if (string.IsNullOrEmpty(name)) return true;
            IntPtr h = GetForegroundWindow();
            if (h == IntPtr.Zero) return false;
            uint pid;
            GetWindowThreadProcessId(h, out pid);
            try
            {
                using (Process p = Process.GetProcessById((int)pid))
                {
                    return string.Equals(p.ProcessName, name, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception) { return false; }
        }

        static bool Same(Report a, Report b)
        {
            return a.wButtons == b.wButtons && a.bLeftTrigger == b.bLeftTrigger &&
                   a.bRightTrigger == b.bRightTrigger && a.sThumbLX == b.sThumbLX &&
                   a.sThumbLY == b.sThumbLY && a.sThumbRX == b.sThumbRX && a.sThumbRY == b.sThumbRY;
        }

        Report Build()
        {
            Report r = new Report();
            ushort b = 0;
            if (Down("DpadUp")) b |= 0x0001;
            if (Down("DpadDown")) b |= 0x0002;
            if (Down("DpadLeft")) b |= 0x0004;
            if (Down("DpadRight")) b |= 0x0008;
            if (Down("Start")) b |= 0x0010;
            if (Down("Back")) b |= 0x0020;
            if (Down("L3")) b |= 0x0040;
            if (Down("R3")) b |= 0x0080;
            if (Down("LB")) b |= 0x0100;
            if (Down("RB")) b |= 0x0200;
            if (Down("A")) b |= 0x1000;
            if (Down("B")) b |= 0x2000;
            if (Down("X")) b |= 0x4000;
            if (Down("Y")) b |= 0x8000;
            r.wButtons = b;
            r.bLeftTrigger = Down("LT") ? (byte)255 : (byte)0;
            r.bRightTrigger = Down("RT") ? (byte)255 : (byte)0;

            bool mu = Down("MoveUp"), md = Down("MoveDown"), ml = Down("MoveLeft"), mr = Down("MoveRight");
            bool moveDiag = (mu != md) && (ml != mr);
            r.sThumbLX = Axis(mr, ml, moveDiag);
            r.sThumbLY = Axis(mu, md, moveDiag);

            bool cu = Down("CamUp"), cd = Down("CamDown"), cl = Down("CamLeft"), cr = Down("CamRight");
            bool camDiag = (cu != cd) && (cl != cr);
            r.sThumbRX = Axis(cr, cl, camDiag);
            r.sThumbRY = Axis(cu, cd, camDiag);
            return r;
        }

        void Loop()
        {
            Report neutral = new Report();
            Report last = neutral;
            bool pauseHeld = false;
            bool active = true;
            int nextCheck = Environment.TickCount;

            // A new pad reads off-center sticks in XInput until it gets a report,
            // and ViGEmBus ignores a report equal to the all-zero one it starts with.
            Report nudge = neutral;
            nudge.sThumbLX = 1;
            vigem_target_x360_update(client, pad, nudge);
            vigem_target_x360_update(client, pad, neutral);

            while (Running)
            {
                if (Down("ExitKey"))
                {
                    Running = false;
                    break;
                }

                bool pauseNow = Down("PauseKey");
                if (pauseNow && !pauseHeld) Paused = !Paused;
                pauseHeld = pauseNow;

                int now = Environment.TickCount;
                if (unchecked(now - nextCheck) >= 0)
                {
                    active = GameActive(GameProcess);
                    nextCheck = unchecked(now + 200);
                }

                Report cur = (Paused || !active) ? neutral : Build();
                if (!Same(cur, last))
                {
                    vigem_target_x360_update(client, pad, cur);
                    last = cur;
                }
                Thread.Sleep(4);
            }
        }
    }

    // ------------------------------------------------------------------
    // Window
    // ------------------------------------------------------------------
    sealed class MainForm : Form
    {
        readonly Engine engine = new Engine();
        readonly string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        Dictionary<string, int[]> map;
        string gameProcess;
        bool onlyInGame;

        Label banner;
        Button toggle;
        FlowLayoutPanel driverBar;
        Label driverLabel;
        Button installBtn;
        ListView list;
        CheckBox onlyChk;
        ComboBox procBox;
        Label footer;
        System.Windows.Forms.Timer timer;

        readonly Dictionary<string, ListViewItem> items = new Dictionary<string, ListViewItem>();
        bool uiOn;
        string capturing;
        bool captureAdd;
        bool driverOk;
        int lastDriverCheck;
        bool installing;
        int installStart;

        static readonly Color Gray = Color.FromArgb(96, 104, 117);
        static readonly Color Green = Color.FromArgb(34, 139, 74);
        static readonly Color Amber = Color.FromArgb(200, 130, 0);
        static readonly Color Red = Color.FromArgb(190, 50, 50);

        public MainForm()
        {
            // Auto-scaling to the screen DPI runs on ResumeLayout.
            SuspendLayout();
            Text = "Keyboard Pad  -  virtual Xbox controller";
            Font = new Font("Segoe UI", 10f);
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(540, 720);
            MinimumSize = new Size(500, 620);

            string g;
            bool only;
            map = Config.Load(out g, out only);
            gameProcess = g;
            onlyInGame = only;

            BuildLayout();
            ResumeLayout(true);

            engine.SetMap(new Dictionary<string, int[]>(map));
            ApplyGameSetting(false);
            RefreshAllItems();

            driverOk = DriverInstalled();
            lastDriverCheck = Environment.TickCount;
            driverBar.Visible = !driverOk;

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 50;
            timer.Tick += delegate { UpdateUi(); };
            timer.Start();

            UpdateUi();
        }

        void BuildLayout()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.SuspendLayout();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 8;
            root.Padding = new Padding(12);
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));   // banner
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));   // start/stop
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // driver bar
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));   // hint
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // list
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // edit buttons
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // game filter
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));   // footer

            banner = new Label();
            banner.Dock = DockStyle.Fill;
            banner.Margin = new Padding(0);
            banner.TextAlign = ContentAlignment.MiddleCenter;
            banner.Font = new Font("Segoe UI", 15f, FontStyle.Bold);
            banner.ForeColor = Color.White;
            banner.BackColor = Gray;
            root.Controls.Add(banner, 0, 0);

            toggle = new Button();
            toggle.Dock = DockStyle.Fill;
            toggle.Margin = new Padding(0, 8, 0, 8);
            toggle.FlatStyle = FlatStyle.Flat;
            toggle.FlatAppearance.BorderSize = 0;
            toggle.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            toggle.ForeColor = Color.White;
            toggle.Cursor = Cursors.Hand;
            toggle.Click += delegate { Toggle(); };
            root.Controls.Add(toggle, 0, 1);

            driverBar = new FlowLayoutPanel();
            driverBar.Dock = DockStyle.Fill;
            driverBar.AutoSize = true;
            driverBar.WrapContents = true;
            driverBar.BackColor = Color.FromArgb(255, 243, 205);
            driverBar.Margin = new Padding(0, 0, 0, 8);
            driverBar.Padding = new Padding(8);
            driverLabel = new Label();
            driverLabel.AutoSize = true;
            driverLabel.Margin = new Padding(0, 6, 8, 0);
            driverLabel.Text = "The virtual controller driver is not installed on this PC.";
            installBtn = new Button();
            installBtn.AutoSize = true;
            installBtn.Text = "Install driver";
            installBtn.Padding = new Padding(8, 2, 8, 2);
            installBtn.Click += delegate { InstallDriver(); };
            driverBar.Controls.Add(driverLabel);
            driverBar.Controls.Add(installBtn);
            driverBar.Visible = false;
            root.Controls.Add(driverBar, 0, 2);

            Label hint = new Label();
            hint.Dock = DockStyle.Fill;
            hint.Margin = new Padding(0);
            hint.ForeColor = Color.FromArgb(90, 90, 90);
            hint.Text = "To change a key: double-click its row, then press the new key or click a mouse button. Esc cancels.";
            root.Controls.Add(hint, 0, 3);

            list = new ListView();
            list.Dock = DockStyle.Fill;
            list.View = View.Details;
            list.FullRowSelect = true;
            list.MultiSelect = false;
            list.HideSelection = false;
            list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            list.ShowItemToolTips = true;
            list.Margin = new Padding(0);
            list.Columns.Add("Controller button", 250);
            list.Columns.Add("Keyboard / mouse", 220);
            list.Resize += delegate { FitColumns(); };
            list.DoubleClick += delegate { BeginCapture(false); };
            list.MouseDown += ListMouseDown;
            for (int i = 0; i < Config.Rows.Length; i++)
            {
                ListViewItem it = new ListViewItem(Config.Rows[i][1]);
                it.SubItems.Add("");
                it.Tag = Config.Rows[i][0];
                list.Items.Add(it);
                items[Config.Rows[i][0]] = it;
            }
            root.Controls.Add(list, 0, 4);

            FlowLayoutPanel edit = new FlowLayoutPanel();
            edit.Dock = DockStyle.Fill;
            edit.AutoSize = true;
            edit.WrapContents = true;
            edit.Margin = new Padding(0, 8, 0, 0);
            edit.Controls.Add(MakeButton("Change key", delegate { BeginCapture(false); }));
            edit.Controls.Add(MakeButton("Add extra key", delegate { BeginCapture(true); }));
            edit.Controls.Add(MakeButton("Reset all", delegate { ResetAll(); }));
            edit.Controls.Add(MakeButton("Test pad...", delegate { OpenTestPanel(); }));
            root.Controls.Add(edit, 0, 5);

            FlowLayoutPanel filter = new FlowLayoutPanel();
            filter.Dock = DockStyle.Fill;
            filter.AutoSize = true;
            filter.WrapContents = true;
            filter.Margin = new Padding(0, 6, 0, 0);
            onlyChk = new CheckBox();
            onlyChk.AutoSize = true;
            onlyChk.Margin = new Padding(0, 6, 4, 0);
            onlyChk.Text = "Only work while this program is in front:";
            onlyChk.Checked = onlyInGame;
            onlyChk.CheckedChanged += delegate { ApplyGameSetting(true); };
            procBox = new ComboBox();
            procBox.DropDownStyle = ComboBoxStyle.DropDown;
            procBox.Width = 170;
            procBox.Text = gameProcess;
            procBox.DropDown += delegate { FillProcessList(); };
            procBox.TextChanged += delegate { ApplyGameSetting(false); };
            procBox.Leave += delegate { ApplyGameSetting(true); };
            procBox.SelectionChangeCommitted += delegate
            {
                // text updates after this event; apply on the next tick
                BeginInvoke(new MethodInvoker(delegate { ApplyGameSetting(true); }));
            };
            filter.Controls.Add(onlyChk);
            filter.Controls.Add(procBox);
            root.Controls.Add(filter, 0, 6);

            footer = new Label();
            footer.Dock = DockStyle.Fill;
            footer.Margin = new Padding(0, 6, 0, 0);
            footer.ForeColor = Color.FromArgb(90, 90, 90);
            footer.Text = "F9 = pause / resume     F12 = stop\nThese work while the game is in front. You can minimize this window.";
            root.Controls.Add(footer, 0, 7);

            Controls.Add(root);
            root.ResumeLayout(false);
            FitColumns();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // At 150% and above the scaled default height exceeds a 1080p screen.
            Rectangle area = Screen.FromControl(this).WorkingArea;
            if (MinimumSize.Height > area.Height) MinimumSize = new Size(MinimumSize.Width, area.Height);
            if (Height > area.Height)
            {
                Height = area.Height;
                Top = area.Top;
            }
        }

        Button MakeButton(string text, EventHandler handler)
        {
            Button b = new Button();
            b.Text = text;
            b.AutoSize = true;
            b.Padding = new Padding(6, 2, 6, 2);
            b.Click += handler;
            return b;
        }

        void FitColumns()
        {
            if (list == null || list.Columns.Count < 2) return;
            int w = list.ClientSize.Width;
            int first = (int)(w * 0.42);
            list.Columns[0].Width = first;
            list.Columns[1].Width = Math.Max(120, w - first);
        }

        // ---------- driver ----------
        static bool DriverInstalled()
        {
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\ViGEmBus"))
                {
                    return k != null;
                }
            }
            catch (Exception) { return false; }
        }

        void InstallDriver()
        {
            string bat = Path.Combine(baseDir, "Install-Driver.bat");
            if (!File.Exists(bat))
            {
                MessageBox.Show(this, "Install-Driver.bat is missing from the program folder.", "Keyboard Pad");
                return;
            }
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(bat);
                psi.UseShellExecute = true;
                psi.WorkingDirectory = baseDir;
                Process.Start(psi);
                installing = true;
                installStart = Environment.TickCount;
                driverLabel.Text = "Installing... approve the Windows prompt. This bar disappears when it is done.";
                installBtn.Enabled = false;
            }
            catch (Exception e)
            {
                MessageBox.Show(this, "Could not start the installer: " + e.Message, "Keyboard Pad");
            }
        }

        // ---------- start / stop ----------
        void Toggle()
        {
            if (uiOn) { StopEngine(); return; }

            engine.SetMap(new Dictionary<string, int[]>(map));
            ApplyGameSetting(false);
            string err = engine.Start(Path.Combine(baseDir, "ViGEmClient.dll"));
            if (err != null)
            {
                MessageBox.Show(this, err, "Keyboard Pad");
                return;
            }
            uiOn = true;
            UpdateUi();
        }

        void StopEngine()
        {
            engine.Stop();
            uiOn = false;
            ClearHighlights();
            UpdateUi();
        }

        // ---------- changing keys ----------
        void BeginCapture(bool add)
        {
            if (list.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "Click a row in the list first.", "Keyboard Pad");
                return;
            }
            ListViewItem it = list.SelectedItems[0];
            capturing = (string)it.Tag;
            captureAdd = add;
            it.SubItems[1].Text = add ? "Extra key...  (Esc = cancel)" : "Press a key...  (Esc = cancel)";
            list.Focus();
        }

        void EndCapture(int vk)
        {
            string name = capturing;
            capturing = null;
            if (name == null) return;
            if (vk > 0)
            {
                List<int> codes = new List<int>();
                int[] cur;
                if (captureAdd && map.TryGetValue(name, out cur)) codes.AddRange(cur);
                if (!codes.Contains(vk)) codes.Add(vk);
                map[name] = codes.ToArray();
                engine.SetMap(new Dictionary<string, int[]>(map));
                Config.Save(map, gameProcess, onlyInGame);
            }
            RefreshItem(name);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (capturing != null)
            {
                Keys k = keyData & Keys.KeyCode;
                if (k == Keys.Escape)
                {
                    EndCapture(0);
                    return true;
                }
                if (k != Keys.None)
                {
                    EndCapture((int)k);
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        void ListMouseDown(object sender, MouseEventArgs e)
        {
            if (capturing == null) return;
            int vk = 0;
            if (e.Button == MouseButtons.Left) vk = 1;
            else if (e.Button == MouseButtons.Right) vk = 2;
            else if (e.Button == MouseButtons.Middle) vk = 4;
            else if (e.Button == MouseButtons.XButton1) vk = 5;
            else if (e.Button == MouseButtons.XButton2) vk = 6;
            if (vk == 0) return;

            string name = capturing;
            EndCapture(vk);
            // The ListView selects the clicked row after this handler returns.
            BeginInvoke(new MethodInvoker(delegate
            {
                ListViewItem it;
                if (items.TryGetValue(name, out it))
                {
                    it.Selected = true;
                    it.Focused = true;
                }
            }));
        }

        void ResetAll()
        {
            if (MessageBox.Show(this, "Put every key back to the default layout?", "Keyboard Pad",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            capturing = null;
            map = Config.Defaults();
            engine.SetMap(new Dictionary<string, int[]>(map));
            Config.Save(map, gameProcess, onlyInGame);
            RefreshAllItems();
        }

        void RefreshItem(string name)
        {
            ListViewItem it;
            if (!items.TryGetValue(name, out it)) return;
            int[] codes;
            if (!map.TryGetValue(name, out codes)) codes = new int[0];
            it.SubItems[1].Text = Config.Describe(codes);
            it.ToolTipText = it.Text + ":  " + it.SubItems[1].Text;
        }

        void RefreshAllItems()
        {
            foreach (string name in items.Keys) RefreshItem(name);
        }

        // ---------- game filter ----------
        void FillProcessList()
        {
            string current = procBox.Text;
            List<string> names = new List<string>();
            Process[] all = Process.GetProcesses();
            for (int i = 0; i < all.Length; i++)
            {
                try
                {
                    if (all[i].MainWindowHandle != IntPtr.Zero && !names.Contains(all[i].ProcessName))
                        names.Add(all[i].ProcessName);
                }
                catch (Exception) { }
                finally { all[i].Dispose(); }
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            procBox.Items.Clear();
            procBox.Items.AddRange(names.ToArray());
            procBox.Text = current;
        }

        void ApplyGameSetting(bool save)
        {
            string g = procBox == null ? gameProcess : procBox.Text.Trim();
            if (g.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) g = g.Substring(0, g.Length - 4);
            gameProcess = g;
            onlyInGame = onlyChk == null ? onlyInGame : onlyChk.Checked;
            engine.GameProcess = (onlyInGame && g.Length > 0) ? g : "";
            if (save && map != null) Config.Save(map, gameProcess, onlyInGame);
        }

        // ---------- misc ----------
        static void OpenTestPanel()
        {
            try { Process.Start(new ProcessStartInfo("control.exe", "joy.cpl") { UseShellExecute = true }); }
            catch (Exception) { }
        }

        void ClearHighlights()
        {
            foreach (ListViewItem it in items.Values)
            {
                if (it.BackColor != SystemColors.Window) it.BackColor = SystemColors.Window;
            }
        }

        void SetBanner(string text, Color color)
        {
            if (banner.Text != text) banner.Text = text;
            if (banner.BackColor != color) banner.BackColor = color;
        }

        void UpdateUi()
        {
            // Stopped from outside (F12)?
            if (uiOn && !engine.Running)
            {
                StopEngine();
                return;
            }

            if (!uiOn)
            {
                SetBanner("OFF", Gray);
                if (toggle.Text != "Start virtual controller") toggle.Text = "Start virtual controller";
                if (toggle.BackColor != Green) toggle.BackColor = Green;
            }
            else
            {
                if (engine.Paused) SetBanner("PAUSED  -  press F9 to resume", Amber);
                else SetBanner("ON  -  virtual controller active", Green);
                if (toggle.Text != "Stop") toggle.Text = "Stop";
                if (toggle.BackColor != Red) toggle.BackColor = Red;
            }

            // driver check every ~1.5 s
            if (unchecked(Environment.TickCount - lastDriverCheck) > 1500)
            {
                lastDriverCheck = Environment.TickCount;
                bool ok = DriverInstalled();
                if (installing && unchecked(Environment.TickCount - installStart) > 90000) installing = false;
                if (ok != driverOk)
                {
                    driverOk = ok;
                    driverBar.Visible = !ok;
                    if (ok)
                    {
                        installing = false;
                    }
                }
                if (!ok && installing == false && !installBtn.Enabled)
                {
                    installBtn.Enabled = true;
                    driverLabel.Text = "The virtual controller driver is not installed on this PC.";
                }
            }

            // show which rows are pressed right now
            if (uiOn && !engine.Paused)
            {
                Color onColor = Color.FromArgb(198, 239, 206);
                foreach (KeyValuePair<string, ListViewItem> kv in items)
                {
                    Color want = engine.Down(kv.Key) ? onColor : SystemColors.Window;
                    if (kv.Value.BackColor != want) kv.Value.BackColor = want;
                }
            }
            else
            {
                ClearHighlights();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            timer.Stop();
            engine.Stop();
            Config.Save(map, gameProcess, onlyInGame);
            base.OnFormClosing(e);
        }
    }

    static class Program
    {
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "KeyboardPad_single_instance_9f3b", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("Keyboard Pad is already running.", "Keyboard Pad");
                    return;
                }
                try { SetProcessDPIAware(); } catch (Exception) { }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }
}
