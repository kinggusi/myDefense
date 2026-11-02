package com.denfense.server.controller;

import com.denfense.server.dto.QuestDtos;
import com.denfense.server.service.QuestService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Profile;
import org.springframework.web.bind.annotation.*;

@RestController
@RequiredArgsConstructor
@Profile({"local", "dev"})
@RequestMapping("/api/quests")
public class QuestController {
    // FUTURE_AUTH_REPLACEMENT: production controller must bind username from JWT principal.
    private final QuestService service;

    @GetMapping
    public QuestDtos.BoardResponse getBoard(@RequestParam String username) {
        return service.getBoard(username);
    }

    @PostMapping("/claims")
    public QuestDtos.ClaimResponse claimQuest(@Valid @RequestBody QuestDtos.QuestClaimRequest request) {
        return service.claimQuest(request);
    }

    @PostMapping("/milestone-claims")
    public QuestDtos.ClaimResponse claimMilestone(@Valid @RequestBody QuestDtos.MilestoneClaimRequest request) {
        return service.claimMilestone(request);
    }

    @PostMapping("/achievement-claims")
    public QuestDtos.ClaimResponse claimAchievement(@Valid @RequestBody QuestDtos.AchievementClaimRequest request) {
        return service.claimAchievement(request);
    }
}
