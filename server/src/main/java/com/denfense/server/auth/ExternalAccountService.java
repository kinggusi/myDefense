package com.denfense.server.auth;

import com.denfense.server.domain.User;
import com.denfense.server.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import java.time.Instant;
import java.util.List;
import java.util.Objects;
import java.util.UUID;

@Service @RequiredArgsConstructor
public class ExternalAccountService {
    private final List<ExternalIdentityVerifier> verifiers;
    private final AuthChallengeRepository challenges;
    private final ExternalIdentityRepository identities;
    private final UserRepository users;
    private final AuthService auth;

    public AuthDtos.ProvidersResponse providers() {
        return new AuthDtos.ProvidersResponse(List.of("GOOGLE", "APPLE").stream()
                .map(provider -> new AuthDtos.ProviderInfo(provider,
                        verifiers.stream().anyMatch(v -> v.provider().equals(provider)),
                        verifiers.stream().anyMatch(v -> v.provider().equals(provider)) ? "" : "NOT_CONFIGURED"))
                .toList());
    }

    @Transactional
    public AuthDtos.ChallengeResponse challenge(String provider, String purpose, Long userId) {
        verifier(provider);
        if (!("LOGIN".equals(purpose) && userId == null) && !("LINK".equals(purpose) && userId != null))
            throw AuthException.forbidden();
        String id = UUID.randomUUID().toString();
        String nonce = AuthSecrets.randomToken();
        challenges.save(new AuthChallenge(id, nonce, provider, purpose, userId, Instant.now().plusSeconds(300)));
        return new AuthDtos.ChallengeResponse(id, nonce, 300);
    }

    @Transactional
    public AuthDtos.SessionResponse login(AuthDtos.ExternalRequest request) {
        var identity = verify(request, "LOGIN", null);
        // Returning linked accounts only: new users first create a guest, then explicitly link it.
        ExternalIdentity linked = identities.findByProviderAndIssuerAndSubject(
                request.provider(), identity.issuer(), identity.subject()).orElseThrow(AuthException::unauthorized);
        return auth.createSession(linked.getUser().getId());
    }

    @Transactional
    public AuthDtos.UserInfo link(long userId, AuthDtos.ExternalRequest request) {
        User user = users.findByIdForUpdate(userId).orElseThrow(AuthException::unauthorized);
        var identity = verify(request, "LINK", userId);
        var existing = identities.findByProviderAndIssuerAndSubject(request.provider(), identity.issuer(), identity.subject());
        if (existing.isPresent()) {
            if (existing.get().getUser().getId() != userId) throw AuthException.linkConflict();
        } else {
            identities.saveAndFlush(new ExternalIdentity(request.provider(), identity.issuer(), identity.subject(), user));
        }
        return new AuthDtos.UserInfo(userId, user.getUsername(), false);
    }

    private ExternalIdentityVerifier.VerifiedIdentity verify(AuthDtos.ExternalRequest request, String purpose, Long userId) {
        var verifier = verifier(request.provider());
        var challenge = challenges.findForUpdate(request.challengeId()).orElseThrow(AuthException::unauthorized);
        if (challenge.isConsumed() || !challenge.getExpiresAt().isAfter(Instant.now())
                || !challenge.getProvider().equals(request.provider()) || !challenge.getPurpose().equals(purpose)
                || !Objects.equals(challenge.getUserId(), userId)) throw AuthException.unauthorized();
        var verified = verifier.verify(request.idToken(), challenge.getNonce());
        if (verified == null || verified.issuer() == null || verified.issuer().isBlank()
                || verified.subject() == null || verified.subject().isBlank()) throw AuthException.unauthorized();
        challenge.consume();
        return verified;
    }

    private ExternalIdentityVerifier verifier(String provider) {
        if (!List.of("GOOGLE", "APPLE").contains(provider)) throw AuthException.providerUnavailable();
        return verifiers.stream().filter(v -> v.provider().equals(provider)).findFirst()
                .orElseThrow(AuthException::providerUnavailable);
    }
}
