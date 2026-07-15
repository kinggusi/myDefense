package com.denfense.server.controller;

import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;

import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;
import static org.mockito.Mockito.verifyNoInteractions;

/**
 * ShopController UUID 파라미터 검증 테스트 (7번 항목)
 *
 * purchaseRequestId는 UUID 타입(@RequestParam UUID)으로 선언되어
 * 잘못된 형식은 Spring의 MethodArgumentTypeMismatchException → HTTP 400으로 자동 처리됨.
 * null/누락 시에는 MissingServletRequestParameterException → HTTP 400.
 */
@SpringBootTest
@AutoConfigureMockMvc
@ActiveProfiles("test")
class ShopControllerUuidValidationTest {

    @Autowired
    private MockMvc mockMvc;

    @MockBean
    private com.denfense.server.service.gacha.GachaPurchaseService gachaPurchaseService;

    @Test
    @DisplayName("잘못된 UUID 형식 → HTTP 400 (Spring 자동 처리, Service 미호출)")
    void invalidUuidFormat_returns400() throws Exception {
        mockMvc.perform(post("/api/shop/gacha/purchase")
                        .param("username", "testUser")
                        .param("productId", "ALIEN_GACHA_SINGLE")
                        .param("purchaseRequestId", "not-a-valid-uuid"))
                .andExpect(status().isBadRequest());
        verifyNoInteractions(gachaPurchaseService);
    }

    @Test
    @DisplayName("purchaseRequestId 누락 → HTTP 400")
    void missingPurchaseRequestId_returns400() throws Exception {
        mockMvc.perform(post("/api/shop/gacha/purchase")
                        .param("username", "testUser")
                        .param("productId", "ALIEN_GACHA_SINGLE"))
                // purchaseRequestId 없음
                .andExpect(status().isBadRequest());
        verifyNoInteractions(gachaPurchaseService);
    }

    @Test
    @DisplayName("비어 있는 UUID 문자열 → HTTP 400")
    void emptyUuidString_returns400() throws Exception {
        mockMvc.perform(post("/api/shop/gacha/purchase")
                        .param("username", "testUser")
                        .param("productId", "ALIEN_GACHA_SINGLE")
                        .param("purchaseRequestId", ""))
                .andExpect(status().isBadRequest());
        verifyNoInteractions(gachaPurchaseService);
    }
}
