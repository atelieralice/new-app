using Godot;

namespace meph
{
    public partial class ConsoleWindow : Window
    {
        private RichTextLabel consoleLog;
        private Button clearButton;
        private Button minimizeButton;
        private Button closeButton;
        private Panel titleBar;
        private bool isDragging = false;

        public override void _Ready()
        {
            // Get the RichTextLabel by its unique name
            consoleLog = GetNode<RichTextLabel>("%ConsoleLog");
            
            if (consoleLog == null)
            {
                GD.PrintErr("Could not find RichTextLabel with unique name %ConsoleLog");
                return;
            }

            SetupNativeWindow();
            SetupWindowFeatures();
            ConnectSignals();
            
            // Initialize your existing ConsoleLog system
            ConsoleLog.Init(consoleLog);
            ConsoleLog.Game("Console window initialized as native window");
        }

        private void SetupNativeWindow()
        {
            // CRITICAL: Force native window behavior
            ForceNative = true;
            
            // CRITICAL: Disable subwindow embedding to make this a native OS window
            GetViewport().SetEmbeddingSubwindows(false);
            
            // Window mode and basic settings
            Mode = ModeEnum.Windowed;
            Title = "Game Console - Debug Output";
            Size = new Vector2I(900, 650);
            MinSize = new Vector2I(400, 300);
            
            // Window positioning
            InitialPosition = WindowInitialPosition.CenterMainWindowScreen;
            
            // Window behavior flags - FIXED: Use correct flag names
            SetFlag(Flags.ResizeDisabled, false); // Allow resizing
            SetFlag(Flags.MinimizeDisabled, false); // Allow minimize
            SetFlag(Flags.MaximizeDisabled, false); // Allow maximize
            SetFlag(Flags.AlwaysOnTop, false); // Not always on top
            SetFlag(Flags.Transparent, false); // Opaque background
            SetFlag(Flags.NoFocus, false); // Can be focused - FIXED: Use NoFocus instead of Unfocusable
            SetFlag(Flags.ExcludeFromCapture, true); // Exclude from screenshots
        }

        private void SetupWindowFeatures()
        {
            // Configure console RichTextLabel
            consoleLog.FitContent = true;
            consoleLog.ScrollActive = true;
            consoleLog.ScrollFollowing = true;
            consoleLog.BbcodeEnabled = true;
            consoleLog.SelectionEnabled = true;
            consoleLog.ContextMenuEnabled = true;
            
            // Find UI elements if they exist
            clearButton = GetNodeOrNull<Button>("%ClearButton");
            minimizeButton = GetNodeOrNull<Button>("%MinimizeButton");
            closeButton = GetNodeOrNull<Button>("%CloseButton");
            titleBar = GetNodeOrNull<Panel>("%TitleBar");
        }

        private void ConnectSignals()
        {
            // Window signals
            CloseRequested += OnCloseRequested;
            FocusEntered += OnFocusEntered;
            FocusExited += OnFocusExited;
            VisibilityChanged += OnVisibilityChanged;
            
            // UI button signals
            if (clearButton != null)
                clearButton.Pressed += OnClearPressed;
            
            if (minimizeButton != null)
                minimizeButton.Pressed += OnMinimizePressed;
            
            if (closeButton != null)
                closeButton.Pressed += OnCloseRequested;
            
            // Title bar dragging
            if (titleBar != null)
            {
                titleBar.GuiInput += OnTitleBarInput;
            }
        }

        private void OnCloseRequested()
        {
            // Hide instead of destroying to preserve console state
            Hide();
            ConsoleLog.Game("Console window hidden");
        }

        private void OnFocusEntered()
        {
            // Visual feedback when window gains focus - FIXED: Window doesn't have Modulate
            // Use alternative visual feedback through theme
            AddThemeColorOverride("background_color", new Color(1.0f, 1.0f, 1.0f, 1.0f));
        }

        private void OnFocusExited()
        {
            // Slightly dim when window loses focus - FIXED: Window doesn't have Modulate
            AddThemeColorOverride("background_color", new Color(0.95f, 0.95f, 0.95f, 1.0f));
        }

        private void OnVisibilityChanged()
        {
            if (Visible)
            {
                ConsoleLog.Game("Console window shown");
            }
        }

        private void OnClearPressed()
        {
            ConsoleLog.Clear();
            ConsoleLog.Game("Console cleared by user");
        }

