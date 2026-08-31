package com.denfense.server.domain;

import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
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
@Table(name = "battle_entry_reservations", uniqueConstraints = @UniqueConstraint(
        name = "uk_battle_entry_reservation_session", columnNames = "battle_session_id"))
public class BattleEntryReservation {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "battle_session_id", nullable = false, length = 64)
    private String battleSessionId;

    @Column(name = "map_id", nullable = false, length = 32)
    private String mapId;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "player_one_user_id", nullable = false)
    @OnDelete(action = OnDeleteAction.CASCADE)
    private User playerOne;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "player_two_user_id", nullable = false)
    @OnDelete(action = OnDeleteAction.CASCADE)
    private User playerTwo;

    @Column(name = "heart_cost", nullable = false)
    private int heartCost;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false, length = 16)
    private BattleEntryStatus status;

    @Enumerated(EnumType.STRING)
    @Column(name = "refund_reason", length = 32)
    private BattleEntryRefundReason refundReason;

    @Column(name = "created_at", nullable = false)
    private LocalDateTime createdAt;

    @Column(name = "updated_at", nullable = false)
    private LocalDateTime updatedAt;

    public BattleEntryReservation(String battleSessionId, String mapId, User playerOne,
                                  User playerTwo, int heartCost) {
        this.battleSessionId = battleSessionId;
        this.mapId = mapId;
        this.playerOne = playerOne;
        this.playerTwo = playerTwo;
        this.heartCost = heartCost;
        this.status = BattleEntryStatus.CHARGED;
        this.createdAt = LocalDateTime.now();
        this.updatedAt = createdAt;
    }

    public boolean matches(String requestedMapId, Long playerOneId, Long playerTwoId) {
        return mapId.equals(requestedMapId)
                && playerOne.getId().equals(playerOneId)
                && playerTwo.getId().equals(playerTwoId);
    }

    public void complete() {
        if (status == BattleEntryStatus.REFUNDED) {
            throw new BusinessException(ErrorCode.BATTLE_ENTRY_REFUNDED);
        }
        if (status == BattleEntryStatus.CHARGED) {
            status = BattleEntryStatus.COMPLETED;
            updatedAt = LocalDateTime.now();
        }
    }

    public boolean refund(BattleEntryRefundReason reason) {
        if (status == BattleEntryStatus.COMPLETED) {
            throw new BusinessException(ErrorCode.BATTLE_ENTRY_REFUND_INVALID);
        }
        if (status == BattleEntryStatus.REFUNDED) return false;
        status = BattleEntryStatus.REFUNDED;
        refundReason = reason;
        updatedAt = LocalDateTime.now();
        return true;
    }
}
