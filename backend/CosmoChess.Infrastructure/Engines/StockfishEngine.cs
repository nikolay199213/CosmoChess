using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Threading.Channels;
using CosmoChess.Domain.Interface.Engines;

namespace CosmoChess.Infrastructure.Engines
{
    public class StockfishEngine : IHostedService, IEngineService
    {
        private readonly Channel<StockfishRequest> _queue = Channel.CreateUnbounded<StockfishRequest>();
        private Process _stockfishProcess;
        private StreamWriter _input;
        private StreamReader _output;
        private Task _processingTask;
        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartStockfish();
            _processingTask = Task.Run(ProcessQueue, cancellationToken);
            return Task.CompletedTask;
        }

        private void StartStockfish()
        {
            _stockfishProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "stockfish", // путь к бинарнику Stockfish
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            _stockfishProcess.Start();
            _input = _stockfishProcess.StandardInput;
            _output = _stockfishProcess.StandardOutput;
        }

        public async Task<string> AnalyzeAsync(string fen, int depth, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var request = new StockfishRequest(fen, depth, tcs, cancellationToken);

            await _queue.Writer.WriteAsync(request, cancellationToken);

            await using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task;
            }
        }

        private async Task ProcessQueue()
        {
            await foreach (var request in _queue.Reader.ReadAllAsync())
            {
                if (request.CancellationToken.IsCancellationRequested)
                    continue;

                try
                {
                    await _input.WriteLineAsync("ucinewgame");
                    await _input.WriteLineAsync($"position fen {request.Fen}");
                    await _input.WriteLineAsync($"go depth {request.Depth}");
                    await _input.FlushAsync();

                    string bestMove = await ReadBestMoveAsync(request.CancellationToken);
                    request.CompletionSource.TrySetResult(bestMove);
                }
                catch (Exception ex)
                {
                    request.CompletionSource.TrySetException(ex);
                }
            }
        }

        private async Task<string> ReadBestMoveAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await _output.ReadLineAsync(cancellationToken);
                if (line != null && line.StartsWith("bestmove"))
                {
                    return line.Split(' ')[1];
                }
            }
            throw new TaskCanceledException("Анализ отменён");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _stockfishProcess?.Kill();
            return Task.CompletedTask;
        }
    }
}
