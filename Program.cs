using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows.Forms;

class DropTarget : Form
{
    PictureBox picture;
    Label messageLabel;
    ContextMenuStrip menu;
    Timer messageTimer;

    Color normalColor = Color.FromArgb(20, 20, 35);
    Color dropColor = Color.FromArgb(100, 50, 180);

    const int ResizeBorder = 20;

    bool moving = false;
    bool resizing = false;

    Point moveStart;
    Point resizeStart;
    Rectangle resizeStartBounds;

    ResizeDirection resizeDirection = ResizeDirection.None;

    enum ResizeDirection
    {
        None,
        Left,
        Right,
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public DropTarget()
    {
        Text = "Open in Midjourney";

        Width = 120;
        Height = 120;
        MinimumSize = new Size(70, 70);

        FormBorderStyle = FormBorderStyle.None;

        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ShowInTaskbar = false;
        AllowDrop = true;
        BackColor = normalColor;

        var area = Screen.PrimaryScreen.WorkingArea;

        Left = area.Right - Width - 12;
        Top = area.Bottom - Height - 12;

        // -------------------------------------------------
        // ICON
        // -------------------------------------------------

        picture = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = normalColor,
            AllowDrop = true
        };

        LoadSharpIcon();

        Controls.Add(picture);

        // -------------------------------------------------
        // MESSAGE
        // -------------------------------------------------

        messageLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(35, 35, 55),
            Font = new Font(
                "Segoe UI",
                9,
                FontStyle.Bold),
            Visible = false,
            AutoSize = false,
            Padding = new Padding(8)
        };

        messageLabel.AllowDrop = true;

        Controls.Add(messageLabel);

        // Keep message above icon.
        messageLabel.BringToFront();

        // -------------------------------------------------
        // MESSAGE TIMER
        // -------------------------------------------------

        messageTimer = new Timer();
        messageTimer.Interval = 3000;
        messageTimer.Tick += (s, e) =>
        {
            messageTimer.Stop();
            HideMessage();
        };

        // -------------------------------------------------
        // RIGHT CLICK MENU
        // -------------------------------------------------

        menu = new ContextMenuStrip();

        ToolStripMenuItem exitItem =
            new ToolStripMenuItem(
                "Exit Open in Midjourney");

        exitItem.Click +=
            (s, e) => Application.Exit();

        menu.Items.Add(exitItem);

        ContextMenuStrip = menu;
        picture.ContextMenuStrip = menu;
        messageLabel.ContextMenuStrip = menu;

        // -------------------------------------------------
        // DRAG & DROP
        // -------------------------------------------------

        DragEnter += OnDragEnter;
        DragLeave += OnDragLeave;
        DragDrop += OnDragDrop;

        picture.DragEnter += OnDragEnter;
        picture.DragLeave += OnDragLeave;
        picture.DragDrop += OnDragDrop;

        messageLabel.DragEnter += OnDragEnter;
        messageLabel.DragLeave += OnDragLeave;
        messageLabel.DragDrop += OnDragDrop;

        // -------------------------------------------------
        // MOUSE
        // -------------------------------------------------

        picture.MouseDown += Picture_MouseDown;
        picture.MouseMove += Picture_MouseMove;
        picture.MouseUp += Picture_MouseUp;

