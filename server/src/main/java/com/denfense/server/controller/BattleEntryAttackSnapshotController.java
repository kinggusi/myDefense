package com.denfense.server.controller;

import com.denfense.server.dto.battle.BattleAttackSnapshotDtos;
import com.denfense.server.service.BattleEntryAttackSnapshotService;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequiredArgsConstructor
@RequestMapping("/api/battle/entry")
public class BattleEntryAttackSnapshotController {

    private final BattleEntryAttackSnapshotService service;

    @GetMapping("/attack-snapshots")
    public BattleAttackSnapshotDtos.Response getAttackSnapshots(
            @RequestParam(defaultValue = "") String playerId) {
        return service.getForPlayer(playerId);
    }
}
