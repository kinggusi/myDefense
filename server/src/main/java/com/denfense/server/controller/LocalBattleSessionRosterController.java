package com.denfense.server.controller;

import com.denfense.server.dto.battle.BattleSessionRosterDtos;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.service.BattleSessionRosterAuthorityAdapter;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Profile;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.net.InetAddress;

/**
 * Development-only roster bridge. It is not loaded in production and only
 * accepts loopback callers on the local machine.
 *
 * FUTURE_AUTH_REPLACEMENT: production matchmaking must expose a separate JWT
 * authenticated adapter; never enable this controller in production.
 */
@RestController
@Profile({"local", "dev"})
@RequiredArgsConstructor
@RequestMapping("/api/dev/battle/session-rosters")
public class LocalBattleSessionRosterController {
    private final BattleSessionRosterAuthorityAdapter adapter;

    @PostMapping
    public BattleSessionRosterDtos.RegisterResponse register(
            HttpServletRequest servletRequest,
            @RequestBody BattleSessionRosterDtos.RegisterRequest request) {
        if (!isLoopback(servletRequest.getRemoteAddr())) {
            throw new BusinessException(ErrorCode.BATTLE_ROSTER_REGISTRATION_FORBIDDEN);
        }
        return adapter.register(request);
    }

    private static boolean isLoopback(String address) {
        try {
            return address != null && InetAddress.getByName(address).isLoopbackAddress();
        } catch (Exception exception) {
            return false;
        }
    }
}
