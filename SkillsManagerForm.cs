// SkillsManager — built by Roota AI · find us on Rednote (小红书): 若塔AI
// Copyright (c) 2026 Roota AI. All rights reserved.

using System.Text;
using SkillsManager.Core;

namespace SkillsManager
{
    /// <summary>
    /// Main window: a notes-style manager for agent skill folders.
    ///
    /// A skill is a FOLDER containing one SKILL.md; the folder NAME is the
    /// skill's identity (the file name is always the same, so folders are the
    /// only way to tell skills apart). That convention is shared by Microsoft
    /// Copilot Cowork, Claude Code, and OpenAI Codex, so the manager offers
    /// each of them as a built-in "library" plus any custom folders the user
    /// adds — one tool for every skills tree on the machine.
    ///
    /// Layout: left = library picker + filterable list of skill folders;
    /// right = the selected folder's SKILL.md, directly editable with an
    /// explicit Save (Ctrl+S). "New Skill" creates &lt;root&gt;\&lt;name&gt;\SKILL.md
    /// (folders auto-created, including the root itself on first use).
    ///
    /// Deliberately NO delete function: deletion goes through Open Folder →
    /// Explorer, which uses the Recycle Bin (and OneDrive version history for
    /// synced roots) - safer than any in-app permanent delete.
    /// </summary>
    internal sealed class SkillsManagerForm : Form
    {
        private const string SkillFileName = "SKILL.md";   // create-name; reads accept any casing

        private readonly ComboBox _libraryCombo;
        private readonly TextBox _filterBox;
        private readonly ListBox _list;
        private readonly TextBox _nameView;
        private readonly TextBox _pathView;
        private readonly TextBox _editor;
        private readonly Button  _saveButton;
        private readonly Label   _statusLabel;

        private readonly ISkillsEnvironment _env = new SystemEnvironment();
        private AppSettings _settings;
        private IReadOnlyList<SkillLibrary> _libraries;
        private SkillLibrary _library;             // currently selected library
        private List<string> _skillDirs = new();   // absolute folder paths
        private string? _loadedDir;                // folder whose file is in the editor
        private DateTime? _loadedWriteTimeUtc;     // file mtime at load, for external-change detection
        private bool _dirty;
        private bool _loading;                     // suppress dirty-tracking during programmatic loads

        internal SkillsManagerForm()
        {
            _settings = SettingsStore.Load();
            _libraries = LibraryCatalog.All(_env, _settings);
            _library = _libraries.FirstOrDefault(l => l.Id == _settings.LastLibraryId) ?? _libraries[0];

            StartPosition = FormStartPosition.CenterScreen;
            ClientSize    = new Size(940, 620);
            MinimumSize   = new Size(720, 460);
            BackColor     = Ui.ContentBackground;
            KeyPreview    = true;                  // route Ctrl+S here before the editor sees it

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill, SplitterDistance = 280, FixedPanel = FixedPanel.Panel1,
                Orientation = Orientation.Vertical
            };
            Controls.Add(split);

            // ── Left: library picker + filter + skill-folder list ──
            // Dock order note: WinForms docks the LAST-added control first, so
            // list (Fill) is added before the Top-docked strips above it.
            _filterBox = new TextBox
            {
                Dock = DockStyle.Top, Font = Ui.FontStatus, BackColor = Color.White,
                PlaceholderText = "Filter by skill (folder) name..."
            };
            _filterBox.TextChanged += (_, _) => RefreshList();
            _list = new ListBox { Dock = DockStyle.Fill, Font = Ui.FontStatus, IntegralHeight = false };
            _list.SelectedIndexChanged += (_, _) => ShowSelected();

            _libraryCombo = new ComboBox
            {
                Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = Ui.FontStatus
            };
            var manageButton = new Button
            {
                Text = "⚙", Dock = DockStyle.Right, Width = 32, FlatStyle = FlatStyle.System,
                Font = Ui.FontStatus
            };
            var tips = new ToolTip();
            tips.SetToolTip(manageButton, "Manage custom skill libraries");
            tips.SetToolTip(_libraryCombo, "Skill library: Copilot Cowork, Claude Code, Codex, or your own folders");
            manageButton.Click += (_, _) => ManageLibraries();
            var libraryPanel = new Panel { Dock = DockStyle.Top, Height = _libraryCombo.PreferredHeight + 6,
                                           Padding = new Padding(0, 0, 0, 6) };
            libraryPanel.Controls.Add(_libraryCombo);
            libraryPanel.Controls.Add(manageButton);

