package com.denfense.server.dto.response;

import com.denfense.server.domain.AlienSpec;
import lombok.Data;
import java.util.List;

@Data
public class LobbyResponseDto {
    private UserDto user;
    private List<AlienInventoryDto> aliens;

    @Data
    public static class UserDto {
        private String username;
        private int gold;
        private int diamond;
        private int heart;
    }

    @Data
    public static class AlienInventoryDto {
        private Long id;
        private String name;
        private String grade;
        private int level;
        private int pieces;
        private int requiredPieces;
        private boolean locked; // 프론트에서 쓸 잠금 여부

        public static AlienInventoryDto fromEntity(AlienSpec spec, int currentLevel, int currentPieces) {
            AlienInventoryDto dto = new AlienInventoryDto();
            dto.setId(spec.getId());
            dto.setName(spec.getName());

            // Enum 처리 (grade가 null일 경우를 대비해 안전하게 처리)
            dto.setGrade(spec.getGrade() != null ? spec.getGrade().name() : "NORMAL");

            dto.setLevel(currentLevel);
            dto.setPieces(currentPieces);

            dto.setRequiredPieces(currentLevel * 10);

            dto.setLocked(spec.isLocked());
            return dto;
        }
    }
}