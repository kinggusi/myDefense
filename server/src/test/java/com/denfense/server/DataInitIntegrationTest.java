package com.denfense.server;

import com.denfense.server.domain.MonsterSpec;
import com.denfense.server.domain.User;
import com.denfense.server.repository.MonsterSpecRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.test.DataInit;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;

import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

@SpringBootTest
public class DataInitIntegrationTest {

    @Autowired
    private DataInit dataInit;

    @Autowired
    private MonsterSpecRepository monsterSpecRepository;

    @Autowired
    private UserRepository userRepository;

    @BeforeEach
    void setUp() {
        // Spring Boot 시작 시 CommandLineRunner로 인해 이미 삽입된 초기 데이터를 지우고 시작
        monsterSpecRepository.deleteAll();
        userRepository.deleteAll();
    }

    @AfterEach
    void tearDown() throws Exception {
        // 공유 컨텍스트를 사용하는 다른 테스트들이 정상 작동할 수 있도록 초기 상태(DataInit 실행 후) 복원
        monsterSpecRepository.deleteAll();
        userRepository.deleteAll();
        dataInit.run();
    }

    @Test
    @DisplayName("MonsterSpec 15건 존재 + User 없음 -> User만 생성")
    void monsterExists_userNotExists_createsUserOnly() throws Exception {
        // given: MonsterSpec 15건 임의 생성
        for (int i = 0; i < 15; i++) {
            MonsterSpec ms = new MonsterSpec();
            ms.setHp(100);
            ms.setGrade(1);
            ms.setName("TestMon" + i);
            monsterSpecRepository.save(ms);
        }
        assertThat(userRepository.count()).isEqualTo(0);

        // when
        dataInit.run();

        // then: User가 1건 생성되어야 함
        assertThat(userRepository.count()).isEqualTo(1);
        User createdUser = userRepository.findAll().get(0);
        assertThat(createdUser.getUsername()).isEqualTo("sh1");

        // MonsterSpec은 15건 유지되어야 하며, 우리가 임의 생성한 이름("TestMon")이 그대로여야 함
        assertThat(monsterSpecRepository.count()).isEqualTo(15);
        List<MonsterSpec> specs = monsterSpecRepository.findAll();
        assertThat(specs.stream().allMatch(m -> m.getName().startsWith("TestMon"))).isTrue();
    }

    @Test
    @DisplayName("User 존재 + MonsterSpec 없음 -> MonsterSpec만 생성, 기존 User 보존")
    void userExists_monsterNotExists_createsMonsterOnly() throws Exception {
        // given: 기존 User 생성
        User existingUser = new User("sh1", "1234");
        existingUser.setGold(999);
        userRepository.save(existingUser);

        assertThat(monsterSpecRepository.count()).isEqualTo(0);

        // when
        dataInit.run();

        // then: MonsterSpec 15건 생성됨
        assertThat(monsterSpecRepository.count()).isEqualTo(15);

        // User는 1건 유지되며 값이 덮어씌워지지 않음(gold 999)
        assertThat(userRepository.count()).isEqualTo(1);
        User user = userRepository.findAll().get(0);
        assertThat(user.getGold()).isEqualTo(999);
    }

    @Test
    @DisplayName("둘 다 존재 -> 추가 생성 및 변경 없음")
    void bothExist_noCreation() throws Exception {
        // given
        for (int i = 0; i < 15; i++) {
            MonsterSpec ms = new MonsterSpec();
            ms.setHp(100);
            ms.setGrade(1);
            ms.setName("TestMon" + i);
            monsterSpecRepository.save(ms);
        }

        User existingUser = new User("sh1", "1234");
        existingUser.setGold(999);
        userRepository.save(existingUser);

        // when
        dataInit.run();

        // then
        assertThat(monsterSpecRepository.count()).isEqualTo(15);
        List<MonsterSpec> specs = monsterSpecRepository.findAll();
        assertThat(specs.stream().allMatch(m -> m.getName().startsWith("TestMon"))).isTrue();

        assertThat(userRepository.count()).isEqualTo(1);
        User user = userRepository.findAll().get(0);
        assertThat(user.getGold()).isEqualTo(999);
    }
}
