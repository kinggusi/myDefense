package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.AccessLevel;
import lombok.Getter;
import lombok.NoArgsConstructor;
import org.hibernate.annotations.OnDelete;
import org.hibernate.annotations.OnDeleteAction;

import java.time.LocalDate;
import java.time.LocalDateTime;

@Entity
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
@Table(name = "daily_content_progresses", uniqueConstraints = @UniqueConstraint(
        name = "uk_daily_content_progress_user_type", columnNames = {"user_id", "content_type"}))
public class DailyContentProgress {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "user_id", nullable = false)
    @OnDelete(action = OnDeleteAction.CASCADE)
    private User user;

    @Enumerated(EnumType.STRING)
    @Column(name = "content_type", nullable = false, length = 32)
    private DailyContentType contentType;

    @Column(name = "highest_cleared_stage", nullable = false)
    private int highestClearedStage;

    @Column(name = "entry_date", nullable = false)
    private LocalDate entryDate;

    @Column(name = "remaining_entries", nullable = false)
    private int remainingEntries;

    @Column(name = "created_at", nullable = false)
    private LocalDateTime createdAt;

    @Column(name = "updated_at", nullable = false)
    private LocalDateTime updatedAt;

    public DailyContentProgress(User user, DailyContentType contentType, LocalDate today, int dailyEntries) {
        this.user = user;
        this.contentType = contentType;
        this.entryDate = today;
        this.remainingEntries = dailyEntries;
        this.createdAt = LocalDateTime.now();
        this.updatedAt = createdAt;
    }

    public void resetIfNeeded(LocalDate today, int dailyEntries) {
        if (!entryDate.equals(today)) {
            entryDate = today;
            remainingEntries = dailyEntries;
            updatedAt = LocalDateTime.now();
        }
    }

    public void consumeEntry() {
        if (remainingEntries <= 0) throw new IllegalStateException("No daily content entries remaining.");
        remainingEntries--;
        updatedAt = LocalDateTime.now();
    }

    public void refundEntry(int dailyEntries) {
        remainingEntries = Math.min(dailyEntries, remainingEntries + 1);
        updatedAt = LocalDateTime.now();
    }

    public boolean clearStage(int stage) {
        if (stage <= highestClearedStage) return false;
        highestClearedStage = stage;
        updatedAt = LocalDateTime.now();
        return true;
    }
}
