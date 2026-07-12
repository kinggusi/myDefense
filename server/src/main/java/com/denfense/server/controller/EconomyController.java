package com.denfense.server.controller;

import com.denfense.server.dto.response.EconomyBalanceResponseDto;
import com.denfense.server.service.EconomyService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@Tag(name = "Economy", description = "계정 영구 재화 통합 관리")
@RestController
@RequestMapping("/api/economy")
@RequiredArgsConstructor
public class EconomyController {
    
    private final EconomyService economyService;

    @Operation(summary = "계정 영구 재화 조회", description = "메인화면, 상점 등에서 사용하는 통합 재화 조회 API")
    @GetMapping("/balance")
    public EconomyBalanceResponseDto getBalance(@RequestParam String username) {
        return economyService.getBalance(username);
    }
}
