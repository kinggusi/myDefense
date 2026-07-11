package com.denfense.server.dto.response;

import com.denfense.server.domain.MutationType;
import lombok.AllArgsConstructor;
import lombok.Getter;

@Getter
@AllArgsConstructor
public class UseInjectorResponseDto {
    private Long alienId;
    private MutationType pendingMutationType;
    private MutationType activeMutationType;
    private Long consumedInjectorId;
    private int gridX;
    private int gridY;
}
