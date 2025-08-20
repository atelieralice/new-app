using Godot;

// Simple RichTextLabel-backed logger. Falls back to GD.Print if not initialized.
namespace meph {
    public static class ConsoleLog {
        private static RichTextLabel _label;

        public static void Init ( RichTextLabel label ) {
            _label = label;
            _label?.Clear ( );
        }

        public static void Clear ( ) => _label?.Clear ( );

        public static void Info ( string text ) => WriteLine ( $"[INFO] {text}" );
        public static void Warn ( string text ) => WriteLine ( $"[WARN] {text}" );
        public static void Error ( string text ) => WriteLine ( $"[ERR] {text}" );

        private static void WriteLine ( string text ) {
            if ( _label != null ) _label.AppendText ( text + "\n" );
            else GD.Print ( text );
        }
    }
}