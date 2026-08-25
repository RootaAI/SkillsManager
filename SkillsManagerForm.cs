// SkillsManager — built by Roota AI · find us on Rednote (小红书): 若塔AI
// Copyright (c) 2026 Roota AI. All rights reserved.

using System.Text;

namespace SkillsManager
{
    /// <summary>
    /// Main window: a notes-style manager for Microsoft Copilot Cowork skills.
    ///
    /// Cowork skills live as FOLDERS under the user's OneDrive-synced
    /// Documents\Cowork\Skills; every folder contains one SKILL.md and the
    /// folder NAME is the skill's identity (the file name is always the same,
    /// so folders are the only way to tell skills apart).
    ///
    /// Layout: left = filterable list of skill folders; right = the selected
    /// folder's SKILL.md, directly editable with an explicit Save. "New Skill"
    /// creates &lt;Skills&gt;\&lt;name&gt;\SKILL.md (folders auto-created,
    /// including Cowork\Skills itself on first use).
    ///
    /// Deliberately NO delete function: deletion goes through Open Folder →
    /// Explorer, which uses the Recycle Bin and OneDrive version history -
    /// safer than any in-app permanent delete.
    /// </summary>
    internal sealed class SkillsManagerForm : Form
    {
        private const string SkillFileName = "SKILL.md";   // create-name; reads accept any casing

        private readonly TextBox _filterBox;
        private readonly ListBox _list;
        private readonly TextBox _nameView;
        private readonly TextBox _pathView;
        private readonly TextBox _editor;
        private readonly Button  _saveButton;
        private readonly Label   _statusLabel;

        private readonly string _root;
        private List<string> _skillDirs = new();   // absolute folder paths
        private string? _loadedDir;                // folder whose file is in the editor
        private bool _dirty;
        private bool _loading;                     // suppress dirty-tracking during programmatic loads

        internal SkillsManagerForm()
        {
            _root = ResolveSkillsRoot();

            Text          = "Copilot Skills Manager (Cowork)";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize    = new Size(940, 620);
            MinimumSize   = new Size(720, 460);
            BackColor     = Ui.ContentBackground;

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill, SplitterDistance = 280, FixedPanel = FixedPanel.Panel1,
                Orientation = Orientation.Vertical
            };
            Controls.Add(split);

            // ── Left: filter + skill-folder list ──
            _filterBox = new TextBox
            {
                Dock = DockStyle.Top, Font = Ui.FontStatus, BackColor = Color.White,
                PlaceholderText = "Filter by skill (folder) name..."
            };
            _filterBox.TextChanged += (_, _) => RefreshList();
            _list = new ListBox { Dock = DockStyle.Fill, Font = Ui.FontStatus, IntegralHeight = false };
            _list.SelectedIndexChanged += (_, _) => ShowSelected();
            split.Panel1.Controls.Add(_list);
            split.Panel1.Controls.Add(_filterBox);
            split.Panel1.Padding = new Padding(8, 8, 4, 8);

            // Created before the editor below so its TextChanged lambda
            // captures a definitely-assigned button (keeps the build at zero
            // nullable warnings); docked into the bottom bar further down.
            _saveButton = MakeButton("Save", (_, _) => SaveCurrent(), DockStyle.Left, Ui.FontButton);

