package com.denfense.server.controller;

import com.denfense.server.domain.UserAlien;
import com.denfense.server.service.AlienService;
import com.denfense.server.service.ShopService;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.*;
import com.denfense.server.dto.response.AlienUpgradeResponseDto;
import com.denfense.server.dto.response.AlienUpgradeStatusResponseDto;
import io.swagger.v3.oas.annotations.Operation;

@RestController
@RequestMapping("/api/aliens")
@RequiredArgsConstructor

public class AlienController {

    private final AlienService alienService;
    private final com.denfense.server.auth.AccountAccess accounts;

    @Operation(summary = "왹져 강화", description = "왹져의 카드를 소모하여 강화합니다. (추후 토큰에서 userId 추출 권장)")
    @PostMapping("/{alienId}/upgrade")
    public AlienUpgradeResponseDto upgrade(@PathVariable int alienId, @RequestParam String username) {
        return alienService.upgradeAlien(accounts.username(username), alienId);
    }

    @GetMapping("/{alienId}/upgrade-status")
    public AlienUpgradeStatusResponseDto getUpgradeStatus(@PathVariable int alienId, @RequestParam String username) {
        return alienService.getUpgradeStatus(accounts.username(username), alienId);
    }
}
