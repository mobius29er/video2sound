using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace VideoToSound
{
    public class MainForm : Form
    {
        private ListView list;
        private Panel listFrame;
        private EmptyState hint;
        private SkateHeader header;
        private SkateButton btnAdd, btnRemove, btnClear, btnConvert, btnCancel, btnBrowse;
        private SkateProgress progress;
        private Label status;
        private SkateGroup grpFormats, grpSave;
        private SkateRadio rdoSameFolder, rdoCustomFolder;
        private TextBox txtOutFolder;

        private FormatSpec[] specs;
        private SkateChip[] formatChecks;
        private ComboBox[] formatQualities;

        private Font fRow, fRowBold, fHead;

        private readonly Converter converter = new Converter();
        private Thread worker;
        private bool running;
        private string ffmpegPath;

        public MainForm(string[] initialFiles)
        {
            BuildUi();

            ffmpegPath = Ffmpeg.Locate();
            if (ffmpegPath == null)
            {
                status.Text = "ffmpeg not found. Put ffmpeg.exe beside this program, or install it on your PATH.";
                status.ForeColor = Skin.Danger;
                btnConvert.Enabled = false;
            }

            if (initialFiles != null) AddFiles(initialFiles);
        }

        // ---------------------------------------------------------------- UI

        private void BuildUi()
        {
            Text = "video2sound";
            ClientSize = new Size(880, 620);
            MinimumSize = new Size(780, 560);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Skin.Ink;
            Font = Skin.Body(8.25F);
            AllowDrop = true;
            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;

            fRow     = Skin.Body(8.25F);
            fRowBold = Skin.BodyBold(8.25F);
            fHead    = Skin.Heavy(7.5F);

            header = new SkateHeader();
            header.Dock = DockStyle.Top;
            Controls.Add(header);

            listFrame = new Panel();
            listFrame.Bounds = new Rectangle(14, 76, 578, 418);
            listFrame.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listFrame.BackColor = Skin.Line;
            listFrame.Padding = new Padding(2);
            Controls.Add(listFrame);

            list = new ListView();
            list.Dock = DockStyle.Fill;
            list.View = View.Details;
            list.FullRowSelect = true;
            list.HideSelection = false;
            list.AllowDrop = true;
            list.BorderStyle = BorderStyle.None;
            list.BackColor = Skin.Panel;
            list.ForeColor = Skin.Text;
            list.OwnerDraw = true;
            list.DrawColumnHeader += ListDrawHeader;
            list.DrawSubItem += ListDrawSubItem;
            list.DrawItem += delegate(object s, DrawListViewItemEventArgs e) { e.DrawDefault = false; };
            list.DragEnter += OnDragEnter;
            list.DragDrop += OnDragDrop;
            list.Columns.Add("File", 300);
            list.Columns.Add("Status", 270);
            listFrame.Controls.Add(list);

            hint = new EmptyState();
            Controls.Add(hint);
            hint.BringToFront();
            Resize += delegate { PositionEmptyState(); };
            PositionEmptyState();

            btnAdd = MakeButton("Add Files", new Rectangle(14, 504, 108, 30),
                                AnchorStyles.Bottom | AnchorStyles.Left, Skin.Acid);
            btnAdd.Click += delegate { PickFiles(); };

            btnRemove = MakeButton("Remove", new Rectangle(130, 504, 108, 30),
                                   AnchorStyles.Bottom | AnchorStyles.Left, Skin.Text);
            btnRemove.Click += delegate { RemoveSelected(); };

            btnClear = MakeButton("Clear", new Rectangle(246, 504, 92, 30),
                                  AnchorStyles.Bottom | AnchorStyles.Left, Skin.Text);
            btnClear.Click += delegate { list.Items.Clear(); UpdateHint(); };

            progress = new SkateProgress();
            progress.Bounds = new Rectangle(14, 546, 578, 22);
            progress.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(progress);

            status = new Label();
            status.Bounds = new Rectangle(14, 576, 578, 34);
            status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            status.ForeColor = Skin.Dim;
            status.Font = Skin.Body(8.25F);
            status.Text = "Add files, flip on the formats you want, then hit CONVERT.";
            Controls.Add(status);

            BuildFormatsBox();
            BuildSaveBox();

            btnConvert = MakeButton("Convert", new Rectangle(606, 498, 168, 40),
                                    AnchorStyles.Bottom | AnchorStyles.Right, Skin.Acid);
            btnConvert.Font = Skin.Heavy(11F);
            btnConvert.Click += delegate { StartConversion(); };

            btnCancel = MakeButton("Stop", new Rectangle(782, 498, 84, 40),
                                   AnchorStyles.Bottom | AnchorStyles.Right, Skin.Pink);
            btnCancel.Enabled = false;
            btnCancel.Click += delegate { CancelConversion(); };

            LoadBranding();

            FormClosing += delegate(object s, FormClosingEventArgs e)
            {
                if (running)
                {
                    DialogResult r = MessageBox.Show(this,
                        "A conversion is still running. Stop it and close?",
                        "video2sound", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (r != DialogResult.Yes) { e.Cancel = true; return; }
                    converter.Cancel();
                }
            };
        }

        /// <summary>
        /// Branding is embedded in the exe so nothing extra has to ship beside it.
        /// A mark.png or logo.png dropped next to the exe still wins, which makes
        /// trying a new logo a file copy rather than a rebuild.
        /// </summary>
        private void LoadBranding()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                string wordmark = Path.Combine(dir, "logo.png");
                if (File.Exists(wordmark)) header.Wordmark = Image.FromFile(wordmark);
            }
            catch { }

            try
            {
                string mark = Path.Combine(dir, "mark.png");
                header.Mark = File.Exists(mark) ? Image.FromFile(mark) : LoadImageResource("mark.png");
            }
            catch { }

            try
            {
                string sticker = Path.Combine(dir, "sticker.png");
                hint.Sticker = File.Exists(sticker)
                    ? Image.FromFile(sticker) : LoadImageResource("sticker.png");
            }
            catch { }

            try
            {
                Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico");
                if (s != null) using (s) Icon = new Icon(s);
            }
            catch { }
        }

        private static Image LoadImageResource(string name)
        {
            try
            {
                Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
                if (s == null) return null;
                using (s)
                {
                    // Image.FromStream needs the stream to outlive the image, so copy
                    // into a MemoryStream that the Image keeps alive.
                    MemoryStream ms = new MemoryStream();
                    byte[] buffer = new byte[8192];
                    int read;
                    while ((read = s.Read(buffer, 0, buffer.Length)) > 0) ms.Write(buffer, 0, read);
                    ms.Position = 0;
                    return Image.FromStream(ms);
                }
            }
            catch { return null; }
        }

        private SkateButton MakeButton(string text, Rectangle bounds, AnchorStyles anchor, Color accent)
        {
            SkateButton b = new SkateButton();
            b.Text = text;
            b.Bounds = bounds;
            b.Anchor = anchor;
            b.Accent = accent;
            Controls.Add(b);
            return b;
        }

        private void BuildFormatsBox()
        {
            specs = FormatSpec.All();
            formatChecks = new SkateChip[specs.Length];
            formatQualities = new ComboBox[specs.Length];

            grpFormats = new SkateGroup();
            grpFormats.Title = "Output formats";
            grpFormats.Accent = Skin.Acid;
            grpFormats.Bounds = new Rectangle(606, 76, 260, 214);
            grpFormats.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(grpFormats);

            for (int i = 0; i < specs.Length; i++)
            {
                FormatSpec spec = specs[i];

                SkateChip chip = new SkateChip();
                chip.Text = spec.Label;
                chip.Bounds = new Rectangle(12, 34 + i * 34, 76, 28);
                chip.Checked = spec.CheckedByDefault;
                grpFormats.Controls.Add(chip);
                formatChecks[i] = chip;

                SkateCombo cmb = new SkateCombo();
                cmb.Bounds = new Rectangle(96, 35 + i * 34, 150, 26);
                cmb.Font = Skin.Body(8.25F);
                cmb.DrawItem += ComboDrawItem;
                cmb.Items.AddRange(spec.Qualities);
                cmb.SelectedIndex = spec.DefaultQualityIndex;
                cmb.Enabled = chip.Checked;
                grpFormats.Controls.Add(cmb);
                formatQualities[i] = cmb;

                SkateCombo capturedCmb = cmb;
                SkateChip capturedChip = chip;
                chip.CheckedChanged += delegate { capturedCmb.Enabled = capturedChip.Checked; };
            }
        }

        private void BuildSaveBox()
        {
            grpSave = new SkateGroup();
            grpSave.Title = "Save to";
            grpSave.Accent = Skin.Pink;
            grpSave.Bounds = new Rectangle(606, 300, 260, 136);
            grpSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(grpSave);

            rdoSameFolder = new SkateRadio();
            rdoSameFolder.Text = "Same folder as the video";
            rdoSameFolder.Bounds = new Rectangle(12, 34, 236, 30);
            rdoSameFolder.Checked = true;
            grpSave.Controls.Add(rdoSameFolder);

            rdoCustomFolder = new SkateRadio();
            rdoCustomFolder.Text = "This folder:";
            rdoCustomFolder.Bounds = new Rectangle(12, 68, 236, 30);
            grpSave.Controls.Add(rdoCustomFolder);

            txtOutFolder = new TextBox();
            txtOutFolder.Bounds = new Rectangle(12, 104, 170, 24);
            txtOutFolder.ReadOnly = true;
            txtOutFolder.BorderStyle = BorderStyle.FixedSingle;
            txtOutFolder.BackColor = Skin.Ink;
            txtOutFolder.ForeColor = Skin.Text;
            txtOutFolder.Font = Skin.Body(8F);
            grpSave.Controls.Add(txtOutFolder);

            btnBrowse = new SkateButton();
            btnBrowse.Text = "…";
            btnBrowse.Accent = Skin.Pink;
            btnBrowse.Bounds = new Rectangle(188, 103, 58, 26);
            btnBrowse.Click += delegate { PickFolder(); };
            grpSave.Controls.Add(btnBrowse);
        }

        // -------------------------------------------------------- custom draw

        private void ListDrawHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(Skin.Raised)) e.Graphics.FillRectangle(b, e.Bounds);
            using (Pen p = new Pen(Skin.Acid, 2f))
                e.Graphics.DrawLine(p, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            Rectangle r = e.Bounds; r.X += 8;
            TextRenderer.DrawText(e.Graphics, e.Header.Text.ToUpperInvariant(), fHead, r, Skin.Acid,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        private void ListDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            bool selected = e.Item.Selected;
            Color bg = selected ? Skin.Raised : Skin.Panel;
            using (SolidBrush b = new SolidBrush(bg)) e.Graphics.FillRectangle(b, e.Bounds);

            if (selected && e.ColumnIndex == 0)
            {
                using (SolidBrush b = new SolidBrush(Skin.Pink))
                    e.Graphics.FillRectangle(b, new Rectangle(e.Bounds.Left, e.Bounds.Top, 3, e.Bounds.Height));
            }

            Rectangle r = e.Bounds; r.X += 8; r.Width -= 10;
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text,
                e.ColumnIndex == 0 ? fRowBold : fRow, r, e.Item.ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }

        private void ComboDrawItem(object sender, DrawItemEventArgs e)
        {
            ComboBox cmb = (ComboBox)sender;
            if (e.Index < 0) return;

            bool selected = (e.State & DrawItemState.Selected) != 0;
            Color bg = selected ? Skin.Acid : Skin.Ink;
            Color fg = selected ? Skin.Ink : Skin.Text;
            if (!cmb.Enabled) fg = Skin.Dim;

            using (SolidBrush b = new SolidBrush(bg)) e.Graphics.FillRectangle(b, e.Bounds);
            Rectangle r = e.Bounds; r.X += 6;
            TextRenderer.DrawText(e.Graphics, cmb.Items[e.Index].ToString(), cmb.Font, r, fg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        // ----------------------------------------------------------- input

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (running) { e.Effect = DragDropEffects.None; return; }
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop)
                     ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            if (running) return;
            string[] dropped = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (dropped != null) AddFiles(dropped);
        }

        private void PickFiles()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Multiselect = true;
                dlg.Title = "Choose videos to extract audio from";
                dlg.Filter = Paths.OpenFilter();
                if (dlg.ShowDialog(this) == DialogResult.OK) AddFiles(dlg.FileNames);
            }
        }

        private void PickFolder()
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Where should the audio files go?";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    txtOutFolder.Text = dlg.SelectedPath;
                    rdoCustomFolder.Checked = true;
                }
            }
        }

        /// <summary>Accepts files and expands any dropped folders one level.</summary>
        private void AddFiles(IEnumerable<string> paths)
        {
            int added = 0, skipped = 0;

            foreach (string raw in paths)
            {
                if (raw == null) continue;

                if (Directory.Exists(raw))
                {
                    try
                    {
                        foreach (string f in Directory.GetFiles(raw))
                            if (AddOne(f)) added++;
                    }
                    catch { skipped++; }
                    continue;
                }

                if (File.Exists(raw)) { if (AddOne(raw)) added++; }
                else skipped++;
            }

            UpdateHint();
            if (added > 0)
            {
                status.ForeColor = Skin.Dim;
                status.Text = list.Items.Count + " file(s) queued."
                            + (skipped > 0 ? "  " + skipped + " skipped." : "");
            }
        }

        private bool AddOne(string path)
        {
            string full;
            try { full = Path.GetFullPath(path); }
            catch { return false; }

            foreach (ListViewItem existing in list.Items)
            {
                if (string.Equals((string)existing.Tag, full, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            ListViewItem item = new ListViewItem(Path.GetFileName(full));
            item.Tag = full;
            item.ForeColor = Skin.Text;
            item.SubItems.Add("Queued");
            list.Items.Add(item);
            return true;
        }

        private void RemoveSelected()
        {
            for (int i = list.SelectedIndices.Count - 1; i >= 0; i--)
                list.Items.RemoveAt(list.SelectedIndices[i]);
            UpdateHint();
        }

        private void UpdateHint() { hint.Visible = list.Items.Count == 0; }

        /// <summary>The empty state is a sibling of the list, so centre it by hand.</summary>
        private void PositionEmptyState()
        {
            if (hint == null || listFrame == null) return;
            Rectangle r = listFrame.Bounds;
            int w = Math.Min(300, r.Width - 20);
            int h = Math.Min(268, r.Height - 20);
            hint.Bounds = new Rectangle(
                r.Left + (r.Width - w) / 2,
                r.Top + (r.Height - h) / 2,
                Math.Max(0, w), Math.Max(0, h));
        }

        // ------------------------------------------------------ conversion

        private class Task
        {
            public int ItemIndex;
            public string Input;
            public FormatSpec Spec;
            public QualityOption Quality;
        }

        private void StartConversion()
        {
            if (list.Items.Count == 0) { Warn("Add some files first."); return; }

            List<int> chosen = new List<int>();
            for (int i = 0; i < formatChecks.Length; i++)
                if (formatChecks[i].Checked) chosen.Add(i);

            if (chosen.Count == 0) { Warn("Flip on at least one output format."); return; }

            string customFolder = null;
            if (rdoCustomFolder.Checked)
            {
                customFolder = txtOutFolder.Text.Trim();
                if (customFolder.Length == 0 || !Directory.Exists(customFolder))
                {
                    Warn("Choose a folder to save into, or switch back to \"Same folder as the video\".");
                    return;
                }
                if (!Paths.IsWritable(customFolder))
                {
                    Warn("That folder is not writable. Pick another one.");
                    return;
                }
            }

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < list.Items.Count; i++)
            {
                foreach (int fi in chosen)
                {
                    Task t = new Task();
                    t.ItemIndex = i;
                    t.Input = (string)list.Items[i].Tag;
                    t.Spec = specs[fi];
                    t.Quality = (QualityOption)formatQualities[fi].SelectedItem;
                    tasks.Add(t);
                }
                list.Items[i].SubItems[1].Text = "Queued";
                list.Items[i].ForeColor = Skin.Text;
            }

            SetRunning(true);
            progress.Maximum = tasks.Count;
            progress.Value = 0;
            converter.Reset();

            string ffmpeg = ffmpegPath;
            worker = new Thread(delegate () { WorkerLoop(tasks, ffmpeg, customFolder); });
            worker.IsBackground = true;
            worker.Start();
        }

        private void WorkerLoop(List<Task> tasks, string ffmpeg, string customFolder)
        {
            int done = 0, failed = 0, completed = 0;
            HashSet<string> outputFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool redirected = false;

            foreach (Task t in tasks)
            {
                if (converter.Cancelled) break;

                string sourceDir = Path.GetDirectoryName(t.Input);
                string outDir = customFolder != null ? customFolder : Paths.FirstWritable(sourceDir);

                if (outDir == null)
                {
                    failed++;
                    Bump(ref completed);
                    SetItemStatus(t.ItemIndex, "Failed: nowhere writable to save", true);
                    continue;
                }
                if (customFolder == null && !Paths.SameDirectory(outDir, sourceDir)) redirected = true;
                outputFolders.Add(outDir);

                SetItemStatus(t.ItemIndex, "Converting " + t.Spec.Label + "…", false);
                Announce("Converting " + Path.GetFileName(t.Input) + " → " + t.Spec.Label
                         + "   (" + (completed + 1) + " of " + tasks.Count + ")");

                string outPath = Converter.BuildOutputPath(t.Input, outDir, t.Spec.Extension);
                ConversionResult r = converter.Run(ffmpeg, t.Input, outPath, t.Quality);

                Bump(ref completed);

                if (r.Success)
                {
                    done++;
                    SetItemStatus(t.ItemIndex, "Done", false);
                }
                else if (!converter.Cancelled)
                {
                    failed++;
                    SetItemStatus(t.ItemIndex, t.Spec.Label + " failed: " + Shorten(r.Error), true);
                }
            }

            int finalDone = done, finalFailed = failed;
            bool finalRedirected = redirected;
            List<string> folders = new List<string>(outputFolders);

            BeginInvoke((MethodInvoker)delegate
            {
                Finish(finalDone, finalFailed, finalRedirected, folders);
            });
        }

        private void Finish(int done, int failed, bool redirected, List<string> folders)
        {
            SetRunning(false);

            if (converter.Cancelled)
            {
                status.ForeColor = Skin.Pink;
                status.Text = "Stopped. " + done + " file(s) had already been written.";
                progress.Value = 0;
                return;
            }

            string msg = done + " file(s) written";
            if (failed > 0) msg += ", " + failed + " failed";
            msg += ".";
            if (redirected)
                msg += "  Source folder was read-only, so those went to your Downloads folder instead.";

            status.ForeColor = failed > 0 ? Skin.Danger : Skin.Acid;
            status.Text = msg;

            if (done > 0 && folders.Count == 1)
            {
                DialogResult r = MessageBox.Show(this, msg + "\n\nOpen the output folder?",
                    "video2sound", MessageBoxButtons.YesNo,
                    failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                if (r == DialogResult.Yes)
                {
                    try { System.Diagnostics.Process.Start(folders[0]); }
                    catch { }
                }
            }
            else if (done > 0 || failed > 0)
            {
                MessageBox.Show(this, msg, "video2sound", MessageBoxButtons.OK,
                    failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
        }

        private void CancelConversion()
        {
            if (!running) return;
            btnCancel.Enabled = false;
            status.ForeColor = Skin.Pink;
            status.Text = "Stopping…";
            converter.Cancel();
        }

        private void SetRunning(bool value)
        {
            running = value;
            btnConvert.Enabled = !value && ffmpegPath != null;
            btnCancel.Enabled = value;
            btnAdd.Enabled = !value;
            btnRemove.Enabled = !value;
            btnClear.Enabled = !value;
            grpFormats.Enabled = !value;
            grpSave.Enabled = !value;
            Cursor = value ? Cursors.AppStarting : Cursors.Default;
        }

        // ------------------------------------------- cross-thread UI helpers

        private void SetItemStatus(int index, string text, bool isError)
        {
            BeginInvoke((MethodInvoker)delegate
            {
                if (index < 0 || index >= list.Items.Count) return;
                ListViewItem item = list.Items[index];
                item.SubItems[1].Text = text;
                item.ForeColor = isError ? Skin.Danger : Skin.Text;
            });
        }

        private void Announce(string text)
        {
            BeginInvoke((MethodInvoker)delegate
            {
                status.ForeColor = Skin.Dim;
                status.Text = text;
            });
        }

        private void Bump(ref int completed)
        {
            completed++;
            int value = completed;
            BeginInvoke((MethodInvoker)delegate { progress.Value = value; });
        }

        private void Warn(string message)
        {
            MessageBox.Show(this, message, "video2sound",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static string Shorten(string text)
        {
            if (string.IsNullOrEmpty(text)) return "unknown error";
            return text.Length > 70 ? text.Substring(0, 70) + "…" : text;
        }
    }
}
