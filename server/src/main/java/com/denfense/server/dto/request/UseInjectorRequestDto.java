package com.denfense.server.dto.request;

import lombok.Data;

@Data
public class UseInjectorRequestDto {
    private Long userId;
    private Long injectorId;
    private Long alienId;
}
