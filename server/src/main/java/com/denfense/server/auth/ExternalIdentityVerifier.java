package com.denfense.server.auth;

/** A configured adapter MUST verify signature, allowed issuer/audience, expiry and the exact nonce.
 * No live provider adapter is installed in the first guest-login slice. */
public interface ExternalIdentityVerifier {
    String provider();
    VerifiedIdentity verify(String idToken, String expectedNonce);
    record VerifiedIdentity(String issuer, String subject) {}
}
