package com.denfense.server.game.object;

import com.denfense.server.domain.BoardObjectType;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public abstract class BoardObject {
    protected Long id;
    protected int gridX;
    protected int gridY;

    public BoardObject(Long id, int gridX, int gridY) {
        this.id = id;
        this.gridX = gridX;
        this.gridY = gridY;
    }

    public abstract BoardObjectType getObjectType();
}