        private void OnMinimizePressed()
        {
            Mode = ModeEnum.Minimized;
        }

        private void OnTitleBarInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseButton)
            {
                if (mouseButton.ButtonIndex == MouseButton.Left)
                {
                    if (mouseButton.Pressed)
                    {
                        isDragging = true;
                        StartDrag();
                    }
                    else
                    {
                        isDragging = false;
                    }
                }
                else if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.DoubleClick)
                {
                    // Double-click to maximize/restore
                    ToggleMaximize();
                }
            }
        }

        public void ToggleVisibility()
        {
            if (Visible)
            {
                Hide();
            }
            else
            {
                Show();
                GrabFocus(); // FIXED: Use GrabFocus() instead of deprecated MoveToForeground()
                RequestAttention();
            }
        }

        public void ToggleMaximize()
        {
            if (Mode == ModeEnum.Maximized)
            {
                Mode = ModeEnum.Windowed;
            }
            else
            {
                Mode = ModeEnum.Maximized;
            }
        }

        public void CenterOnScreen()
        {
            MoveToCenter();
        }

        public void SaveConsoleToFile()
        {
            if (consoleLog == null) return;
            
            var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var filename = $"console_log_{timestamp}.txt";
            var filepath = $"user://logs/{filename}";
            
            // Create logs directory if it doesn't exist - FIXED: Use static method correctly
            DirAccess.MakeDirRecursiveAbsolute("user://logs");
            
            // Save console text
            var file = FileAccess.Open(filepath, FileAccess.ModeFlags.Write);
            if (file != null)
            {
                file.StoreString(consoleLog.GetParsedText());
                file.Close();
                ConsoleLog.Game($"Console log saved to: {filepath}");
            }
            else
            {
                ConsoleLog.Error("Failed to save console log");
            }
        }

        // Handle keyboard shortcuts
        public override void _UnhandledKeyInput(InputEvent @event)
        {
            if (!HasFocus()) return;
            
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                // Ctrl+C to copy selected text
                if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.C)
                {
                    var selectedText = consoleLog.GetSelectedText();
                    if (!string.IsNullOrEmpty(selectedText))
                    {
                        DisplayServer.ClipboardSet(selectedText);
                        ConsoleLog.Info("Selected text copied to clipboard");
                    }
                }
                
                // Ctrl+A to select all
                if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.A)
                {
                    consoleLog.SelectAll();
                }
                
                // Ctrl+L to clear console
                if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.L)
                {
                    OnClearPressed();
                }
                
                // Ctrl+S to save log
                if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.S)
                {
                    SaveConsoleToFile();
                }
                
                // ESC to hide window
                if (keyEvent.Keycode == Key.Escape)
                {
                    Hide();
                }
            }
        }

        // Handle window input for custom behaviors
        public override void _Input(InputEvent @event)
        {
            if (!HasFocus()) return;
            
            // Handle mouse wheel for font size adjustment
            if (@event is InputEventMouseButton mouseButton && mouseButton.CtrlPressed)
            {
                if (mouseButton.ButtonIndex == MouseButton.WheelUp)
                {
                    AdjustFontSize(1);
                    GetViewport().SetInputAsHandled(); // FIXED: Use GetViewport().SetInputAsHandled() instead of AcceptEvent()
                }
                else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
                {
                    AdjustFontSize(-1);
                    GetViewport().SetInputAsHandled(); // FIXED: Use GetViewport().SetInputAsHandled() instead of AcceptEvent()
                }
            }
        }

        private void AdjustFontSize(int delta)
        {
            if (consoleLog == null) return;
            
            var currentSize = consoleLog.GetThemeFontSize("normal_font_size");
            var newSize = Mathf.Clamp(currentSize + delta, 8, 32);
            
            consoleLog.AddThemeFontSizeOverride("normal_font_size", newSize);
            ConsoleLog.Info($"Font size adjusted to: {newSize}");
        }

        // Cleanup
        public override void _ExitTree()
        {
            if (clearButton != null) clearButton.Pressed -= OnClearPressed;
            if (minimizeButton != null) minimizeButton.Pressed -= OnMinimizePressed;
            if (closeButton != null) closeButton.Pressed -= OnCloseRequested;
            
            CloseRequested -= OnCloseRequested;
            FocusEntered -= OnFocusEntered;
            FocusExited -= OnFocusExited;
            VisibilityChanged -= OnVisibilityChanged;
        }
    }
}