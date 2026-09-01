package com.denfense.server.service;

import com.denfense.server.balance.DailyContentBalance;
import com.denfense.server.domain.*;
import com.denfense.server.dto.DailyContentDtos;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.DailyContentProgressRepository;
import com.denfense.server.repository.DailyContentRunRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.balance.DailyContentBalanceRegistry;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.util.StringUtils;

import java.time.LocalDate;
import java.util.Arrays;
import java.util.List;

@Service
@RequiredArgsConstructor
public class DailyContentService {
    public static final int DAILY_ENTRIES = 3;

    private final UserRepository users;
    private final DailyContentProgressRepository progresses;
    private final DailyContentRunRepository runs;
    private final DailyContentBalanceRegistry balances;
    private final DailyContentTimeProvider time;

    @Transactional
    public DailyContentDtos.ProgressResponse getProgress(String username) {
        User user = lockUser(username);
        LocalDate today = today();
        List<DailyContentDtos.ContentProgress> contents = Arrays.stream(DailyContentType.values())
                .map(type -> toProgress(requireProgress(user, type, today), type))
                .toList();
        return new DailyContentDtos.ProgressResponse(user.getUsername(), today, contents);
    }

    @Transactional
    public DailyContentDtos.RunResponse enter(DailyContentDtos.EnterRequest request) {
        String requestId = requiredText(request.requestId());
        User user = lockUser(request.username());
        DailyContentRun existing = runs.findByUserIdAndEntryRequestId(user.getId(), requestId).orElse(null);
        if (existing != null) return resolveExisting(
                existing, user, request.contentType(), request.stage(), DailyContentOperation.ENTER);

        DailyContentBalance balance = requireBalance(request.contentType(), request.stage());
        DailyContentProgress progress = requireProgress(user, request.contentType(), today());
        requireStageUnlocked(progress, balance.stage());
        consume(progress);

        DailyContentRun run = runs.saveAndFlush(new DailyContentRun(
                requestId, user, request.contentType(), request.stage(), DailyContentOperation.ENTER,
                DailyContentRunStatus.ENTERED));
        return response(run, progress, false);
    }

    @Transactional
    public DailyContentDtos.RunResponse sweep(DailyContentDtos.SweepRequest request) {
        String requestId = requiredText(request.requestId());
        User user = lockUser(request.username());
        DailyContentRun existing = runs.findByUserIdAndEntryRequestId(user.getId(), requestId).orElse(null);
        if (existing != null) return resolveExisting(
                existing, user, request.contentType(), request.stage(), DailyContentOperation.SWEEP);

        DailyContentBalance balance = requireBalance(request.contentType(), request.stage());
        DailyContentProgress progress = requireProgress(user, request.contentType(), today());
        if (progress.getHighestClearedStage() < balance.stage()) {
            throw new BusinessException(ErrorCode.DAILY_CONTENT_SWEEP_LOCKED);
        }
        consume(progress);
        DailyContentRun run = new DailyContentRun(
                requestId, user, request.contentType(), request.stage(), DailyContentOperation.SWEEP,
                DailyContentRunStatus.ENTERED);
        grant(user, request.contentType(), balance.repeatReward());
        run.sweep(balance.repeatReward());
        runs.saveAndFlush(run);
        return response(run, progress, false);
    }

    /** Trusted Battle adapter boundary. HTTP exposure is local/dev only. */
    @Transactional
    public DailyContentDtos.RunResponse submitResult(DailyContentDtos.ResultRequest request) {
        requiredText(request.requestId());
        requiredText(request.runId());
        validateResultPayload(request);
        User user = lockUser(request.username());
        DailyContentRun run = runs.findByRunId(request.runId())
                .orElseThrow(() -> new BusinessException(ErrorCode.DAILY_CONTENT_RUN_NOT_FOUND));
        if (!run.getUser().getId().equals(user.getId())) {
            throw new BusinessException(ErrorCode.DAILY_CONTENT_RESULT_INVALID);
        }
        DailyContentProgress progress = requireProgress(user, run.getContentType(), today());

        if (run.getStatus() != DailyContentRunStatus.ENTERED) {
            if (run.matchesResult(request.requestId().trim(), request.outcome(), request.refundReason())) {
                return response(run, progress, true);
            }
            throw new BusinessException(ErrorCode.DAILY_CONTENT_REQUEST_CONFLICT);
        }

        switch (request.outcome()) {
            case CLEARED -> completeClear(request.requestId().trim(), run, progress, user);
            case FAILED -> run.fail(request.requestId().trim());
            case REFUNDED -> completeRefund(request, run, progress);
        }
        runs.flush();
        return response(run, progress, false);
    }

