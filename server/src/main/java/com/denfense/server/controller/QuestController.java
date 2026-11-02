package com.denfense.server.controller;

import com.denfense.server.dto.QuestDtos;
import com.denfense.server.service.QuestService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Profile;
import org.springframework.web.bind.annotation.*;

@RestController
@RequiredArgsConstructor
@Profile("(local | dev) & !prod & !production")
@RequestMapping("/api/quests")
public class QuestController {
    // FUTURE_AUTH_REPLACEMENT: production controller must bind username from JWT principal.
    private final QuestService service;
    private final com.denfense.server.auth.AccountAccess accounts;

    @GetMapping
    public QuestDtos.BoardResponse getBoard(@RequestParam String username) {
        return service.getBoard(accounts.username(username));
    }

    @PostMapping("/claims")
    public QuestDtos.ClaimResponse claimQuest(@Valid @RequestBody QuestDtos.QuestClaimRequest request) {
        accounts.username(request.username());
        return service.claimQuest(request);
    }

    @PostMapping("/milestone-claims")
    public QuestDtos.ClaimResponse claimMilestone(@Valid @RequestBody QuestDtos.MilestoneClaimRequest request) {
        accounts.username(request.username());
        return service.claimMilestone(request);
    }

    @PostMapping("/achievement-claims")
    public QuestDtos.ClaimResponse claimAchievement(@Valid @RequestBody QuestDtos.AchievementClaimRequest request) {
        accounts.username(request.username());
        return service.claimAchievement(request);
    }
}
