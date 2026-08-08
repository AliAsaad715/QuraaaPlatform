using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.AiAssistant.Interfaces;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Quraaa.Infrastructure.Services
{
    // Raw HttpClient + hand-rolled JSON, matching GoogleBooksService's style
    // rather than pulling in an SDK — OpenAI's chat/completions endpoint is a
    // plain REST/JSON call, nowhere near the complexity that justified using
    // the Stripe.net package for the checkout flow.
    public class OpenAiService : IOpenAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;
        private readonly ILogger<OpenAiService> _logger;

        public OpenAiService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            // Default kept in code as a documented fallback; override via
            // config without a redeploy. GPT-5.6 Luna is a comparable,
            // slightly newer alternative at the same cost tier if preferred.
            _model = configuration["OpenAi:Model"] ?? configuration["OpenAi__Model"] ?? "gpt-5.4-mini";
            var apiKey = configuration["OpenAi:ApiKey"] ?? configuration["OpenAi__ApiKey"] ?? string.Empty;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<string?> GetCompletionAsync(
            string systemPrompt, string userMessage, int maxTokens, CancellationToken cancellationToken = default)
        {
            var request = new ChatCompletionRequest(
                Model: _model,
                Messages:
                [
                    new ChatMessage("system", systemPrompt),
                    new ChatMessage("user", userMessage)
                ],
                MaxTokens: maxTokens,
                Temperature: 0.3);

            try
            {
                var response = await _httpClient.PostAsJsonAsync("chat/completions", request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        // Expected under the free tier's request-per-minute cap, not
                        // exceptional — the resilience handler on this HttpClient (see
                        // InfrastructureDependencyInjectionHandler) already retried with
                        // backoff before this response got here, so reaching this branch
                        // means retries are exhausted. Retry-After is logged (when the
                        // API sent one) to distinguish rate-limit throttling from an
                        // actual outage when reading logs.
                        var retryAfter = response.Headers.RetryAfter?.Delta?.ToString()
                            ?? response.Headers.RetryAfter?.Date?.ToString()
                            ?? "not provided";
                        _logger.LogWarning(
                            "OpenAI API rate-limited (429) after retries. Retry-After: {RetryAfter}", retryAfter);
                    }
                    else
                    {
                        _logger.LogWarning("OpenAI API returned {StatusCode}", response.StatusCode);
                    }

                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(
                    cancellationToken: cancellationToken);

                return result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
            }
            catch (Exception ex)
            {
                // Same fail-soft shape as GoogleBooksService: caller sees
                // null, treats it as "unavailable," never an unhandled 500.
                _logger.LogError(ex, "OpenAI API call failed");
                return null;
            }
        }

        private record ChatCompletionRequest(
            [property: JsonPropertyName("model")] string Model,
            [property: JsonPropertyName("messages")] List<ChatMessage> Messages,
            [property: JsonPropertyName("max_tokens")] int MaxTokens,
            [property: JsonPropertyName("temperature")] double Temperature);

        private record ChatMessage(
            [property: JsonPropertyName("role")] string Role,
            [property: JsonPropertyName("content")] string Content);

        private record ChatCompletionResponse(
            [property: JsonPropertyName("choices")] List<ChatChoice>? Choices);

        private record ChatChoice(
            [property: JsonPropertyName("message")] ChatMessage? Message);
    }
}