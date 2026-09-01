package com.denfense.server.controller;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.dto.response.LobbyResponseDto;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.UserAlienRepository;
import com.denfense.server.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.Comparator;
import java.util.List;
import java.util.Map;
import java.util.function.Function;
import java.util.stream.Collectors;

@RestController
@RequestMapping("/api/lobby")
@RequiredArgsConstructor
public class LobbyController {

    private final UserRepository userRepository;
    private final AlienSpecRepository alienSpecRepository;
    private final UserAlienRepository userAlienRepository;
    private final com.denfense.server.service.HeartPolicy heartPolicy;
    private final com.denfense.server.service.UpgradeCostPolicy upgradeCostPolicy;
    private final com.denfense.server.service.StarterAlienCollectionService starterAlienCollectionService;

    @GetMapping("/info/{username}")
    public ResponseEntity<?> getLobbyInfo(@PathVariable String username) {
        // 1. 유저 조회 (존재하지 않는 유저 처리)
        User user = starterAlienCollectionService.ensureStarterCollection(username);

        // 2. 하트 실시간 계산 (DB 값 변경 안함)
        com.denfense.server.service.HeartSnapshot heartSnapshot = heartPolicy.calculate(user.getHeart(), user.getLastHeartUpdateTime());

        // 3. 전체 왹져 사전(Spec) 가져오기 (alienId 오름차순)
        List<AlienSpec> allSpecs = alienSpecRepository.findAll();
        allSpecs.sort(Comparator.comparing(AlienSpec::getId));

        // 4. 해당 유저의 UserAlien 목록 가져오기 및 N+1 방지용 Map 생성
        List<UserAlien> myAliens = userAlienRepository.findAllByUser(user);
        Map<Long, UserAlien> myAlienMap = myAliens.stream()
                .collect(Collectors.toMap(ua -> ua.getAlienSpec().getId(), Function.identity()));

        // 5. 응답 DTO 조립
        LobbyResponseDto response = new LobbyResponseDto();

        // 유저 정보 매핑
        LobbyResponseDto.UserDto userDto = new LobbyResponseDto.UserDto();
        userDto.setUsername(user.getUsername());
        userDto.setGold(user.getGold());
        userDto.setDiamond(user.getDiamond());
        userDto.setHeart(heartSnapshot.calculatedHeart());
        userDto.setUniversalPiece(user.getUniversalPiece());
        userDto.setGrowthCell(user.getGrowthCell());
        userDto.setMutationCatalyst(user.getMutationCatalyst());
        userDto.setAccountLevel(user.getAccountLevel());
        userDto.setNextHeartRecoveryAt(heartSnapshot.nextHeartRecoveryAt());
        response.setUser(userDto);

        // 유닛 목록 매핑 (Spec + UserAlien 조합)
        List<LobbyResponseDto.AlienInventoryDto> inventoryList = allSpecs.stream()
                .map(spec -> {
                    UserAlien userAlien = myAlienMap.get(spec.getId());
                    int requiredPieces = userAlien != null && userAlien.getLevel() < upgradeCostPolicy.getMaxLevel()
                            ? upgradeCostPolicy.calculate(userAlien.getLevel()).getRequiredPieces()
                            : 0;
                    return LobbyResponseDto.AlienInventoryDto.fromEntity(spec, userAlien, requiredPieces);
                })
                .collect(Collectors.toList());

        response.setAliens(inventoryList);

        return ResponseEntity.ok(response);
    }
}
