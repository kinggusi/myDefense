package com.denfense.server.auth;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import java.time.Instant;

@Entity @Getter @NoArgsConstructor @Table(name = "auth_challenges")
public class AuthChallenge {
    @Id @Column(length = 36) private String id;
    @Column(nullable = false, length = 43) private String nonce;
    @Column(nullable = false, length = 16) private String provider;
    @Column(nullable = false, length = 8) private String purpose;
    private Long userId;
    @Column(nullable = false) private Instant expiresAt;
    private boolean consumed;
    public AuthChallenge(String id, String nonce, String provider, String purpose, Long userId, Instant expiresAt) {
        this.id = id; this.nonce = nonce; this.provider = provider; this.purpose = purpose;
        this.userId = userId; this.expiresAt = expiresAt;
    }
    public void consume() { consumed = true; }
}
