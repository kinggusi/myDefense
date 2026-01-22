package com.denfense.server.controller;

import com.denfense.server.domain.UserAlien;
import com.denfense.server.service.AlienService;
import com.denfense.server.service.ShopService;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/aliens")
@RequiredArgsConstructor

public class AlienController {

    private final AlienService alienService;

    //
    /**
     * 왹져 업그레이드
     * * [POST] http://localhost:8080/api/aliens/1/upgrade?username=MyDev
     * - username: 유저 아이디
     * - count: 1 또는 10 (안 보내면 기본값 1)
     */
    @PostMapping("/{alienId}/upgrade")
    public UserAlien upgrade(@PathVariable int alienId, @RequestParam String username) {
        return alienService.aleinUpgrade(username, alienId);
    }
}
