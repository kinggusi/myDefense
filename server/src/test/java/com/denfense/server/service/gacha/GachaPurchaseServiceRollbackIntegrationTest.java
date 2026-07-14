package com.denfense.server.service.gacha;

import com.denfense.server.domain.User;
import com.denfense.server.repository.UserRepository;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.MockBean;
import org.springframework.test.context.ActiveProfiles;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.when;

@SpringBootTest
@ActiveProfiles("test")
class GachaPurchaseServiceRollbackIntegrationTest {

    @Autowired
    private GachaPurchaseService gachaPurchaseService;

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private com.denfense.server.repository.UserAlienRepository userAlienRepository;

    @MockBean
    private GachaDrawService gachaDrawService; // 여기서 에러 발생시켜 롤백 유도

    private User testUser;

    @BeforeEach
    void setUp() {
        testUser = new User("rollbackUser", "pw");
        testUser.setDiamond(500);
        userRepository.save(testUser);
    }

    @AfterEach
    void tearDown() {
        userRepository.deleteAll();
    }

    @Test
    @DisplayName("실제 트랜잭션 롤백 통합 테스트 - 차감 이후 지급 중 예외 발생 시 다이아 차감이 DB 수준에서 롤백되는지 검증")
    void purchase_rollbackOnException() {
        // 추첨 시 예외 발생 유도 (다이아 차감 이후 시점)
        when(gachaDrawService.draw(anyString())).thenThrow(new RuntimeException("지급 중 런타임 에러 발생"));

        assertThatThrownBy(() -> gachaPurchaseService.purchase("rollbackUser", "ALIEN_GACHA_SINGLE"))
                .isInstanceOf(RuntimeException.class)
                .hasMessage("지급 중 런타임 에러 발생");

        // 트랜잭션 롤백 확인 (다이아 500이 그대로 유지되어야 함)
        User updatedUser = userRepository.findByUsername("rollbackUser").orElseThrow();
        assertThat(updatedUser.getDiamond()).isEqualTo(500);

        // UserAlien 생성 없음 확인
        java.util.List<com.denfense.server.domain.UserAlien> userAliens = userAlienRepository.findAllByUser(updatedUser);
        assertThat(userAliens).isEmpty();
    }
}
