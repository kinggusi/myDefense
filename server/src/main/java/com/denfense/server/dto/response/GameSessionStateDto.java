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
    private boolean isGameOver;

    private List<BoardObjectStateDto> boardObjects;
    private int currentKidnapCost;
}
