package com.denfense.server.dto.response;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.AllArgsConstructor;
import lombok.Getter;
import java.util.List;

@Getter
@AllArgsConstructor
public class GameSessionStateDto {
    private Long userId;
    private int remainingGold;

    @JsonProperty("isGameOver")
    private boolean isGameOver; // 정확한 JSON 키 'isGameOver' 바인딩 보장

    private List<BoardObjectStateDto> boardObjects;
}
