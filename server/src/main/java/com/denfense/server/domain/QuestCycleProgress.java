package com.denfense.server.domain;

import com.denfense.server.balance.QuestCycleType;
import jakarta.persistence.*;
import lombok.AccessLevel;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Entity
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
@Table(name = "quest_cycle_progresses", uniqueConstraints = @UniqueConstraint(
        name = "uk_quest_cycle_progress_user_quest_cycle",
        columnNames = {"user_id", "quest_id", "cycle_key"}))
public class QuestCycleProgress {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "user_id", nullable = false)
    private User user;

    @Column(name = "quest_id", nullable = false, length = 64)
    private String questId;

    @Enumerated(EnumType.STRING)
    @Column(name = "cycle_type", nullable = false, length = 16)
    private QuestCycleType cycleType;

    @Column(name = "cycle_key", nullable = false, length = 16)
    private String cycleKey;

    @Column(nullable = false)
    private long progress;

    public QuestCycleProgress(User user, String questId, QuestCycleType cycleType, String cycleKey) {
        this.user = user;
        this.questId = questId;
        this.cycleType = cycleType;
        this.cycleKey = cycleKey;
    }

    public void add(long amount) {
        if (amount <= 0) throw new IllegalArgumentException("Quest cycle progress amount must be positive.");
        progress = Math.addExact(progress, amount);
    }
}
