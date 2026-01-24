package com.denfense.server.dto.response;

import com.denfense.server.domain.UserAlien;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Getter
@NoArgsConstructor
public class UserAlienResponseDto {

    private long userAlienId;
    private String alienName;
    private String description;
    private int level;
    private int pieces;

    // Entity -> DTO 변환 생성자
    public UserAlienResponseDto(UserAlien entity) {

        this.userAlienId = entity.getId();
        // [핵심] 연결된 Spec에서 정보 쏙 빼오기
        this.alienName = entity.getAlienSpec().getName();
        this.description = entity.getAlienSpec().getDescription();

        this.level = entity.getLevel();
        this.pieces = entity.getPieces();

    }
}
