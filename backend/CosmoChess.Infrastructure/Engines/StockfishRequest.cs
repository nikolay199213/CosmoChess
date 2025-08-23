namespace CosmoChess.Infrastructure.Engines;

public class StockfishRequest(
    string fen,
    int depth,
    TaskCompletionSource<string> completionSource,
    CancellationToken cancellationToken)
{
    public string Fen { get; set; } = fen;
    public int Depth { get; set; } = depth;
    public TaskCompletionSource<string> CompletionSource { get; set; } = completionSource;
    public CancellationToken CancellationToken { get; set; } = cancellationToken;
}