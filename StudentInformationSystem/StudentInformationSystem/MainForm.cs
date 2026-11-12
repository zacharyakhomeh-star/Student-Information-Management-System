using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace StudentInformationSystem
{
    //=====================================================================
    //SIMS - Student Information Management System (C#, Windows Forms)
    //quick paper map for view guidance and for consistent structural coding
    //also para madali ang pag hahanap natin sa code when needed sa paper - can press Ctrl+F and search the tag to copy code fast
    //code contains the following;
    //PAPER 1.3 Main features - Login, Student Sign Up, Dashboard counts,
    //                          Student Records (add / edit / delete),
    //                          Pending Registrations, Manage Teachers,
    //                          Teacher Sign Up (one-time invite codes)
    //Paper 2.2 Problem solving - one #region per feature (see region names)
    //Paper 3   Program flow = Welcome -> Login or Sign Up -> Dashboard -> pages from the menu
    //Paper 4   Program code = this whole file (MainForm.cs)
    //Paper 5.1 Conditionals and Loops
    //Paper 5.2 Functions (methods with parameters / return values)
    //Paper 5.3 GUI Programming (controls + click events)
    //Paper 5.4 Classes and Collections (List<T>, LINQ)
    //Paper 6.2 Libraries: none needed (windows Forms is built in).
    //Paper 7.2 Planned improvement: MySQL database (draft only)

    // accounts and records are stored in memory only (they reset when the app closes).
    // MySQL planned database though not connected yet.
    //=====================================================================

    public class MainForm : Form
    {
        #region 1. data and setup Paper 4 / 5.4
        private readonly Color Brown = Color.FromArgb(91, 58, 41);
        private readonly Color DarkBrown = Color.FromArgb(55, 37, 29);
        private readonly Color Blue = Color.FromArgb(52, 105, 180);
        private readonly Color Yellow = Color.FromArgb(245, 190, 60);
        private readonly Color Cream = Color.FromArgb(248, 246, 243);
        private readonly Color White = Color.White;

        //Paper 5.4 Collections: List<T> keeps all accounts and student records in memory
        private readonly List<Account> accounts = new List<Account>();
        private readonly List<StudentRecord> students = new List<StudentRecord>();
        private readonly List<InviteCode> inviteCodes = new List<InviteCode>();
        private Account currentAccount;
        private Panel header, sidebar, content;
        private Label pageTitle, pageDescription;
        private DataGridView table;
        private bool menuOpen;

        //temporary first administrator, can be anything, but need na need na same yung code mo pati yung i-iinput mo sa "login"
        private const string InitialAdminUsername = "admin";
        private const string InitialAdminPassword = "Admin@123";

        //MySQL placeholder for later setup, as no database connection is active yet
        private const string MySqlConnectionString =
            "server=localhost;user id=root;password=YOUR_PASSWORD;database=sims_db;";

        //Paper 5.4 Classes: each class groups the data of one thing (an account / a student record).
        private class Account
        {
            public string Username, Password, Role, StudentID, FullName, Course, VerificationDetail;
            public string Status; //Pending, Approved, Rejected
        }

        //one-time code the admin gives to a teacher so he/she can register on their own
        private class InviteCode
        {
            public string Code, UsedBy;   //UsedBy stays empty until a teacher registers with the code
            public bool Used;
        }

        private class StudentRecord
        {
            public string StudentID, FullName, Course, YearLevel, Status, Grades;
        }

        public MainForm()
        {
            Text = "Student Information Management System";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1100, 700);
            MinimumSize = new Size(900, 580);
            BackColor = Cream;
            Font = new Font("PT Root UI", 10);

            accounts.Add(new Account { Username = InitialAdminUsername, Password = InitialAdminPassword,
                Role = "Admin", FullName = "System Administrator", Status = "Approved" });

            // sample student record so thaat the student/teacher screens have a starting example
            students.Add(new StudentRecord { StudentID = "SCC-0001", FullName = "Sample Student",
                Course = "BSCS", YearLevel = "1st Year", Status = "Active", Grades = "Not yet encoded" });

            ShowWelcome();
        }
        #endregion

        #region 2. helper functions = Paper 5.2
        private Font F(float size, FontStyle style = FontStyle.Regular)
        {
            try { return new Font("PT Root UI", size, style); }
            catch { return new Font("Segoe UI", size, style); }
        }

        //Paper 5.2 function with parameters and a return value; reused for every button in the app
        private Button MakeButton(string text, int x, int y, int w, int h, Color back, Color fore)
        {
            Button b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h),
                BackColor = back, ForeColor = fore, Font = F(10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        /* Paper 5.2 function: places a text label so its baseline sits on the bottom edge of the logo,
                so "SIMS" stands on the ground next to the logo instead of floating at the top
                - note that this was a bug before, an annoying one  */
        private Label MakeGroundedLabel(string text, Font font, Color color, int x, PictureBox logo)
        {
            Label lbl = new Label { Text = text, Font = font, ForeColor = color, AutoSize = true };
            float descent = font.GetHeight() * font.FontFamily.GetCellDescent(font.Style) / font.FontFamily.GetLineSpacing(font.Style);
            lbl.Location = new Point(x, logo.Bottom - lbl.PreferredHeight + (int)Math.Round(descent));
            return lbl;
        }

        /* Paper 5.2 function: shows "SIMS" with "Student Information Management System" right below it
                the subtitle stands on the bottom edge of the logo and "SIMS" sits directly on top of it */
        private void AddBrandTitle(Panel parent, PictureBox logo, int x, float simsSize)
        {
            Label subtitle = MakeGroundedLabel("Student Information Management System", F(9), Color.Gainsboro, x, logo);
            Label sims = new Label { Text = "SIMS", Font = F(simsSize, FontStyle.Bold), ForeColor = White, AutoSize = true };
            float descent = sims.Font.GetHeight() * sims.Font.FontFamily.GetCellDescent(sims.Font.Style) / sims.Font.FontFamily.GetLineSpacing(sims.Font.Style);
            sims.Location = new Point(x, subtitle.Top - sims.PreferredHeight + (int)Math.Round(descent));
            parent.Controls.Add(sims);
            parent.Controls.Add(subtitle);
        }

        private void ClearScreen()
        {
            Controls.Clear();
            header = sidebar = content = null;
            table = null;
        }

        #endregion

        #region 3. welcome Screen = Paper 3
        private void ShowWelcome()
        {
            ClearScreen();
            Panel top = new Panel { Dock = DockStyle.Top, Height = 82, BackColor = DarkBrown };
            PictureBox welcomeLogo = LoadSchoolLogo(new Point(24, 12), new Size(58, 58));
            top.Controls.Add(welcomeLogo);

            //"SIMS" with the full system name below it, standing on the logo's bottom edge
            AddBrandTitle(top, welcomeLogo, 94, 24);
            Controls.Add(top);

            Panel center = new Panel { Size = new Size(570, 340), BackColor = White,
                Location = new Point((ClientSize.Width - 570) / 2, 175), Anchor = AnchorStyles.None };
            center.Controls.Add(new Label { Text = "Welcome to SIMS!", Font = F(25, FontStyle.Bold),
                ForeColor = Brown, Location = new Point(35, 35), AutoSize = true });
            center.Controls.Add(new Label { Text = "Your student information, organized in one place.",
                Font = F(10), ForeColor = Color.DimGray, Location = new Point(38, 82), AutoSize = true });
            Button login = MakeButton("Login", 38, 155, 220, 48, Blue, White);
            Button signup = MakeButton("Student Sign Up", 278, 155, 250, 48, Yellow, DarkBrown);

            //Paper 5.3 gui: cclick event handlers connect each button to an action.
            login.Click += (s, e) => ShowLogin();
            signup.Click += (s, e) => ShowSignup();
            center.Controls.Add(login); center.Controls.Add(signup);
            Button teacherSignup = MakeButton("Teacher Sign Up (Invite Code)", 38, 218, 490, 44, Brown, White);
            teacherSignup.Click += (s, e) => ShowTeacherSignup();
            center.Controls.Add(teacherSignup);
            center.Controls.Add(new Label { Text = "Teachers; ask the administrator for a one-time invite code.",
                Font = F(9), ForeColor = Color.DimGray, Location = new Point(38, 280), AutoSize = true });
            Controls.Add(center);
            Resize += WelcomeResize;
            CenterWelcome(center);
        }

        private void WelcomeResize(object sender, EventArgs e)
        {
            foreach (Control c in Controls)
                if (c is Panel && c.Height == 340) CenterWelcome((Panel)c);
        }
        private void CenterWelcome(Control c) { c.Left = (ClientSize.Width - c.Width) / 2; c.Top = (ClientSize.Height - c.Height) / 2 + 25; }

        #endregion

        #region 4. Login and Student Sign Up = Paper 1.3 / 2.2
        private void ShowLogin()
        {
            using (Form f = MakeDialog("Login", 430, 330))
            {
                TextBox user = AddInput(f, "Username", 65);
                TextBox pass = AddInput(f, "Password", 120); pass.UseSystemPasswordChar = true;
                Button go = MakeButton("Login", 235, 205, 145, 38, Blue, White);
                Button cancel = MakeButton("Back", 75, 205, 145, 38, Brown, White);
                cancel.Click += (s, e) => f.Close();
                //Paper 5.1 conditionals: checks username, password and approval before allowing login
                go.Click += (s, e) => {
                    Account a = accounts.FirstOrDefault(x => x.Username.Equals(user.Text.Trim(), StringComparison.OrdinalIgnoreCase)
                        && x.Password == pass.Text && x.Status == "Approved");
                    if (a == null) { MessageBox.Show("Login failed. Check your details or account approval status.", "SIMS"); return; }
                    currentAccount = a; f.DialogResult = DialogResult.OK; f.Close();
                };
                f.Controls.Add(go); f.Controls.Add(cancel);
                if (f.ShowDialog(this) == DialogResult.OK) ShowDashboard();
            }
        }

        private void ShowSignup()
        {
            using (Form f = MakeDialog("Student Registration", 470, 475))
            {
                TextBox id = AddInput(f, "Student ID", 58);
                TextBox name = AddInput(f, "Full Name", 108);
                TextBox course = AddInput(f, "Course / Program", 158);
                TextBox username = AddInput(f, "Create Username", 208);
                TextBox password = AddInput(f, "Create Password", 258); password.UseSystemPasswordChar = true;
                TextBox verify = AddInput(f, "Verification detail", 308);
                Label note = new Label { Text = "Enter your Student ID and a detail teachers can use to verify your identity (e.g. section).",
                    Font = F(8), ForeColor = Color.DimGray, Location = new Point(25, 355), Size = new Size(410, 34) };
                f.Controls.Add(note);
                Button submit = MakeButton("Submit for Approval", 240, 397, 195, 36, Blue, White);
                Button back = MakeButton("Back", 25, 397, 100, 36, Brown, White);
                back.Click += (s, e) => f.Close();
                submit.Click += (s, e) => {
                    if (new[] { id.Text, name.Text, course.Text, username.Text, password.Text, verify.Text }.Any(string.IsNullOrWhiteSpace))
                    { MessageBox.Show("Please complete all fields.", "SIMS"); return; }
                    if (accounts.Any(a => a.Username.Equals(username.Text.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                        accounts.Any(a => a.StudentID == id.Text.Trim() && a.Role == "Student" && a.Status != "Rejected"))
                    { MessageBox.Show("That username or Student ID is already registered.", "SIMS"); return; }
                    accounts.Add(new Account { Username = username.Text.Trim(), Password = password.Text,
                        Role = "Student", StudentID = id.Text.Trim(), FullName = name.Text.Trim(),
                        Course = course.Text.Trim(), VerificationDetail = verify.Text.Trim(), Status = "Pending" });
                    MessageBox.Show("Registration submitted. A teacher must approve your account before you can log in.\n\nVerification detail: " + verify.Text.Trim(), "SIMS");
                    f.Close();
                };
                f.Controls.Add(submit); f.Controls.Add(back); f.ShowDialog(this);
            }
        }

        //teacher registers on their own, but only with a one time invite code from the admin
        private void ShowTeacherSignup()
        {
            using (Form f = MakeDialog("Teacher Registration", 470, 400))
            {
                TextBox code = AddInput(f, "Invite Code", 65);
                TextBox name = AddInput(f, "Full Name", 115);
                TextBox username = AddInput(f, "Create Username", 165);
                TextBox password = AddInput(f, "Create Password", 215); password.UseSystemPasswordChar = true;
                f.Controls.Add(new Label { Text = "Ask the administrator for your one-time invite code.",
                    Font = F(8), ForeColor = Color.DimGray, Location = new Point(25, 260), Size = new Size(410, 20) });
                Button submit = MakeButton("Register", 240, 300, 140, 36, Blue, White);
                Button back = MakeButton("Back", 25, 300, 100, 36, Brown, White);
                back.Click += (s, e) => f.Close();
                submit.Click += (s, e) => {
                    if (new[] { code.Text, name.Text, username.Text, password.Text }.Any(string.IsNullOrWhiteSpace))
                    { MessageBox.Show("Please complete all fields.", "SIMS"); return; }

                    //Paper 5.1 conditionals: the code must exist, must not be used yet, and the username must be free
                    InviteCode invite = inviteCodes.FirstOrDefault(c => c.Code.Equals(code.Text.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (invite == null)
                    { MessageBox.Show("Invalid invite code. Check it or ask the administrator.", "SIMS"); return; }
                    if (invite.Used)
                    { MessageBox.Show("This invite code was already used. Ask the administrator for a new one.", "SIMS"); return; }
                    if (accounts.Any(a => a.Username.Equals(username.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
                    { MessageBox.Show("That username is already taken.", "SIMS"); return; }

                    accounts.Add(new Account { Username = username.Text.Trim(), Password = password.Text,
                        Role = "Teacher", FullName = name.Text.Trim(), Status = "Approved" });
                    invite.Used = true; //one time: the code cannot be used again
                    invite.UsedBy = name.Text.Trim();
                    MessageBox.Show("Registration successful! You can now log in.", "SIMS");
                    f.Close();
                };
                f.Controls.Add(submit); f.Controls.Add(back); f.ShowDialog(this);
            }
        }

        private Form MakeDialog(string title, int width, int height)
        {
            Form f = new Form { Text = title, Size = new Size(width, height), StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false,
                BackColor = Cream, Font = F(9) };
            f.Controls.Add(new Label { Text = title, Font = F(18, FontStyle.Bold), ForeColor = Brown,
                Location = new Point(25, 18), AutoSize = true });
            return f;
        }

        private TextBox AddInput(Form f, string label, int y)
        {
            f.Controls.Add(new Label { Text = label, Location = new Point(25, y), Size = new Size(150, 25), Font = F(9, FontStyle.Bold) });
            TextBox box = new TextBox { Location = new Point(180, y - 4), Size = new Size(200, 27), Font = F(9) };
            f.Controls.Add(box); return box;
        }

        #endregion

        #region 5. Dashboard  [PAPER 1.3 / 5.3]
        private void ShowDashboard()
        {
            ClearScreen();
            header = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = DarkBrown };
            Button hamburger = MakeButton("☰", 12, 12, 48, 48, DarkBrown, White);
            hamburger.Font = F(20); hamburger.Click += (s, e) => { menuOpen = !menuOpen; UpdateLayout(); };
            header.Controls.Add(hamburger);
            PictureBox headerLogo = LoadSchoolLogo(new Point(68, 9), new Size(56, 56));
            header.Controls.Add(headerLogo);
            AddBrandTitle(header, headerLogo, 132, 21);
            Label accountLabel = new Label { Text = currentAccount.FullName + "  |  " + currentAccount.Role,
                Font = F(9), ForeColor = Color.Gainsboro, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(ClientSize.Width - 300, 29) };
            header.Controls.Add(accountLabel);
            Button headerLogout = MakeButton("Log Out", 0, 17, 92, 40, Brown, White);
            headerLogout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            headerLogout.Location = new Point(ClientSize.Width - 108, 17);
            headerLogout.Click += (s, e) => Logout();
            header.Controls.Add(headerLogout);
            sidebar = new Panel { Width = 0, BackColor = DarkBrown, Location = new Point(0, 76), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left };
            AddNav("Dashboard", 25, () => ShowDashboardPage());
            if (currentAccount.Role != "Student")
            {
                AddNav("Students", 80, () => ShowStudentsPage());
                AddNav("Pending Registrations", 135, () => ShowPendingPage());
                if (currentAccount.Role == "Admin") AddNav("Manage Teachers", 190, () => ShowTeachersPage());
            }
            Button logout = MakeButton("↪  Log Out", 15, ClientSize.Height - 70, 190, 42, DarkBrown, Color.FromArgb(240, 200, 190));
            logout.Anchor = AnchorStyles.Left | AnchorStyles.Bottom; logout.Click += (s, e) => Logout();
            sidebar.Controls.Add(logout);
            content = new Panel { Location = new Point(0, 76), Size = new Size(ClientSize.Width, ClientSize.Height - 76),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Cream, AutoScroll = true };
            Controls.Add(content); Controls.Add(sidebar); Controls.Add(header);
            Resize += DashboardResize;
            ShowDashboardPage(); UpdateLayout();
        }

        private void AddNav(string text, int y, Action action)
        {
            string glyph = text == "Dashboard" ? "▦" : text == "Students" ? "♙" :
                text == "Pending Registrations" ? "◷" : text == "Manage Teachers" ? "♧" : "•";
            Button b = MakeButton(glyph + "   " + text, 0, y, 220, 45, DarkBrown, White);
            b.TextAlign = ContentAlignment.MiddleLeft; b.Padding = new Padding(22, 0, 0, 0);
            b.Click += (s, e) => action(); sidebar.Controls.Add(b);
        }

        private void DashboardResize(object sender, EventArgs e) { UpdateLayout(); }
        private void UpdateLayout()
        {
            if (sidebar == null || content == null) return;
            sidebar.Width = menuOpen ? 220 : 0;
            sidebar.Height = ClientSize.Height - 76;
            content.Left = menuOpen ? 220 : 0;
            content.Width = ClientSize.Width - (menuOpen ? 220 : 0);
            content.Height = ClientSize.Height - 76;
            if (header != null)
            {
                foreach (Control c in header.Controls)
                    if (c is Button && c.Text == "Log Out") c.Left = ClientSize.Width - 108;
                    else if (c is Label && c.Text.Contains("  |  ")) c.Left = Math.Max(300, ClientSize.Width - 300);
            }
        }

        private void SetPage(string title, string description)
        {
            content.Controls.Clear();
            content.Controls.Add(new Label { Text = title, Font = F(23, FontStyle.Bold), ForeColor = Brown,
                Location = new Point(32, 28), AutoSize = true });
            content.Controls.Add(new Label { Text = description, Font = F(9), ForeColor = Color.DimGray,
                Location = new Point(35, 68), AutoSize = true });
        }

        private void ShowDashboardPage()
        {
            // greets whoever logs in, it will greet their own full name (admin, teacher, or student)
            SetPage("Welcome, " + currentAccount.FullName + "!", "Here's what's happening in your SIMS workspace today.");

            Panel welcome = new Panel { Location = new Point(35, 105), Size = new Size(760, 112), BackColor = White };
            welcome.Controls.Add(new Label { Text = "Good to see you again!", Font = F(17, FontStyle.Bold),
                ForeColor = Brown, Location = new Point(22, 18), AutoSize = true });
            welcome.Controls.Add(new Label { Text = currentAccount.Role == "Student" ? "Use the menu to get started." : "Use the shortcuts below or the menu to get started.", Font = F(10),
                ForeColor = Color.DimGray, Location = new Point(24, 58), AutoSize = true });
            content.Controls.Add(welcome);

            if (currentAccount.Role == "Student")
            {
                StudentRecord r = students.FirstOrDefault(x => x.StudentID == currentAccount.StudentID);
                string info = r == null ? "Your student record will appear after it is added by a teacher." :
                    "Student ID: " + r.StudentID + "\nName: " + r.FullName + "\nCourse: " + r.Course + "\nYear Level: " + r.YearLevel + "\nStatus: " + r.Status + "\nGrades: " + r.Grades;
                Panel details = new Panel { Location = new Point(35, 240), Size = new Size(500, 190), BackColor = White };
                details.Controls.Add(new Label { Text = "♙  MY STUDENT INFORMATION", Font = F(12, FontStyle.Bold), ForeColor = Brown,
                    Location = new Point(20, 18), AutoSize = true });
                details.Controls.Add(new Label { Text = info, Font = F(10), ForeColor = Color.FromArgb(55,45,40),
                    Location = new Point(22, 55), Size = new Size(450, 120) });
                content.Controls.Add(details);
            }
            else
            {
                AddStat("TOTAL STUDENTS", students.Count.ToString(), 35, 240, Yellow);
                AddStat("PENDING REGISTRATIONS", accounts.Count(a => a.Role == "Student" && a.Status == "Pending").ToString(), 300, 240, Blue);
            }

            /* big cards above show the numbers; these tiles are shortcuts with big icons
               this was also a bug before */
            if (currentAccount.Role != "Student")
            {
                int shortcutY = 395;
                AddShortcut("students", "Students", 35, shortcutY, () => ShowStudentsPage());
                AddShortcut("pending", "Pending Registrations", 245, shortcutY, () => ShowPendingPage());
                if (currentAccount.Role == "Admin")
                    AddShortcut("teachers", "Manage Teachers", 455, shortcutY, () => ShowTeachersPage());
            }
        }
        /* dashboard shortcut tile: a big icon on top, the page name below
                    clicking it opens that page */
        private void AddShortcut(string iconKind, string title, int x, int y, Action action)
        {
            Panel tile = new Panel { Location = new Point(x, y), Size = new Size(190, 112), BackColor = White, Cursor = Cursors.Hand };
            tile.Paint += (s, e) => {
                using (Pen border = new Pen(Color.FromArgb(225, 218, 210)))
                    e.Graphics.DrawRectangle(border, 0, 0, tile.Width - 1, tile.Height - 1);
                DrawTileIcon(e.Graphics, iconKind, new Rectangle((tile.Width - 56) / 2, 12, 56, 56));
            };
            Label name = new Label { Text = title, Font = F(10, FontStyle.Bold), ForeColor = Brown,
                Location = new Point(0, 76), Size = new Size(190, 28), TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand };
            EventHandler open = (s, e) => action();
            tile.Click += open; name.Click += open;
            tile.Controls.Add(name);
            content.Controls.Add(tile);
        }

        //Paper 5.3 gui: the icons are drawn with GDI+ shapes, so they stay sharp and need no image files
        //r is a 56 x 56 square. kind = "students", "pending" or "teachers"
        private void DrawTileIcon(Graphics g, string kind, Rectangle r)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush brown = new SolidBrush(Brown))
            using (SolidBrush gold = new SolidBrush(Yellow))
            using (Pen brownPen = new Pen(Brown, 3f))
            using (Pen goldPen = new Pen(Yellow, 3f))
            using (Pen whitePen = new Pen(White, 2f))
            {
                brownPen.StartCap = brownPen.EndCap = LineCap.Round;
                goldPen.StartCap = goldPen.EndCap = LineCap.Round;

                if (kind == "students")
                {
                    //two people: the one in the back is gold, the one in front is brown
                    g.FillEllipse(gold, r.X + 30, r.Y + 6, 16, 16);
                    g.FillPie(gold, r.X + 22, r.Y + 26, 32, 32, 180, 180);
                    g.FillEllipse(brown, r.X + 10, r.Y + 10, 18, 18);
                    g.FillPie(brown, r.X, r.Y + 32, 38, 38, 180, 180);
                    g.DrawPie(whitePen, r.X, r.Y + 32, 38, 38, 180, 180);
                }
                else if (kind == "pending")
                {
                    //clock
                    Rectangle face = new Rectangle(r.X + 4, r.Y + 4, 48, 48);
                    g.FillEllipse(gold, face);
                    using (Pen ring = new Pen(Brown, 4f)) g.DrawEllipse(ring, face);
                    Point mid = new Point(r.X + 28, r.Y + 28);
                    g.DrawLine(brownPen, mid, new Point(mid.X, mid.Y - 14));
                    g.DrawLine(brownPen, mid, new Point(mid.X + 10, mid.Y + 5));
                }
                else if (kind == "teachers")
                {
                    //classroom board on an easel
                    g.FillRectangle(brown, r.X + 2, r.Y + 4, 52, 30);
                    g.DrawLine(goldPen, r.X + 10, r.Y + 13, r.X + 34, r.Y + 13);
                    g.DrawLine(goldPen, r.X + 10, r.Y + 22, r.X + 26, r.Y + 22);
                    g.DrawLine(brownPen, r.X + 6, r.Y + 36, r.X + 50, r.Y + 36);
                    g.DrawLine(brownPen, r.X + 16, r.Y + 36, r.X + 10, r.Y + 52);
                    g.DrawLine(brownPen, r.X + 40, r.Y + 36, r.X + 46, r.Y + 52);
                }
            }
        }

        private void Logout()
        {
            DialogResult answer = MessageBox.Show("Are you sure you want to log out?", "Log Out",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer == DialogResult.Yes)
            {
                currentAccount = null;
                menuOpen = false;
                ShowWelcome();
            }
        }

        //loads the "Saint_Claire_College.png" and resizes it once to the exact pixel size of the box
        //with high-quality scaling, so the logo stays sharp instead of ut being blurry - bug din before
        private PictureBox LoadSchoolLogo(Point location, Size size)
        {
            PictureBox picture = new PictureBox { Location = location, Size = size,
                SizeMode = PictureBoxSizeMode.CenterImage, BackColor = Color.Transparent };
            string logoPath = FindLogoFile();
            if (logoPath != null)
            {
                try { using (Image img = Image.FromFile(logoPath)) picture.Image = MakeCrispLogo(img, size); }
                catch { }
            }
            if (picture.Image == null)
            {
                picture.BackColor = White;
                picture.Controls.Add(new Label { Text = "SCC", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                    Font = F(13, FontStyle.Bold), ForeColor = Brown });
            }
            return picture;
        }

        //looks for Saint_Claire_College.png next to the app, in the project folder, then in Downloads
        private string FindLogoFile()
        {
            const string fileName = "Saint_Claire_College.png";
            List<string> folders = new List<string> { AppContext.BaseDirectory, Application.StartupPath,
                Directory.GetCurrentDirectory(),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") };
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 5 && dir != null; i++) { folders.Add(dir.FullName); dir = dir.Parent; }
            foreach (string folder in folders)
            {
                string path = Path.Combine(folder, fileName);
                if (File.Exists(path)) return path;
            }
            return null;
        }

        //scales the image to fit the box (keeping its shape) using the best gdi+ quality settings
        private Bitmap MakeCrispLogo(Image source, Size box)
        {
            Bitmap result = new Bitmap(box.Width, box.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                float ratio = Math.Min((float)box.Width / source.Width, (float)box.Height / source.Height);
                int w = Math.Max(1, (int)(source.Width * ratio));
                int h = Math.Max(1, (int)(source.Height * ratio));
                Rectangle target = new Rectangle((box.Width - w) / 2, (box.Height - h) / 2, w, h);
                using (ImageAttributes attrs = new ImageAttributes())
                {
                    attrs.SetWrapMode(WrapMode.TileFlipXY); //avoids a faint halo on the edges
                    g.DrawImage(source, target, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attrs);
                }
            }
            return result;
        }

        private void AddStat(string title, string value, int x, int y, Color accent)
        {
            Panel p = new Panel { Location = new Point(x, y), Size = new Size(240, 125), BackColor = White };
            p.Controls.Add(new Panel { Location = new Point(0, 0), Size = new Size(6, 125), BackColor = accent });
            p.Controls.Add(new Label { Text = title, Font = F(9, FontStyle.Bold), ForeColor = Color.DimGray, Location = new Point(20, 18), AutoSize = true });
            p.Controls.Add(new Label { Text = value, Font = F(30, FontStyle.Bold), ForeColor = Brown, Location = new Point(20, 48), AutoSize = true });
            content.Controls.Add(p);
        }

        #endregion

        #region 6. student records (ddd/edit/delete) = Paper 1.3 / 2.2
        private void ShowStudentsPage()
        {
            SetPage("Students", "Add, edit, search, and manage student records");
            Button add = MakeButton("Add Student", 35, 100, 125, 35, Yellow, DarkBrown);
            add.Click += (s, e) => EditStudent(null); content.Controls.Add(add);
            table = MakeTable(new[] { "Student ID", "Name", "Course", "Year Level", "Status", "Grades" }, 35, 150, 790, 330);

            //Paper 5.1 looping: foreach fills the table with every student record
            foreach (StudentRecord r in students) table.Rows.Add(r.StudentID, r.FullName, r.Course, r.YearLevel, r.Status, r.Grades);
            content.Controls.Add(table);
            Button edit = MakeButton("Edit Selected", 35, 495, 130, 35, Blue, White);
            Button del = MakeButton("Delete Selected", 180, 495, 140, 35, Brown, White);
            edit.Click += (s, e) => { StudentRecord r = SelectedStudent(); if (r != null) EditStudent(r); };
            del.Click += (s, e) => { StudentRecord r = SelectedStudent(); if (r != null && MessageBox.Show("Delete this record?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes) { students.Remove(r); ShowStudentsPage(); } };
            content.Controls.Add(edit); content.Controls.Add(del);
        }

        private DataGridView MakeTable(string[] columns, int x, int y, int w, int h)
        {
            DataGridView g = new DataGridView { Location = new Point(x, y), Size = new Size(w, h),
                BackgroundColor = White, BorderStyle = BorderStyle.None, ReadOnly = true, AllowUserToAddRows = false,
                MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false };
            g.ColumnHeadersDefaultCellStyle.BackColor = Brown; g.ColumnHeadersDefaultCellStyle.ForeColor = White;
            //header keeps its own color when a row is clicked (this was the "Student ID" bar lighting up)

            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Brown;
            g.ColumnHeadersDefaultCellStyle.SelectionForeColor = White;
            g.EnableHeadersVisualStyles = false; g.DefaultCellStyle.Font = F(9);
            g.DefaultCellStyle.ForeColor = Brown;
            
            //the clicked row is now clearly highlighted (the old color was almost the same as the background), was also a bug
            g.DefaultCellStyle.SelectionBackColor = Yellow;
            g.DefaultCellStyle.SelectionForeColor = DarkBrown;
            g.RowsDefaultCellStyle.SelectionBackColor = Yellow;
            g.RowsDefaultCellStyle.SelectionForeColor = DarkBrown;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 248, 245);
            g.AlternatingRowsDefaultCellStyle.SelectionBackColor = Yellow;
            g.AlternatingRowsDefaultCellStyle.SelectionForeColor = DarkBrown;
            foreach (string c in columns) g.Columns.Add(c, c);

            //start with nothing selected, so Edit/Delete never act on a row you did not click
            bool cleared = false;
            g.Paint += (s, e) => {
                if (cleared) return;
                cleared = true;
                g.BeginInvoke(new Action(() => { if (!g.IsDisposed) { g.ClearSelection(); g.CurrentCell = null; } }));
            };
            return g;
        }

        private StudentRecord SelectedStudent()
        {
            if (table == null || table.SelectedRows.Count == 0) { MessageBox.Show("Select a student first."); return null; }
            string id = Convert.ToString(table.SelectedRows[0].Cells[0].Value);
            return students.FirstOrDefault(s => s.StudentID == id);
        }

        private void EditStudent(StudentRecord record)
        {
            using (Form f = MakeDialog(record == null ? "Add Student" : "Edit Student", 450, 390))
            {
                TextBox id = AddInput(f, "Student ID", 65), name = AddInput(f, "Full Name", 110), course = AddInput(f, "Course", 155), year = AddInput(f, "Year Level", 200), grade = AddInput(f, "Grades", 245);
                if (record != null) { id.Text = record.StudentID; id.Enabled = false; name.Text = record.FullName; course.Text = record.Course; year.Text = record.YearLevel; grade.Text = record.Grades; }
                Button save = MakeButton("Save", 260, 300, 130, 36, Blue, White);
                save.Click += (s, e) => {
                    if (new[] { id.Text, name.Text, course.Text, year.Text }.Any(string.IsNullOrWhiteSpace)) { MessageBox.Show("Complete the required fields."); return; }
                    if (record == null)
                    {
                        if (students.Any(x => x.StudentID.Equals(id.Text.Trim(), StringComparison.OrdinalIgnoreCase))) { MessageBox.Show("Student ID already exists."); return; }
                        record = new StudentRecord { StudentID = id.Text.Trim() }; students.Add(record);
                    }
                    record.FullName = name.Text.Trim(); record.Course = course.Text.Trim(); record.YearLevel = year.Text.Trim(); record.Grades = string.IsNullOrWhiteSpace(grade.Text) ? "Not yet encoded" : grade.Text.Trim(); record.Status = "Active";
                    f.DialogResult = DialogResult.OK; f.Close();
                };
                f.Controls.Add(save); if (f.ShowDialog(this) == DialogResult.OK) ShowStudentsPage();
            }
        }

        #endregion

        #region 7. pending registrations + Paper 1.3 / 2.2
        private void ShowPendingPage()
        {
            SetPage("Pending Registrations", "Review student sign-up requests and verify their details");
            table = MakeTable(new[] { "Student ID", "Name", "Course", "Verification Detail", "Username", "Status" }, 35, 115, 790, 300);
            foreach (Account a in accounts.Where(x => x.Role == "Student" && x.Status == "Pending"))
                table.Rows.Add(a.StudentID, a.FullName, a.Course, a.VerificationDetail, a.Username, a.Status);
            content.Controls.Add(table);
            Button approve = MakeButton("Approve", 35, 435, 110, 36, Blue, White);
            Button reject = MakeButton("Reject", 160, 435, 110, 36, Brown, White);
            approve.Click += (s, e) => ReviewRegistration(true);
            reject.Click += (s, e) => ReviewRegistration(false);
            content.Controls.Add(approve); content.Controls.Add(reject);
        }

        private void ReviewRegistration(bool approve)
        {
            if (table == null || table.SelectedRows.Count == 0) { MessageBox.Show("Select an application first."); return; }
            string username = Convert.ToString(table.SelectedRows[0].Cells[4].Value);
            Account a = accounts.FirstOrDefault(x => x.Username == username && x.Status == "Pending");
            if (a == null) return;
            a.Status = approve ? "Approved" : "Rejected";
            //Paper 5.1 conditionals: an approved student is copied into the student records
            if (approve && !students.Any(s => s.StudentID == a.StudentID))
                students.Add(new StudentRecord { StudentID = a.StudentID, FullName = a.FullName, Course = a.Course,
                    YearLevel = "Not assigned", Status = "Active", Grades = "Not yet encoded" });
            MessageBox.Show(approve ? "Student account approved." : "Registration rejected. The student must submit a new registration.", "SIMS");
            ShowPendingPage();
        }

        #endregion

        #region 8. manage teachers = Paper 1.3 / 2.2
        private void ShowTeachersPage()
        {
            SetPage("Manage Teachers", "Create teacher accounts, or generate invite codes so teachers can register themselves");
            table = MakeTable(new[] { "Full Name", "Username", "Role" }, 35, 115, 700, 300);
            foreach (Account a in accounts.Where(x => x.Role == "Teacher")) table.Rows.Add(a.FullName, a.Username, a.Role);
            content.Controls.Add(table);
            Button create = MakeButton("Create Teacher Account", 35, 435, 205, 38, Yellow, DarkBrown);
            create.Click += (s, e) => CreateTeacher(); content.Controls.Add(create);
            Button invites = MakeButton("Teacher Invite Codes", 255, 435, 190, 38, Blue, White);
            invites.Click += (s, e) => ShowInviteCodes(); content.Controls.Add(invites);
        }

        private void CreateTeacher()
        {
            using (Form f = MakeDialog("Create Teacher Account", 450, 330))
            {
                TextBox name = AddInput(f, "Full Name", 65), user = AddInput(f, "Username", 115), pass = AddInput(f, "Temporary Password", 165);
                Button save = MakeButton("Create Account", 245, 220, 145, 36, Blue, White);
                save.Click += (s, e) => {
                    if (new[] { name.Text, user.Text, pass.Text }.Any(string.IsNullOrWhiteSpace)) { MessageBox.Show("Complete all fields."); return; }
                    if (accounts.Any(a => a.Username.Equals(user.Text.Trim(), StringComparison.OrdinalIgnoreCase))) { MessageBox.Show("Username already exists."); return; }
                    accounts.Add(new Account { FullName = name.Text.Trim(), Username = user.Text.Trim(), Password = pass.Text, Role = "Teacher", Status = "Approved" });
                    MessageBox.Show("Teacher account created. Give the teacher their username and temporary password securely.");
                    f.DialogResult = DialogResult.OK; f.Close();
                };
                f.Controls.Add(save); if (f.ShowDialog(this) == DialogResult.OK) ShowTeachersPage();
            }
        }

        //admin window: lists every invite code, generates new ones, and copies one to the clipboard
        private void ShowInviteCodes()
        {
            using (Form f = MakeDialog("Teacher Invite Codes", 520, 470))
            {
                DataGridView grid = MakeTable(new[] { "Invite Code", "Status", "Used By" }, 25, 65, 455, 270);
                f.Controls.Add(grid);
                Action refresh = () => {
                    grid.Rows.Clear();
                    foreach (InviteCode c in inviteCodes) grid.Rows.Add(c.Code, c.Used ? "Used" : "Unused", c.UsedBy);
                };
                refresh();

                Button generate = MakeButton("Generate New Code", 25, 350, 185, 38, Yellow, DarkBrown);
                Button copy = MakeButton("Copy Selected", 225, 350, 150, 38, Blue, White);
                Button close = MakeButton("Close", 390, 350, 90, 38, Brown, White);
                close.Click += (s, e) => f.Close();

                generate.Click += (s, e) => {
                    InviteCode ic = new InviteCode { Code = NewInviteCode(), UsedBy = "" };
                    inviteCodes.Add(ic);
                    refresh();
                    grid.CurrentCell = grid.Rows[grid.Rows.Count - 1].Cells[0];   // highlight the new code
                    TryCopy(ic.Code);
                    MessageBox.Show("New invite code:\n\n" + ic.Code + "\n\nIt was copied to your clipboard. " +
                        "Give it to ONE teacher only; it stops working after it is used.", "SIMS");
                };
                copy.Click += (s, e) => {
                    if (grid.SelectedRows.Count == 0) { MessageBox.Show("Select a code first."); return; }
                    TryCopy(Convert.ToString(grid.SelectedRows[0].Cells[0].Value));
                    MessageBox.Show("Copied to clipboard.", "SIMS");
                };
                f.Controls.Add(generate); f.Controls.Add(copy); f.Controls.Add(close);
                f.ShowDialog(this);
            }
        }

        //makees a code like T-4K9Q. look-alike characters (0/O, 1/I) are left out so it is easy to read out loud
        //Paper 5.1 looping: a for loop builds the 4 characters; do/while repeats if the code already exists
        private string NewInviteCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            string code;
            do
            {
                char[] part = new char[4];
                for (int i = 0; i < part.Length; i++)
                    part[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
                code = "T-" + new string(part);
            } while (inviteCodes.Any(c => c.Code == code));
            return code;
        }

        private void TryCopy(string text)
        {
            try { Clipboard.SetText(text); } catch { }   //clipboard can be busy; the code is still shown on screen
        }
        #endregion

        #region 9. planned MySQL Database (DRAFT) Paper 7.2
        /* 
         plan: to save accounts and student records in a MySQL database instead of memory,
               so the data is still there after the app is closed 

        -SQL (planned table)-
         CREATE DATABASE sims_db;
         CREATE TABLE students (
             student_id VARCHAR(20)  PRIMARY KEY,
             full_name  VARCHAR(100) NOT NULL,
             course     VARCHAR(50),
             year_level VARCHAR(20),
             status     VARCHAR(20),
             grades     VARCHAR(100)
         );
        
         -- C# (planned method; the @ values are parameters, which keep user input safe) --
         private void SaveStudentToDatabase(StudentRecord s)
         {
             using (MySqlConnection conn = new MySqlConnection(MySqlConnectionString))
             {
                 conn.Open();
                 string sql = "INSERT INTO students (student_id, full_name, course, year_level, status, grades) " +
                              "VALUES (@id, @name, @course, @year, @status, @grades)";
                 using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                 {
                     cmd.Parameters.AddWithValue("@id", s.StudentID);
                     cmd.Parameters.AddWithValue("@name", s.FullName);
                     cmd.Parameters.AddWithValue("@course", s.Course);
                     cmd.Parameters.AddWithValue("@year", s.YearLevel);
                     cmd.Parameters.AddWithValue("@status", s.Status);
                     cmd.Parameters.AddWithValue("@grades", s.Grades);
                     cmd.ExecuteNonQuery();
                 }
             }
         } */
        #endregion
    }
}