            split.Panel1.Controls.Add(_list);
            split.Panel1.Controls.Add(_filterBox);
            split.Panel1.Controls.Add(libraryPanel);
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
            KeyDown += (_, e) =>
            {
                if (e.Control && e.KeyCode == Keys.S)
                {
                    e.SuppressKeyPress = true;     // stop the editor's ding
                    if (_saveButton.Enabled) SaveCurrent();
                }
            };
            FormClosing += (_, e) =>
            {
                // Same guard as switching skills: never silently drop edits.
                if (_dirty && !ConfirmDiscardOrSave()) e.Cancel = true;
            };

            _libraryCombo.SelectedIndexChanged += (_, _) => OnLibraryPicked();
            PopulateLibraryCombo();
            ApplyLibrary();
        }

        private static Button MakeButton(string text, EventHandler onClick, DockStyle dock, Font font)
        {
            var b = new Button { Text = text, AutoSize = true, Dock = dock, Font = font, FlatStyle = FlatStyle.System,
                                 Padding = new Padding(8, 4, 8, 4), Margin = new Padding(4, 0, 4, 0) };
            b.Click += onClick;
            return b;
        }

        // ── Libraries ─────────────────────────────────────────────────────────
        private void PopulateLibraryCombo()
        {
            _loading = true;
            try
            {
                _libraryCombo.Items.Clear();
                foreach (var lib in _libraries) _libraryCombo.Items.Add(lib.Name);
                int idx = _libraries.ToList().FindIndex(l => l.Id == _library.Id);
                _libraryCombo.SelectedIndex = idx >= 0 ? idx : 0;
            }
            finally { _loading = false; }
        }

        private void OnLibraryPicked()
        {
            if (_loading) return;
            int idx = _libraryCombo.SelectedIndex;
            if (idx < 0 || idx >= _libraries.Count || _libraries[idx].Id == _library.Id) return;

            // Guard unsaved edits before the whole list is repointed elsewhere.
            if (_dirty && !ConfirmDiscardOrSave())
            {
                _loading = true;
                _libraryCombo.SelectedIndex = _libraries.ToList().FindIndex(l => l.Id == _library.Id);
                _loading = false;
                return;
            }

            _library = _libraries[idx];
            _settings.LastLibraryId = _library.Id;
            TrySaveSettings();
            ApplyLibrary();
        }

        private void ApplyLibrary()
        {
            Text = $"Skills Manager — {_library.Name}";
            _filterBox.Text = "";
            _loadedDir = null;
            _loadedWriteTimeUtc = null;
            LoadSkills();
        }

        /// <summary>Add/remove custom libraries; built-ins are not editable.</summary>
        private void ManageLibraries()
        {
            if (_dirty && !ConfirmDiscardOrSave()) return;

            using var dlg = new Form
            {
                Text = "Manage skill libraries",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false,
                ClientSize = new Size(520, 300)
            };
            var info = new Label
            {
                Text = "Built-in libraries (Copilot Cowork, Claude Code, Codex) are always available.\n" +
                       "Add any folder that contains one sub-folder per skill with a SKILL.md inside.",
                Dock = DockStyle.Top, Height = 40, Font = Ui.FontMeta, ForeColor = Color.Gray,
                Padding = new Padding(10, 6, 10, 0)
            };
            var listBox = new ListBox { Dock = DockStyle.Fill, Font = Ui.FontStatus, IntegralHeight = false };
            void Reload()
            {
                listBox.Items.Clear();
                foreach (var c in _settings.CustomLibraries)
                    listBox.Items.Add($"{c.Name}  ({c.Path})");
            }
            Reload();

            var buttons = new Panel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(10, 8, 10, 8) };
            var addButton    = MakeButton("Add folder...", (_, _) =>
            {
                using var picker = new FolderBrowserDialog
                {
                    Description = "Pick a skills root: it holds one folder per skill, each with a SKILL.md.",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = true
                };
                if (picker.ShowDialog(dlg) != DialogResult.OK) return;
                string path = picker.SelectedPath;
                if (_settings.CustomLibraries.Any(c => string.Equals(c.Path, path, StringComparison.OrdinalIgnoreCase)))
                    return;   // already listed
                _settings.CustomLibraries.Add(new CustomLibrary { Name = Path.GetFileName(path), Path = path });
                Reload();
            }, DockStyle.Left, Ui.FontStatus);
            var removeButton = MakeButton("Remove", (_, _) =>
            {
                int i = listBox.SelectedIndex;
                if (i < 0) return;
                _settings.CustomLibraries.RemoveAt(i);
                Reload();
            }, DockStyle.Left, Ui.FontStatus);
            var closeButton  = MakeButton("Close", (_, _) => dlg.Close(), DockStyle.Right, Ui.FontStatus);
            buttons.Controls.Add(addButton);
            buttons.Controls.Add(removeButton);
            buttons.Controls.Add(closeButton);

