package com.denfense.server.auth;

import com.denfense.server.domain.User;
import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;

/** Local test login only. This does not grant any administrator privileges. */
@Entity @Getter @NoArgsConstructor
@Table(name = "local_developer_credentials")
public class LocalDeveloperCredential {
    @Id @Column(length = 64) private String username;
    @OneToOne(optional = false) @JoinColumn(name = "user_id", nullable = false, unique = true)
    private User user;
    @Column(nullable = false, length = 100) private String passwordHash;

    public LocalDeveloperCredential(String username, User user, String passwordHash) {
        this.username = username; this.user = user; this.passwordHash = passwordHash;
    }
}
