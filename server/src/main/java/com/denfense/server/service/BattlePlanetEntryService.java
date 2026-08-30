package com.denfense.server.service;

import com.denfense.server.domain.*;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.BattleEntryReservationRepository;
import com.denfense.server.repository.BattleSettlementRepository;
import com.denfense.server.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Service
@RequiredArgsConstructor
public class BattlePlanetEntryService {
    public static final int HEART_COST = 1;

    private final BattleEntryReservationRepository reservations;
    private final BattleSettlementRepository settlements;
    private final UserRepository users;
    private final BattleEntryReservationWriter writer;

    public EntryResult reserve(String battleSessionId, String mapId, Long playerOneId, Long playerTwoId) {
        String session = battleSessionId.trim();
        String map = mapId.trim();
        if (playerOneId.equals(playerTwoId)) {
            throw new BusinessException(ErrorCode.BATTLE_ROSTER_REGISTRATION_INVALID);
        }
        try {
            var created = writer.create(session, map, playerOneId, playerTwoId);
            return new EntryResult(created.status(), created.alreadyProcessed());
        } catch (DataIntegrityViolationException uniqueConflict) {
            BattleEntryReservation winner = reservations.findByBattleSessionId(session).orElse(null);
            if (winner == null || !winner.matches(map, playerOneId, playerTwoId)) {
                throw new BusinessException(ErrorCode.BATTLE_ENTRY_CONFLICT);
            }
            if (winner.getStatus() == BattleEntryStatus.REFUNDED) {
                throw new BusinessException(ErrorCode.BATTLE_ENTRY_REFUNDED);
            }
            return new EntryResult(winner.getStatus(), true);
        }
    }

    @Transactional
    public RefundResult refund(String battleSessionId, BattleEntryRefundReason reason) {
        if (reason == null) throw new BusinessException(ErrorCode.BATTLE_ENTRY_REFUND_INVALID);
        String session = battleSessionId.trim();
        BattleEntryReservation snapshot = reservations.findByBattleSessionId(session)
                .orElseThrow(() -> new BusinessException(ErrorCode.BATTLE_ENTRY_REFUND_INVALID));
        List<User> locked = users.findAllByIdInForUpdate(
                List.of(snapshot.getPlayerOne().getId(), snapshot.getPlayerTwo().getId()));
        BattleEntryReservation reservation = reservations.findByBattleSessionIdForUpdate(session)
                .orElseThrow(() -> new BusinessException(ErrorCode.BATTLE_ENTRY_REFUND_INVALID));
        if (settlements.findByBattleSessionId(session).isPresent()) {
            throw new BusinessException(ErrorCode.BATTLE_ENTRY_REFUND_INVALID);
        }
        if (reservation.getStatus() == BattleEntryStatus.REFUNDED) {
            return new RefundResult(reservation.getStatus(), true);
        }
        boolean refunded = reservation.refund(reason);
        if (refunded) {
            locked.forEach(user -> user.refundHeart(reservation.getHeartCost()));
        }
        return new RefundResult(reservation.getStatus(), !refunded);
    }

    @Transactional
    public void assertUsable(String battleSessionId) {
        reservations.findByBattleSessionId(battleSessionId).ifPresent(reservation -> {
            if (reservation.getStatus() == BattleEntryStatus.REFUNDED) {
                throw new BusinessException(ErrorCode.BATTLE_ENTRY_REFUNDED);
            }
        });
    }

    @Transactional
    public void completeIfReserved(String battleSessionId) {
        reservations.findByBattleSessionIdForUpdate(battleSessionId)
                .ifPresent(BattleEntryReservation::complete);
    }

    public record EntryResult(BattleEntryStatus status, boolean alreadyProcessed) {
    }

    public record RefundResult(BattleEntryStatus status, boolean alreadyProcessed) {
    }
}
