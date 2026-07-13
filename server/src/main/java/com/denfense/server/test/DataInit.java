package com.denfense.server.test;

import com.denfense.server.domain.MonsterSpec;
import com.denfense.server.domain.User;
import com.denfense.server.repository.MonsterSpecRepository;
import com.denfense.server.repository.UserRepository;
import jakarta.transaction.Transactional;
import lombok.RequiredArgsConstructor;
import org.springframework.boot.CommandLineRunner;
import org.springframework.stereotype.Component;

import java.util.ArrayList;
import java.util.List;

import org.springframework.core.annotation.Order;

@Component
@RequiredArgsConstructor
@Order(10)
public class DataInit implements CommandLineRunner {

    private final UserRepository userRepository;
    private final MonsterSpecRepository monsterSpecRepository;

    @Override
    @Transactional
    public void run(String... args) throws Exception {
        System.out.println("====== [TEST DATA] 몬스터/유저 데이터 확인 및 생성 시작 ======");

        if (monsterSpecRepository.count() == 0) {
            List<MonsterSpec> mc = createMonster(1, MonsterSpec.MonsterType.NORMAL, 3);
            List<MonsterSpec> mc1 = createMonster(2, MonsterSpec.MonsterType.NORMAL, 3);
            List<MonsterSpec> mc2 = createMonster(3, MonsterSpec.MonsterType.NORMAL, 3);
            List<MonsterSpec> mc3 = createMonster(4, MonsterSpec.MonsterType.NORMAL, 3);
            List<MonsterSpec> mc4 = createMonster(5, MonsterSpec.MonsterType.NORMAL, 3);
            System.out.println("====== [TEST DATA] 몬스터 생성 완료 (15건) ======");
        }

        if (userRepository.count() == 0) {
            User user = new User("sh1", "1234");
            user.setGold(100000);
            user.setDiamond(1000000);
            userRepository.save(user);
            System.out.println("====== [TEST DATA] 유저 생성 완료 (1건) ======");
        }

        System.out.println("====== [TEST DATA] 초기화 완료 ======");
    }

    private List<MonsterSpec> createMonster(int grade, MonsterSpec.MonsterType type, int count) {
        List<MonsterSpec> createdSpecs = new ArrayList<>();
        for (int i = 0; i < count; i++) {
            MonsterSpec ms = new MonsterSpec();
            // 수정: HP를 현실적으로 조정 (예: 유닛 공격력이 10~50 수준이므로, 등급에 따라 100부터 시작하도록)
            int monhp1 = 100;
            int monhp2 = 200;
            int monhp3 = 500;

            if (grade > 3) {
                if (count == 0) {
                    ms.setHp(monhp1 * grade);
                } else if (count == 1) {
                    ms.setHp(monhp2 * grade);
                } else if (count == 2) {
                    ms.setHp(monhp3 * grade);
                }
            } else {
                if (count == 0) {
                    ms.setHp(monhp1);
                } else if (count == 1) {
                    ms.setHp(monhp2);
                } else if (count == 2) {
                    ms.setHp(monhp3);
                }
            }

            ms.setGrade(grade);
            ms.setType(type); // 타입 저장 추가
            ms.setName("몬스터" + grade + "-" + i); // 이름 구분을 명확히
            ms.setMoveSpeed(1.2);
            ms.setDropGold(20);

            monsterSpecRepository.save(ms);
            createdSpecs.add(ms);
        }
        return createdSpecs;
    }
}