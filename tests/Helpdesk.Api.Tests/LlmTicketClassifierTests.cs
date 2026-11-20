using Helpdesk.Api.Classification;
using Helpdesk.Api.Domain;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace Helpdesk.Api.Tests;

public class LlmTicketClassifierTests
{
    private sealed class FakeChatClient(Func<IList<ChatMessage>, string> answer) : IChatClient
    {
        public IList<ChatMessage>? LastMessages { get; private set; }

        public ChatOptions? LastOptions { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            LastMessages = messages.ToList();
            LastOptions = options;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, answer(LastMessages))));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private static LlmTicketClassifier Classifier(Func<IList<ChatMessage>, string> answer) =>
        new(new FakeChatClient(answer), NullLogger<LlmTicketClassifier>.Instance);

    [Fact]
    public async Task ValidJson_IsReadAsTypedClassification()
    {
        var classifier = Classifier(_ => """{ "category": "Network", "priority": "High" }""");

        var result = await classifier.ClassifyAsync("VPN impossible", "Le VPN refuse la connexion.");

        Assert.Equal(new TicketClassification(TicketCategory.Network, TicketPriority.High), result);
    }

    [Fact]
    public async Task UnknownCategory_ReturnsNull()
    {
        var classifier = Classifier(_ => """{ "category": "Plomberie", "priority": "High" }""");

        Assert.Null(await classifier.ClassifyAsync("Fuite d'eau", "Il y a une fuite sous l'évier."));
    }

    [Fact]
    public async Task FreeTextAnswer_ReturnsNull()
    {
        var classifier = Classifier(_ => "Catégorie : réseau, priorité haute.");

        Assert.Null(await classifier.ClassifyAsync("VPN impossible", "Le VPN refuse la connexion."));
    }

    [Fact]
    public async Task UnreachableModel_ReturnsNull()
    {
        var classifier = Classifier(_ => throw new HttpRequestException("Connexion refusée"));

        Assert.Null(await classifier.ClassifyAsync("VPN impossible", "Le VPN refuse la connexion."));
    }

    [Fact]
    public async Task CallerCancellation_IsNotSwallowed()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var classifier = Classifier(_ => throw new OperationCanceledException(cts.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            classifier.ClassifyAsync("VPN impossible", "Le VPN refuse la connexion.", cts.Token));
    }

    [Fact]
    public async Task Request_AsksForJsonAndSendsTicketAfterRules()
    {
        var chat = new FakeChatClient(_ => """{ "category": "Printing", "priority": "Low" }""");
        var classifier = new LlmTicketClassifier(chat, NullLogger<LlmTicketClassifier>.Instance);

        await classifier.ClassifyAsync("Imprimante en panne", "L'imprimante du 2e étage ne répond plus.");

        Assert.IsType<ChatResponseFormatJson>(chat.LastOptions?.ResponseFormat);
        Assert.Equal(0f, chat.LastOptions?.Temperature);

        var messages = chat.LastMessages!;
        Assert.Equal(ChatRole.System, messages[0].Role);
        Assert.Contains("Printing", messages[0].Text);
        Assert.Equal(ChatRole.User, messages[^1].Role);
        Assert.Contains("Imprimante en panne", messages[^1].Text);
    }
}