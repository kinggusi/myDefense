package com.denfense.server.controller;

import com.denfense.server.dto.response.UserAlienResponseDto;
import com.denfense.server.service.ShopService;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

@RestController
@RequestMapping("/api/shop")
@RequiredArgsConstructor
public class ShopController {

    private final ShopService shopService;

    /**
     * 왹져 소환 (Gacha) API
     * * [POST] /api/shop/gacha?username=MyDev&count=10
     * - username: 유저 아이디
     * - count: 1 또는 10 (안 보내면 기본값 1)
     */
    @PostMapping("/gacha")
    public List<UserAlienResponseDto> gacha(@RequestParam String username,
                                            @RequestParam(defaultValue = "1") int count) {

        // 서비스에게 주문 전달하고, 결과(변경된 왹져 목록)를 바로 반환
        return shopService.gachaAlien(username, count);
    }
}
