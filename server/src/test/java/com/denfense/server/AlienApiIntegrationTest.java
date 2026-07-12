package com.denfense.server;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import com.denfense.server.service.UpgradeCostPolicy;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.client.TestRestTemplate;
import org.springframework.boot.test.web.server.LocalServerPort;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

import static org.junit.jupiter.api.Assertions.assertEquals;

@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
public class AlienApiIntegrationTest {

    @LocalServerPort
    private int port;

    @Autowired
    private TestRestTemplate restTemplate;

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
        testAlienSpec.setName("HTTP테스트왹져");
        testAlienSpec.setGrade(AlienSpec.Grade.NORMAL);
        alienSpecRepository.save(testAlienSpec);

        testUser = new User("ApiTester", "pass");
        testUser.setGold(10000);
        testUser.setUniversalPiece(100);
        testUser.setGrowthCell(50);
        userRepository.save(testUser);

        testUserAlien = new UserAlien(testUser, testAlienSpec);
        testUserAlien.setLevel(1);
        testUserAlien.setPieces(50);
        userAlienRepository.save(testUserAlien);
    }

    private void printResult(String testName, String url, HttpStatus status, String responseBody) {
        System.out.println("=== " + testName + " ===");
        System.out.println("URL: " + url);
        System.out.println("Status: " + status);
        System.out.println("Response: " + responseBody);
        System.out.println("=====================");
    }

    @Test
    void testApi_1_EnoughPieces() {
        String url = "http://localhost:" + port + "/api/aliens/" + testAlienSpec.getId() + "/upgrade?username=ApiTester";
        ResponseEntity<String> response = restTemplate.postForEntity(url, null, String.class);
        printResult("1. 카드 충분 정상 강화", url, (HttpStatus) response.getStatusCode(), response.getBody());
    }

    @Test
    void testApi_2_SubstituteCoin() {
        testUserAlien.setPieces(2);
        userAlienRepository.save(testUserAlien);
        String url = "http://localhost:" + port + "/api/aliens/" + testAlienSpec.getId() + "/upgrade?username=ApiTester";
        ResponseEntity<String> response = restTemplate.postForEntity(url, null, String.class);
        printResult("2. 카드 부족 + universalPiece 사용", url, (HttpStatus) response.getStatusCode(), response.getBody());
    }

    @Test
    void testApi_3_NotEnoughGold() {
        testUser.setGold(10);
        userRepository.save(testUser);
        String url = "http://localhost:" + port + "/api/aliens/" + testAlienSpec.getId() + "/upgrade?username=ApiTester";
        ResponseEntity<String> response = restTemplate.postForEntity(url, null, String.class);
        printResult("3. 골드 부족", url, (HttpStatus) response.getStatusCode(), response.getBody());
    }

    @Test
    void testApi_4_NotEnoughGrowthCell() {
        testUserAlien.setLevel(10);
        userAlienRepository.save(testUserAlien);
        testUser.setGrowthCell(1);
        userRepository.save(testUser);
        String url = "http://localhost:" + port + "/api/aliens/" + testAlienSpec.getId() + "/upgrade?username=ApiTester";
        ResponseEntity<String> response = restTemplate.postForEntity(url, null, String.class);
        printResult("4. 성장 세포 부족", url, (HttpStatus) response.getStatusCode(), response.getBody());
    }

    @Test
    void testApi_5_MaxLevel() {
        testUserAlien.setLevel(UpgradeCostPolicy.MAX_LEVEL);
        userAlienRepository.save(testUserAlien);
        String url = "http://localhost:" + port + "/api/aliens/" + testAlienSpec.getId() + "/upgrade?username=ApiTester";
        ResponseEntity<String> response = restTemplate.postForEntity(url, null, String.class);
        printResult("5. 최대 레벨", url, (HttpStatus) response.getStatusCode(), response.getBody());
    }

    @Test
    void testApi_6_UserNotFound() {
        String url = "http://localhost:" + port + "/api/aliens/" + testAlienSpec.getId() + "/upgrade?username=UnknownUser";
        ResponseEntity<String> response = restTemplate.postForEntity(url, null, String.class);
        printResult("6. 존재하지 않는 사용자", url, (HttpStatus) response.getStatusCode(), response.getBody());
    }

    @Test
    void testApi_7_AlienNotFound() {
        String url = "http://localhost:" + port + "/api/aliens/999/upgrade?username=ApiTester";
        ResponseEntity<String> response = restTemplate.postForEntity(url, null, String.class);
        printResult("7. 보유하지 않은 Alien", url, (HttpStatus) response.getStatusCode(), response.getBody());
    }
}
