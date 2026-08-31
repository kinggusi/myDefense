package com.denfense.server.service;

import com.denfense.server.domain.BattleEntryStatus;
import com.denfense.server.domain.BattlePlayerSettlement;
import com.denfense.server.domain.BattleResult;
import com.denfense.server.domain.BattleSettlement;
import com.denfense.server.domain.User;
import com.denfense.server.dto.battle.BattleSettlementDtos;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.BattleEntryReservationRepository;
import com.denfense.server.repository.BattlePlayerSettlementRepository;
import com.denfense.server.repository.BattleSettlementRepository;
import com.denfense.server.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Propagation;
import org.springframework.transaction.annotation.Transactional;

import java.util.Map;
import java.util.function.Function;
import java.util.stream.Collectors;

@Component
@RequiredArgsConstructor
public class BattleSettlementWriter {
    private final BattleSettlementRepository settlements;
    private final BattlePlayerSettlementRepository players;
    private final UserRepository users;
    private final BattleEntryReservationRepository entries;

    @Transactional(propagation = Propagation.REQUIRES_NEW)
    public WriteResult create(BattleSettlementDtos.Request request) {
        Map<Integer, User> usersBySlot = request.players().stream().collect(Collectors.toMap(
                BattleSettlementDtos.Player::playerSlot,
                player -> users.findByUsername(player.playerId())
                        .orElseThrow(() -> new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH))));

        // Fail closed and serialize a trusted Heart refund with Settlement creation.
        var entry = entries.findByBattleSessionIdForUpdate(request.battleSessionId())
                .orElseThrow(() -> new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH));
        if (entry.getStatus() == BattleEntryStatus.REFUNDED) {
            throw new BusinessException(ErrorCode.BATTLE_ENTRY_REFUNDED);
        }
        if (!entry.matches(request.mapId(), usersBySlot.get(1).getId(), usersBySlot.get(2).getId())) {
            throw new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH);
        }
        if (entry.getStatus() == BattleEntryStatus.COMPLETED) {
            BattleSettlement existing = settlements.findByBattleSessionId(request.battleSessionId())
                    .orElseThrow(() -> new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH));
            return new WriteResult(existing, false);
        }
        if (entry.getStatus() != BattleEntryStatus.CHARGED) {
            throw new BusinessException(ErrorCode.BATTLE_PARTICIPANT_MISMATCH);
        }

        BattleSettlement settlement = new BattleSettlement(
                request.battleSessionId(), request.requestId(), request.summaryHash(),
                request.balanceVersion(), request.contentHash(), BattleResult.valueOf(request.result()),
                request.finalWave(), request.mapId(), request.startedAt(), request.finishedAt());
        settlements.save(settlement);
        for (BattleSettlementDtos.Player player : request.players()) {
            User user = usersBySlot.get(player.playerSlot());
            players.save(new BattlePlayerSettlement(
                    settlement, user, player.playerSlot(), player.eliminated(), player.eliminatedWave(),
                    player.kills(), player.supportKills(), player.bossKills(),
                    player.initialInGameGold(), player.inGameGoldEarned(), player.inGameGoldSpent(),
                    player.finalInGameGold(), player.abandoned()));
        }
        settlements.flush();
        players.flush();
        return new WriteResult(settlement, true);
    }

    public record WriteResult(BattleSettlement settlement, boolean created) {
    }
}