        messageLabel.MouseDown += Picture_MouseDown;
        messageLabel.MouseMove += Picture_MouseMove;
        messageLabel.MouseUp += Picture_MouseUp;
    }

    // -----------------------------------------------------
    // LOAD SHARP ICON
    // -----------------------------------------------------

    void LoadSharpIcon()
    {
        string iconPath =
            Path.Combine(
                AppContext.BaseDirectory,
                "Open_in_Midjourney.ico");

        if (!File.Exists(iconPath))
            return;

        try
        {
            using (Icon sourceIcon =
                new Icon(iconPath))
            {
                using (Icon largeIcon =
                    new Icon(
                        sourceIcon,
                        new Size(128, 128)))
                {
                    using (Bitmap source =
                        largeIcon.ToBitmap())
                    {
                        Bitmap finalBitmap =
                            new Bitmap(
                                110,
                                110,
                                PixelFormat.Format32bppArgb);

                        using (Graphics g =
                            Graphics.FromImage(
                                finalBitmap))
                        {
                            g.CompositingMode =
                                CompositingMode.SourceCopy;

                            g.CompositingQuality =
                                CompositingQuality.HighQuality;

                            g.InterpolationMode =
                                InterpolationMode.HighQualityBicubic;

                            g.SmoothingMode =
                                SmoothingMode.HighQuality;

                            g.PixelOffsetMode =
                                PixelOffsetMode.HighQuality;

                            g.DrawImage(
                                source,
                                new Rectangle(
                                    5,
                                    5,
                                    100,
                                    100));
                        }

                        picture.Image =
                            finalBitmap;
                    }
                }
            }
        }
        catch
        {
        }
    }

    // -----------------------------------------------------
    // SHOW ERROR MESSAGE
    // -----------------------------------------------------

    void ShowMessage(string text)
    {
        picture.Visible = false;

        messageLabel.Text = text;
        messageLabel.Visible = true;
        messageLabel.BringToFront();

        messageTimer.Stop();
        messageTimer.Start();
    }

    // -----------------------------------------------------
    // HIDE ERROR MESSAGE
    // -----------------------------------------------------

    void HideMessage()
    {
        messageTimer.Stop();

        messageLabel.Visible = false;
        picture.Visible = true;

        picture.BringToFront();
    }

    // -----------------------------------------------------
    // MOUSE DOWN
    // -----------------------------------------------------

    void Picture_MouseDown(
        object sender,
        MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        resizeDirection =
            GetResizeDirection(e.Location);

        if (resizeDirection !=
            ResizeDirection.None)
        {
            resizing = true;

            resizeStart =
                PointToScreen(e.Location);

            resizeStartBounds =
                Bounds;

            return;
        }

        moving = true;

        moveStart =
            PointToScreen(e.Location);
    }

    // -----------------------------------------------------
    // MOUSE MOVE
    // -----------------------------------------------------

    void Picture_MouseMove(
        object sender,
        MouseEventArgs e)
    {
        Point screenPoint =
            PointToScreen(e.Location);

        if (resizing)
        {
            ResizeWindow(screenPoint);
            return;
        }

        if (moving)
        {
            int dx =
                screenPoint.X - moveStart.X;

            int dy =
                screenPoint.Y - moveStart.Y;

            Left += dx;
            Top += dy;

            moveStart =
                screenPoint;

            return;
        }

        UpdateCursor(e.Location);
    }

    // -----------------------------------------------------
    // MOUSE UP
    // -----------------------------------------------------

    void Picture_MouseUp(
        object sender,
        MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            moving = false;
            resizing = false;

            resizeDirection =
                ResizeDirection.None;

            Cursor =
                Cursors.Default;
        }
    }

    // -----------------------------------------------------
    // DETERMINE RESIZE DIRECTION
    // -----------------------------------------------------

    ResizeDirection GetResizeDirection(
        Point p)
    {
        bool left =
            p.X <= ResizeBorder;

        bool right =
            p.X >= ClientSize.Width -
                   ResizeBorder;

        bool top =
            p.Y <= ResizeBorder;

        bool bottom =
            p.Y >= ClientSize.Height -
                   ResizeBorder;

        if (left && top)
            return ResizeDirection.TopLeft;

        if (right && top)
            return ResizeDirection.TopRight;

        if (left && bottom)
            return ResizeDirection.BottomLeft;

        if (right && bottom)
            return ResizeDirection.BottomRight;

        if (left)
            return ResizeDirection.Left;

        if (right)
            return ResizeDirection.Right;

        if (top)
            return ResizeDirection.Top;

        if (bottom)
            return ResizeDirection.Bottom;

        return ResizeDirection.None;
    }

    // -----------------------------------------------------
    // UPDATE CURSOR
    // -----------------------------------------------------

    void UpdateCursor(Point p)
    {
        switch (GetResizeDirection(p))
        {
            case ResizeDirection.Left:
            case ResizeDirection.Right:
                Cursor = Cursors.SizeWE;
                break;

            case ResizeDirection.Top:
            case ResizeDirection.Bottom:
                Cursor = Cursors.SizeNS;
                break;

            case ResizeDirection.TopLeft:
            case ResizeDirection.BottomRight:
                Cursor = Cursors.SizeNWSE;
                break;

            case ResizeDirection.TopRight:
            case ResizeDirection.BottomLeft:
                Cursor = Cursors.SizeNESW;
                break;

            default:
                Cursor = Cursors.Default;
                break;
        }
    }

    // -----------------------------------------------------
    // RESIZE WINDOW
    // -----------------------------------------------------

    void ResizeWindow(Point current)
    {
        int dx =
            current.X - resizeStart.X;

        int dy =
            current.Y - resizeStart.Y;

        Rectangle r =
            resizeStartBounds;

        switch (resizeDirection)
        {
            case ResizeDirection.Left:
                r.X += dx;
                r.Width -= dx;
                break;

            case ResizeDirection.Right:
                r.Width += dx;
                break;

            case ResizeDirection.Top:
                r.Y += dy;
                r.Height -= dy;
                break;

            case ResizeDirection.Bottom:
                r.Height += dy;
                break;

            case ResizeDirection.TopLeft:
                r.X += dx;
                r.Width -= dx;
                r.Y += dy;
                r.Height -= dy;
                break;

            case ResizeDirection.TopRight:
                r.Width += dx;
                r.Y += dy;
                r.Height -= dy;
                break;

            case ResizeDirection.BottomLeft:
                r.X += dx;
                r.Width -= dx;
                r.Height += dy;
                break;

            case ResizeDirection.BottomRight:
                r.Width += dx;
                r.Height += dy;
                break;
        }

        if (r.Width < MinimumSize.Width)
        {
            if (resizeDirection ==
                ResizeDirection.Left ||
                resizeDirection ==
                ResizeDirection.TopLeft ||
                resizeDirection ==
                ResizeDirection.BottomLeft)
            {
                r.X =
                    resizeStartBounds.Right -
                    MinimumSize.Width;
            }

            r.Width =
                MinimumSize.Width;
        }

        if (r.Height < MinimumSize.Height)
        {
            if (resizeDirection ==
                ResizeDirection.Top ||
                resizeDirection ==
                ResizeDirection.TopLeft ||
                resizeDirection ==
                ResizeDirection.TopRight)
            {
                r.Y =
                    resizeStartBounds.Bottom -
                    MinimumSize.Height;
            }

            r.Height =
                MinimumSize.Height;
        }

        Bounds = r;
    }

    // -----------------------------------------------------
    // DRAG ENTER
    // -----------------------------------------------------

    void OnDragEnter(
        object sender,
        DragEventArgs e)
    {
        if (e.Data.GetDataPresent(
            DataFormats.FileDrop))
        {
            e.Effect =
                DragDropEffects.Copy;

            BackColor = dropColor;
            picture.BackColor = dropColor;
        }
    }

    // -----------------------------------------------------
    // DRAG LEAVE
    // -----------------------------------------------------

    void OnDragLeave(
        object sender,
        EventArgs e)
    {
        BackColor = normalColor;
        picture.BackColor = normalColor;
    }

    // -----------------------------------------------------
    // DROP
    // -----------------------------------------------------

    void OnDragDrop(
        object sender,
        DragEventArgs e)
    {
        string[] files =
            (string[])e.Data.GetData(
                DataFormats.FileDrop);

        bool foundJob = false;

        foreach (string file in files)
        {
            string id =
                GetJobId(file);

            if (!String.IsNullOrEmpty(id))
            {
                foundJob = true;

                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName =
                            "https://www.midjourney.com/jobs/"
                            + id,

                        UseShellExecute = true
                    });
            }
        }

        BackColor = normalColor;
        picture.BackColor = normalColor;

        if (!foundJob)
        {
            ShowMessage(
                "No Midjourney Job ID found in image metadata");
        }
    }

    // -----------------------------------------------------
    // FIND MIDJOURNEY JOB ID
    // -----------------------------------------------------

    string GetJobId(string path)
    {
        try
        {
            byte[] bytes =
                File.ReadAllBytes(path);

            string pattern =
                @"(?is)Job\s*ID\s*:\s*([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})";

            Encoding[] encodings =
            {
                Encoding.UTF8,
                Encoding.Unicode,
                Encoding.BigEndianUnicode
            };

            foreach (
                Encoding encoding
                in encodings)
            {
                string text =
                    encoding.GetString(bytes);

                Match match =
                    Regex.Match(
                        text,
                        pattern,
                        RegexOptions.IgnoreCase);

                if (match.Success)
                    return match.Groups[1].Value;
            }
        }
        catch
        {
        }

        return null;
    }

    // -----------------------------------------------------
    // START
    // -----------------------------------------------------

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();

        Application.SetCompatibleTextRenderingDefault(
            false);

        Application.Run(
            new DropTarget());
    }
}