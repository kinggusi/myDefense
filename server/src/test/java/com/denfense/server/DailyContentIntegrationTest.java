package com.denfense.server;

import com.denfense.server.domain.*;
import com.denfense.server.dto.DailyContentDtos;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.DailyContentRunRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.DailyContentService;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Primary;
import org.springframework.boot.test.context.TestConfiguration;
import org.springframework.context.annotation.Import;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDate;
import java.time.ZoneId;

import static org.junit.jupiter.api.Assertions.*;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@SpringBootTest
@ActiveProfiles("local")
@Transactional
@Import(DailyContentIntegrationTest.TimeConfig.class)
@AutoConfigureMockMvc
class DailyContentIntegrationTest {
    @Autowired DailyContentService service;
    @Autowired UserRepository users;
    @Autowired DailyContentRunRepository runs;
    @Autowired MutableDailyContentTimeProvider time;
    @Autowired MockMvc mockMvc;

    private String username;

    @BeforeEach
    void setUp() {
        username = "daily-" + java.util.UUID.randomUUID();
        time.set(LocalDate.of(2026, 8, 30));
        users.saveAndFlush(new User(username, "pw"));
    }

    @Test
    void progressInitializesBothContentsAtKstDayBoundary() {
        var response = service.getProgress(username);

        assertEquals(LocalDate.of(2026, 8, 30), response.entryDate());
        assertEquals(2, response.contents().size());
        response.contents().forEach(content -> {
            assertEquals(3, content.remainingEntries());
            assertEquals(0, content.highestClearedStage());
            assertTrue(content.stages().get(0).unlocked());
            assertFalse(content.stages().get(0).sweepable());
            assertFalse(content.stages().get(1).unlocked());
        });
    }

    @Test
    void enterClearAndRetryAreIdempotentAndFirstClearPaysBonusOnce() {
        var request = new DailyContentDtos.EnterRequest(
                "entry-1", username, DailyContentType.CULTIVATION_ZONE, 1);
        var entered = service.enter(request);
        var repeatedEntry = service.enter(request);

        assertEquals(DailyContentRunStatus.ENTERED, entered.status());
        assertEquals(2, entered.remainingEntries());
        assertTrue(repeatedEntry.alreadyProcessed());
        assertEquals(1, runs.count());

        var result = new DailyContentDtos.ResultRequest(
                "result-1", entered.runId(), username, DailyContentDtos.ResultOutcome.CLEARED, null);
        var cleared = service.submitResult(result);
        var repeatedResult = service.submitResult(result);

        assertEquals(DailyContentRunStatus.CLEARED, cleared.status());
        assertEquals(10, cleared.rewardAmount());
        assertEquals(10, cleared.growthCell());
        assertTrue(cleared.firstClear());
        assertTrue(repeatedResult.alreadyProcessed());
        assertEquals(10, repeatedResult.growthCell());
        assertEquals(1, service.getProgress(username).contents().get(0).highestClearedStage());
    }

    @Test
    void failedRunConsumesEntryAndSweepPaysRepeatReward() {
        var first = service.enter(new DailyContentDtos.EnterRequest(
                "entry-clear", username, DailyContentType.CULTIVATION_ZONE, 1));
        service.submitResult(new DailyContentDtos.ResultRequest(
                "result-clear", first.runId(), username, DailyContentDtos.ResultOutcome.CLEARED, null));

        var second = service.enter(new DailyContentDtos.EnterRequest(
                "entry-fail", username, DailyContentType.CULTIVATION_ZONE, 2));
        var failed = service.submitResult(new DailyContentDtos.ResultRequest(
                "result-fail", second.runId(), username, DailyContentDtos.ResultOutcome.FAILED, null));
        assertEquals(DailyContentRunStatus.FAILED, failed.status());
        assertEquals(10, failed.growthCell());

        var swept = service.sweep(new DailyContentDtos.SweepRequest(
                "sweep-1", username, DailyContentType.CULTIVATION_ZONE, 1));
        assertEquals(DailyContentRunStatus.SWEPT, swept.status());
        assertEquals(5, swept.rewardAmount());
        assertEquals(15, swept.growthCell());
        assertEquals(0, swept.remainingEntries());
    }

    @Test
    void trustedServerAbortRefundsEntryExactlyOnce() {
        var entered = service.enter(new DailyContentDtos.EnterRequest(
                "entry-refund", username, DailyContentType.MUTATION_LAB, 1));
        var request = new DailyContentDtos.ResultRequest(
                "result-refund", entered.runId(), username, DailyContentDtos.ResultOutcome.REFUNDED,
                DailyContentRefundReason.SERVER_ABORTED);

        var refunded = service.submitResult(request);
        var repeated = service.submitResult(request);

        assertEquals(DailyContentRunStatus.REFUNDED, refunded.status());
        assertEquals(3, refunded.remainingEntries());
        assertEquals(0, refunded.mutationCatalyst());
        assertTrue(repeated.alreadyProcessed());
        assertEquals(3, repeated.remainingEntries());
    }

