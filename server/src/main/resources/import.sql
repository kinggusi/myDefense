-- 서버 켜질 때 자동으로 들어갈 데이터
INSERT INTO users (username, gold, diamond) VALUES ('MyDev', 5000, 100);
INSERT INTO users (username, gold, diamond) VALUES ('TeamDev', 0, 0);

-- [NEW] 1. 아군 왹져 데이터 (AlienSpec) 2026.01.21
-- 1번: 잡졸 왹져 (공속 빠름, 사거리 짧음)
INSERT INTO alien_specs (name, base_atk, base_hp, atk_speed, range) VALUES ('1번 왹져', 10, 100, 1.5, 3.0);
-- 2번: 저격 왹져 (공속 느림, 사거리 김, 한방 셈)
INSERT INTO alien_specs (name, base_atk, base_hp, atk_speed, range) VALUES ('2번 왹져', 50, 80, 0.5, 10.0);

-- [NEW] 2. 적군 몬스터 데이터 (MonsterSpec) 2026.01.21
-- 1번: 빠른 놈 (체력 낮음)
INSERT INTO monster_specs (name, hp, move_speed, drop_gold) VALUES ('적1', 50, 5.0, 10);
-- 2번: 튼튼한 놈 (느림)
INSERT INTO monster_specs (name, hp, move_speed, drop_gold) VALUES ('적2', 300, 2.0, 50);