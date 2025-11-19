// AGS-Billing - Minimal but functional Windows Forms App (single-file layout)
// NOTE: This single-file C# source creates UI programmatically (no Designer files).
// To use with .NET 6/7/8 WinForms:
// 1) Create a new Windows Forms App (C#) targeting .NET 8.0 in Visual Studio.
// 2) Add a new .cs file and paste this content (or replace Program.cs if preferred).
// 3) Install NuGet package: System.IO.Ports (see instructions below).
// 4) Build & Run.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AGSBilling
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Ensure data folder
            Directory.CreateDirectory("data");

            // Seed sample CSVs if missing
            DataManager.SeedIfMissing();

            Application.Run(new FormLogin());
        }
    }

    #region Models
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } // "Admin" or "Staff"
        public override string ToString() => Username + " (" + Role + ")";
    }

    public class FnbItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public int Stock { get; set; }
    }

    public class PricePackage
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // Prabayar, Paket, Pascabayar
        public double PricePerHour { get; set; }
        public double PackagePrice { get; set; }
        public int PackageDurationMinutes { get; set; }
    }

    public class OrderFnb
    {
        public string ItemId { get; set; }
        public string ItemName { get; set; }
        public int Qty { get; set; }
        public bool Delivered { get; set; }
        public double PriceEach { get; set; }
        public double Total => PriceEach * Qty;
    }

    public class BillingSession
    {
        public int UnitId { get; set; }
        public string Status { get; set; } = "IDLE";
        public PricePackage SelectedPrice { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? TargetEndTime { get; set; }
        public TimeSpan Elapsed => DateTime.Now - StartTime;
        public double CurrentRupiah { get; set; }
        public List<OrderFnb> Orders { get; set; } = new List<OrderFnb>();
        public string StaffUsername { get; set; }
    }

    public class TransactionLog
    {
        public DateTime Timestamp { get; set; }
        public int UnitId { get; set; }
        public string Staff { get; set; }
        public string SessionType { get; set; }
        public double DurationHours { get; set; }
        public double TotalPs { get; set; }
        public double TotalFnb { get; set; }
    }
    #endregion

    #region DataManager (CSV simple)
    public static class DataManager
    {
        public static string UsersFile = "data/Users.csv";
        public static string FnbFile = "data/FnbStock.csv";
        public static string PricesFile = "data/Prices.csv";
        public static string TransactionsFile = "data/Transactions.csv";

        public static List<User> LoadUsers()
        {
            var list = new List<User>();
            if (!File.Exists(UsersFile)) return list;
            foreach (var line in File.ReadAllLines(UsersFile))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(',');
                if (parts.Length < 3) continue;
                list.Add(new User { Username = parts[0], Password = parts[1], Role = parts[2] });
            }
            return list;
        }
        public static void SaveUsers(List<User> users)
        {
            var lines = users.Select(u => $"{u.Username},{u.Password},{u.Role}").ToArray();
            File.WriteAllLines(UsersFile, lines);
        }

        public static List<FnbItem> LoadFnb()
        {
            var list = new List<FnbItem>();
            if (!File.Exists(FnbFile)) return list;
            foreach (var line in File.ReadAllLines(FnbFile))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var p = line.Split(',');
                if (p.Length < 4) continue;
                list.Add(new FnbItem { Id = p[0], Name = p[1], Price = double.Parse(p[2]), Stock = int.Parse(p[3]) });
            }
            return list;
        }
        public static void SaveFnb(List<FnbItem> items)
        {
            var lines = items.Select(i => $"{i.Id},{i.Name},{i.Price},{i.Stock}").ToArray();
            File.WriteAllLines(FnbFile, lines);
        }

        public static List<PricePackage> LoadPrices()
        {
            var list = new List<PricePackage>();
            if (!File.Exists(PricesFile)) return list;
            foreach (var line in File.ReadAllLines(PricesFile))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var p = line.Split(',');
                if (p.Length < 6) continue;
                list.Add(new PricePackage
                {
                    Id = p[0],
                    Name = p[1],
                    Type = p[2],
                    PricePerHour = double.Parse(p[3]),
                    PackagePrice = double.Parse(p[4]),
                    PackageDurationMinutes = int.Parse(p[5])
                });
            }
            return list;
        }
        public static void SavePrices(List<PricePackage> list)
        {
            var lines = list.Select(x => $"{x.Id},{x.Name},{x.Type},{x.PricePerHour},{x.PackagePrice},{x.PackageDurationMinutes}").ToArray();
            File.WriteAllLines(PricesFile, lines);
        }

        public static void AppendTransaction(TransactionLog log)
        {
            var line = $"{log.Timestamp:O},{log.UnitId},{log.Staff},{log.SessionType},{log.DurationHours},{log.TotalPs},{log.TotalFnb}";
            File.AppendAllText(TransactionsFile, line + Environment.NewLine);
        }

        public static List<TransactionLog> LoadTransactions()
        {
            var outp = new List<TransactionLog>();
            if (!File.Exists(TransactionsFile)) return outp;
            foreach (var line in File.ReadAllLines(TransactionsFile))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var p = line.Split(',');
                if (p.Length < 7) continue;
                outp.Add(new TransactionLog
                {
                    Timestamp = DateTime.Parse(p[0]),
                    UnitId = int.Parse(p[1]),
                    Staff = p[2],
                    SessionType = p[3],
                    DurationHours = double.Parse(p[4]),
                    TotalPs = double.Parse(p[5]),
                    TotalFnb = double.Parse(p[6])
                });
            }
            return outp;
        }

        public static void SeedIfMissing()
        {
            if (!File.Exists(UsersFile))
            {
                var lines = new[] { "admin,admin123,Admin", "staff1,pass123,Staff" };
                File.WriteAllLines(UsersFile, lines);
            }
            if (!File.Exists(FnbFile))
            {
                var lines = new[] { "FN001,Mie Instan,8000,20", "FN002,Teh Botol,5000,30" };
                File.WriteAllLines(FnbFile, lines);
            }
            if (!File.Exists(PricesFile))
            {
                var lines = new[] {
                    "PR001,Prabayar,Prabayar,5000,0,60", // PricePerHour used (5000 per hour)
                    "PK001,Paket 3 Jam,Paket,0,25000,180",
                    "PS001,Pascabayar,Pascabayar,6000,0,0"
                };
                File.WriteAllLines(PricesFile, lines);
            }
            if (!File.Exists(TransactionsFile)) File.WriteAllText(TransactionsFile, "");
        }
    }
    #endregion

    #region HardwareControl (Serial)
    public static class HardwareControl
    {
        static SerialPort port;
        public static string LastPortName { get; private set; }

        public static string[] GetPorts() => SerialPort.GetPortNames();

        public static bool Open(string portName, int baud = 115200)
        {
            try
            {
                if (port != null && port.IsOpen) port.Close();
                port = new SerialPort(portName, baud);
                port.Open();
                LastPortName = portName;
                return true;
            }
            catch { return false; }
        }
        public static void Close() { try { if (port != null && port.IsOpen) port.Close(); } catch { } }
        public static bool SendOn(int unit)
        {
            return Send($"ON:{unit}");
        }
        public static bool SendOff(int unit)
        {
            return Send($"OFF:{unit}");
        }
        static bool Send(string txt)
        {
            try { if (port != null && port.IsOpen) { port.WriteLine(txt); return true; } return false; }
            catch { return false; }
        }
    }
    #endregion

    #region Forms
    public class FormLogin : Form
    {
        TextBox txtUser, txtPass;
        Button btnLogin;
        public FormLogin()
        {
            Text = "AGS-Billing - Login";
            Width = 380; Height = 220; StartPosition = FormStartPosition.CenterScreen;

            var lblU = new Label { Left = 20, Top = 20, Text = "Username", AutoSize = true };
            txtUser = new TextBox { Left = 120, Top = 18, Width = 200 };
            var lblP = new Label { Left = 20, Top = 60, Text = "Password", AutoSize = true };
            txtPass = new TextBox { Left = 120, Top = 58, Width = 200, UseSystemPasswordChar = true };
            btnLogin = new Button { Left = 120, Top = 100, Width = 100, Text = "Login" };
            btnLogin.Click += BtnLogin_Click;

            Controls.AddRange(new Control[] { lblU, txtUser, lblP, txtPass, btnLogin });
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            var users = DataManager.LoadUsers();
            var u = users.FirstOrDefault(x => x.Username == txtUser.Text && x.Password == txtPass.Text);
            if (u == null) { MessageBox.Show("Username atau password salah."); return; }
            Hide();
            var main = new FormMain(u);
            main.FormClosed += (s, ev) => this.Close();
            main.Show();
        }
    }

    public class FormMain : Form
    {
        User currentUser;
        TabControl tabs;
        // Shared
        List<BillingSession> sessions = new List<BillingSession>();
        List<PricePackage> prices;
        List<FnbItem> fnbItems;
        System.Windows.Forms.Timer uiTimer = new System.Windows.Forms.Timer();

        // Controls for Staff tab
        DataGridView dgvUnits;
        ComboBox cbUnitSelect, cbPriceSelect;
        Button btnStart, btnStop, btnAddFnb, btnMove;
        ListBox lbOrders;
        ComboBox cbPorts;
        Button btnOpenPort, btnClosePort;

        // Admin tab controls
        DataGridView dgvUsers, dgvFnb, dgvPrices;
        Button btnAddUser, btnSaveUsers, btnSaveFnb, btnSavePrices;

        public FormMain(User user)
        {
            currentUser = user;
            Text = "AGS-Billing - " + currentUser.Username + " (" + currentUser.Role + ")";
            Width = 1000; Height = 700; StartPosition = FormStartPosition.CenterScreen;

            prices = DataManager.LoadPrices();
            fnbItems = DataManager.LoadFnb();

            // Create default sessions (10 units) or as many as Admin sets later
            for (int i = 1; i <= 10; i++) sessions.Add(new BillingSession { UnitId = i });

            tabs = new TabControl { Dock = DockStyle.Fill };
            var tabStaff = new TabPage("Staff") { AutoScroll = true };
            var tabAdmin = new TabPage("Admin") { AutoScroll = true };
            tabs.TabPages.Add(tabStaff);
            if (currentUser.Role == "Admin") tabs.TabPages.Add(tabAdmin);

            InitializeStaffTab(tabStaff);
            if (currentUser.Role == "Admin") InitializeAdminTab(tabAdmin);

            Controls.Add(tabs);

            uiTimer.Interval = 1000; uiTimer.Tick += UiTimer_Tick; uiTimer.Start();
        }

        private void UiTimer_Tick(object sender, EventArgs e)
        {
            RefreshUnitsGrid();
        }

        void InitializeStaffTab(TabPage tab)
        {
            var lblPort = new Label { Left = 10, Top = 10, Text = "Serial Port:" };
            cbPorts = new ComboBox { Left = 90, Top = 6, Width = 120 };
            cbPorts.Items.AddRange(HardwareControl.GetPorts());
            btnOpenPort = new Button { Left = 220, Top = 5, Width = 60, Text = "Open" };
            btnClosePort = new Button { Left = 290, Top = 5, Width = 60, Text = "Close" };
            btnOpenPort.Click += (s, e) => { if (cbPorts.SelectedItem != null) HardwareControl.Open(cbPorts.SelectedItem.ToString()); };
            btnClosePort.Click += (s, e) => { HardwareControl.Close(); };

            dgvUnits = new DataGridView { Left = 10, Top = 40, Width = 620, Height = 400, ReadOnly = true, AllowUserToAddRows = false };
            dgvUnits.SelectionMode = DataGridViewSelectionMode.FullRowSelect; dgvUnits.MultiSelect = false;

            cbUnitSelect = new ComboBox { Left = 650, Top = 40, Width = 120 };
            cbUnitSelect.Items.AddRange(Enumerable.Range(1, sessions.Count).Select(i => i.ToString()).ToArray());
            cbPriceSelect = new ComboBox { Left = 650, Top = 80, Width = 180 };
            foreach (var p in prices) cbPriceSelect.Items.Add(p.Id + " - " + p.Name + " (" + p.Type + ")");

            btnStart = new Button { Left = 650, Top = 120, Width = 80, Text = "Start" };
            btnStop = new Button { Left = 740, Top = 120, Width = 80, Text = "Stop" };
            btnMove = new Button { Left = 650, Top = 160, Width = 170, Text = "Pindah Billing" };
            btnAddFnb = new Button { Left = 650, Top = 200, Width = 170, Text = "Tambah F&B" };

            lbOrders = new ListBox { Left = 650, Top = 240, Width = 310, Height = 200 };

            btnStart.Click += BtnStart_Click;
            btnStop.Click += BtnStop_Click;
            btnAddFnb.Click += BtnAddFnb_Click;
            btnMove.Click += BtnMove_Click;

            tab.Controls.AddRange(new Control[] { lblPort, cbPorts, btnOpenPort, btnClosePort, dgvUnits, cbUnitSelect, cbPriceSelect, btnStart, btnStop, btnMove, btnAddFnb, lbOrders });

            RefreshUnitsGrid();
        }

        void InitializeAdminTab(TabPage tab)
        {
            dgvUsers = new DataGridView { Left = 10, Top = 10, Width = 480, Height = 200, AllowUserToAddRows = true };
            dgvFnb = new DataGridView { Left = 10, Top = 220, Width = 480, Height = 200, AllowUserToAddRows = true };
            dgvPrices = new DataGridView { Left = 10, Top = 430, Width = 480, Height = 200, AllowUserToAddRows = true };

            btnSaveUsers = new Button { Left = 500, Top = 10, Width = 120, Text = "Save Users" };
            btnSaveFnb = new Button { Left = 500, Top = 220, Width = 120, Text = "Save F&B" };
            btnSavePrices = new Button { Left = 500, Top = 430, Width = 120, Text = "Save Prices" };

            btnSaveUsers.Click += (s, e) => { SaveUsersFromGrid(); };
            btnSaveFnb.Click += (s, e) => { SaveFnbFromGrid(); };
            btnSavePrices.Click += (s, e) => { SavePricesFromGrid(); };

            // Fill data
            var users = DataManager.LoadUsers();
            dgvUsers.DataSource = new BindingSource { DataSource = users };
            var fnb = DataManager.LoadFnb();
            dgvFnb.DataSource = new BindingSource { DataSource = fnb };
            dgvPrices.DataSource = new BindingSource { DataSource = DataManager.LoadPrices() };

            tab.Controls.AddRange(new Control[] { dgvUsers, dgvFnb, dgvPrices, btnSaveUsers, btnSaveFnb, btnSavePrices });
        }

        void SaveUsersFromGrid()
        {
            try
            {
                var list = (dgvUsers.DataSource as BindingSource).List.Cast<User>().ToList();
                DataManager.SaveUsers(list);
                MessageBox.Show("Users saved.");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
        void SaveFnbFromGrid()
        {
            try
            {
                var list = (dgvFnb.DataSource as BindingSource).List.Cast<FnbItem>().ToList();
                DataManager.SaveFnb(list);
                MessageBox.Show("F&B saved.");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
        void SavePricesFromGrid()
        {
            try
            {
                var list = (dgvPrices.DataSource as BindingSource).List.Cast<PricePackage>().ToList();
                DataManager.SavePrices(list);
                MessageBox.Show("Prices saved.");
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        void RefreshUnitsGrid()
        {
            if (dgvUnits.InvokeRequired) { dgvUnits.Invoke(new Action(RefreshUnitsGrid)); return; }
            var table = new System.Data.DataTable();
            table.Columns.Add("Unit");
            table.Columns.Add("Status");
            table.Columns.Add("Tipe Harga");
            table.Columns.Add("Staff");
            table.Columns.Add("Sisa Waktu");
            table.Columns.Add("Rupiah Saat Ini");

            foreach (var s in sessions)
            {
                string sisa = "-";
                if (s.Status == "RUNNING_PRABAYAR" || s.Status == "RUNNING_PASCABAYAR")
                {
                    if (s.TargetEndTime.HasValue) sisa = (s.TargetEndTime.Value - DateTime.Now).TotalSeconds > 0 ? (s.TargetEndTime.Value - DateTime.Now).ToString(@"hh\:mm\:ss") : "00:00:00";
                    else sisa = s.Elapsed.ToString(@"hh\:mm\:ss");
                }
                table.Rows.Add(s.UnitId, s.Status, s.SelectedPrice?.Name ?? "-", s.StaffUsername ?? "-", sisa, s.CurrentRupiah.ToString("N0"));
            }

            dgvUnits.DataSource = table;

            // Update orders box if a unit selected
            if (int.TryParse(cbUnitSelect.Text, out int uid))
            {
                var session = sessions.FirstOrDefault(x => x.UnitId == uid);
                lbOrders.Items.Clear();
                if (session != null)
                {
                    foreach (var o in session.Orders) lbOrders.Items.Add($"{o.ItemName} x{o.Qty} - {(o.Delivered ? "Terkirim" : "Belum")} - Rp{o.Total:N0}");
                }
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(cbUnitSelect.Text, out int uid)) { MessageBox.Show("Pilih unit terlebih dahulu (kolom Unit)."); return; }
            if (cbPriceSelect.SelectedIndex < 0) { MessageBox.Show("Pilih tipe harga."); return; }
            var price = prices[cbPriceSelect.SelectedIndex];
            var session = sessions.First(x => x.UnitId == uid);
            if (session.Status != "IDLE") { MessageBox.Show("Unit saat ini tidak IDLE."); return; }

            session.SelectedPrice = price;
            session.Status = price.Type == "Prabayar" || price.Type == "Paket" ? "RUNNING_PRABAYAR" : "RUNNING_PASCABAYAR";
            session.StartTime = DateTime.Now;
            session.StaffUsername = currentUser.Username;
            if (price.Type == "Prabayar")
            {
                session.TargetEndTime = session.StartTime.AddMinutes(price.PackageDurationMinutes > 0 ? price.PackageDurationMinutes : 60);
                // For Prabayar PricePerHour used. If PackageDurationMinutes==0 default 60
            }
            else if (price.Type == "Paket")
            {
                session.TargetEndTime = session.StartTime.AddMinutes(price.PackageDurationMinutes);
                session.CurrentRupiah = price.PackagePrice;
            }
            else // Pascabayar
            {
                session.TargetEndTime = null; session.CurrentRupiah = 0;
            }

            // Send to hardware
            HardwareControl.SendOn(uid);
            RefreshUnitsGrid();
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(cbUnitSelect.Text, out int uid)) { MessageBox.Show("Pilih unit."); return; }
            var session = sessions.First(x => x.UnitId == uid);
            if (session.Status == "IDLE") { MessageBox.Show("Unit sudah IDLE."); return; }

            // Calculate totals
            double totalFnb = session.Orders.Sum(o => o.Total);
            double totalPs = 0;
            if (session.SelectedPrice != null)
            {
                if (session.SelectedPrice.Type == "Paket") totalPs = session.SelectedPrice.PackagePrice;
                else if (session.SelectedPrice.Type == "Prabayar")
                {
                    // compute minutes elapsed vs package duration
                    var usedMinutes = (session.TargetEndTime.HasValue ? (session.TargetEndTime.Value - session.StartTime).TotalMinutes : (DateTime.Now - session.StartTime).TotalMinutes);
                    // But to keep simple, if PricePerHour >0 compute hours from actual elapsed
                    usedMinutes = (DateTime.Now - session.StartTime).TotalMinutes;
                    totalPs = Math.Ceiling(usedMinutes / 60.0) * session.SelectedPrice.PricePerHour;
                }
                else // Pascabayar
                {
                    var usedMinutes = (DateTime.Now - session.StartTime).TotalMinutes;
                    totalPs = Math.Ceiling(usedMinutes / 60.0) * session.SelectedPrice.PricePerHour;
                }
            }

            var log = new TransactionLog { Timestamp = DateTime.Now, UnitId = session.UnitId, Staff = session.StaffUsername, SessionType = session.SelectedPrice?.Type ?? "-", DurationHours = (DateTime.Now - session.StartTime).TotalHours, TotalPs = totalPs, TotalFnb = totalFnb };
            DataManager.AppendTransaction(log);

            // Reset session
            session.Status = "IDLE"; session.SelectedPrice = null; session.StartTime = default; session.TargetEndTime = null; session.CurrentRupiah = 0; session.Orders.Clear(); session.StaffUsername = null;

            HardwareControl.SendOff(uid);
            RefreshUnitsGrid();
            MessageBox.Show($"Session stopped. PS: Rp{totalPs:N0}, F&B: Rp{totalFnb:N0}");
        }

        private void BtnAddFnb_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(cbUnitSelect.Text, out int uid)) { MessageBox.Show("Pilih unit."); return; }
            var session = sessions.First(x => x.UnitId == uid);
            if (session.Status == "IDLE") { MessageBox.Show("Unit belum aktif."); return; }

            // show a simple dialog to choose item
            var dlg = new FormSelectFnb(fnbItems);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var item = dlg.SelectedItem;
                var qty = dlg.Quantity;
                if (item.Stock < qty) { MessageBox.Show("Stok tidak cukup."); return; }
                item.Stock -= qty; DataManager.SaveFnb(fnbItems);
                session.Orders.Add(new OrderFnb { ItemId = item.Id, ItemName = item.Name, Qty = qty, Delivered = false, PriceEach = item.Price });
                RefreshUnitsGrid();
            }
        }

        private void BtnMove_Click(object sender, EventArgs e)
        {
            // prompt source unit and dest unit
            var dlg = new FormMoveBilling(sessions.Count);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                int src = dlg.Source, dst = dlg.Destination;
                var ssrc = sessions.First(x => x.UnitId == src);
                var sdst = sessions.First(x => x.UnitId == dst);
                if (ssrc.Status == "IDLE") { MessageBox.Show("Sumber tidak aktif."); return; }
                if (sdst.Status != "IDLE") { MessageBox.Show("Tujuan harus IDLE."); return; }
                sdst.Status = ssrc.Status; sdst.SelectedPrice = ssrc.SelectedPrice; sdst.StartTime = ssrc.StartTime; sdst.TargetEndTime = ssrc.TargetEndTime; sdst.CurrentRupiah = ssrc.CurrentRupiah; sdst.Orders = ssrc.Orders; sdst.StaffUsername = ssrc.StaffUsername;
                // clear source
                ssrc.Status = "IDLE"; ssrc.SelectedPrice = null; ssrc.StartTime = default; ssrc.TargetEndTime = null; ssrc.CurrentRupiah = 0; ssrc.Orders = new List<OrderFnb>(); ssrc.StaffUsername = null;
                RefreshUnitsGrid();
            }
        }
    }

    public class FormSelectFnb : Form
    {
        public FnbItem SelectedItem { get; private set; }
        public int Quantity { get; private set; } = 1;
        ListBox lb; NumericUpDown nud; Button ok;
        public FormSelectFnb(List<FnbItem> items)
        {
            Text = "Pilih F&B"; Width = 360; Height = 360; StartPosition = FormStartPosition.CenterParent;
            lb = new ListBox { Left = 10, Top = 10, Width = 320, Height = 220 };
            foreach (var i in items) lb.Items.Add(i);
            nud = new NumericUpDown { Left = 10, Top = 240, Width = 100, Minimum = 1, Maximum = 100, Value = 1 };
            ok = new Button { Left = 10, Top = 280, Width = 100, Text = "OK" };
            ok.Click += Ok_Click;
            Controls.AddRange(new Control[] { lb, nud, ok });
        }
        private void Ok_Click(object sender, EventArgs e)
        {
            if (lb.SelectedItem == null) { MessageBox.Show("Pilih item."); return; }
            SelectedItem = (FnbItem)lb.SelectedItem;
            Quantity = (int)nud.Value;
            DialogResult = DialogResult.OK; Close();
        }
    }

    public class FormMoveBilling : Form
    {
        public int Source { get; private set; }
        public int Destination { get; private set; }
        ComboBox cbs, cbd; Button ok;
        public FormMoveBilling(int maxUnits)
        {
            Text = "Pindah Billing"; Width = 300; Height = 180; StartPosition = FormStartPosition.CenterParent;
            cbs = new ComboBox { Left = 20, Top = 20, Width = 200 }; cbd = new ComboBox { Left = 20, Top = 60, Width = 200 };
            for (int i = 1; i <= maxUnits; i++) { cbs.Items.Add(i.ToString()); cbd.Items.Add(i.ToString()); }
            ok = new Button { Left = 20, Top = 100, Width = 80, Text = "OK" }; ok.Click += Ok_Click;
            Controls.AddRange(new Control[] { cbs, cbd, ok });
        }
        private void Ok_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(cbs.Text, out int s) || !int.TryParse(cbd.Text, out int d)) { MessageBox.Show("Pilih kedua unit."); return; }
            Source = s; Destination = d; DialogResult = DialogResult.OK; Close();
        }
    }
    #endregion
}
