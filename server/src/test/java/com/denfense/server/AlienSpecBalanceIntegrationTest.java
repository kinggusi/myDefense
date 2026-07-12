package com.denfense.server;

import com.denfense.server.balance.AlienSpecBalance;
import com.denfense.server.domain.AlienSpec;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.service.balance.BalanceRegistry;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;
import org.springframework.test.annotation.DirtiesContext;

@SpringBootTest
@DirtiesContext(classMode = DirtiesContext.ClassMode.AFTER_CLASS)
public class AlienSpecBalanceIntegrationTest {

    @Autowired
    private AlienSpecRepository alienSpecRepository;

    @Autowired
    private BalanceRegistry balanceRegistry;

    @Test
    @DisplayName("DataInit으로 생성된 DB 왹져 데이터와 alien-spec.json 엑셀 데이터가 정확히 일치해야 한다")
    @DirtiesContext(methodMode = DirtiesContext.MethodMode.BEFORE_METHOD)
    void verifyDbAndExcelMatch() {
        // 1. DB 전체 조회 (DataInit이 서버 기동 시 32건 적재함)
        List<AlienSpec> dbSpecs = alienSpecRepository.findAll();
        
        // 2. Registry 전체 조회 (alien-spec.json에서 32건 적재됨)
        List<AlienSpecBalance> excelSpecs = balanceRegistry.getAllAlienSpecs();

        // 3. 개수 검증
        assertThat(dbSpecs).hasSize(32);
        assertThat(excelSpecs).hasSize(32);

        // 4. 내용 검증 (ID 매칭)
        for (AlienSpecBalance excelSpec : excelSpecs) {
            AlienSpec dbSpec = dbSpecs.stream()
                    .filter(s -> s.getId().equals(excelSpec.alienId()))
                    .findFirst()
                    .orElseThrow(() -> new AssertionError("DB에 ID가 없는 왹져 발견: " + excelSpec.alienId()));

            assertThat(dbSpec.getName()).isEqualTo(excelSpec.name());
            assertThat(dbSpec.getDescription()).isEqualTo(excelSpec.description());
            assertThat(dbSpec.getGrade().name()).isEqualTo(excelSpec.grade());
            assertThat(dbSpec.getBaseAtk()).isEqualTo(excelSpec.baseAttack());
            assertThat(dbSpec.getBaseMp()).isEqualTo(excelSpec.baseMp());
            assertThat(dbSpec.getAtkSpeed()).isEqualTo(excelSpec.attackSpeed());
            assertThat(dbSpec.getRange()).isEqualTo(excelSpec.attackRange());
            assertThat(dbSpec.getEvolutionTargetId()).isEqualTo(excelSpec.evolutionTargetId());
            assertThat(dbSpec.isLocked()).isEqualTo(excelSpec.isLocked());
        }
    }
}
