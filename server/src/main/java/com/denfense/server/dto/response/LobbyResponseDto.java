package com.denfense.server.dto.response;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.UserAlien;
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
        private int universalPiece;
        private int growthCell;
        private java.time.LocalDateTime nextHeartRecoveryAt;
    }

    @Data
    public static class AlienInventoryDto {
        private Long id;
        private String name;
        private String description;
        private String grade;
        private int level;
        private int pieces;
        private int requiredPieces;
        private boolean locked; // 레거시 호환: 미보유 여부만 나타낸다.

        // 신규 추가 필드
        private boolean owned;
        private int baseAtk;
        private int baseMp;
        private double atkSpeed;
        private double range;
        private Long evolutionTargetId;
        private boolean specLocked;

        public static AlienInventoryDto fromEntity(AlienSpec spec, UserAlien userAlien, int requiredPieces) {
            AlienInventoryDto dto = new AlienInventoryDto();
            dto.setId(spec.getId());
            dto.setName(spec.getName());
            dto.setDescription(spec.getDescription());
            dto.setGrade(spec.getGrade() != null ? spec.getGrade().name() : "NORMAL");

            dto.setBaseAtk(spec.getBaseAtk());
            dto.setBaseMp(spec.getBaseMp());
            dto.setAtkSpeed(spec.getAtkSpeed());
            dto.setRange(spec.getRange());
            dto.setEvolutionTargetId(spec.getEvolutionTargetId());
            dto.setSpecLocked(spec.isLocked());

            if (userAlien != null) {
                dto.setOwned(true);
                dto.setLevel(userAlien.getLevel());
                dto.setPieces(userAlien.getPieces());
                dto.setRequiredPieces(requiredPieces);
                dto.setLocked(false);
            } else {
                dto.setOwned(false);
                dto.setLevel(0); // 기존 1에서 0으로 정책 변경 (미보유 상태 명확화)
                dto.setPieces(0);
                dto.setRequiredPieces(0);
                dto.setLocked(true);
            }

            return dto;
        }
    }
}
