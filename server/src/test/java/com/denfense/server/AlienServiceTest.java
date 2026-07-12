package com.denfense.server;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.dto.response.AlienUpgradeResponseDto;
import com.denfense.server.exception.BusinessException;
import com.denfense.server.exception.ErrorCode;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.AlienService;
import com.denfense.server.service.UpgradeCostPolicy;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.atomic.AtomicInteger;

import static org.junit.jupiter.api.Assertions.*;

@SpringBootTest
class AlienServiceTest {

    @Autowired
    private AlienService alienService;

    @Autowired
    private UserRepository userRepository;

    @Autowired
    private UserAlienRepository userAlienRepository;

    @Autowired
    private AlienSpecRepository alienSpecRepository;

    private User testUser;
    private AlienSpec testAlienSpec;
    private UserAlien testUserAlien;

    @BeforeEach
    void setUp() {
        userAlienRepository.deleteAll();
        userRepository.deleteAll();
        alienSpecRepository.deleteAll();

        testAlienSpec = new AlienSpec();
        testAlienSpec.setName("테스트왹져");
        testAlienSpec.setGrade(AlienSpec.Grade.NORMAL);
        testAlienSpec.setLocked(false);
        alienSpecRepository.save(testAlienSpec);

        testUser = new User("Tester", "password");
        testUser.setGold(10000);
        testUser.setUniversalPiece(100);
        testUser.setGrowthCell(50);
        userRepository.save(testUser);

        testUserAlien = new UserAlien(testUser, testAlienSpec);
        testUserAlien.setLevel(1);
        testUserAlien.setPieces(50); // 충분한 조각
        userAlienRepository.save(testUserAlien);
    }

    @Test
    @DisplayName("카드 충분 정상 강화")
    void upgrade_enoughPieces_success() {
        // level 1 cost: 5 pieces, 100 gold, 0 cell
        AlienUpgradeResponseDto response = alienService.upgradeAlien(testUser.getUsername(), testAlienSpec.getId().intValue());
        
        assertEquals(2, response.getAfterLevel());
        assertEquals(5, response.getUsedPieces());
        assertEquals(0, response.getUsedUniversalPiece());
        assertEquals(100, response.getUsedGold());
        assertEquals(0, response.getUsedGrowthCell());
        
        User user = userRepository.findById(testUser.getId()).get();
        assertEquals(9900, user.getGold()); // 10000 - 100
        assertEquals(100, user.getUniversalPiece());
    }

    @Test
    @DisplayName("카드 부족분만 대체 코인 사용")
    void upgrade_notEnoughPieces_usesUniversalPiece() {
        testUserAlien.setPieces(2);
        userAlienRepository.save(testUserAlien);
        
        // cost is 5. We have 2 pieces. Shortage is 3.
        AlienUpgradeResponseDto response = alienService.upgradeAlien(testUser.getUsername(), testAlienSpec.getId().intValue());
        
        assertEquals(2, response.getUsedPieces());
        assertEquals(3, response.getUsedUniversalPiece());
        
        User user = userRepository.findById(testUser.getId()).get();
        assertEquals(97, user.getUniversalPiece()); // 100 - 3
    }

    @Test
    @DisplayName("카드+대체코인 총합 부족 예외")
    void upgrade_notEnoughTotalPieces_throwsException() {
        testUserAlien.setPieces(2);
        userAlienRepository.save(testUserAlien);
        
        testUser.setUniversalPiece(2); // total 4 < 5
        userRepository.save(testUser);
        
        BusinessException ex = assertThrows(BusinessException.class, () -> 
            alienService.upgradeAlien(testUser.getUsername(), testAlienSpec.getId().intValue()));
            
        assertEquals(ErrorCode.INSUFFICIENT_ALIEN_PIECES, ex.getErrorCode());
    }

    @Test
    @DisplayName("골드 부족 예외")
    void upgrade_notEnoughGold_throwsException() {
        testUser.setGold(50); // need 100
        userRepository.save(testUser);
        
        BusinessException ex = assertThrows(BusinessException.class, () -> 
            alienService.upgradeAlien(testUser.getUsername(), testAlienSpec.getId().intValue()));
            
        assertEquals(ErrorCode.INSUFFICIENT_ACCOUNT_GOLD, ex.getErrorCode());
    }

