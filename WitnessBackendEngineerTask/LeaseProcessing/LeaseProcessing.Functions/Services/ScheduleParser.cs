using LeaseProcessing.Functions.Models;
using System.Globalization;
using System.Text.RegularExpressions;
using WitnessBackendEngineerTask.Common.Models;

namespace LeaseProcessing.Functions.Services;

public sealed class ScheduleParser : IScheduleParser
{
    private readonly Regex _titleRegex;
    private readonly Regex _colSplitRegex;

    private readonly Regex _planRefCueRegex;
    private readonly Regex _colourOnlyRegex;

    private readonly Regex _termHintRegex;
    private readonly Regex _multiSpaceRegex;

    public ScheduleParser()
    {
        _titleRegex = new Regex(@"\b[A-Z]{3}\d{4,}\b", RegexOptions.Compiled);
        _colSplitRegex = new Regex(@"\s{2,}", RegexOptions.Compiled);

        _planRefCueRegex = new Regex(
            @"\b(edged|numbered)\b|\(\s*part\s+of\s*\)|\bin\s+(blue|brown|red|green|yellow)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        _colourOnlyRegex = new Regex(
            @"^(blue|brown|red|green|yellow)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        _termHintRegex = new Regex(
            @"\b(year|years|from|beginning|ending|starting|to|including)\b|" +
            @"\b(january|february|march|april|may|june|july|august|september|october|november|december)\b|" +
            @"\b\d{1,2}\.\d{1,2}\.\d{2,4}\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        _multiSpaceRegex = new Regex(@"\s{2,}", RegexOptions.Compiled);
    }

    public IReadOnlyList<ParsedScheduleNoticeOfLease> Parse(IReadOnlyList<RawScheduleNoticeOfLease> rawSchedules)
    {
        var results = new List<ParsedScheduleNoticeOfLease>(rawSchedules.Count);

        foreach (var raw in rawSchedules)
        {
            var entry = new ParsedScheduleNoticeOfLease
            {
                EntryNumber = int.TryParse(raw.EntryNumber, out var n) ? n : 0,
                EntryDate = ParseEntryDate(raw.EntryDate),

                Notes = new List<string>()
            };

            var regParts = new List<string>();
            var propParts = new List<string>();
            var termParts = new List<string>();

            var planRefSeen = false;
            var inNotes = false;

            var lesseesTitle = string.Empty;

            IEnumerable<string> lines = raw.EntryText ?? [];

            foreach (var rawLine in lines)
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                var trimmed = rawLine.Trim();

                // NOTE lines are preserved in the same order.
                if (trimmed.StartsWith("NOTE", StringComparison.OrdinalIgnoreCase))
                {
                    inNotes = true;
                    entry.Notes.Add(trimmed);
                    continue;
                }

                // Continue NOTE text if a NOTE entry wraps to the next source line.
                if (inNotes)
                {
                    if (entry.Notes.Count > 0)
                        entry.Notes[entry.Notes.Count - 1] = entry.Notes[entry.Notes.Count - 1] + " " + trimmed;
                    continue;
                }

                var cols = SplitColumns(trimmed);
                if (cols.Count == 0)
                    continue;

                if (!planRefSeen && cols.Exists(LooksLikePlanRef))
                    planRefSeen = true;

                if (cols.Count >= 4)
                {
                    regParts.Add(cols[0]);
                    propParts.Add(cols[1]);
                    termParts.Add(cols[2]);

                    if (string.IsNullOrWhiteSpace(lesseesTitle))
                        lesseesTitle = ExtractTitle(cols[3]) ?? string.Empty;

                    continue;
                }

                if (cols.Count == 3)
                {
                    regParts.Add(cols[0]);
                    propParts.Add(cols[1]);
                    termParts.Add(cols[2]);

                    continue;
                }

                if (cols.Count == 2)
                {
                    HandleTwoColumns(
                        cols[0], cols[1],
                        regParts, propParts, termParts,
                        ref lesseesTitle,
                        ref planRefSeen);

                    continue;
                }

                // cols.Count == 1
                HandleOneColumn(
                    cols[0],
                    regParts, propParts, termParts,
                    ref planRefSeen);
            }

            entry.RegistrationDateAndPlanRef = Normalize(regParts);
            entry.PropertyDescription = Normalize(propParts);
            entry.DateOfLeaseAndTerm = Normalize(termParts);

            // fallback: якщо титул не знайшовся в явній “title колонці”
            if (string.IsNullOrWhiteSpace(lesseesTitle))
                lesseesTitle = ExtractTitleFromAnyLine(lines) ?? string.Empty;

            entry.LesseesTitle = lesseesTitle;

            results.Add(entry);
        }

        return results;
    }

