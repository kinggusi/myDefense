package com.denfense.server;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.service.balance.AlienSpecSeedService;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.mockito.Mockito;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.mock.mockito.SpyBean;
import org.springframework.test.annotation.DirtiesContext;

import java.util.ArrayList;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;

@SpringBootTest(properties = "spring.datasource.url=jdbc:h2:mem:testdb_rollback;MODE=MySQL")
@DirtiesContext(classMode = DirtiesContext.ClassMode.AFTER_CLASS)
public class AlienSpecSeedRollbackIntegrationTest {

    @SpyBean
    private AlienSpecRepository alienSpecRepository;

    @Autowired
    private AlienSpecSeedService seedService;

    @Test
    @DisplayName("Seed Service 단일 트랜잭션 전체 롤백 검증")
    void duplicatePk_seedServiceRollback() {
        // 1. 기존 DB 상태 확인 (SeedRunner 등에 의해 이미 48건이 들어있음)
        long countBefore = alienSpecRepository.count();
        assertThat(countBefore).isGreaterThan(0);

        // ID 1은 실제로 DB에 존재함
        assertThat(alienSpecRepository.existsById(1L)).isTrue();

        // 2. 동시성 / Stale Read 모사:
        // 실제 findAllIds()는 [1, 2, ..., 48]을 반환하겠지만,
        // Spy를 이용해 ID 1이 없다고 속임. (예: [2, 3, ..., 48]만 반환)
        List<Long> mockedIds = new ArrayList<>();
        for (long i = 2; i <= 48; i++) {
            mockedIds.add(i);
        }
        Mockito.doReturn(mockedIds).when(alienSpecRepository).findAllIds();

        // 3. Seed Service 호출
        // SeedService는 ID 1이 누락되었다고 판단하고 persist(ID=1)을 시도할 것임.
        // 그리고 flush 시 PK 중복 예외가 발생하여 전체 트랜잭션이 롤백되어야 함.
        // Spring의 @Transactional 프록시가 RuntimeException(PersistenceException, DataAccessException 등)을 잡아서 롤백시킴
        assertThatThrownBy(() -> seedService.seed())
                .isInstanceOf(Exception.class); // DataIntegrityViolationException 등 발생 예상

        // 4. 예외 발생 후 DB 재조회 (새 트랜잭션 / 영속성 컨텍스트 분리 상태)
        // Mock 복원
        Mockito.reset(alienSpecRepository);

        // 충돌 행(ID 1)은 유지되어야 함
        assertThat(alienSpecRepository.existsById(1L)).isTrue();

        // 전체 건수도 롤백되어 이전과 동일해야 함 (새로운 데이터가 부분 삽입되지 않음)
        assertThat(alienSpecRepository.count()).isEqualTo(countBefore);
    }
}
