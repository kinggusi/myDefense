package com.denfense.server.controller;

import com.denfense.server.dto.gacha.GachaPurchaseResponseDto;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.service.gacha.GachaPurchaseService;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.test.web.servlet.MockMvc;

import java.util.List;
import java.util.UUID;

import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@SpringBootTest
@AutoConfigureMockMvc
@ActiveProfiles("test")
class GachaPurchaseErrorContractApiTest {

    @Autowired
    private MockMvc mockMvc;

    @MockBean
    private GachaPurchaseService gachaPurchaseService;

    @Test
    @DisplayName("정상 구매는 HTTP 200")
    void success_returns200() throws Exception {
        UUID requestId = UUID.randomUUID();
        GachaPurchaseResponseDto response = new GachaPurchaseResponseDto(
                "ALIEN_GACHA_SINGLE", "DIAMOND", 500, 0, 1, List.of(), List.of());
        when(gachaPurchaseService.purchase("user", "ALIEN_GACHA_SINGLE", requestId)).thenReturn(response);

        perform("user", "ALIEN_GACHA_SINGLE", requestId)
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.productId").value("ALIEN_GACHA_SINGLE"));
    }

    @Test
    @DisplayName("사용자 없음은 HTTP 404와 USER_NOT_FOUND")
    void userNotFound_returns404() throws Exception {
        assertBusinessError(ErrorCode.USER_NOT_FOUND);
    }

    @Test
    @DisplayName("상품 없음은 HTTP 404와 SHOP_PRODUCT_NOT_FOUND")
    void productNotFound_returns404() throws Exception {
        assertBusinessError(ErrorCode.SHOP_PRODUCT_NOT_FOUND);
    }

    @Test
    @DisplayName("비활성 상품은 HTTP 422와 SHOP_PRODUCT_INACTIVE")
    void inactiveProduct_returns422() throws Exception {
        assertBusinessError(ErrorCode.SHOP_PRODUCT_INACTIVE);
    }

    @Test
    @DisplayName("가챠 풀 없음은 HTTP 404와 GACHA_POOL_NOT_FOUND")
    void poolNotFound_returns404() throws Exception {
        assertBusinessError(ErrorCode.GACHA_POOL_NOT_FOUND);
    }

    @Test
    @DisplayName("비활성 가챠 풀은 HTTP 422와 GACHA_POOL_INACTIVE")
    void inactivePool_returns422() throws Exception {
        assertBusinessError(ErrorCode.GACHA_POOL_INACTIVE);
    }

    @Test
    @DisplayName("다이아 부족은 HTTP 409와 INSUFFICIENT_DIAMOND")
    void insufficientDiamond_returns409() throws Exception {
        assertBusinessError(ErrorCode.INSUFFICIENT_DIAMOND);
    }

    @Test
    @DisplayName("requestId와 productId 충돌은 HTTP 409와 PURCHASE_REQUEST_CONFLICT")
    void purchaseRequestConflict_returns409() throws Exception {
        assertBusinessError(ErrorCode.PURCHASE_REQUEST_CONFLICT);
    }

    @Test
    @DisplayName("처리 중인 동일 구매는 HTTP 409와 PURCHASE_ALREADY_PROCESSING")
    void purchaseAlreadyProcessing_returns409() throws Exception {
        assertBusinessError(ErrorCode.PURCHASE_ALREADY_PROCESSING);
    }

    @Test
    @DisplayName("미지원 재화는 HTTP 422와 UNSUPPORTED_CURRENCY")
    void unsupportedCurrency_returns422() throws Exception {
        assertBusinessError(ErrorCode.UNSUPPORTED_CURRENCY);
    }

    @Test
    @DisplayName("AlienSpec 없음은 HTTP 404와 ALIEN_SPEC_NOT_FOUND")
    void alienSpecNotFound_returns404() throws Exception {
        assertBusinessError(ErrorCode.ALIEN_SPEC_NOT_FOUND);
    }

    @Test
    @DisplayName("저장 응답 복원 실패는 HTTP 500과 PURCHASE_RESPONSE_RESTORE_FAILED")
    void responseRestoreFailed_returns500() throws Exception {
        assertBusinessError(ErrorCode.PURCHASE_RESPONSE_RESTORE_FAILED);
    }

    @Test
    @DisplayName("예상하지 못한 RuntimeException은 HTTP 500과 INTERNAL_SERVER_ERROR")
    void unexpectedRuntimeException_returns500() throws Exception {
        UUID requestId = UUID.randomUUID();
        when(gachaPurchaseService.purchase("user", "ALIEN_GACHA_SINGLE", requestId))
                .thenThrow(new RuntimeException("unexpected"));

        perform("user", "ALIEN_GACHA_SINGLE", requestId)
                .andExpect(status().isInternalServerError())
                .andExpect(jsonPath("$.code").value(ErrorCode.INTERNAL_SERVER_ERROR.name()))
                .andExpect(jsonPath("$.message").value(ErrorCode.INTERNAL_SERVER_ERROR.getMessage()));
    }

    private void assertBusinessError(ErrorCode errorCode) throws Exception {
        UUID requestId = UUID.randomUUID();
        when(gachaPurchaseService.purchase("user", "ALIEN_GACHA_SINGLE", requestId))
                .thenThrow(new BusinessException(errorCode));

        perform("user", "ALIEN_GACHA_SINGLE", requestId)
                .andExpect(status().is(errorCode.getStatus().value()))
                .andExpect(jsonPath("$.code").value(errorCode.name()))
                .andExpect(jsonPath("$.message").value(errorCode.getMessage()));
    }

    private org.springframework.test.web.servlet.ResultActions perform(
            String username, String productId, UUID requestId) throws Exception {
        return mockMvc.perform(post("/api/shop/gacha/purchase")
                .param("username", username)
                .param("productId", productId)
                .param("purchaseRequestId", requestId.toString()));
    }
}
