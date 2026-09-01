package com.denfense.server.controller;

import com.denfense.server.dto.DailyContentDtos;
import com.denfense.server.service.DailyContentService;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.*;
import org.springframework.context.annotation.Profile;

@RestController
@RequiredArgsConstructor
@Profile({"local", "dev"})
@RequestMapping("/api/daily-contents")
public class DailyContentController {
    // FUTURE_AUTH_REPLACEMENT: production controller must bind username from JWT principal.
    private final DailyContentService service;

    @GetMapping
    public DailyContentDtos.ProgressResponse getProgress(@RequestParam String username) {
        return service.getProgress(username);
    }

    @PostMapping("/entries")
    public DailyContentDtos.RunResponse enter(@Valid @RequestBody DailyContentDtos.EnterRequest request) {
        return service.enter(request);
    }

    @PostMapping("/sweeps")
    public DailyContentDtos.RunResponse sweep(@Valid @RequestBody DailyContentDtos.SweepRequest request) {
        return service.sweep(request);
    }
}
