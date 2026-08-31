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
    private int accountLevel = 1;
    private LocalDateTime lastHeartUpdateTime;

    @OneToMany(mappedBy = "user", cascade = CascadeType.ALL)
    private List<UserAlien> userAliens = new ArrayList<>();

    public User(String username, String password) {
        this.username = username;
        this.password = password;
        this.gold = 0; // 초기 골드는 0으로 시작 (나중에 setGold로 수정 가능)
        this.universalPiece = 0;
        this.growthCell = 0;
        this.accountLevel = 1;
    }


    public void decreaseDiamond(int amount){
        int diamond = this.diamond - amount;
        if (diamond < 0) {
            throw new IllegalStateException("다이아가 부족합니다.");
        }
        this.diamond = diamond;
    }

    public void spendDiamond(int amount) {
        if (amount < 0) {
            throw new com.denfense.server.exception.BusinessException(com.denfense.server.exception.ErrorCode.INVALID_REQUEST);
        }
        if (diamond < amount) {
            throw new com.denfense.server.exception.BusinessException(com.denfense.server.exception.ErrorCode.INSUFFICIENT_DIAMOND);
        }
        diamond -= amount;
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

    public void earnGold(int amount) {
        if (amount < 0) {
            throw new com.denfense.server.exception.BusinessException(com.denfense.server.exception.ErrorCode.INVALID_REQUEST, "지급 금액은 0 이상이어야 합니다.");
        }
        long newGold = (long) this.gold + amount;
        if (newGold > Integer.MAX_VALUE) {
            this.gold = Integer.MAX_VALUE;
        } else {
            this.gold = (int) newGold;
        }
    }

    public void earnUniversalPiece(int amount) {
        if (amount < 0) throw new com.denfense.server.exception.BusinessException(com.denfense.server.exception.ErrorCode.INVALID_REQUEST, "대체 코인 지급량은 0 이상이어야 합니다.");
        long value = (long) this.universalPiece + amount;
        this.universalPiece = value > Integer.MAX_VALUE ? Integer.MAX_VALUE : (int) value;
    }

    public void earnDiamond(int amount) {
        if (amount < 0) throw new com.denfense.server.exception.BusinessException(com.denfense.server.exception.ErrorCode.INVALID_REQUEST, "젬 지급량은 0 이상이어야 합니다.");
        long value = (long) this.diamond + amount;
        this.diamond = value > Integer.MAX_VALUE ? Integer.MAX_VALUE : (int) value;
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
    /**
     * @deprecated Use {@link com.denfense.server.service.HeartPolicy} directly.
     * This method alters entity state and may cause unintended DB updates in read-only scenarios.
     */
    @Deprecated
    public void calculateOfflineHearts() {
        com.denfense.server.service.HeartSnapshot snapshot = new com.denfense.server.service.HeartPolicy().calculate(this.heart, this.lastHeartUpdateTime);
        this.heart = snapshot.calculatedHeart();
        this.lastHeartUpdateTime = snapshot.effectiveLastHeartUpdateTime();
    }

    public void applyHeartSnapshot(com.denfense.server.service.HeartSnapshot snapshot) {
        this.heart = snapshot.calculatedHeart();
        this.lastHeartUpdateTime = snapshot.effectiveLastHeartUpdateTime();
    }

    public void spendHeart(int amount) {
        if (amount <= 0) {
            throw new com.denfense.server.exception.BusinessException(com.denfense.server.exception.ErrorCode.INVALID_REQUEST, "소비할 하트 개수는 양수여야 합니다.");
        }
        if (this.heart < amount) {
            throw new com.denfense.server.exception.BusinessException(com.denfense.server.exception.ErrorCode.INSUFFICIENT_HEART, "하트가 부족합니다.");
        }
        this.heart -= amount;
    }

    public void refundHeart(int amount) {
        if (amount <= 0) {
            throw new com.denfense.server.exception.BusinessException(com.denfense.server.exception.ErrorCode.INVALID_REQUEST,
                    "반환할 하트 개수는 양수여야 합니다.");
        }
        this.heart = Math.min(com.denfense.server.service.HeartPolicy.MAX_HEART, this.heart + amount);
    }
}
