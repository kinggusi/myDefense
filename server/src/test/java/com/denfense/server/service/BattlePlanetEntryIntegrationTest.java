package com.denfense.server.service;

import com.denfense.server.domain.*;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.BattleEntryReservationRepository;
import com.denfense.server.repository.UserPlanetUnlockRepository;
import com.denfense.server.repository.UserRepository;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;

import java.time.LocalDateTime;
import java.util.UUID;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

@SpringBootTest
class BattlePlanetEntryIntegrationTest {
    @Autowired BattlePlanetEntryService entries;
    @Autowired PlanetProgressionService progression;
    @Autowired UserRepository users;
    @Autowired UserPlanetUnlockRepository unlocks;
    @Autowired BattleEntryReservationRepository reservations;

    @Test
    void newAccountHasOnlyNeptuneUnlocked() {
        User user = user(10);

        var result = progression.getProgress(user.getUsername());

        assertThat(result.planets()).hasSize(9);
        assertThat(result.planets()).filteredOn(planet -> planet.unlocked())
                .extracting(planet -> planet.mapId())
                .containsExactly("NEPTUNE");
    }

    @Test
    void reserveChargesExactlyOneHeartPerPlayerAndRetryIsIdempotent() {
        User one = user(5);
        User two = user(7);
        String session = id("entry");

        var first = entries.reserve(session, "NEPTUNE", one.getId(), two.getId());
        var retry = entries.reserve(session, "NEPTUNE", one.getId(), two.getId());

        assertThat(first.alreadyProcessed()).isFalse();
        assertThat(retry.alreadyProcessed()).isTrue();
        assertThat(users.findById(one.getId()).orElseThrow().getHeart()).isEqualTo(4);
        assertThat(users.findById(two.getId()).orElseThrow().getHeart()).isEqualTo(6);
        assertThat(reservations.findByBattleSessionId(session)).isPresent();
    }

    @Test
    void bothPlayersMustUnlockRequestedPlanet() {
        User one = user(5);
        User two = user(5);
        unlock(one, "URANUS");

        assertCode(ErrorCode.PLANET_LOCKED,
                () -> entries.reserve(id("locked"), "URANUS", one.getId(), two.getId()));
        assertThat(users.findById(one.getId()).orElseThrow().getHeart()).isEqualTo(5);
        assertThat(users.findById(two.getId()).orElseThrow().getHeart()).isEqualTo(5);
    }

    @Test
    void insufficientHeartRollsBackBothPlayers() {
        User one = user(1);
        User two = user(0);

        assertCode(ErrorCode.INSUFFICIENT_HEART,
                () -> entries.reserve(id("heart"), "NEPTUNE", one.getId(), two.getId()));
        assertThat(users.findById(one.getId()).orElseThrow().getHeart()).isEqualTo(1);
        assertThat(users.findById(two.getId()).orElseThrow().getHeart()).isZero();
    }

    @Test
    void sameSessionWithDifferentRosterConflictsWithoutExtraCharge() {
        User one = user(5);
        User two = user(5);
        User three = user(5);
        String session = id("conflict");
        entries.reserve(session, "NEPTUNE", one.getId(), two.getId());

        assertCode(ErrorCode.BATTLE_ENTRY_CONFLICT,
                () -> entries.reserve(session, "NEPTUNE", one.getId(), three.getId()));
        assertThat(users.findById(one.getId()).orElseThrow().getHeart()).isEqualTo(4);
        assertThat(users.findById(two.getId()).orElseThrow().getHeart()).isEqualTo(4);
        assertThat(users.findById(three.getId()).orElseThrow().getHeart()).isEqualTo(5);
    }

    @Test
    void trustedFailureRefundsOnceAndRefundedSessionCannotBeReused() {
        User one = user(5);
        User two = user(5);
        String session = id("refund");
        entries.reserve(session, "NEPTUNE", one.getId(), two.getId());

        var first = entries.refund(session, BattleEntryRefundReason.SESSION_FAILED);
        var retry = entries.refund(session, BattleEntryRefundReason.SESSION_FAILED);

        assertThat(first.alreadyProcessed()).isFalse();
        assertThat(retry.alreadyProcessed()).isTrue();
        assertThat(users.findById(one.getId()).orElseThrow().getHeart()).isEqualTo(5);
        assertThat(users.findById(two.getId()).orElseThrow().getHeart()).isEqualTo(5);
        assertCode(ErrorCode.BATTLE_ENTRY_REFUNDED,
                () -> entries.reserve(session, "NEPTUNE", one.getId(), two.getId()));
    }

    @Test
    void completedEntryCannotBeRefunded() {
        User one = user(5);
        User two = user(5);
        String session = id("complete");
        entries.reserve(session, "NEPTUNE", one.getId(), two.getId());
        entries.completeIfReserved(session);

        assertCode(ErrorCode.BATTLE_ENTRY_REFUND_INVALID,
                () -> entries.refund(session, BattleEntryRefundReason.SERVER_ABORTED));
    }