            dlg.Controls.Add(listBox);
            dlg.Controls.Add(info);
            dlg.Controls.Add(buttons);
            dlg.ShowDialog(this);

            TrySaveSettings();
            _libraries = LibraryCatalog.All(_env, _settings);
            if (_libraries.All(l => l.Id != _library.Id))
            {
                _library = _libraries[0];          // current custom library was removed
                _settings.LastLibraryId = _library.Id;
                TrySaveSettings();
                ApplyLibrary();
            }
            PopulateLibraryCombo();
        }

        private void TrySaveSettings()
        {
            try { SettingsStore.Save(_settings); }
            catch (Exception ex) { SetStatus("Could not save settings: " + ex.Message, Color.Firebrick); }
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
            string root = _library.Root;
            _skillDirs = new List<string>();
            try
            {
                if (Directory.Exists(root))
                    _skillDirs = Directory.GetDirectories(root)
                        .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                        .ToList();
            }
            catch (Exception ex)
            {
                SetStatus("Could not read the skills folder: " + ex.Message, Color.Firebrick);
            }

            RefreshList(selectDir);

            if (!Directory.Exists(root))
                SetStatus($"Skills folder not found yet - New Skill creates {root}", Color.Gray);
            else if (_skillDirs.Count == 0)
                SetStatus($"No skill folders in {root} - New Skill to start.", Color.Gray);
            else
                SetStatus($"{_skillDirs.Count} skill(s) in {root}", Color.Gray);
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
                _loadedWriteTimeUtc = null;
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
                    _editor.Text = TextUtil.NormalizeToCrLf(File.ReadAllText(file));
                    _loadedWriteTimeUtc = File.GetLastWriteTimeUtc(file);
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

                // External-change guard: the skills tree is often synced
                // (OneDrive) or edited by agents/other machines. If the file
                // on disk is newer than what was loaded, saving would silently
                // destroy that version - ask first.
                if (File.Exists(file) && _loadedWriteTimeUtc is { } loadedAt
                    && File.GetLastWriteTimeUtc(file) != loadedAt)
                {
                    var overwrite = MessageBox.Show(
                        $"'{Path.GetFileName(file)}' changed on disk after it was loaded here\n" +
                        "(synced from another device, or edited by another program).\n\n" +
                        "Overwrite it with your version? Choosing No keeps both: your text stays\n" +
                        "in the editor, and Refresh shows the disk version.",
                        "File changed outside the editor",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                    if (overwrite != DialogResult.Yes)
                    {
                        SetStatus("Not saved - the file on disk is newer. Refresh to view it.", Color.Firebrick);
                        return;
                    }
                }

                // UTF-8 without BOM + LF endings: SKILL.md is Markdown read by
                // agent runtimes and diffed in git - a BOM would be a stray
                // character to some readers.
                File.WriteAllText(file, TextUtil.NormalizeToLf(_editor.Text), new UTF8Encoding(false));
                _loadedWriteTimeUtc = File.GetLastWriteTimeUtc(file);
                _dirty = false;
                _saveButton.Enabled = false;
                AuditLogger.Log("SKILL-SAVE", $"{_library.Id}:{Path.GetFileName(_loadedDir)}");
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
            if (name == null) return;
            name = name.Trim();

            if (SkillName.Validate(name) is { } error)
            {
                MessageBox.Show(error, "New Skill", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string dir = Path.Combine(_library.Root, name);
                if (Directory.Exists(dir))
                {
                    // Existing folder: just select it (its file loads).
                    LoadSkills(dir);
                    return;
                }

                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, SkillFileName);
                File.WriteAllText(file, "# " + name + "\n\n", new UTF8Encoding(false));
                AuditLogger.Log("SKILL-CREATE", $"{_library.Id}:{name}");
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
                Text = $"Skill name in '{_library.Name}' (becomes the folder name - the agent recognizes the skill by it):",
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
                Directory.CreateDirectory(_library.Root);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{_library.Root}\"",
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
            if (choice == DialogResult.Yes)
            {
                SaveCurrent();
                if (_dirty) return false;   // save was declined (external change) or failed - stay
            }
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
                            SelectedText = TextUtil.NormalizeToCrLf(Clipboard.GetText());
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
