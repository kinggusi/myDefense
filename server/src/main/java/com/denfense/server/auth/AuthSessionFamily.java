package com.denfense.server.auth;
import com.denfense.server.domain.User;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import java.time.Instant;

@Entity @Getter @NoArgsConstructor @Table(name = "auth_session_families")
public class AuthSessionFamily {
    @Id @Column(length = 36) private String id;
    @ManyToOne(optional = false) @JoinColumn(name = "user_id", nullable = false) private User user;
    @Column(nullable = false) private Instant expiresAt;
    private boolean revoked;
    public AuthSessionFamily(String id, User user, Instant expiresAt) {
        this.id = id; this.user = user; this.expiresAt = expiresAt;
    }
    public void revoke() { revoked = true; }
    public boolean activeAt(Instant instant) { return !revoked && expiresAt.isAfter(instant); }
}
