package com.denfense.server.auth;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.SecureRandom;
import java.util.Base64;
import java.util.HexFormat;

final class AuthSecrets {
    private static final SecureRandom RANDOM = new SecureRandom();
    private AuthSecrets() {}
    static String randomToken() {
        byte[] bytes = new byte[32];
        RANDOM.nextBytes(bytes);
        return Base64.getUrlEncoder().withoutPadding().encodeToString(bytes);
    }
    static String hash(String value) {
        try {
            return HexFormat.of().formatHex(MessageDigest.getInstance("SHA-256")
                    .digest(value.getBytes(StandardCharsets.UTF_8)));
        } catch (java.security.NoSuchAlgorithmException impossible) {
            throw new IllegalStateException("SHA-256 is unavailable", impossible);
        }
    }
    static void requireSecret(String secret) {
        if (secret == null || !secret.matches("[A-Za-z0-9_-]{43}")) throw AuthException.unauthorized();
        try {
            byte[] decoded = Base64.getUrlDecoder().decode(secret);
            if (decoded.length != 32 || !Base64.getUrlEncoder().withoutPadding().encodeToString(decoded).equals(secret))
                throw AuthException.unauthorized();
        } catch (IllegalArgumentException invalid) { throw AuthException.unauthorized(); }
    }
}
