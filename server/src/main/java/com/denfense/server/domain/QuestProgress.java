package com.denfense.server.domain;

import jakarta.persistence.*;
import lombok.AccessLevel;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Entity
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
@Table(name = "quest_progresses", uniqueConstraints = @UniqueConstraint(
        name = "uk_quest_progress_user_condition",
        columnNames = {"user_id", "quest_condition_id"}))
public class QuestProgress {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "user_id", nullable = false)
    private User user;

    @Column(name = "quest_condition_id", nullable = false, length = 128)
    private String questConditionId;

    @Column(nullable = false)
    private long progress;

    public QuestProgress(User user, String questConditionId) {
        this.user = user;
        this.questConditionId = questConditionId;
    }

    public void add(long amount) {
        if (amount <= 0) throw new IllegalArgumentException("Quest progress amount must be positive.");
        progress = Math.addExact(progress, amount);
    }
}
