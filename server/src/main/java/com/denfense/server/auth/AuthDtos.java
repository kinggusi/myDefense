package com.denfense.server.auth;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;
import java.util.List;

public final class AuthDtos {
    private AuthDtos() {}
    public record GuestRequest(@NotBlank @Size(min = 43, max = 43) String guestSecret) {}
    public record RefreshRequest(@NotBlank @Size(max = 128) String refreshToken) {}
    public record UserInfo(long userId, String username, boolean isGuest) {}
    public record SessionResponse(String accessToken, String refreshToken, String tokenType,
                                  long expiresIn, long refreshExpiresIn, UserInfo user) {}
    public record ProviderInfo(String provider, boolean available, String reason) {}
    public record ProvidersResponse(List<ProviderInfo> providers) {}
    public record ChallengeRequest(@NotBlank String provider, @NotBlank String purpose) {}
    public record ChallengeResponse(String challengeId, String nonce, long expiresIn) {}
    public record ExternalRequest(@NotBlank String provider, @NotBlank @Size(max = 16384) String idToken,
                                  @NotBlank String challengeId) {}
}
