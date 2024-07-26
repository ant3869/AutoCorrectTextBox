using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TechCommand.Core.Helpers;

namespace TechCommand.Core.Services;
public class TimedOperationService
{
    private static bool IsOperationInProgress;

    public static async Task ExecuteTimedOperationAsync(Func<Task> operation, string operationDescription)
    {
        Logger.Instance?.Log($"Starting operation: {operationDescription}.", LogLevel.Information);
        IsOperationInProgress = true;

        var stopwatch = Stopwatch.StartNew();
        await operation();
        stopwatch.Stop();

        IsOperationInProgress = false;
        Logger.Instance?.Log($"{operationDescription} finished in {stopwatch.Elapsed.TotalSeconds} seconds.", LogLevel.Information);
    }
}
