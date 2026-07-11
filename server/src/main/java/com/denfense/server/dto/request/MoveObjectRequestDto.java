package com.denfense.server.dto.request;

import lombok.Data;

@Data
public class MoveObjectRequestDto {
    private Long userId;
    private Long objectId;
    private int newX;
    private int newY;
}