            // ── Right: name + path + editable SKILL.md ──
            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(4, 8, 8, 8)
            };
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _nameView = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None,
                                      Font = Ui.FontGridHeader, BackColor = Ui.ContentBackground };
            _pathView = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None,
                                      Font = Ui.FontMeta, ForeColor = Color.Gray,
                                      BackColor = Ui.ContentBackground, Margin = new Padding(0, 2, 0, 6) };
            // PlainPasteTextBox + WordWrap:
            // Markdown copied from browsers/editors usually carries lone-LF
            // line endings, and a stock WinForms TextBox pastes those as ONE
            // run-together line - a bulleted SKILL.md turned into unstructured
            // text. The subclass normalizes every paste route (Ctrl+V, context
            // menu, Shift+Ins) to CRLF; wrap keeps long Markdown prose readable.
            _editor = new PlainPasteTextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical,
                                    WordWrap = true, Font = Ui.FontEditor, BackColor = Color.White,
                                    AcceptsReturn = true, AcceptsTab = true };
            _editor.TextChanged += (_, _) =>
            {
                if (_loading) return;
                _dirty = true;
                _saveButton.Enabled = true;
            };
            right.Controls.Add(_nameView, 0, 0);
            right.Controls.Add(_pathView, 0, 1);
            right.Controls.Add(_editor, 0, 2);
            split.Panel2.Controls.Add(right);

            // ── Bottom bar ──
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Ui.ContentBackground,
                                     Padding = new Padding(14, 8, 14, 8) };
            var newButton     = MakeButton("New Skill", (_, _) => NewSkill(), DockStyle.Left, Ui.FontButton);
            var folderButton  = MakeButton("Open Folder", (_, _) => OpenSkillsFolder(), DockStyle.Left, Ui.FontStatus);
            var refreshButton = MakeButton("Refresh", (_, _) => LoadSkills(_loadedDir), DockStyle.Left, Ui.FontStatus);
            var closeButton   = MakeButton("Close", (_, _) => Close(), DockStyle.Right, Ui.FontStatus);
            _statusLabel = new Label { Dock = DockStyle.Fill, Font = Ui.FontMeta, ForeColor = Color.Gray,
                                       TextAlign = ContentAlignment.MiddleCenter, AutoEllipsis = true };
            bottom.Controls.Add(_statusLabel);
            bottom.Controls.Add(closeButton);
            bottom.Controls.Add(newButton);
            bottom.Controls.Add(_saveButton);
            bottom.Controls.Add(folderButton);
            bottom.Controls.Add(refreshButton);
            Controls.Add(bottom);

            _saveButton.Enabled = false;
            FormClosing += (_, e) =>
            {
                // Same guard as switching skills: never silently drop edits.
                if (_dirty && !ConfirmDiscardOrSave()) e.Cancel = true;
            };

            LoadSkills();
        }

        private static Button MakeButton(string text, EventHandler onClick, DockStyle dock, Font font)
        {
            var b = new Button { Text = text, AutoSize = true, Dock = dock, Font = font, FlatStyle = FlatStyle.System,
                                 Padding = new Padding(8, 4, 8, 4), Margin = new Padding(4, 0, 4, 0) };
            b.Click += onClick;
            return b;
        }

        // ── Skills root resolution ────────────────────────────────────────────
        /// <summary>
        /// The default Cowork skills location is &lt;Documents&gt;\Cowork\Skills on
        /// the OneDrive-synced Documents folder. SpecialFolder.MyDocuments
        /// follows OneDrive Known Folder Move automatically, so it is the
        /// primary candidate; the OneDrive env-var paths cover setups where
        /// Documents is NOT redirected but the OneDrive folder still hosts a
        /// Documents tree. First EXISTING candidate wins; when none exists yet
        /// the primary is used and created on first save/New Skill.
        /// </summary>
        internal static string ResolveSkillsRoot()
        {
            var candidates = new List<string>();
            string myDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrEmpty(myDocs))
                candidates.Add(Path.Combine(myDocs, "Cowork", "Skills"));
            foreach (var env in new[] { "OneDriveCommercial", "OneDrive" })
            {
                string? od = Environment.GetEnvironmentVariable(env);
                if (!string.IsNullOrEmpty(od))
                {
                    string p = Path.Combine(od, "Documents", "Cowork", "Skills");
                    if (!candidates.Contains(p, StringComparer.OrdinalIgnoreCase))
                        candidates.Add(p);
                }
            }
            foreach (var c in candidates)
                if (Directory.Exists(c)) return c;
            return candidates.Count > 0
                ? candidates[0]
                : Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SkillsManager", "Cowork", "Skills");
        }

        /// <summary>Existing skill file in a folder (any casing of skill.md), or the create-path.</summary>
        private static string SkillFilePath(string dir)
        {
            try
            {
                var existing = Directory.GetFiles(dir, "*.md", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault(f => Path.GetFileName(f).Equals(SkillFileName, StringComparison.OrdinalIgnoreCase));
                if (existing != null) return existing;
            }
            catch { /* unreadable folder - fall through to the create-path */ }
            return Path.Combine(dir, SkillFileName);
        }

        // ── Load / list ───────────────────────────────────────────────────────
        private void LoadSkills(string? selectDir = null)
        {
            _skillDirs = new List<string>();
            try
            {
                if (Directory.Exists(_root))
                    _skillDirs = Directory.GetDirectories(_root)
                        .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                        .ToList();
            }
            catch (Exception ex)
            {
                SetStatus("Could not read the skills folder: " + ex.Message, Color.Firebrick);
            }

            RefreshList(selectDir);

            if (!Directory.Exists(_root))
                SetStatus($"Skills folder not found yet - New Skill creates {_root}", Color.Gray);
            else if (_skillDirs.Count == 0)
                SetStatus($"No skill folders in {_root} - New Skill to start.", Color.Gray);
            else
                SetStatus($"{_skillDirs.Count} skill(s) in {_root}", Color.Gray);
        }

        private List<string> Filtered()
        {
            string f = _filterBox.Text.Trim().ToLowerInvariant();
            if (f.Length == 0) return _skillDirs;
            return _skillDirs.Where(d => Path.GetFileName(d).ToLowerInvariant().Contains(f)).ToList();
        }

        private void RefreshList(string? selectDir = null)
        {
            var shown = Filtered();
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var d in shown) _list.Items.Add(Path.GetFileName(d));
            _list.EndUpdate();

            if (shown.Count > 0)
            {
                int idx = 0;
                if (selectDir != null)
                {
                    int found = shown.FindIndex(d => d.Equals(selectDir, StringComparison.OrdinalIgnoreCase));
                    if (found >= 0) idx = found;
                }
                _list.SelectedIndex = idx;
            }
            else ShowSelected();
        }

        private string? SelectedDir()
        {
            var shown = Filtered();
            return _list.SelectedIndex >= 0 && _list.SelectedIndex < shown.Count ? shown[_list.SelectedIndex] : null;
        }

        private void ShowSelected()
        {
            var dir = SelectedDir();
            if (string.Equals(dir, _loadedDir, StringComparison.OrdinalIgnoreCase)) return;

            // Guard unsaved edits before the editor is repointed elsewhere.
            if (_dirty && !ConfirmDiscardOrSave())
            {
                // Put the selection back on the still-loaded skill.
                if (_loadedDir != null)
                {
                    var shown = Filtered();
                    int back = shown.FindIndex(d => d.Equals(_loadedDir, StringComparison.OrdinalIgnoreCase));
                    if (back >= 0) { _loading = true; _list.SelectedIndex = back; _loading = false; }
                }
                return;
            }

            _loading = true;
            try
            {
                _loadedDir = dir;
                _dirty = false;
                _saveButton.Enabled = false;

                if (dir == null)
                {
                    _nameView.Text = "";
                    _pathView.Text = "";
                    _editor.Text = "";
                    _editor.ReadOnly = true;
                    return;
                }

                _editor.ReadOnly = false;
                _nameView.Text = Path.GetFileName(dir);
                string file = SkillFilePath(dir);
                _pathView.Text = file;

                if (File.Exists(file))
                {
                    _editor.Text = File.ReadAllText(file)
                        .Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
                }
                else
                {
                    _editor.Text = "";
                    SetStatus($"{Path.GetFileName(dir)} has no {SkillFileName} yet - Save creates it.", Color.Gray);
                }
            }
            catch (Exception ex)
            {
                _editor.Text = "";
                SetStatus("Could not read the skill file: " + ex.Message, Color.Firebrick);
            }
            finally { _loading = false; }
        }

        // ── Actions ───────────────────────────────────────────────────────────
        private void SaveCurrent()
        {
            if (_loadedDir == null) return;
            try
            {
                Directory.CreateDirectory(_loadedDir);
                string file = SkillFilePath(_loadedDir);
                // UTF-8 without BOM: SKILL.md is Markdown consumed by Copilot
                // Cowork - a BOM would be a stray character to some readers.
                File.WriteAllText(file, _editor.Text.Replace("\r\n", "\n"), new UTF8Encoding(false));
                _dirty = false;
                _saveButton.Enabled = false;
                AuditLogger.Log("SKILL-SAVE", Path.GetFileName(_loadedDir));
                SetStatus($"Saved {file}", Color.FromArgb(0, 128, 0));
            }
            catch (Exception ex)
            {
                SetStatus("Save failed: " + ex.Message, Color.Firebrick);
            }
        }

        private void NewSkill()
        {
            if (_dirty && !ConfirmDiscardOrSave()) return;

            string? name = PromptForSkillName();
            if (string.IsNullOrWhiteSpace(name)) return;
            name = name.Trim();

            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("The skill name becomes a folder name - it cannot contain \\ / : * ? \" < > |",
                    "New Skill", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string dir = Path.Combine(_root, name);
                if (Directory.Exists(dir))
                {
                    // Existing folder: just select it (its file loads).
                    LoadSkills(dir);
                    return;
                }

                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, SkillFileName);
                File.WriteAllText(file, "# " + name + "\n\n", new UTF8Encoding(false));
                AuditLogger.Log("SKILL-CREATE", name);
                LoadSkills(dir);
                SetStatus($"Created {file}", Color.FromArgb(0, 128, 0));
            }
            catch (Exception ex)
            {
                SetStatus("Create failed: " + ex.Message, Color.Firebrick);
            }
        }

        private string? PromptForSkillName()
        {
            using var dlg = new Form
            {
                Text = "New Skill",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false
            };
            // AutoSize + MaximumSize wrap instead of a fixed Height: a fixed
            // pixel height clips the second text line at higher DPI scales.
            // Everything below is laid out from the label's measured height,
            // and the dialog is sized last.
            var label = new Label
            {
                Text = "Skill name (becomes the folder name - Copilot recognizes the skill by it):",
                Left = 14, Top = 12, AutoSize = true, MaximumSize = new Size(392, 0),
                Font = Ui.FontStatus
            };
            int boxTop = 12 + label.PreferredSize.Height + 8;
            var box = new TextBox { Left = 14, Top = boxTop, Width = 392, Font = Ui.FontStatus };
            int btnTop = boxTop + box.PreferredHeight + 12;
            var ok = new Button { Text = "Create", DialogResult = DialogResult.OK, Left = 226, Top = btnTop, Width = 88 };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 320, Top = btnTop, Width = 86 };
            dlg.ClientSize = new Size(420, btnTop + ok.Height + 12);
            dlg.Controls.AddRange(new Control[] { label, box, ok, cancel });
            dlg.AcceptButton = ok;
            dlg.CancelButton = cancel;
            return dlg.ShowDialog(this) == DialogResult.OK ? box.Text : null;
        }

        private void OpenSkillsFolder()
        {
            try
            {
                Directory.CreateDirectory(_root);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{_root}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                SetStatus("Could not open the folder: " + ex.Message, Color.Firebrick);
            }
        }

        /// <summary>True = proceed (saved or deliberately discarded); false = stay.</summary>
        private bool ConfirmDiscardOrSave()
        {
            var choice = MessageBox.Show(
                $"Save changes to '{(_loadedDir != null ? Path.GetFileName(_loadedDir) : "")}' first?",
                "Unsaved changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (choice == DialogResult.Cancel) return false;
            if (choice == DialogResult.Yes) SaveCurrent();
            else { _dirty = false; _saveButton.Enabled = false; }
            return true;
        }

        private void SetStatus(string text, Color color) { _statusLabel.Text = text; _statusLabel.ForeColor = color; }

        /// <summary>
        /// TextBox whose every paste normalizes line endings to CRLF. A stock
        /// multiline TextBox renders lone-LF text as a single run-together
        /// line, which destroyed the structure of Markdown pasted from
        /// browsers, VS Code, or Copilot output. WM_PASTE interception covers
        /// ALL paste routes (Ctrl+V, Shift+Ins, context-menu Paste).
        /// </summary>
        private sealed class PlainPasteTextBox : TextBox
        {
            private const int WM_PASTE = 0x0302;

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_PASTE)
                {
                    try
                    {
                        if (Clipboard.ContainsText())
                        {
                            string t = Clipboard.GetText()
                                .Replace("\r\n", "\n").Replace('\r', '\n')
                                .Replace("\n", "\r\n");
                            SelectedText = t;   // replaces the selection like a native paste
                            return;
                        }
                    }
                    catch { /* clipboard locked by another app - fall through to native paste */ }
                }
                base.WndProc(ref m);
            }
        }
    }
}
