using Godot;

namespace meph {
    // Simple RichTextLabel-backed logger. Falls back to GD.Print if not initialized.
    public static class ConsoleLog {
        private static RichTextLabel _label;
        private static bool _isInitialized = false;

        public static void Init(RichTextLabel label) {
            _label = label;
            _isInitialized = _label != null;
            
            if (_isInitialized) {
                _label.Clear();
                _label.AppendText("[color=#00ff88][SYSTEM] Console initialized[/color]\n");
            } else {
                GD.PrintErr("ConsoleLog: Failed to initialize - RichTextLabel is null");
            }
        }

        public static void Clear() {
            if (_isInitialized) {
                _label?.Clear();
            }
        }

        public static void Info(string text, string category = "INFO") => 
            WriteLine($"[{category}] {text}");
            
        public static void Warn(string text, string category = "WARN") => 
            WriteLine($"[{category}] {text}", "#ffaa00");
            
        public static void Error(string text, string category = "ERR") => 
            WriteLine($"[{category}] {text}", "#ff4444");
            
        // Game-specific categories with distinct colors
        public static void Game(string text) => 
            WriteLine($"[GAME] {text}", "#00ff88");
            
        public static void Combat(string text) => 
            WriteLine($"[COMBAT] {text}", "#ff8800");
            
        public static void Factor(string text) => 
            WriteLine($"[FACTOR] {text}", "#8888ff");
            
        public static void Resource(string text) => 
            WriteLine($"[RESOURCE] {text}", "#88ff88");
            
        public static void Equip(string text) => 
            WriteLine($"[EQUIP] {text}", "#ff88ff");

        public static void Action(string text) =>
            WriteLine($"[ACTION] {text}", "#ffff88");

        private static void WriteLine(string text, string color = "#ffffff") {
            if (_isInitialized && _label != null) {
                _label.AppendText($"[color={color}]{text}[/color]\n");
                
                // Auto-scroll to bottom
                _label.CallDeferred("scroll_to_line", _label.GetLineCount() - 1);
            } else {
                // Fallback to GD.Print with timestamp
                GD.Print($"[{System.DateTime.Now:HH:mm:ss}] {text}");
            }
        }
    }
}