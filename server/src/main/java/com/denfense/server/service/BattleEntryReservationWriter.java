package com.denfense.server.service;

import com.denfense.server.domain.BattleEntryReservation;
import com.denfense.server.domain.BattleEntryStatus;
import com.denfense.server.domain.User;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.BattleEntryReservationRepository;
import com.denfense.server.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Propagation;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;
import java.util.Map;
import java.util.function.Function;
import java.util.stream.Collectors;

@Component
@RequiredArgsConstructor
public class BattleEntryReservationWriter {
    private final BattleEntryReservationRepository reservations;
    private final UserRepository users;
    private final HeartPolicy heartPolicy;
    private final PlanetProgressionService progression;

    @Transactional(propagation = Propagation.REQUIRES_NEW)
    public CreateResult create(String session, String map, Long playerOneId, Long playerTwoId) {
        List<User> locked = users.findAllByIdInForUpdate(List.of(playerOneId, playerTwoId));
        if (locked.size() != 2) throw new BusinessException(ErrorCode.USER_NOT_FOUND);
        Map<Long, User> byId = locked.stream().collect(Collectors.toMap(User::getId, Function.identity()));
        User playerOne = byId.get(playerOneId);
        User playerTwo = byId.get(playerTwoId);

        BattleEntryReservation existing = reservations.findByBattleSessionIdForUpdate(session).orElse(null);
        if (existing != null) {
            if (!existing.matches(map, playerOneId, playerTwoId)) {
                throw new BusinessException(ErrorCode.BATTLE_ENTRY_CONFLICT);
            }
            if (existing.getStatus() == BattleEntryStatus.REFUNDED) {
                throw new BusinessException(ErrorCode.BATTLE_ENTRY_REFUNDED);
            }
            return new CreateResult(existing.getStatus(), true);
        }

        progression.requireUnlocked(playerOne, map);
        progression.requireUnlocked(playerTwo, map);
        applyRecovery(playerOne);
        applyRecovery(playerTwo);
        playerOne.spendHeart(BattlePlanetEntryService.HEART_COST);
        playerTwo.spendHeart(BattlePlanetEntryService.HEART_COST);
        reservations.saveAndFlush(new BattleEntryReservation(
                session, map, playerOne, playerTwo, BattlePlanetEntryService.HEART_COST));
        return new CreateResult(BattleEntryStatus.CHARGED, false);
    }

    private void applyRecovery(User user) {
        user.applyHeartSnapshot(heartPolicy.calculate(user.getHeart(), user.getLastHeartUpdateTime()));
    }

    public record CreateResult(BattleEntryStatus status, boolean alreadyProcessed) {
    }
}
