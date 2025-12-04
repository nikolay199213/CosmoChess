using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using CosmoChess.Domain;
using CosmoChess.Domain.Interface.Engines;
using CosmoChess.Domain.ValueObject;

namespace CosmoChess.Infrastructure.Engines
{
    public class StockfishEngine : IHostedService, IEngineService
    {
        private readonly Channel<StockfishRequestBase> _queue = Channel.CreateUnbounded<StockfishRequestBase>();
        private readonly string _stockfishPath;
        private readonly ILogger<StockfishEngine> _logger;
        private readonly AppConfiguration _configuration;
        private Process _stockfishProcess;
        private StreamWriter _input;
        private StreamReader _output;
        private Task _processingTask;

        public StockfishEngine(AppConfiguration configuration, ILogger<StockfishEngine> logger)
        {
            _stockfishPath = configuration.StockfishPath;
            _configuration = configuration;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartStockfish();
            _processingTask = Task.Run(ProcessQueue, cancellationToken);
            return Task.CompletedTask;
        }

        private void StartStockfish()
        {
            _logger.LogInformation("Starting Stockfish from: {Path}", _stockfishPath);

            _stockfishProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _stockfishPath,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            _stockfishProcess.Start();
            _input = _stockfishProcess.StandardInput;
            _output = _stockfishProcess.StandardOutput;

            _logger.LogInformation("Stockfish started successfully");
        }

        public async Task<string> AnalyzeAsync(string fen, int depth, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var request = new StockfishRequest(fen, depth, tcs, cancellationToken);

            await _queue.Writer.WriteAsync(request, cancellationToken);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_configuration.StockfishAnalysisTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await using (linkedCts.Token.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task;
            }
        }

        public async Task<AnalysisResult> AnalyzeMultiPvAsync(string fen, int depth, int multiPv, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<AnalysisResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var request = new StockfishMultiPvRequest(fen, depth, multiPv, tcs, cancellationToken);

            await _queue.Writer.WriteAsync(request, cancellationToken);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_configuration.StockfishAnalysisTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await using (linkedCts.Token.Register(() => tcs.TrySetCanceled()))
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
                    switch (request)
                    {
                        case StockfishRequest simpleRequest:
                            await ProcessSimpleRequest(simpleRequest);
                            break;
                        case StockfishMultiPvRequest multiPvRequest:
                            await ProcessMultiPvRequest(multiPvRequest);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Stockfish request");

                    if (request is StockfishRequest sr)
                        sr.CompletionSource.TrySetException(ex);
                    else if (request is StockfishMultiPvRequest mr)
                        mr.CompletionSource.TrySetException(ex);
                }
            }
        }

        private async Task ProcessSimpleRequest(StockfishRequest request)
        {
            await _input.WriteLineAsync("ucinewgame");

            // Configure Stockfish UCI options for optimal analysis
            await _input.WriteLineAsync($"setoption name Hash value {_configuration.StockfishHashSize}");
            await _input.WriteLineAsync($"setoption name Threads value {_configuration.StockfishThreads}");

            await _input.WriteLineAsync($"position fen {request.Fen}");
            await _input.WriteLineAsync($"go depth {request.Depth}");
            await _input.FlushAsync();

            string bestMove = await ReadBestMoveAsync(request.CancellationToken);
            request.CompletionSource.TrySetResult(bestMove);
        }

        private async Task ProcessMultiPvRequest(StockfishMultiPvRequest request)
        {
            await _input.WriteLineAsync("ucinewgame");

            // Configure Stockfish UCI options for optimal analysis
            await _input.WriteLineAsync($"setoption name Hash value {_configuration.StockfishHashSize}");
            await _input.WriteLineAsync($"setoption name Threads value {_configuration.StockfishThreads}");
            await _input.WriteLineAsync("setoption name UCI_ShowWDL value true");
            await _input.WriteLineAsync($"setoption name MultiPV value {request.MultiPv}");

            await _input.WriteLineAsync($"position fen {request.Fen}");
            await _input.WriteLineAsync($"go depth {request.Depth}");
            await _input.FlushAsync();

            // Extract side to move from FEN (needed for score interpretation)
            var isBlackToMove = IsBlackToMove(request.Fen);

            var result = await ReadMultiPvAsync(request.Depth, request.MultiPv, isBlackToMove, request.CancellationToken);

            // Reset MultiPV to 1 for future requests
            await _input.WriteLineAsync("setoption name MultiPV value 1");
            await _input.FlushAsync();

            request.CompletionSource.TrySetResult(result);
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
            throw new TaskCanceledException("Analysis cancelled");
        }

