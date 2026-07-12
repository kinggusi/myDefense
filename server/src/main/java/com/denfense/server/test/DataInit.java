package com.denfense.server.test;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.MonsterSpec;
import com.denfense.server.domain.User;
import com.denfense.server.repository.AlienSpecRepository;
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

    private final AlienSpecRepository alienSpecRepository;
    private final UserRepository userRepository;
    private final MonsterSpecRepository monsterSpecRepository;

    @Override
    @Transactional
    public void run(String... args) throws Exception {
        // 데이터가 없을 때만 실행 (중복 생성 방지)
        if (alienSpecRepository.count() > 0) return;

        System.out.println("====== [TEST DATA] 왹져 데이터 생성 시작 ======");

        // 1. 전설 7마리 (최상위라 진화 타겟 없음) -> IDs: 1~7
        List<AlienSpec> legends = createAliens(AlienSpec.Grade.LEGEND, 7, 5.0, null, 1);

        // 2. 유니크 7마리 (전설과 연결)
        // Unique 1 -> Legend 1 로 진화 -> IDs: 8~14
        List<AlienSpec> uniques = createAliens(AlienSpec.Grade.UNIQUE, 7, 2.5, legends, 8);

        // 3. 에픽 7마리 (유니크와 연결)
        // Epic 1 -> Unique 1 로 진화 -> IDs: 15~21
        List<AlienSpec> epics = createAliens(AlienSpec.Grade.EPIC, 7, 1.5, uniques, 15);

        // 4. 노말 7마리 (에픽과 연결)
        // Normal 1 -> Epic 1 로 진화 -> IDs: 22~28
        List<AlienSpec> normals = createAliens(AlienSpec.Grade.NORMAL, 7, 1.0, epics, 22);

        List<MonsterSpec> mc = createMonster(1, MonsterSpec.MonsterType.NORMAL, 3);
        List<MonsterSpec> mc1 = createMonster(2, MonsterSpec.MonsterType.NORMAL, 3);
        List<MonsterSpec> mc2 = createMonster(3, MonsterSpec.MonsterType.NORMAL, 3);
        List<MonsterSpec> mc3 = createMonster(4, MonsterSpec.MonsterType.NORMAL, 3);
        List<MonsterSpec> mc4 = createMonster(5, MonsterSpec.MonsterType.NORMAL, 3);

        // (전설 합성은 선택권 방식이라 고정 타겟 ID는 일단 null로 둠) -> IDs: 29~32
        createAliens(AlienSpec.Grade.MYTHIC, 4, 10.0, null, 29);

        System.out.println("====== [TEST DATA] 생성 완료 ======");

        // 6. 테스트용 유저 생성 (로그인 테스트용)
        if (userRepository.findByUsername("testUser").isEmpty()) {
            User user = new User("sh1", "1234");

            user.setGold(100000);
            user.setDiamond(1000000);
            userRepository.save(user);
        }

        System.out.println("====== [TEST DATA] 생성 완료 (총 41종) ======");
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




    private List<AlienSpec> createAliens(AlienSpec.Grade grade, int count, double multiplier, List<AlienSpec> upperSpecs, long startId) {
        List<AlienSpec> createdSpecs = new ArrayList<>();

        for (int i = 0; i < count; i++) {
            AlienSpec spec = new AlienSpec();
            spec.setId(startId + i);
            // 이름 예: NORMAL 1, LEGEND 3
            spec.setName(grade.name() + " " + (i + 1));
            spec.setGrade(grade);
            spec.setDescription(grade.name() + " 등급의 강력한 왹져입니다.");

            // 스탯 설정 (대충 배율 곱해서 설정)
            spec.setBaseAtk((int)(10 * multiplier));
            spec.setBaseMp((int)(100 * multiplier)); // 체력 대신 마나(MP)라면 이렇게
            spec.setAtkSpeed(1.0); // 초당 1회 공격
            spec.setRange(3.5);    // 사거리 3.5칸

            // 기본 잠금 상태 (노말~전설은 해금 / 진화체는 잠금)
            if (grade == AlienSpec.Grade.MYTHIC) {
                spec.setLocked(true);
            } else {
                spec.setLocked(false);
            }

            // [핵심] 족보 연결 (EvolutionTargetId 설정)
            // 상위 등급 리스트가 있고, 인덱스가 맞으면 연결
            if (upperSpecs != null && i < upperSpecs.size()) {
                spec.setEvolutionTargetId(upperSpecs.get(i).getId());
            }

            alienSpecRepository.save(spec);
            createdSpecs.add(spec);
        }
        return createdSpecs;
    }
}