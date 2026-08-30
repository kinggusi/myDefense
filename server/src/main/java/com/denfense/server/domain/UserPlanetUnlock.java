package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.AccessLevel;
import lombok.Getter;
import lombok.NoArgsConstructor;
import org.hibernate.annotations.OnDelete;
import org.hibernate.annotations.OnDeleteAction;

import java.time.LocalDateTime;

@Entity
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
@Table(name = "user_planet_unlocks", uniqueConstraints = @UniqueConstraint(
        name = "uk_user_planet_unlock_user_map", columnNames = {"user_id", "map_id"}))
public class UserPlanetUnlock {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "user_id", nullable = false)
    @OnDelete(action = OnDeleteAction.CASCADE)
    private User user;

    @Column(name = "map_id", nullable = false, length = 32)
    private String mapId;

    @Enumerated(EnumType.STRING)
    @Column(name = "unlock_source", nullable = false, length = 32)
    private PlanetUnlockSource unlockSource;

    @Column(name = "source_battle_session_id", length = 64)
    private String sourceBattleSessionId;

    @Column(name = "unlocked_at", nullable = false)
    private LocalDateTime unlockedAt;

    public UserPlanetUnlock(User user, String mapId, PlanetUnlockSource unlockSource,
                            String sourceBattleSessionId) {
        this.user = user;
        this.mapId = mapId;
        this.unlockSource = unlockSource;
        this.sourceBattleSessionId = sourceBattleSessionId;
        this.unlockedAt = LocalDateTime.now();
    }
}