    @Test
    void concurrentSameReservationCreatesOneRowAndChargesOnce() throws Exception {
        User one = user(5);
        User two = user(5);
        String session = id("concurrent");
        CountDownLatch ready = new CountDownLatch(2);
        CountDownLatch start = new CountDownLatch(1);
        var executor = Executors.newFixedThreadPool(2);
        try {
            var task = (java.util.concurrent.Callable<BattlePlanetEntryService.EntryResult>) () -> {
                ready.countDown();
                start.await(5, TimeUnit.SECONDS);
                return entries.reserve(session, "NEPTUNE", one.getId(), two.getId());
            };
            var first = executor.submit(task);
            var second = executor.submit(task);
            assertThat(ready.await(5, TimeUnit.SECONDS)).isTrue();
            start.countDown();

            assertThat(java.util.List.of(first.get(10, TimeUnit.SECONDS), second.get(10, TimeUnit.SECONDS)))
                    .extracting(BattlePlanetEntryService.EntryResult::alreadyProcessed)
                    .containsExactlyInAnyOrder(false, true);
        } finally {
            executor.shutdownNow();
        }
        assertThat(reservations.findByBattleSessionId(session)).isPresent();
        assertThat(users.findById(one.getId()).orElseThrow().getHeart()).isEqualTo(4);
        assertThat(users.findById(two.getId()).orElseThrow().getHeart()).isEqualTo(4);
    }

    @Test
    void concurrentDisjointRostersUsingSameSessionReturnDomainConflict() throws Exception {
        User one = user(5);
        User two = user(5);
        User three = user(5);
        User four = user(5);
        String session = id("disjoint");
        CountDownLatch ready = new CountDownLatch(2);
        CountDownLatch start = new CountDownLatch(1);
        var executor = Executors.newFixedThreadPool(2);
        try {
            var first = executor.submit(() -> concurrentResult(ready, start, session, one.getId(), two.getId()));
            var second = executor.submit(() -> concurrentResult(ready, start, session, three.getId(), four.getId()));
            assertThat(ready.await(5, TimeUnit.SECONDS)).isTrue();
            start.countDown();

            assertThat(java.util.List.of(first.get(10, TimeUnit.SECONDS), second.get(10, TimeUnit.SECONDS)))
                    .containsExactlyInAnyOrder("SUCCESS", ErrorCode.BATTLE_ENTRY_CONFLICT.name());
        } finally {
            executor.shutdownNow();
        }
        assertThat(reservations.findAll()).filteredOn(row -> row.getBattleSessionId().equals(session)).hasSize(1);
        assertThat(java.util.List.of(one, two, three, four).stream()
                .map(user -> users.findById(user.getId()).orElseThrow().getHeart())
                .toList()).containsExactlyInAnyOrder(4, 4, 5, 5);
    }

    @Test
    void allPlanetsUnlockSequentiallyAndSunHasNoSuccessor() {
        User user = user(5);
        progression.getProgress(user.getUsername());
        var order = java.util.List.of(
                "NEPTUNE", "URANUS", "SATURN", "JUPITER", "MARS",
                "EARTH", "VENUS", "MERCURY", "SUN");
        for (int index = 0; index < order.size() - 1; index++) {
            assertThat(progression.unlockNext(user, order.get(index), id("clear"))).isTrue();
            assertThat(unlocks.findByUserIdAndMapId(user.getId(), order.get(index + 1))).isPresent();
        }
        assertThat(progression.unlockNext(user, "NEPTUNE", id("reclear"))).isFalse();
        assertThat(progression.unlockNext(user, "SUN", id("sun"))).isFalse();
        assertThat(progression.getProgress(user.getUsername()).planets())
                .allMatch(planet -> planet.unlocked());
    }

    private User user(int heart) {
        User user = new User(id("user"), "pw");
        user.setHeart(heart);
        user.setLastHeartUpdateTime(LocalDateTime.now());
        return users.saveAndFlush(user);
    }

    private void unlock(User user, String mapId) {
        unlocks.saveAndFlush(new UserPlanetUnlock(user, mapId, PlanetUnlockSource.PREVIOUS_PLANET_CLEAR, id("source")));
    }

    private String concurrentResult(CountDownLatch ready, CountDownLatch start, String session,
                                    Long playerOneId, Long playerTwoId) throws InterruptedException {
        ready.countDown();
        start.await(5, TimeUnit.SECONDS);
        try {
            entries.reserve(session, "NEPTUNE", playerOneId, playerTwoId);
            return "SUCCESS";
        } catch (BusinessException exception) {
            return exception.getErrorCode().name();
        }
    }

    private static void assertCode(ErrorCode code, org.assertj.core.api.ThrowableAssert.ThrowingCallable call) {
        assertThatThrownBy(call).isInstanceOf(BusinessException.class)
                .extracting(exception -> ((BusinessException) exception).getErrorCode())
                .isEqualTo(code);
    }

    private static String id(String prefix) {
        return prefix + "-" + UUID.randomUUID().toString().replace("-", "");
    }
}