    @Test
    void lockedStageAndConflictingRequestAreRejected() {
        BusinessException locked = assertThrows(BusinessException.class, () -> service.enter(
                new DailyContentDtos.EnterRequest("locked", username, DailyContentType.CULTIVATION_ZONE, 2)));
        assertEquals(ErrorCode.DAILY_CONTENT_STAGE_LOCKED, locked.getErrorCode());

        service.enter(new DailyContentDtos.EnterRequest(
                "same-request", username, DailyContentType.CULTIVATION_ZONE, 1));
        BusinessException conflict = assertThrows(BusinessException.class, () -> service.enter(
                new DailyContentDtos.EnterRequest("same-request", username, DailyContentType.MUTATION_LAB, 1)));
        assertEquals(ErrorCode.DAILY_CONTENT_REQUEST_CONFLICT, conflict.getErrorCode());
    }

    @Test
    void nextKstDateResetsEachContentEntryCount() {
        service.enter(new DailyContentDtos.EnterRequest(
                "before-midnight", username, DailyContentType.CULTIVATION_ZONE, 1));
        assertEquals(2, service.getProgress(username).contents().get(0).remainingEntries());

        time.set(LocalDate.of(2026, 8, 31));
        var reset = service.getProgress(username);

        assertEquals(LocalDate.of(2026, 8, 31), reset.entryDate());
        reset.contents().forEach(content -> assertEquals(3, content.remainingEntries()));
    }

    @Test
    void requestIdCannotBeReusedAcrossEnterAndSweep() {
        service.enter(new DailyContentDtos.EnterRequest(
                "cross-operation", username, DailyContentType.CULTIVATION_ZONE, 1));

        BusinessException conflict = assertThrows(BusinessException.class, () -> service.sweep(
                new DailyContentDtos.SweepRequest(
                        "cross-operation", username, DailyContentType.CULTIVATION_ZONE, 1)));
        assertEquals(ErrorCode.DAILY_CONTENT_REQUEST_CONFLICT, conflict.getErrorCode());
    }

    @Test
    void resultRetryMustMatchOutcomeAndRefundReason() {
        var entered = service.enter(new DailyContentDtos.EnterRequest(
                "result-payload-entry", username, DailyContentType.CULTIVATION_ZONE, 1));
        service.submitResult(new DailyContentDtos.ResultRequest(
                "same-result", entered.runId(), username, DailyContentDtos.ResultOutcome.FAILED, null));

        BusinessException conflict = assertThrows(BusinessException.class, () -> service.submitResult(
                new DailyContentDtos.ResultRequest(
                        "same-result", entered.runId(), username,
                        DailyContentDtos.ResultOutcome.REFUNDED, DailyContentRefundReason.SERVER_ABORTED)));
        assertEquals(ErrorCode.DAILY_CONTENT_REQUEST_CONFLICT, conflict.getErrorCode());
    }

    @Test
    void clearAndFailedRejectRefundReasonBeforeAnyTerminalMutation() {
        var entered = service.enter(new DailyContentDtos.EnterRequest(
                "invalid-result-entry", username, DailyContentType.CULTIVATION_ZONE, 1));

        BusinessException invalid = assertThrows(BusinessException.class, () -> service.submitResult(
                new DailyContentDtos.ResultRequest(
                        "invalid-result", entered.runId(), username,
                        DailyContentDtos.ResultOutcome.CLEARED, DailyContentRefundReason.SERVER_ABORTED)));
        assertEquals(ErrorCode.DAILY_CONTENT_RESULT_INVALID, invalid.getErrorCode());

        var cleared = service.submitResult(new DailyContentDtos.ResultRequest(
                "valid-result", entered.runId(), username,
                DailyContentDtos.ResultOutcome.CLEARED, null));
        assertEquals(DailyContentRunStatus.CLEARED, cleared.status());
        assertEquals(10, cleared.growthCell());
    }

    @Test
    void localResultEndpointRejectsNonLoopbackCaller() throws Exception {
        mockMvc.perform(post("/api/dev/daily-contents/results")
                        .with(request -> { request.setRemoteAddr("192.0.2.10"); return request; })
                        .contentType("application/json")
                        .content("""
                                {"requestId":"result","runId":"00000000-0000-0000-0000-000000000000",
                                 "username":"%s","outcome":"FAILED"}
                                """.formatted(username)))
                .andExpect(status().isForbidden())
                .andExpect(jsonPath("$.code").value("DAILY_CONTENT_RESULT_FORBIDDEN"));
    }

    @TestConfiguration
    static class TimeConfig {
        @Bean
        @Primary
        MutableDailyContentTimeProvider mutableDailyContentTimeProvider() {
            return new MutableDailyContentTimeProvider();
        }
    }

    static class MutableDailyContentTimeProvider extends com.denfense.server.service.DailyContentTimeProvider {
        private LocalDate current = LocalDate.of(2026, 8, 30);

        void set(LocalDate value) {
            current = value;
        }

        @Override
        public LocalDate today() {
            return current;
        }
    }
}
