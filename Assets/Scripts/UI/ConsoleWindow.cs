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
        private bool isInitialized = false;

        public override void _Ready()
        {
            // Prevent multiple initialization
            if (isInitialized) return;
            
            // CRITICAL: Set native window properties FIRST
            SetupNativeWindow();
            
            // Find or create the console log
            SetupConsoleLog();
            
            if (consoleLog == null)
            {
                GD.PrintErr("Failed to setup console log - ConsoleWindow initialization failed");
                return;
            }

            SetupWindowFeatures();
            ConnectSignals();
            
            // Initialize the ConsoleLog system
            ConsoleLog.Init(consoleLog);
            ConsoleLog.Game("Console window initialized as native window");
            
            isInitialized = true;
        }

        private void SetupNativeWindow()
        {
            // CRITICAL: Hide window FIRST to prevent ForceNative error
            Visible = false;
            
            // Set ForceNative while window is hidden
            ForceNative = true;
            
            // Window mode and basic settings
            Mode = ModeEnum.Windowed;
            Title = "Game Console - Debug Output";
            Size = new Vector2I(900, 650);
            MinSize = new Vector2I(400, 300);
            
            // Window positioning
            InitialPosition = WindowInitialPosition.CenterMainWindowScreen;
            
            // Window behavior flags
            SetFlag(Flags.ResizeDisabled, false); // Allow resizing
            SetFlag(Flags.MinimizeDisabled, false); // Allow minimize
            SetFlag(Flags.MaximizeDisabled, false); // Allow maximize
            SetFlag(Flags.AlwaysOnTop, false); // Not always on top
            SetFlag(Flags.Transparent, false); // Opaque background
            SetFlag(Flags.NoFocus, false); // Can be focused
            SetFlag(Flags.ExcludeFromCapture, true); // Exclude from screenshots
        }

        private void SetupConsoleLog()
        {
            // Try to find existing RichTextLabel (from scene)
            consoleLog = GetNodeOrNull<RichTextLabel>("%ConsoleLog") ?? GetNodeOrNull<RichTextLabel>("ConsoleLog");
            
            // If not found, create one programmatically
            if (consoleLog == null)
            {
                CreateConsoleLogProgrammatically();
            }
        }

        private void CreateConsoleLogProgrammatically()
        {
            consoleLog = new RichTextLabel
            {
                Name = "ConsoleLog",
                BbcodeEnabled = true,
                ScrollFollowing = true,
                SelectionEnabled = true,
                ContextMenuEnabled = true,
                FitContent = true,
                ScrollActive = true
            };
            
            // Set to fill the entire window with padding
            consoleLog.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            consoleLog.OffsetLeft = 8;
            consoleLog.OffsetRight = -8;
            consoleLog.OffsetTop = 8;
            consoleLog.OffsetBottom = -8;
            
            // Add to this window
            AddChild(consoleLog);
            
            GD.Print("Created RichTextLabel programmatically for console window");
        }

        private void SetupWindowFeatures()
        {
            // Configure console RichTextLabel
            if (consoleLog != null)
            {
                consoleLog.FitContent = true;
                consoleLog.ScrollActive = true;
                consoleLog.ScrollFollowing = true;
                consoleLog.BbcodeEnabled = true;
                consoleLog.SelectionEnabled = true;
                consoleLog.ContextMenuEnabled = true;
            }
            
            // Find UI elements if they exist (from scene)
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
            
            // UI button signals (if they exist from scene)
            if (clearButton != null)
                clearButton.Pressed += OnClearPressed;
            
            if (minimizeButton != null)
                minimizeButton.Pressed += OnMinimizePressed;
            
            if (closeButton != null)
                closeButton.Pressed += OnCloseRequested;
            
            // Title bar dragging (if it exists from scene)
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
            // Visual feedback when window gains focus
            if (HasThemeColorOverride("background_color"))
                AddThemeColorOverride("background_color", new Color(1.0f, 1.0f, 1.0f, 1.0f));
        }

        private void OnFocusExited()
        {
            // Slightly dim when window loses focus
            if (HasThemeColorOverride("background_color"))
                AddThemeColorOverride("background_color", new Color(0.95f, 0.95f, 0.95f, 1.0f));
        }

        private void OnVisibilityChanged()
        {
            if (Visible && isInitialized)
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
                // Bring window to front when shown
                if (HasMethod("grab_focus"))
                    GrabFocus();
                if (HasMethod("request_attention"))
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
            if (HasMethod("move_to_center"))
                MoveToCenter();
        }

        public void SaveConsoleToFile()
        {
            if (consoleLog == null) return;
            
            var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var filename = $"console_log_{timestamp}.txt";
            var filepath = $"user://logs/{filename}";
            
            // Create logs directory if it doesn't exist
            if (!DirAccess.DirExistsAbsolute("user://logs"))
            {
                DirAccess.MakeDirRecursiveAbsolute("user://logs");
            }
            
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
            if (!HasFocus() || !isInitialized) return;
            
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                // Ctrl+C to copy selected text
                if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.C)
                {
                    if (consoleLog != null)
                    {
                        var selectedText = consoleLog.GetSelectedText();
                        if (!string.IsNullOrEmpty(selectedText))
                        {
                            DisplayServer.ClipboardSet(selectedText);
                            ConsoleLog.Info("Selected text copied to clipboard");
                        }
                    }
                }
                
                // Ctrl+A to select all
                if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.A)
                {
                    consoleLog?.SelectAll();
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
            if (!HasFocus() || !isInitialized) return;
            
            // Handle mouse wheel for font size adjustment
            if (@event is InputEventMouseButton mouseButton && mouseButton.CtrlPressed)
            {
                if (mouseButton.ButtonIndex == MouseButton.WheelUp)
                {
                    AdjustFontSize(1);
                    GetViewport().SetInputAsHandled();
                }
                else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
                {
                    AdjustFontSize(-1);
                    GetViewport().SetInputAsHandled();
                }
            }
        }

        private void AdjustFontSize(int delta)
        {
            if (consoleLog == null) return;
            
            var currentSize = consoleLog.GetThemeFontSize("normal_font_size");
            if (currentSize == 0) currentSize = 12; // Default font size
            
            var newSize = Mathf.Clamp(currentSize + delta, 8, 32);
            
            consoleLog.AddThemeFontSizeOverride("normal_font_size", newSize);
            ConsoleLog.Info($"Font size adjusted to: {newSize}");
        }

        // Cleanup
        public override void _ExitTree()
        {
            // Disconnect signals safely
            if (clearButton != null && clearButton.IsConnected("pressed", Callable.From(OnClearPressed)))
                clearButton.Pressed -= OnClearPressed;
                
            if (minimizeButton != null && minimizeButton.IsConnected("pressed", Callable.From(OnMinimizePressed)))
                minimizeButton.Pressed -= OnMinimizePressed;
                
            if (closeButton != null && closeButton.IsConnected("pressed", Callable.From(OnCloseRequested)))
                closeButton.Pressed -= OnCloseRequested;
            
            // Disconnect window signals
            CloseRequested -= OnCloseRequested;
            FocusEntered -= OnFocusEntered;
            FocusExited -= OnFocusExited;
            VisibilityChanged -= OnVisibilityChanged;
            
            if (titleBar != null)
            {
                titleBar.GuiInput -= OnTitleBarInput;
            }
            
            isInitialized = false;
        }
    }
}