    private DateOnly? ParseEntryDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateOnly.TryParseExact(
            value.Trim(),
            "dd.MM.yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;
    }

    private List<string> SplitColumns(string line)
    {
        return _colSplitRegex
            .Split(line.TrimEnd())
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
    }

    private void HandleTwoColumns(
        string first,
        string second,
        List<string> regParts,
        List<string> propParts,
        List<string> termParts,
        ref string lesseesTitle,
        ref bool planRefSeen)
    {
        var title = ExtractTitle(second);
        if (!string.IsNullOrWhiteSpace(title))
        {
            if (string.IsNullOrWhiteSpace(lesseesTitle))
                lesseesTitle = title;

            if (LooksLikePlanRef(first) || planRefSeen)
            {
                regParts.Add(first);
                planRefSeen = true;
                return;
            }

            if (termParts.Count > 0 || LooksLikeTerm(first))
            {
                termParts.Add(first);
                return;
            }

            propParts.Add(first);
            return;
        }

        if (LooksLikePlanRef(first) || (planRefSeen && _colourOnlyRegex.IsMatch(first)))
        {
            regParts.Add(first);
            planRefSeen = true;

            if (termParts.Count > 0 || LooksLikeTerm(second))
                termParts.Add(second);
            else
                propParts.Add(second);

            return;
        }

        if (LooksLikePlanRef(second) || (planRefSeen && _colourOnlyRegex.IsMatch(second)))
        {
            if (termParts.Count > 0 || LooksLikeTerm(first))
                termParts.Add(first);
            else
                propParts.Add(first);

            regParts.Add(second);
            planRefSeen = true;
            return;
        }

        if (termParts.Count > 0 || LooksLikeTerm(second))
        {
            propParts.Add(first);
            termParts.Add(second);
            return;
        }

        regParts.Add(first);
        propParts.Add(second);
    }

    private void HandleOneColumn(
        string fragment,
        List<string> regParts,
        List<string> propParts,
        List<string> termParts,
        ref bool planRefSeen)
    {
        if (LooksLikePlanRef(fragment) || (planRefSeen && _colourOnlyRegex.IsMatch(fragment)))
        {
            regParts.Add(fragment);
            planRefSeen = true;
            return;
        }

        if (termParts.Count > 0)
        {
            termParts.Add(fragment);
            return;
        }

        if (propParts.Count > 0)
        {
            propParts.Add(fragment);
            return;
        }

        regParts.Add(fragment);
    }

    private bool LooksLikePlanRef(string fragment) => _planRefCueRegex.IsMatch(fragment);

    private bool LooksLikeTerm(string fragment) => _termHintRegex.IsMatch(fragment);

    private string Normalize(List<string> parts)
    {
        var joined = string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p))).Trim();
        return _multiSpaceRegex.Replace(joined, " ");
    }

    private string? ExtractTitle(string fragment)
    {
        var m = _titleRegex.Match(fragment);
        return m.Success ? m.Value : null;
    }

    private string? ExtractTitleFromAnyLine(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var title = ExtractTitle(line);
            if (!string.IsNullOrWhiteSpace(title))
                return title;
        }

        return null;
    }
}
