using CosmoChess.Domain.ValueObject;

namespace CosmoChess.Infrastructure.Engines;

public abstract class StockfishRequestBase
{
    public string Fen { get; set; }
    public int Depth { get; set; }
    public CancellationToken CancellationToken { get; set; }
}

public class StockfishRequest : StockfishRequestBase
{
    public TaskCompletionSource<string> CompletionSource { get; set; }

    public StockfishRequest(
        string fen,
        int depth,
        TaskCompletionSource<string> completionSource,
        CancellationToken cancellationToken)
    {
        Fen = fen;
        Depth = depth;
        CompletionSource = completionSource;
        CancellationToken = cancellationToken;
    }
}

public class StockfishMultiPvRequest : StockfishRequestBase
{
    public int MultiPv { get; set; }
    public TaskCompletionSource<AnalysisResult> CompletionSource { get; set; }

    public StockfishMultiPvRequest(
        string fen,
        int depth,
        int multiPv,
        TaskCompletionSource<AnalysisResult> completionSource,
        CancellationToken cancellationToken)
    {
        Fen = fen;
        Depth = depth;
        MultiPv = multiPv;
        CompletionSource = completionSource;
        CancellationToken = cancellationToken;
    }
}
