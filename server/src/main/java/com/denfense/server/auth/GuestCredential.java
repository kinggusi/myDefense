package com.denfense.server.auth;

import com.denfense.server.domain.User;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Entity @Getter @NoArgsConstructor
@Table(name = "auth_guest_credentials")
public class GuestCredential {
    @Id @Column(length = 64) private String secretHash;
    @OneToOne(optional = false) @JoinColumn(name = "user_id", nullable = false, unique = true)
    private User user;
    public GuestCredential(String secretHash, User user) { this.secretHash = secretHash; this.user = user; }
}
