package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.AccessLevel;
import lombok.Getter;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;

@Entity
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
@Table(name = "quest_settlement_applications", uniqueConstraints = @UniqueConstraint(
        name = "uk_quest_settlement_user_condition",
        columnNames = {"battle_settlement_id", "user_id", "quest_condition_id"}))
public class QuestSettlementApplication {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "battle_settlement_id", nullable = false)
    private BattleSettlement battleSettlement;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "user_id", nullable = false)
    private User user;

    @Column(name = "quest_condition_id", nullable = false, length = 128)
    private String questConditionId;

    @Column(nullable = false)
    private long appliedAmount;

    @Column(nullable = false)
    private LocalDateTime appliedAt;

    public QuestSettlementApplication(BattleSettlement battleSettlement, User user,
                                      String questConditionId, long appliedAmount) {
        this.battleSettlement = battleSettlement;
        this.user = user;
        this.questConditionId = questConditionId;
        this.appliedAmount = appliedAmount;
        this.appliedAt = LocalDateTime.now();
    }
}
