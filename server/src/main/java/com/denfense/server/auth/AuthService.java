package com.denfense.server.auth;

import com.denfense.server.domain.User;
import com.denfense.server.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import java.time.Instant;
import java.util.UUID;

@Service @RequiredArgsConstructor
public class AuthService {
    public static final long REFRESH_SECONDS = 30L * 24 * 60 * 60;
    private final GuestAccountWriter guestWriter;
    private final GuestCredentialRepository guests;
    private final UserRepository users;
    private final RefreshSessionRepository refreshTokens;
    private final AuthSessionFamilyRepository families;
    private final ExternalIdentityRepository identities;
    private final LocalDeveloperCredentialRepository localCredentials;
    private final JwtTokenService jwt;
    private final org.springframework.transaction.PlatformTransactionManager transactionManager;

    public AuthDtos.SessionResponse guest(String secret) {
        jwt.requireAvailable();
        AuthSecrets.requireSecret(secret);
        String hash = AuthSecrets.hash(secret);
        long userId;
        try { userId = guestWriter.findOrCreate(hash); }
        catch (DataIntegrityViolationException race) {
            userId = guests.findById(hash).orElseThrow(() -> race).getUser().getId();
        }
        final long resolvedUserId = userId;
        return new org.springframework.transaction.support.TransactionTemplate(transactionManager)
                .execute(status -> createSession(resolvedUserId));
    }

    @Transactional
    public AuthDtos.SessionResponse createSession(long userId) {
        jwt.requireAvailable();
        User user = users.findById(userId).orElseThrow(AuthException::unauthorized);
        Instant expiresAt = Instant.now().plusSeconds(REFRESH_SECONDS);
        String familyId = UUID.randomUUID().toString();
        families.saveAndFlush(new AuthSessionFamily(familyId, user, expiresAt));
        return issue(user, familyId, expiresAt);
    }

    // Revocation MUST commit even when replay causes an authentication error.
    @Transactional(noRollbackFor = AuthException.class)
    public AuthDtos.SessionResponse refresh(String token) {
        jwt.requireAvailable();
        AuthSecrets.requireSecret(token);
        String hash = AuthSecrets.hash(token);
        String familyId = refreshTokens.findFamilyIdByHash(hash).orElseThrow(AuthException::unauthorized);
        AuthSessionFamily family = families.findForUpdate(familyId).orElseThrow(AuthException::unauthorized);
        RefreshSession current = refreshTokens.findForUpdate(hash).orElseThrow(AuthException::unauthorized);
        if (!family.activeAt(Instant.now()) || current.isRevoked()) throw AuthException.unauthorized();
        if (current.isConsumed()) {
            family.revoke();
            families.saveAndFlush(family);
            throw AuthException.unauthorized();
        }
        if (!current.getExpiresAt().isAfter(Instant.now())) throw AuthException.unauthorized();
        current.consume();
        refreshTokens.save(current);
        return issue(family.getUser(), family.getId(), family.getExpiresAt());
    }

    @Transactional
    public void logout(String token) {
        AuthSecrets.requireSecret(token);
        refreshTokens.findById(AuthSecrets.hash(token)).ifPresent(session ->
                families.findForUpdate(session.getFamilyId()).ifPresent(AuthSessionFamily::revoke));
    }

    @Transactional(readOnly = true)
    public AccountPrincipal authenticate(String token) {
        var decoded = jwt.decode(token);
        long userId;
        try { userId = Long.parseLong(decoded.getSubject()); }
        catch (NumberFormatException invalid) { throw AuthException.unauthorized(); }
        AuthSessionFamily family = families.findById(decoded.getClaimAsString("sid"))
                .orElseThrow(AuthException::unauthorized);
        if (!family.activeAt(Instant.now()) || family.getUser().getId() != userId) throw AuthException.unauthorized();
        return new AccountPrincipal(userId, family.getUser().getUsername(), family.getId());
    }

    @Transactional(readOnly = true)
    public AuthDtos.UserInfo userInfo(long userId) {
        User user = users.findById(userId).orElseThrow(AuthException::unauthorized);
        return new AuthDtos.UserInfo(user.getId(), user.getUsername(), isGuest(userId));
    }

    private AuthDtos.SessionResponse issue(User user, String familyId, Instant expiresAt) {
        String token = AuthSecrets.randomToken();
        refreshTokens.saveAndFlush(new RefreshSession(AuthSecrets.hash(token), familyId, user, expiresAt));
        return new AuthDtos.SessionResponse(jwt.issue(user.getId(), familyId), token, "Bearer",
                JwtTokenService.ACCESS_SECONDS, Math.max(0, expiresAt.getEpochSecond() - Instant.now().getEpochSecond()),
                new AuthDtos.UserInfo(user.getId(), user.getUsername(), isGuest(user.getId())));
    }

    private boolean isGuest(long userId) {
        return !identities.existsByUserId(userId) && !localCredentials.existsByUserId(userId);
    }
}
