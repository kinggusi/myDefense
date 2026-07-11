package com.denfense.server.dto.response;

import com.denfense.server.domain.AlienSpec;
import com.denfense.server.domain.BoardObjectType;
import com.denfense.server.domain.MutationType;
import lombok.Builder;
import lombok.Getter;

@Getter
@Builder
public class BoardObjectStateDto {
    private Long id;
    private BoardObjectType objectType; // ALIEN or MUTATION_INJECTOR
    private int gridX;
    private int gridY;

    // Alien fields
    private AlienSpec alienSpec;
    private String grade; // Alien 등급 노출
    private MutationType pendingMutationType;
    private MutationType activeMutationType;
    private int mutationRerollCount; // 원시타입 int 로 고정하여 JSON 호환 보장

    // Injector fields
    private MutationType mutationType;
}
