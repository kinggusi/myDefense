package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

@Entity
@Getter
@Setter
@NoArgsConstructor
@Table(name = "user_aliens") // 테이블 이름: user_aliens
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
    public void aleinUpgrade(int cost) {
        if (this.pieces < cost) {
            throw new IllegalStateException("조각이 부족합니다!");
        }
        this.pieces -= cost;
        this.level++;
    }
}
