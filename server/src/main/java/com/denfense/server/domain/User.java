package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;

@Entity
@Getter
@Setter
@NoArgsConstructor
@Table(name = "users") // DB 테이블 이름 지정
public class User {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    private String username;
    private String password;
    private int gold;
    private int diamond;
    private int heart;
    private int universalPiece;
    private int growthCell;
    private LocalDateTime lastHeartUpdateTime;

    @OneToMany(mappedBy = "user", cascade = CascadeType.ALL)
    private List<UserAlien> userAliens = new ArrayList<>();

    public User(String username, String password) {
        this.username = username;
        this.password = password;
        this.gold = 0; // 초기 골드는 0으로 시작 (나중에 setGold로 수정 가능)
        this.universalPiece = 0;
        this.growthCell = 0;
    }


    public void decreaseDiamond(int amount){
        int diamond = this.diamond - amount;
        if (diamond < 0) {
            throw new IllegalStateException("다이아가 부족합니다.");
        }
        this.diamond = diamond;
    }

    public void spendGold(int amount) {
        if (amount < 0) {
            throw new com.denfense.server.exception.BusinessException(com.denfense.server.exception.ErrorCode.INVALID_REQUEST, "차감 금액은 0 이상이어야 합니다.");
        }
        if (this.gold < amount) {
            throw new com.denfense.server.exception.BusinessException(com.denfense.server.exception.ErrorCode.INSUFFICIENT_ACCOUNT_GOLD, "골드가 부족합니다.");
        }
        this.gold -= amount;
    }

    public void spendUniversalPiece(int amount) {
        if (amount < 0) {
            throw new com.denfense.server.exception.BusinessException(com.denfense.server.exception.ErrorCode.INVALID_REQUEST, "차감 개수는 0 이상이어야 합니다.");
        }
        if (this.universalPiece < amount) {
            throw new com.denfense.server.exception.BusinessException(com.denfense.server.exception.ErrorCode.INSUFFICIENT_ALIEN_PIECES, "대체 코인이 부족합니다.");
        }
        this.universalPiece -= amount;
    }

    public void spendGrowthCell(int amount) {
        if (amount < 0) {
            throw new com.denfense.server.exception.BusinessException(com.denfense.server.exception.ErrorCode.INVALID_REQUEST, "차감 개수는 0 이상이어야 합니다.");
        }
        if (this.growthCell < amount) {
            throw new com.denfense.server.exception.BusinessException(com.denfense.server.exception.ErrorCode.INSUFFICIENT_GROWTH_CELL, "성장 세포가 부족합니다.");
        }
        this.growthCell -= amount;
    }

    // 하트 계산 로직 (도메인 메서드)
    public void calculateOfflineHearts() {
        int maxNaturalHeart = 100;    // 소프트 캡
        int rechargeMinutes = 15;     // 15분
        int heartsPerInterval = 10;   // 10개씩

        if (this.lastHeartUpdateTime == null) {
            this.lastHeartUpdateTime = LocalDateTime.now();
            this.heart = maxNaturalHeart; // 미안하니까(?) 하트를 꽉 채워줍니다.
            return;
        }

        // 1. 이미 100개 이상이면 시간만 갱신하고 종료
        if (this.heart >= maxNaturalHeart) {
            this.lastHeartUpdateTime = LocalDateTime.now();
            return;
        }

        LocalDateTime now = LocalDateTime.now();
        long minutesPassed = java.time.Duration.between(this.lastHeartUpdateTime, now).toMinutes();

        // 2. 15분이 지났을 때만 계산
        if (minutesPassed >= rechargeMinutes) {
            int intervals = (int) (minutesPassed / rechargeMinutes);
            int earnedHearts = intervals * heartsPerInterval;
            int newHeart = this.heart + earnedHearts;

            if (newHeart >= maxNaturalHeart) {
                this.heart = maxNaturalHeart;
                this.lastHeartUpdateTime = now;
            } else {
                this.heart = newHeart;
                // 사용한 시간만큼만 갱신 (자투리 시간 보존)
                this.lastHeartUpdateTime = this.lastHeartUpdateTime.plusMinutes((long) intervals * rechargeMinutes);
            }
        }
    }
}