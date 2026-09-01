package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.AccessLevel;
import lombok.Getter;
import lombok.NoArgsConstructor;
import org.hibernate.annotations.OnDelete;
import org.hibernate.annotations.OnDeleteAction;

import java.time.LocalDateTime;
import java.util.UUID;

@Entity
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
@Table(name = "daily_content_runs", uniqueConstraints = {
        @UniqueConstraint(name = "uk_daily_content_run_user_entry_request", columnNames = {"user_id", "entry_request_id"})
})
public class DailyContentRun {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @Column(name = "run_id", nullable = false, unique = true, length = 36)
    private String runId;

    @Column(name = "entry_request_id", nullable = false, length = 64)
    private String entryRequestId;

    @Column(name = "result_request_id", length = 64)
    private String resultRequestId;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "user_id", nullable = false)
    @OnDelete(action = OnDeleteAction.CASCADE)
    private User user;

    @Enumerated(EnumType.STRING)
    @Column(name = "content_type", nullable = false, length = 32)
    private DailyContentType contentType;

    @Column(nullable = false)
    private int stage;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false, length = 16)
    private DailyContentOperation operation;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false, length = 16)
    private DailyContentRunStatus status;

    @Enumerated(EnumType.STRING)
    @Column(name = "refund_reason", length = 32)
    private DailyContentRefundReason refundReason;

    @Column(name = "reward_amount", nullable = false)
    private int rewardAmount;

    @Column(name = "first_clear", nullable = false)
    private boolean firstClear;

    @Column(name = "created_at", nullable = false)
    private LocalDateTime createdAt;

    @Column(name = "updated_at", nullable = false)
    private LocalDateTime updatedAt;

    public DailyContentRun(String entryRequestId, User user, DailyContentType contentType, int stage,
                           DailyContentOperation operation, DailyContentRunStatus status) {
        this.runId = UUID.randomUUID().toString();
        this.entryRequestId = entryRequestId;
        this.user = user;
        this.contentType = contentType;
        this.stage = stage;
        this.operation = operation;
        this.status = status;
        this.createdAt = LocalDateTime.now();
        this.updatedAt = createdAt;
    }

    public boolean matches(User requestedUser, DailyContentType requestedType, int requestedStage,
                           DailyContentOperation requestedOperation) {
        return user.getId().equals(requestedUser.getId()) && contentType == requestedType
                && stage == requestedStage && operation == requestedOperation;
    }

    public boolean matchesResult(String requestId, com.denfense.server.dto.DailyContentDtos.ResultOutcome outcome,
                                 DailyContentRefundReason requestedRefundReason) {
        if (!requestId.equals(resultRequestId)) return false;
        return switch (outcome) {
            case CLEARED -> status == DailyContentRunStatus.CLEARED && requestedRefundReason == null;
            case FAILED -> status == DailyContentRunStatus.FAILED && requestedRefundReason == null;
            case REFUNDED -> status == DailyContentRunStatus.REFUNDED && refundReason == requestedRefundReason;
        };
    }

    public void clear(String requestId, int reward, boolean firstClear) {
        resultRequestId = requestId;
        status = DailyContentRunStatus.CLEARED;
        rewardAmount = reward;
        this.firstClear = firstClear;
        updatedAt = LocalDateTime.now();
    }

    public void fail(String requestId) {
        resultRequestId = requestId;
        status = DailyContentRunStatus.FAILED;
        updatedAt = LocalDateTime.now();
    }

    public void refund(String requestId, DailyContentRefundReason reason) {
        resultRequestId = requestId;
        status = DailyContentRunStatus.REFUNDED;
        refundReason = reason;
        updatedAt = LocalDateTime.now();
    }

    public void sweep(int reward) {
        status = DailyContentRunStatus.SWEPT;
        rewardAmount = reward;
        updatedAt = LocalDateTime.now();
    }
}
