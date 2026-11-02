package com.denfense.server.auth;

import com.nimbusds.jose.JWSAlgorithm;
import com.nimbusds.jose.jwk.JWKSet;
import com.nimbusds.jose.jwk.OctetSequenceKey;
import com.nimbusds.jose.jwk.source.ImmutableJWKSet;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.security.oauth2.jose.jws.MacAlgorithm;
import org.springframework.security.oauth2.jwt.*;
import org.springframework.security.oauth2.core.*;
import org.springframework.stereotype.Service;
import javax.crypto.spec.SecretKeySpec;
import java.time.Instant;
import java.util.Base64;
import java.util.List;
import java.util.UUID;

@Service
public class JwtTokenService {
    public static final long ACCESS_SECONDS = 900;
    public static final String ISSUER = "mydefense-auth";
    public static final String AUDIENCE = "mydefense-api";
    private final JwtEncoder encoder;
    private final JwtDecoder decoder;

    public JwtTokenService(AuthEnvironment environment,
                           @Value("${mydefense.auth.signing-key-base64:}") String configuredKey) {
        byte[] bytes = null;
        if (!configuredKey.isBlank()) {
            try { bytes = Base64.getDecoder().decode(configuredKey); }
            catch (IllegalArgumentException ignored) { /* unavailable, never silently replace a bad configured key */ }
        } else if (environment.isDevelopment()) {
            bytes = new byte[32];
            new java.security.SecureRandom().nextBytes(bytes);
        }
        if (bytes == null || bytes.length < 32) { encoder = null; decoder = null; return; }
        var key = new SecretKeySpec(bytes, "HmacSHA256");
        var jwk = new OctetSequenceKey.Builder(key).keyID("account-v1").algorithm(JWSAlgorithm.HS256).build();
        encoder = new NimbusJwtEncoder(new ImmutableJWKSet<>(new JWKSet(jwk)));
        var jwtDecoder = NimbusJwtDecoder.withSecretKey(key).macAlgorithm(MacAlgorithm.HS256).build();
        OAuth2TokenValidator<Jwt> audience = jwt -> List.of(AUDIENCE).equals(jwt.getAudience())
                ? OAuth2TokenValidatorResult.success()
                : OAuth2TokenValidatorResult.failure(new OAuth2Error("invalid_token"));
        jwtDecoder.setJwtValidator(new DelegatingOAuth2TokenValidator<>(
                JwtValidators.createDefaultWithIssuer(ISSUER), audience));
        decoder = jwtDecoder;
    }
    public void requireAvailable() { if (encoder == null) throw AuthException.unavailable(); }
    public String issue(long userId, String familyId) {
        requireAvailable();
        Instant now = Instant.now();
        var claims = JwtClaimsSet.builder().issuer(ISSUER).audience(List.of(AUDIENCE))
                .subject(Long.toString(userId)).issuedAt(now).expiresAt(now.plusSeconds(ACCESS_SECONDS))
                .id(UUID.randomUUID().toString()).claim("sid", familyId).build();
        var header = JwsHeader.with(MacAlgorithm.HS256).type("JWT").keyId("account-v1").build();
        return encoder.encode(JwtEncoderParameters.from(header, claims)).getTokenValue();
    }
    public Jwt decode(String value) {
        requireAvailable();
        try {
            Jwt jwt = decoder.decode(value);
            Instant now = Instant.now();
            if (!"account-v1".equals(jwt.getHeaders().get("kid")) || !"JWT".equals(jwt.getHeaders().get("typ"))
                    || jwt.getExpiresAt() == null || jwt.getIssuedAt() == null || jwt.getId() == null
                    || jwt.getClaimAsString("sid") == null || jwt.getSubject() == null
                    || !jwt.getSubject().matches("[1-9][0-9]{0,18}")
                    || !jwt.getExpiresAt().isAfter(now)
                    || jwt.getIssuedAt().isAfter(now.plusSeconds(60))
                    || !jwt.getExpiresAt().isAfter(jwt.getIssuedAt())
                    || jwt.getExpiresAt().isAfter(jwt.getIssuedAt().plusSeconds(ACCESS_SECONDS)))
                throw AuthException.unauthorized();
            UUID.fromString(jwt.getId());
            UUID.fromString(jwt.getClaimAsString("sid"));
            return jwt;
        } catch (JwtException | IllegalArgumentException invalid) { throw AuthException.unauthorized(); }
    }
}
