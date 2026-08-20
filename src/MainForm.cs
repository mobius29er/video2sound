using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace VideoToSound
{
    public class MainForm : Form
    {
        private ListView list;
        private Label hint;
        private Button btnAdd, btnRemove, btnClear, btnConvert, btnCancel, btnBrowse;
        private ProgressBar progress;
        private Label status;
        private GroupBox grpFormats, grpSave;
        private RadioButton rdoSameFolder, rdoCustomFolder;
        private TextBox txtOutFolder;

        private FormatSpec[] specs;
        private CheckBox[] formatChecks;
        private ComboBox[] formatQualities;

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
                status.Text = "ffmpeg was not found. Put ffmpeg.exe beside this program, or install it on your PATH.";
                status.ForeColor = Color.Firebrick;
                btnConvert.Enabled = false;
            }

            if (initialFiles != null) AddFiles(initialFiles);
        }

        // ---------------------------------------------------------------- UI

        private void BuildUi()
        {
            Text = "video2sound";
            ClientSize = new Size(880, 580);
            MinimumSize = new Size(760, 520);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            BackColor = SystemColors.Control;
            AllowDrop = true;
            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;

            list = new ListView();
            list.Bounds = new Rectangle(12, 12, 580, 448);
            list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            list.View = View.Details;
            list.FullRowSelect = true;
            list.GridLines = false;
            list.HideSelection = false;
            list.AllowDrop = true;
            list.BackColor = Color.White;
            list.DragEnter += OnDragEnter;
            list.DragDrop += OnDragDrop;
            list.Columns.Add("File", 300);
            list.Columns.Add("Status", 262);
            Controls.Add(list);

            hint = new Label();
            hint.Text = "Drag video files here";
            hint.TextAlign = ContentAlignment.MiddleCenter;
            hint.ForeColor = Color.FromArgb(140, 140, 140);
            hint.Font = new Font("Segoe UI", 11F);
            hint.BackColor = Color.White;
            hint.Bounds = new Rectangle(14, 190, 576, 40);
            hint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(hint);
            hint.BringToFront();

            btnAdd = MakeButton("Add Files…", new Rectangle(12, 468, 100, 28),
                                AnchorStyles.Bottom | AnchorStyles.Left);
            btnAdd.Click += delegate { PickFiles(); };

            btnRemove = MakeButton("Remove Selected", new Rectangle(120, 468, 130, 28),
                                   AnchorStyles.Bottom | AnchorStyles.Left);
            btnRemove.Click += delegate { RemoveSelected(); };

            btnClear = MakeButton("Clear", new Rectangle(258, 468, 74, 28),
                                  AnchorStyles.Bottom | AnchorStyles.Left);
            btnClear.Click += delegate { list.Items.Clear(); UpdateHint(); };

            progress = new ProgressBar();
            progress.Bounds = new Rectangle(12, 508, 580, 18);
            progress.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(progress);

            status = new Label();
            status.Bounds = new Rectangle(12, 532, 580, 36);
            status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            status.ForeColor = Color.FromArgb(90, 90, 90);
            status.Text = "Add files, tick the formats you want, then press Convert.";
            Controls.Add(status);

            BuildFormatsBox();
            BuildSaveBox();

            btnConvert = MakeButton("Convert", new Rectangle(604, 464, 172, 36),
                                    AnchorStyles.Bottom | AnchorStyles.Right);
            btnConvert.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnConvert.Click += delegate { StartConversion(); };

            btnCancel = MakeButton("Cancel", new Rectangle(784, 464, 84, 36),
                                   AnchorStyles.Bottom | AnchorStyles.Right);
            btnCancel.Enabled = false;
            btnCancel.Click += delegate { CancelConversion(); };

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

        private Button MakeButton(string text, Rectangle bounds, AnchorStyles anchor)
        {
            Button b = new Button();
            b.Text = text;
            b.Bounds = bounds;
            b.Anchor = anchor;
            b.UseVisualStyleBackColor = true;
            Controls.Add(b);
            return b;
        }

        private void BuildFormatsBox()
        {
            specs = FormatSpec.All();
            formatChecks = new CheckBox[specs.Length];
            formatQualities = new ComboBox[specs.Length];

            grpFormats = new GroupBox();
            grpFormats.Text = "Output formats";
            grpFormats.Bounds = new Rectangle(604, 12, 264, 200);
            grpFormats.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(grpFormats);

            for (int i = 0; i < specs.Length; i++)
            {
                FormatSpec spec = specs[i];

                CheckBox chk = new CheckBox();
                chk.Text = spec.Label;
                chk.Bounds = new Rectangle(14, 26 + i * 32, 74, 22);
                chk.Checked = spec.CheckedByDefault;
                grpFormats.Controls.Add(chk);
                formatChecks[i] = chk;

                ComboBox cmb = new ComboBox();
                cmb.Bounds = new Rectangle(94, 24 + i * 32, 156, 22);
                cmb.DropDownStyle = ComboBoxStyle.DropDownList;
                cmb.Items.AddRange(spec.Qualities);
                cmb.SelectedIndex = spec.DefaultQualityIndex;
                cmb.Enabled = chk.Checked;
                grpFormats.Controls.Add(cmb);
                formatQualities[i] = cmb;

                ComboBox captured = cmb;
                CheckBox capturedChk = chk;
                chk.CheckedChanged += delegate { captured.Enabled = capturedChk.Checked; };
            }
        }

        private void BuildSaveBox()
        {
            grpSave = new GroupBox();
            grpSave.Text = "Save to";
            grpSave.Bounds = new Rectangle(604, 222, 264, 130);
            grpSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(grpSave);

            rdoSameFolder = new RadioButton();
            rdoSameFolder.Text = "Same folder as the video";
            rdoSameFolder.Bounds = new Rectangle(14, 24, 236, 22);
            rdoSameFolder.Checked = true;
            grpSave.Controls.Add(rdoSameFolder);

            rdoCustomFolder = new RadioButton();
            rdoCustomFolder.Text = "This folder:";
            rdoCustomFolder.Bounds = new Rectangle(14, 50, 236, 22);
            grpSave.Controls.Add(rdoCustomFolder);

            txtOutFolder = new TextBox();
            txtOutFolder.Bounds = new Rectangle(14, 76, 180, 22);
            txtOutFolder.ReadOnly = true;
            txtOutFolder.BackColor = Color.White;
            grpSave.Controls.Add(txtOutFolder);

            btnBrowse = new Button();
            btnBrowse.Text = "…";
            btnBrowse.Bounds = new Rectangle(200, 75, 50, 24);
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += delegate { PickFolder(); };
            grpSave.Controls.Add(btnBrowse);
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
                        {
                            if (AddOne(f)) added++;
                        }
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
                status.ForeColor = Color.FromArgb(90, 90, 90);
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

        private void UpdateHint()
        {
            hint.Visible = list.Items.Count == 0;
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
            if (list.Items.Count == 0)
            {
                Warn("Add some files first.");
                return;
            }

            List<int> chosen = new List<int>();
            for (int i = 0; i < formatChecks.Length; i++)
                if (formatChecks[i].Checked) chosen.Add(i);

            if (chosen.Count == 0)
            {
                Warn("Tick at least one output format.");
                return;
            }

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
                list.Items[i].ForeColor = SystemColors.WindowText;
            }

            SetRunning(true);
            progress.Minimum = 0;
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
            Dictionary<int, int> perItemFailures = new Dictionary<int, int>();
            bool redirected = false;

            foreach (Task t in tasks)
            {
                if (converter.Cancelled) break;

                string sourceDir = Path.GetDirectoryName(t.Input);
                string outDir = customFolder != null ? customFolder : Paths.FirstWritable(sourceDir);

                if (outDir == null)
                {
                    failed++;
                    Bump(ref completed, tasks.Count);
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

                Bump(ref completed, tasks.Count);

                if (r.Success)
                {
                    done++;
                    SetItemStatus(t.ItemIndex, "Done", false);
                }
                else if (!converter.Cancelled)
                {
                    failed++;
                    if (!perItemFailures.ContainsKey(t.ItemIndex)) perItemFailures[t.ItemIndex] = 0;
                    perItemFailures[t.ItemIndex]++;
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
                status.ForeColor = Color.Firebrick;
                status.Text = "Cancelled. " + done + " file(s) had already been written.";
                progress.Value = 0;
                return;
            }

            string msg = done + " file(s) written";
            if (failed > 0) msg += ", " + failed + " failed";
            msg += ".";
            if (redirected)
                msg += "  The source folder was read-only, so those went to your Downloads folder instead.";

            status.ForeColor = failed > 0 ? Color.Firebrick : Color.FromArgb(20, 110, 40);
            status.Text = msg;

            if (done > 0 && folders.Count == 1)
            {
                DialogResult r = MessageBox.Show(this,
                    msg + "\n\nOpen the output folder?",
                    "video2sound",
                    MessageBoxButtons.YesNo,
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
            status.ForeColor = Color.Firebrick;
            status.Text = "Cancelling…";
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
                item.ForeColor = isError ? Color.Firebrick : SystemColors.WindowText;
            });
        }

        private void Announce(string text)
        {
            BeginInvoke((MethodInvoker)delegate
            {
                status.ForeColor = Color.FromArgb(90, 90, 90);
                status.Text = text;
            });
        }

        private void Bump(ref int completed, int total)
        {
            completed++;
            int value = completed;
            BeginInvoke((MethodInvoker)delegate
            {
                progress.Value = Math.Min(value, progress.Maximum);
            });
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
