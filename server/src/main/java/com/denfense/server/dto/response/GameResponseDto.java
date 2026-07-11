package com.denfense.server.dto.response;


import com.denfense.server.game.object.BoardObject;
import lombok.AllArgsConstructor;
import lombok.Getter;

@Getter
@AllArgsConstructor
public class GameResponseDto {
    private String message;
    private BoardObject alien;
    private int remainingGold;
    private boolean isGameOver;
}