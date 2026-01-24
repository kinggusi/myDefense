package com.denfense.server.dto.request;

import lombok.Getter;
import lombok.NoArgsConstructor;

@Getter
@NoArgsConstructor
public class MergeRequestDto {
    private Long userId;    // 누가
    private Long sourceId;  // 이걸 집어서
    private Long targetId;  // 여기에 놓았다
}