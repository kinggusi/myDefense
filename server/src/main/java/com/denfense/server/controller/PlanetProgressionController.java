package com.denfense.server.controller;

import com.denfense.server.dto.PlanetProgressionDtos;
import com.denfense.server.service.PlanetProgressionService;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequiredArgsConstructor
@RequestMapping("/api/planet-progressions")
public class PlanetProgressionController {
    private final PlanetProgressionService progression;

    @GetMapping
    public PlanetProgressionDtos.Response get(@RequestParam String username) {
        return progression.getProgress(username);
    }
}
