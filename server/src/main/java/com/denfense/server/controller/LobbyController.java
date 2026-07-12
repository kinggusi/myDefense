package com.denfense.server.controller;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.User;
import com.denfense.server.domain.UserAlien;
import com.denfense.server.dto.response.LobbyResponseDto;
import com.denfense.server.repository.AlienSpecRepository;
import com.denfense.server.repository.UserRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;
import java.util.stream.Collectors;

@RestController
@RequestMapping("/api/lobby")
@RequiredArgsConstructor
public class LobbyController {

    private final UserRepository userRepository;
    private final AlienSpecRepository alienSpecRepository;
    private final com.denfense.server.service.HeartPolicy heartPolicy;

    @GetMapping("/info/{username}")
    public ResponseEntity<?> getLobbyInfo(@PathVariable String username) {
        // 1. 유저 조회
        User user = userRepository.findByUsername(username)
                .orElseThrow(() -> new RuntimeException("유저 없음"));

        // 2. 하트 실시간 계산 (DB 값 변경 안함)
        com.denfense.server.service.HeartSnapshot heartSnapshot = heartPolicy.calculate(user.getHeart(), user.getLastHeartUpdateTime());

        // 3. 전체 왹져 사전(Spec) 가져오기
        List<AlienSpec> allSpecs = alienSpecRepository.findAll();

        // 4. 응답 DTO 조립
        LobbyResponseDto response = new LobbyResponseDto();

        // 유저 정보 매핑
        LobbyResponseDto.UserDto userDto = new LobbyResponseDto.UserDto();
        userDto.setUsername(user.getUsername());
        userDto.setGold(user.getGold());
        userDto.setDiamond(user.getDiamond());
        userDto.setHeart(heartSnapshot.calculatedHeart());
        response.setUser(userDto);

        // 유닛 목록 매핑 (Spec + UserAlien 조합)
        List<LobbyResponseDto.AlienInventoryDto> inventoryList = allSpecs.stream().map(spec -> {
            LobbyResponseDto.AlienInventoryDto dto = new LobbyResponseDto.AlienInventoryDto();
            dto.setId(spec.getId());
            dto.setName(spec.getName());
            dto.setGrade(spec.getGrade().name());

            // 유저가 이 유닛을 보유 중인지 확인
            UserAlien myData = user.getUserAliens().stream()
                    .filter(ua -> ua.getAlienSpec().getId().equals(spec.getId()))
                    .findFirst()
                    .orElse(null);

            if (myData != null) {
                dto.setLevel(myData.getLevel());
                dto.setPieces(myData.getPieces());
                dto.setLocked(false);
            } else {
                dto.setLevel(1);
                dto.setPieces(0);
                dto.setLocked(spec.isLocked()); // 기본 잠금 설정 따름
            }
            return dto;
        }).collect(Collectors.toList());

        response.setAliens(inventoryList);

        return ResponseEntity.ok(response);

    }

}
