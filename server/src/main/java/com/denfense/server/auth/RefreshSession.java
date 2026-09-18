package com.denfense.server.auth;

import com.denfense.server.domain.User;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import java.time.Instant;

@Entity @Getter @NoArgsConstructor
@Table(name = "auth_refresh_sessions", indexes = @Index(name = "ix_auth_refresh_family", columnList = "familyId"))
public class RefreshSession {
    @Id @Column(length = 64) private String tokenHash;
    @Column(nullable = false, length = 36) private String familyId;
    @ManyToOne(optional = false) @JoinColumn(name = "user_id", nullable = false) private User user;
    @Column(nullable = false) private Instant expiresAt;
    private boolean consumed;
    private boolean revoked;
    public RefreshSession(String tokenHash, String familyId, User user, Instant expiresAt) {
        this.tokenHash = tokenHash; this.familyId = familyId; this.user = user; this.expiresAt = expiresAt;
    }
    public void consume() { consumed = true; }
    public void revoke() { revoked = true; }
}
