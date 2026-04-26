using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AutoParts.Application.Common.Interfaces;
using AutoParts.Application.Common.Models;
using AutoParts.Application.Features.Auth;
using Google.Apis.Auth;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutoParts.Infrastructure.Identity;

public class GoogleSignInCommandHandler : IRequestHandler<GoogleSignInCommand, AuthResult>
{
    private readonly IIdentityService _identity;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleSignInCommandHandler> _log;
    private static readonly HttpClient _http = new();

    public GoogleSignInCommandHandler(
        IIdentityService identity,
        IConfiguration configuration,
        ILogger<GoogleSignInCommandHandler> log)
    {
        _identity = identity;
        _configuration = configuration;
        _log = log;
    }

    private class TokenExchangeResponse
    {
        [JsonPropertyName("id_token")] public string? IdToken { get; set; }
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("error")] public string? Error { get; set; }
        [JsonPropertyName("error_description")] public string? ErrorDescription { get; set; }
    }

    public async Task<AuthResult> Handle(GoogleSignInCommand request, CancellationToken ct)
    {
        var clientId = _configuration["GoogleAuth:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            return AuthResult.Failure("Google sign-in is not configured on the server.");

        // Resolve the ID token: either passed in directly or obtained by exchanging the auth code.
        string? idToken = request.IdToken;

        if (string.IsNullOrWhiteSpace(idToken) && !string.IsNullOrWhiteSpace(request.Code))
        {
            var clientSecret = _configuration["GoogleAuth:ClientSecret"];
            var redirectUri = _configuration["GoogleAuth:RedirectUri"] ?? "postmessage";

            if (string.IsNullOrWhiteSpace(clientSecret))
                return AuthResult.Failure("Google client secret is not configured on the server.");

            try
            {
                var form = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string,string>("client_id", clientId),
                    new KeyValuePair<string,string>("client_secret", clientSecret),
                    new KeyValuePair<string,string>("code", request.Code!),
                    new KeyValuePair<string,string>("grant_type", "authorization_code"),
                    new KeyValuePair<string,string>("redirect_uri", redirectUri),
                });

                var resp = await _http.PostAsync("https://oauth2.googleapis.com/token", form, ct);
                var body = await resp.Content.ReadFromJsonAsync<TokenExchangeResponse>(cancellationToken: ct);

                if (!resp.IsSuccessStatusCode || body is null || string.IsNullOrWhiteSpace(body.IdToken))
                {
                    _log.LogWarning("Google code exchange failed: {Error} {Description}", body?.Error, body?.ErrorDescription);
                    return AuthResult.Failure(body?.ErrorDescription ?? "Google sign-in failed.");
                }
                idToken = body.IdToken;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Google code exchange threw.");
                return AuthResult.Failure("Could not reach Google to complete sign-in.");
            }
        }

        if (string.IsNullOrWhiteSpace(idToken))
            return AuthResult.Failure("Missing Google credentials.");

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                });
        }
        catch (InvalidJwtException ex)
        {
            _log.LogWarning(ex, "Google ID token validation failed.");
            return AuthResult.Failure("Google sign-in token is invalid or expired.");
        }

        if (string.IsNullOrWhiteSpace(payload.Email))
            return AuthResult.Failure("Google account does not have an email address.");
        if (!payload.EmailVerified)
            return AuthResult.Failure("Google account email is not verified.");

        var fullName = payload.Name ?? $"{payload.GivenName} {payload.FamilyName}".Trim();
        return await _identity.SignInWithExternalAsync(payload.Email, fullName, "Google");
    }
}
