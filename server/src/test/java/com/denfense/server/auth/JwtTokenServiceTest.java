package com.denfense.server.auth;

import com.nimbusds.jose.*;
import com.nimbusds.jose.crypto.MACSigner;
import com.nimbusds.jwt.*;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;
import org.springframework.mock.env.MockEnvironment;
import java.time.Instant;
import java.util.*;
import static org.assertj.core.api.Assertions.*;

class JwtTokenServiceTest {
    private static final byte[] KEY = new byte[32]; // Test fixture only, never application configuration.
    private final JwtTokenService service = new JwtTokenService(new AuthEnvironment(new MockEnvironment()),
            Base64.getEncoder().encodeToString(KEY));

    @Test void roundTrip() {
        String family = UUID.randomUUID().toString();
        var jwt = service.decode(service.issue(42, family));
        assertThat(jwt.getSubject()).isEqualTo("42");
        assertThat(jwt.getClaimAsString("sid")).isEqualTo(family);
    }

    @ParameterizedTest @ValueSource(strings = {"", "prod", "production", "local,prod", "dev,production"})
    void absentKeyFailsClosedOutsideExplicitDevelopment(String profiles) {
        var env = new MockEnvironment();
        if (!profiles.isEmpty()) env.setActiveProfiles(profiles.split(","));
        assertThatThrownBy(() -> new JwtTokenService(new AuthEnvironment(env), "").requireAvailable())
                .isInstanceOf(AuthException.class);
    }

    @Test void defaultLocalIsNotExplicitLocalAndInvalidConfiguredKeyNeverFallsBack() {
        var env = new MockEnvironment(); env.setDefaultProfiles("local");
        assertThat(new AuthEnvironment(env).isDevelopment()).isFalse();
        env.setActiveProfiles("local");
        new JwtTokenService(new AuthEnvironment(env), "").requireAvailable();
        assertThatThrownBy(() -> new JwtTokenService(new AuthEnvironment(env), "bad-key").requireAvailable())
                .isInstanceOf(AuthException.class);
    }

    @ParameterizedTest @ValueSource(strings = {"aud", "iss", "expired", "future", "duration", "order",
            "iat", "exp", "jti", "sid", "sub", "kid", "typ", "signature", "blank"})
    void signedButInvalidClaimsAreRejected(String invalid) throws Exception {
        Instant now = Instant.now();
        var claims = new JWTClaimsSet.Builder().issuer(JwtTokenService.ISSUER).audience(JwtTokenService.AUDIENCE)
                .subject("42").issueTime(Date.from(now)).expirationTime(Date.from(now.plusSeconds(900)))
                .jwtID(UUID.randomUUID().toString()).claim("sid", UUID.randomUUID().toString());
        switch (invalid) {
            case "aud" -> claims.audience((List<String>) null);
            case "iss" -> claims.issuer("other");
            case "expired" -> claims.issueTime(Date.from(now.minusSeconds(1000))).expirationTime(Date.from(now.minusSeconds(1)));
            case "future" -> claims.issueTime(Date.from(now.plusSeconds(120))).expirationTime(Date.from(now.plusSeconds(900)));
            case "duration" -> claims.expirationTime(Date.from(now.plusSeconds(901)));
            case "order" -> claims.issueTime(Date.from(now.plusSeconds(50))).expirationTime(Date.from(now.plusSeconds(40)));
            case "iat" -> claims.issueTime(null);
            case "exp" -> claims.expirationTime(null);
            case "jti" -> claims.jwtID("not-a-uuid");
            case "sid" -> claims.claim("sid", "");
            case "sub" -> claims.subject("0");
            case "blank" -> claims.subject("");
        }
        var header = new JWSHeader.Builder(JWSAlgorithm.HS256)
                .type(new JOSEObjectType(invalid.equals("typ") ? "OTHER" : "JWT"))
                .keyID(invalid.equals("kid") ? "other-key" : "account-v1").build();
        var jwt = new SignedJWT(header, claims.build());
        byte[] signingKey = KEY.clone(); if (invalid.equals("signature")) signingKey[0] = 1;
        jwt.sign(new MACSigner(signingKey));
        assertThatThrownBy(() -> service.decode(jwt.serialize())).isInstanceOf(AuthException.class);
    }
}
