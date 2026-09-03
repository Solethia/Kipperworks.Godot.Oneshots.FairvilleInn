namespace FairvilleInn.Domain;

public sealed class Visitor
{
    private readonly IReadOnlyList<string> _lines;
    private int _nextLine;

    public Visitor(string name, IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            throw new ArgumentException("A visitor needs at least one line of dialogue.", nameof(lines));
        }

        Name = name;
        _lines = lines;
    }

    public string Name { get; }

    public string Speak()
    {
        var line = _lines[_nextLine];
        _nextLine = (_nextLine + 1) % _lines.Count;
        return line;
    }
}
