using System.Text.RegularExpressions;

namespace DFN_BMS.Helpers
{
    public static class BarcodeParser
    {
        /// <summary>
        /// Dispatches to the correct parser based on the customer's Barcode Type.
        ///   TYPE1 -> Hyundai / GS1-128 DataMatrix format
        ///   TYPE2 -> KIA plain-text format ("PartNo Date SerialNo")
        /// If barcodeType is missing/unrecognized, falls back to sniffing the
        /// raw string so scanning still works for un-configured customers.
        /// </summary>
        public static (string? PartNo, string? SerialNo, int Qty) Parse(string barcode, string? barcodeType)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return (null, null, 1);

            var type = (barcodeType ?? string.Empty).Trim().ToUpperInvariant();

            if (type == "TYPE2")
                return ParseKia(barcode);

            if (type == "TYPE1")
                return ParseHyundai(barcode);

            // ── No type configured — sniff the format ────────────────────────
            return barcode.TrimStart().StartsWith("[)>")
                ? ParseHyundai(barcode)
                : ParseKia(barcode);
        }

        // ── TYPE2 / KIA: "<PartNo> <DDMMYY> <SerialNo>" ─────────────────────
        // e.g. "28530-08360 170726 101037" — trailing $ optional, scanner may
        // or may not send it, so never require it.
        private static (string?, string?, int) ParseKia(string barcode)
        {
            var clean = CleanTrailer(barcode.Trim());

            var tokens = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length < 3)
                return (null, null, 1);

            string partNo = CleanTrailer(tokens[0]);
            string serialNo = CleanTrailer(tokens[2]);

            return (partNo, serialNo, 1);
        }

        // ── TYPE1 / HYUNDAI: ANSI MH10.8.2 / GS1-128 DataMatrix ─────────────
        //
        // Known barcode structure (Notepad++ shows GS/RS as highlight blocks):
        //   [)> RS 06 GS VT9JH GS P28700T7230 GS SYF51 GS GS T260516C1B2A00654484 GS 1A0000 GS GS GS RS EOT #$
        //
        // When browser/scanner strips control chars, it arrives as:
        //   [)>06VT9JHP28700T7230SYF51T260516C1B2A00654851A0000#$
        //
        // Rules:
        //   Part No  → the field starting with "P" that matches a known part number pattern
        //   Serial   → the field starting with "T" followed by a DATE (6 digits YYMMDD),
        //              which distinguishes "T260516..." (serial) from "T7230" (part suffix)
        private static (string?, string?, int) ParseHyundai(string barcode)
        {
            // ── Strategy 1: split on real GS1 control characters ─────────────
            char[] ctrlSeparators = { '\u001D', '\u001E', '\u001C', '\u0004' };

            if (barcode.IndexOfAny(ctrlSeparators) >= 0)
            {
                var fields = barcode
                    .Split(ctrlSeparators, StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => f.Trim())
                    .Where(f => f.Length > 1)
                    .ToList();

                string? pField = fields.FirstOrDefault(f => f.StartsWith("P"))?.Substring(1);
                string? tField = fields
                    .FirstOrDefault(f => f.StartsWith("T") && f.Length > 7 && char.IsDigit(f[1]))
                    ?.Substring(1);

                if (pField != null && tField != null)
                    return (CleanTrailer(pField), CleanTrailer(tField), 1);
            }

            // ── Strategy 2: no control chars — use specific regex patterns ───
            var partMatch = Regex.Match(barcode, @"P([0-9]{5}[A-Z][0-9A-Z]+)");
            var serialMatch = Regex.Match(barcode, @"T(\d{6}[A-Z0-9]+)");

            string? partNo = partMatch.Success ? CleanTrailer(partMatch.Groups[1].Value) : null;
            string? serialNo = serialMatch.Success ? CleanTrailer(serialMatch.Groups[1].Value) : null;

            // ── Strategy 3: fallback — any P-field ────────────────────────────
            if (partNo == null)
            {
                var pm = Regex.Match(barcode, @"(?<![A-Z0-9])P([A-Z0-9]{5,})");
                if (pm.Success) partNo = CleanTrailer(pm.Groups[1].Value);
            }

            // BOX QR
            var boxQrMatch = Regex.Match(barcode, @"(28700T\d+)\s+(\d+)\s+(\d{10,})\$?");
            if (boxQrMatch.Success)
            {
                return (
                    boxQrMatch.Groups[1].Value,
                    boxQrMatch.Groups[3].Value,
                    int.Parse(boxQrMatch.Groups[2].Value)
                );
            }

            // GS1 fallback for serial
            if (serialNo == null)
            {
                var tMatches = Regex.Matches(barcode, @"T([A-Z0-9]{6,})");
                serialNo = tMatches.Cast<Match>()
                    .Select(m => CleanTrailer(m.Groups[1].Value))
                    .OrderByDescending(s => s.Length)
                    .FirstOrDefault();
            }

            return (partNo, serialNo, 1);
        }

        private static string CleanTrailer(string s)
            => s.TrimEnd('#', '$', ' ', '\0');
    }
}