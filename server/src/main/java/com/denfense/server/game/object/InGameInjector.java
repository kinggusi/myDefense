package com.denfense.server.game.object;

import com.denfense.server.domain.BoardObjectType;
import com.denfense.server.domain.MutationType;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class InGameInjector extends BoardObject {
    private MutationType mutationType; // 이 인젝터가 나타내는 생체변이 DNA 종류

    public InGameInjector(Long id, MutationType mutationType, int gridX, int gridY) {
        super(id, gridX, gridY);
        if (mutationType == null || mutationType == MutationType.NONE) {
            throw new IllegalArgumentException("인젝터는 유효한 MutationType을 가져야 합니다.");
        }
        this.mutationType = mutationType;
    }

    @Override
    public BoardObjectType getObjectType() {
        return BoardObjectType.MUTATION_INJECTOR;
    }
}