    private void validateResultPayload(DailyContentDtos.ResultRequest request) {
        boolean refunded = request.outcome() == DailyContentDtos.ResultOutcome.REFUNDED;
        if (refunded != (request.refundReason() != null)) {
            throw new BusinessException(ErrorCode.DAILY_CONTENT_RESULT_INVALID);
        }
    }

    private void completeClear(String requestId, DailyContentRun run, DailyContentProgress progress, User user) {
        DailyContentBalance balance = requireBalance(run.getContentType(), run.getStage());
        boolean firstClear = progress.clearStage(run.getStage());
        int reward = balance.repeatReward() + (firstClear ? balance.firstClearReward() : 0);
        grant(user, run.getContentType(), reward);
        run.clear(requestId, reward, firstClear);
    }

    private void completeRefund(DailyContentDtos.ResultRequest request, DailyContentRun run,
                                DailyContentProgress progress) {
        if (request.refundReason() == null) {
            throw new BusinessException(ErrorCode.DAILY_CONTENT_RESULT_INVALID);
        }
        progress.refundEntry(DAILY_ENTRIES);
        run.refund(request.requestId().trim(), request.refundReason());
    }

    private DailyContentDtos.ContentProgress toProgress(DailyContentProgress progress, DailyContentType type) {
        List<DailyContentDtos.StageReward> stages = balances.getAll().stream()
                .filter(balance -> balance.contentType().equals(type.name()) && balance.enabled())
                .map(balance -> new DailyContentDtos.StageReward(
                        balance.stage(), balance.repeatReward(), balance.firstClearReward(),
                        balance.stage() <= progress.getHighestClearedStage() + 1,
                        balance.stage() <= progress.getHighestClearedStage()))
                .toList();
        return new DailyContentDtos.ContentProgress(
                type, progress.getRemainingEntries(), progress.getHighestClearedStage(), stages);
    }

    private DailyContentDtos.RunResponse response(DailyContentRun run, DailyContentProgress progress,
                                                   boolean alreadyProcessed) {
        return new DailyContentDtos.RunResponse(
                run.getRunId(), run.getContentType(), run.getStage(), run.getStatus(),
                progress.getRemainingEntries(), run.getRewardAmount(), run.isFirstClear(), alreadyProcessed,
                run.getUser().getGrowthCell(), run.getUser().getMutationCatalyst());
    }

    private DailyContentDtos.RunResponse resolveExisting(DailyContentRun run, User user,
                                                         DailyContentType type, int stage,
                                                         DailyContentOperation operation) {
        if (!run.matches(user, type, stage, operation)) {
            throw new BusinessException(ErrorCode.DAILY_CONTENT_REQUEST_CONFLICT);
        }
        return response(run, requireProgress(user, type, today()), true);
    }

    private DailyContentProgress requireProgress(User user, DailyContentType type, LocalDate today) {
        DailyContentProgress progress = progresses.findByUserIdAndContentType(user.getId(), type)
                .orElseGet(() -> progresses.save(new DailyContentProgress(user, type, today, DAILY_ENTRIES)));
        progress.resetIfNeeded(today, DAILY_ENTRIES);
        return progress;
    }

    private DailyContentBalance requireBalance(DailyContentType type, int stage) {
        try {
            return balances.get(type, stage);
        } catch (IllegalArgumentException exception) {
            throw new BusinessException(ErrorCode.DAILY_CONTENT_NOT_FOUND);
        }
    }

    private void requireStageUnlocked(DailyContentProgress progress, int stage) {
        if (stage > progress.getHighestClearedStage() + 1) {
            throw new BusinessException(ErrorCode.DAILY_CONTENT_STAGE_LOCKED);
        }
    }

    private void consume(DailyContentProgress progress) {
        if (progress.getRemainingEntries() <= 0) {
            throw new BusinessException(ErrorCode.DAILY_CONTENT_ENTRY_EXHAUSTED);
        }
        progress.consumeEntry();
    }

    private void grant(User user, DailyContentType type, int amount) {
        if (type == DailyContentType.CULTIVATION_ZONE) user.earnGrowthCell(amount);
        else user.earnMutationCatalyst(amount);
    }

    private User lockUser(String username) {
        return users.findByUsernameForUpdate(requiredText(username))
                .orElseThrow(() -> new BusinessException(ErrorCode.USER_NOT_FOUND));
    }

    private static String requiredText(String value) {
        if (!StringUtils.hasText(value)) throw new BusinessException(ErrorCode.INVALID_REQUEST);
        return value.trim();
    }

    private LocalDate today() {
        return time.today();
    }
}
