package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Entity
@Getter
@Setter
@NoArgsConstructor
@Table(name = "user_aliens", uniqueConstraints = {
    @UniqueConstraint(name = "uk_user_alien_user_spec", columnNames = {"user_id", "alien_id"})
})
public class UserAlien {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "user_id")
    private User user;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "alien_id")
    private AlienSpec alienSpec;

    private int level;
    private int pieces;

    public UserAlien(User user, AlienSpec alienSpec) {
        this.user = user;
        this.alienSpec = alienSpec;
        this.level = 1; // 처음 얻으면 1레벨
        this.pieces = 0;
    }

    // 조각 추가 및 레벨업 체크 로직
    public void addPieces(int cnt) {
        this.pieces += cnt;
    }

    // 강화 시 조각 소모 및 레벨업
    public void upgradeAlien(int usedPieces) {
        if (this.pieces < usedPieces) {
            throw new com.denfense.server.exception.BusinessException(com.denfense.server.exception.ErrorCode.INSUFFICIENT_ALIEN_PIECES, "조각이 부족합니다!");
        }
        this.pieces -= usedPieces;
        this.level++;
    }
}
