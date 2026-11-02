package com.denfense.server.tutorial;

import jakarta.persistence.*;
import lombok.Getter;
import lombok.NoArgsConstructor;
import java.time.Instant;
import java.util.UUID;

@Entity @Getter @NoArgsConstructor
@Table(name="tutorial_progress")
public class TutorialProgress {
    @Id private Long userId;
    @Enumerated(EnumType.STRING) @Column(nullable=false) private TutorialStep step;
    @Column(nullable=false) private long revision;
    private UUID practiceRunId;
    private Instant rewardGrantedAt;
    private Instant completedAt;
    public TutorialProgress(long userId) { this.userId=userId; step=TutorialStep.NOT_STARTED; }
    void advance(TutorialStep next) { step=next; revision++; }
    void startPractice(UUID run) { practiceRunId=run; revision++; }
    void reward() { rewardGrantedAt=Instant.now(); advance(TutorialStep.BREEDING_GUIDE); }
    void complete() { completedAt=Instant.now(); advance(TutorialStep.COMPLETED); }
}
