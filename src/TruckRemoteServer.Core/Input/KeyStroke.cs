using System;

namespace TruckRemoteServer.Input
{
    //A key of the PC keyboard as the game reads it: its scan code (independent of the keyboard layout), the extended
    //flag of the keys of the extended part of the keyboard, and a key held with it (e.g. Shift), if any
    public sealed class KeyStroke : IEquatable<KeyStroke>
    {
        public KeyStroke(short scanCode, bool extended = false, KeyStroke modifier = null)
        {
            ScanCode = scanCode;
            Extended = extended;
            Modifier = modifier;
        }

        public short ScanCode { get; }
        public bool Extended { get; }
        public KeyStroke Modifier { get; }

        public KeyStroke With(KeyStroke modifier) => new KeyStroke(ScanCode, Extended, modifier);

        public bool Equals(KeyStroke other) => other != null && ScanCode == other.ScanCode && Extended == other.Extended
            && Equals(Modifier, other.Modifier);

        public override bool Equals(object obj) => Equals(obj as KeyStroke);

        public override int GetHashCode() => (ScanCode * 2 + (Extended ? 1 : 0)) * 31 + (Modifier?.GetHashCode() ?? 0);

        public override string ToString() => (Modifier != null ? Modifier + "+" : "") + "0x" + ScanCode.ToString("X2",
            System.Globalization.CultureInfo.InvariantCulture) + (Extended ? "e" : "");
    }
}
