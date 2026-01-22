package com.denfense.server.dto.response;

import com.denfense.server.domain.UserAlien;
import lombok.Getter;
import lombok.NoArgsConstructor;

@Getter
@NoArgsConstructor
public class UserAlienResponseDto {

    private int alienId;
    private int level;
    private int pieces;

    // Entity -> DTO 변환 생성자
    public UserAlienResponseDto(UserAlien entity) {
        this.alienId = entity.getAlienId();
        this.level = entity.getLevel();
        this.pieces = entity.getPieces();

    }
}
