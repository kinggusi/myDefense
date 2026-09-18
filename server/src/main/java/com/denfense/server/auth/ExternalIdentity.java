package com.denfense.server.auth;
import com.denfense.server.domain.User;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Entity @Getter @NoArgsConstructor
@Table(name = "auth_external_identities", uniqueConstraints = @UniqueConstraint(
        name = "uk_auth_external_identity", columnNames = {"provider", "issuer", "subject"}))
public class ExternalIdentity {
    @Id @GeneratedValue(strategy = GenerationType.IDENTITY) private Long id;
    @Column(nullable = false, length = 16) private String provider;
    @Column(nullable = false, length = 256) private String issuer;
    @Column(nullable = false, length = 256) private String subject;
    @ManyToOne(optional = false) @JoinColumn(name = "user_id", nullable = false) private User user;
    public ExternalIdentity(String provider, String issuer, String subject, User user) {
        this.provider = provider; this.issuer = issuer; this.subject = subject; this.user = user;
    }
}
