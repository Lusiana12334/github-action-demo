using PEXC.Case.Tools.Migration.Transformations;

namespace PEXC.Case.Tools.Migration;

internal class ProgressReportPrinter
{
    private bool _initialPrint;

    private readonly DateTime _start = DateTime.UtcNow;

    public void PrintReport(int counter, IList<FilterWrapper> filters)
    {
        DateTime end = DateTime.UtcNow;
        var time = end - _start;
        var performance = counter / time.TotalSeconds;
        var performanceMin = counter / time.TotalMinutes;

        if (_initialPrint)
            ClearLines(1 + filters.Count);

        Console.WriteLine(
            $"Processed {counter}, performance: {performance:#.###} [rec/s] {performanceMin:#.###} [rec/min] time: {time}");

        foreach (var filterWrapper in filters)
            Console.WriteLine(filterWrapper.Performance);

        _initialPrint = true;
    }

    private static void ClearLines(int numberOfLines)
    {
        int top = Console.CursorTop;
        for (int i = 0; i < numberOfLines; i++)
        {
            Console.SetCursorPosition(0, top - i);
            Console.Write(new string(' ', Console.WindowWidth));
        }

        Console.SetCursorPosition(0, top - numberOfLines);
    }
}