        private async Task<AnalysisResult> ReadMultiPvAsync(int targetDepth, int multiPv, bool isBlackToMove, CancellationToken cancellationToken)
        {
            var lines = new Dictionary<int, AnalysisLine>();
            var bestMove = "";
            var minAcceptableDepth = Math.Max(1, targetDepth - 3); // Accept depth within 3 of target

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await _output.ReadLineAsync(cancellationToken);

                if (line == null) continue;

                if (line.StartsWith("bestmove"))
                {
                    bestMove = line.Split(' ')[1];
                    break;
                }

                // Accept info lines with multipv that are close to target depth
                if (line.StartsWith("info") && line.Contains("multipv"))
                {
                    var currentDepth = ExtractDepth(line);
                    if (currentDepth >= minAcceptableDepth)
                    {
                        var analysisLine = ParseInfoLine(line, isBlackToMove);
                        if (analysisLine != null)
                        {
                            // Update if this is deeper analysis or first time seeing this rank
                            if (!lines.ContainsKey(analysisLine.Rank) || currentDepth >= targetDepth)
                            {
                                lines[analysisLine.Rank] = analysisLine;
                            }
                        }
                    }
                }
            }

            var resultLines = lines.Values
                .OrderBy(l => l.Rank)
                .Take(multiPv)
                .ToList();

            return new AnalysisResult(resultLines, targetDepth);
        }

        private bool IsBlackToMove(string fen)
        {
            // FEN format: <position> <side-to-move> <castling> <en-passant> <halfmove> <fullmove>
            // Second field indicates side to move: 'w' for white, 'b' for black
            var fenParts = fen.Split(' ');
            return fenParts.Length > 1 && fenParts[1] == "b";
        }

        private int ExtractDepth(string line)
        {
            var match = Regex.Match(line, @"\bdepth (\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        private AnalysisLine ParseInfoLine(string line, bool isBlackToMove)
        {
            try
            {
                // Parse multipv
                var multipvMatch = Regex.Match(line, @"multipv (\d+)");
                if (!multipvMatch.Success) return null;
                int rank = int.Parse(multipvMatch.Groups[1].Value);

                // Parse score
                int score = 0;
                bool isMate = false;
                int? mateIn = null;

                var mateMatch = Regex.Match(line, @"score mate (-?\d+)");
                if (mateMatch.Success)
                {
                    isMate = true;
                    mateIn = int.Parse(mateMatch.Groups[1].Value);
                    score = mateIn > 0 ? 10000 : -10000;
                }
                else
                {
                    var cpMatch = Regex.Match(line, @"score cp (-?\d+)");
                    if (cpMatch.Success)
                    {
                        score = int.Parse(cpMatch.Groups[1].Value);
                    }
                }

                // Stockfish returns scores from the perspective of the side to move
                // We need to convert to white's perspective for consistent UI display
                if (isBlackToMove)
                {
                    score = -score;
                    if (mateIn.HasValue)
                    {
                        mateIn = -mateIn.Value;
                    }
                }

                // Parse pv (principal variation)
                var pvMatch = Regex.Match(line, @" pv (.+)$");
                string pv = pvMatch.Success ? pvMatch.Groups[1].Value : "";

                // Get the first move
                string move = pv.Split(' ').FirstOrDefault() ?? "";

                return new AnalysisLine(
                    Rank: rank,
                    Move: move,
                    MoveSan: move, // Will be converted on frontend
                    Score: score,
                    IsMate: isMate,
                    MateIn: mateIn,
                    Pv: pv
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse info line: {Line}", line);
                return null;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _stockfishProcess?.Kill();
            return Task.CompletedTask;
        }
    }
}