    @Test
    @DisplayName("10->11 강화 시 성장 세포 사용")
    void upgrade_usesGrowthCell_atLevel10() {
        testUserAlien.setLevel(10);
        userAlienRepository.save(testUserAlien);
        
        // level 10 cost: 50 pieces, 1000 gold, 2 cell
        AlienUpgradeResponseDto response = alienService.upgradeAlien(testUser.getUsername(), testAlienSpec.getId().intValue());
        
        assertEquals(2, response.getUsedGrowthCell());
        
        User user = userRepository.findById(testUser.getId()).get();
        assertEquals(48, user.getGrowthCell()); // 50 - 2
    }

    @Test
    @DisplayName("성장 세포 부족 예외")
    void upgrade_notEnoughGrowthCell_throwsException() {
        testUserAlien.setLevel(10);
        userAlienRepository.save(testUserAlien);
        
        testUser.setGrowthCell(1); // need 2
        userRepository.save(testUser);
        
        BusinessException ex = assertThrows(BusinessException.class, () -> 
            alienService.upgradeAlien(testUser.getUsername(), testAlienSpec.getId().intValue()));
            
        assertEquals(ErrorCode.INSUFFICIENT_GROWTH_CELL, ex.getErrorCode());
    }

    @Test
    @DisplayName("최대 레벨 도달 예외")
    void upgrade_maxLevel_throwsException() {
        testUserAlien.setLevel(UpgradeCostPolicy.MAX_LEVEL);
        userAlienRepository.save(testUserAlien);
        
        BusinessException ex = assertThrows(BusinessException.class, () -> 
            alienService.upgradeAlien(testUser.getUsername(), testAlienSpec.getId().intValue()));
            
        assertEquals(ErrorCode.MAX_ALIEN_LEVEL_REACHED, ex.getErrorCode());
    }

    @Test
    @DisplayName("동시 요청, 2회분 재화 충분 시 두 건 순차 성공")
    void upgrade_concurrent_bothSucceed() throws InterruptedException {
        int threadCount = 2;
        ExecutorService executorService = Executors.newFixedThreadPool(threadCount);
        CountDownLatch latch = new CountDownLatch(threadCount);
        AtomicInteger successCount = new AtomicInteger();
        
        for(int i = 0; i < threadCount; i++) {
            executorService.execute(() -> {
                try {
                    alienService.upgradeAlien(testUser.getUsername(), testAlienSpec.getId().intValue());
                    successCount.incrementAndGet();
                } catch (Exception e) {
                    System.out.println("Exception: " + e.getMessage());
                } finally {
                    latch.countDown();
                }
            });
        }
        
        latch.await();
        assertEquals(2, successCount.get());
        
        UserAlien updatedAlien = userAlienRepository.findById(testUserAlien.getId()).get();
        assertEquals(3, updatedAlien.getLevel()); // 1 -> 2 -> 3
    }
    
    @Test
    @DisplayName("동시 요청, 1회분 재화만 있을 때 한 건만 성공")
    void upgrade_concurrent_onlyOneSucceeds() throws InterruptedException {
        // level 1: 5 pieces, level 2: 10 pieces. Total 15 pieces needed for 2 upgrades.
        // Let's set pieces to 6, so second upgrade will need 10 but we have 1 piece left and 0 universal.
        testUser.setUniversalPiece(0);
        userRepository.save(testUser);
        
        testUserAlien.setPieces(6);
        userAlienRepository.save(testUserAlien);
        
        int threadCount = 2;
        ExecutorService executorService = Executors.newFixedThreadPool(threadCount);
        CountDownLatch latch = new CountDownLatch(threadCount);
        AtomicInteger successCount = new AtomicInteger();
        AtomicInteger failCount = new AtomicInteger();
        
        for(int i = 0; i < threadCount; i++) {
            executorService.execute(() -> {
                try {
                    alienService.upgradeAlien(testUser.getUsername(), testAlienSpec.getId().intValue());
                    successCount.incrementAndGet();
                } catch (BusinessException e) {
                    failCount.incrementAndGet();
                } finally {
                    latch.countDown();
                }
            });
        }
        
        latch.await();
        assertEquals(1, successCount.get());
        assertEquals(1, failCount.get());
        
        UserAlien updatedAlien = userAlienRepository.findById(testUserAlien.getId()).get();
        assertEquals(2, updatedAlien.getLevel()); 
    }

    @Test
    @DisplayName("동일 유저-왹져 중복 저장 시 유니크 제약조건 예외 발생")
    void saveDuplicateUserAlien_throwsException() {
        UserAlien duplicateAlien = new UserAlien(testUser, testAlienSpec);
        
        assertThrows(org.springframework.dao.DataIntegrityViolationException.class, () -> {
            userAlienRepository.saveAndFlush(duplicateAlien);
        });
    }
}
