package com.denfense.server.dto.request;

import jakarta.validation.constraints.NotBlank;
import lombok.Data;

@Data
public class GameFinishRequestDto {
    @NotBlank(message = "username은 필수입니다.")
    private String username;

    @NotBlank(message = "sessionId는 필수입니다.")
    private String sessionId;
}
