package com.denfense.server.dto.response;


import com.denfense.server.game.object.InGameAlien;
import lombok.AllArgsConstructor;
import lombok.Getter;

@Getter
@AllArgsConstructor
public class GameResponseDto {
    private String message;
    private InGameAlien alien;
    private int remainingGold;
    private boolean isGameOver;
}