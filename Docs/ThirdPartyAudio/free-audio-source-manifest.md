# 무료 오디오 원본 목록

프로젝트에 사용할 후보 음원을 내려받아 `ExternalAssets/Audio`에 보관한다. 이 폴더는 Unity가 원본 전체를 임포트하지 않도록 Git과 `Assets`에서 제외한다. 실제 게임에 사용하는 클립만 선별하여 `Assets/JoseonHunter/Audio`로 복사하고 해당 출처를 유지한다.

## Kenney — UI Audio

- 원본: https://www.kenney.nl/assets/ui-audio
- 파일: `ExternalAssets/Audio/Kenney/kenney_ui-audio.zip`
- SHA-256: `946FC23A63D535D693EB31B2EABB80C8C28D6351E2186B344CEB71B2CB1D5EB6`
- 내용: 버튼, 선택, 전환 효과음
- 라이선스: Creative Commons Zero 1.0 (CC0)
- 상업적 사용: 허용
- 저작자 표시: 선택 사항

## Kenney — RPG Audio

- 원본: https://www.kenney.nl/assets/rpg-audio
- 파일: `ExternalAssets/Audio/Kenney/kenney_rpg-audio.zip`
- SHA-256: `6DBEAF8544DA958D8F2ADCB4A4A4B76C1ADE34A05F8AB9EDCCD327DA7375F38B`
- 내용: 동전, 칼날, 천, 책, 장비와 발걸음 효과음
- 라이선스: Creative Commons Zero 1.0 (CC0)
- 상업적 사용: 허용
- 저작자 표시: 선택 사항

## Kenney — Impact Sounds

- 원본: https://www.kenney.nl/assets/impact-sounds
- 파일: `ExternalAssets/Audio/Kenney/kenney_impact-sounds.zip`
- SHA-256: `029D734AF1582474EDF3A694D1B0CEBC97C1C152F2F39FA34D4C2BAFC5DE77F8`
- 내용: 가벼운·무거운 타격, 금속, 목재, 유리, 종과 지면별 발걸음 효과음
- 라이선스: Creative Commons Zero 1.0 (CC0)
- 상업적 사용: 허용
- 저작자 표시: 선택 사항

## OpenGameArt — 80 CC0 RPG SFX

- 원본: https://opengameart.org/content/80-cc0-rpg-sfx
- 파일: `ExternalAssets/Audio/OpenGameArt/80-CC0-RPG-SFX.zip`
- SHA-256: `1C2F06FF4E8563B5B8B745B23CF213C1474142A69BB82BD8F5E10D9B3F7A7BBD`
- 내용: 칼날, 괴물 피격·사망, 동전·보석, 불·일반 주문 효과음
- 라이선스: Creative Commons Zero 1.0 (CC0)
- 상업적 사용: 허용
- 저작자 표시: 선택 사항

## OpenGameArt — Battle Sound Effects

- 원본: https://opengameart.org/content/battle-sound-effects
- 파일: `ExternalAssets/Audio/OpenGameArt/battle_sound_effects_0.zip`
- SHA-256: `44E3D26B2378D2EB3A4F28B4C5CBC71908AD13C7389038348DBE9B8CDE4F4C05`
- 내용: 활 발사음 1개와 무기 휘두름음 3개
- 라이선스: Creative Commons Zero 1.0 (CC0) 조건으로 사용
- 상업적 사용: 허용
- 저작자 표시: 선택 사항

## 선별 원칙

- 동일한 역할의 소리를 전부 Unity에 넣지 않고 2~5개 변형만 사용한다.
- UI, 획득, 무기, 피격, 보스 경고 그룹으로 분류한다.
- 모바일 동시 재생량과 빌드 용량을 줄이기 위해 짧은 효과음은 Vorbis 압축 및 메모리 로드 정책을 별도로 적용한다.
- 배경 음악은 조선 민속 판타지 분위기와 맞는 무료 음원을 아직 찾지 못했으므로 이번 원본 묶음에 포함하지 않는다.

## 실제 Unity 편입 목록

아래 32개만 `Assets/JoseonHunter/Resources/Audio/CC0`에 편입했다. 동일 파일을 여러 게임 큐에서 재사용하므로 UI 취소, 부적처럼 전용 복사본이 필요 없는 역할은 별도 에셋을 만들지 않았다. 일반 몬스터 사망음은 다수 동시 재생으로 소리가 뭉개지는 것을 막기 위해 의도적으로 편입·재생하지 않는다.

| Unity 파일 | 원본 파일 | 용도 |
|---|---|---|
| `UI/ui_click.ogg` | Kenney UI `click1.ogg` | 일반 클릭·취소 |
| `UI/ui_confirm.ogg` | Kenney UI `click3.ogg` | 확인·출전·증강 확정 |
| `Pickups/experience.ogg` | 80 RPG `item_gem_01.ogg` | 경험치 획득 |
| `Pickups/yeopjeon_1.ogg` | 80 RPG `item_coins_01.ogg` | 엽전 획득 변형 1 |
| `Pickups/yeopjeon_2.ogg` | 80 RPG `item_coins_02.ogg` | 엽전 획득 변형 2 |
| `Pickups/magnet.ogg` | 80 RPG `spell_01.ogg` | 자석·주술 부적 |
| `Pickups/level_up.ogg` | 80 RPG `item_gem_04.ogg` | 레벨업·승전 |
| `Weapons/gakgung.wav` | Battle SFX `Bow.wav` | 각궁 |
| `Weapons/hwando.wav` | Battle SFX `swish_2.wav` | 환도 비검 |
| `Weapons/thunder_bomb.ogg` | 80 RPG `spell_fire_03.ogg` | 벽력탄 |
| `Weapons/frost_flask.ogg` | 80 RPG `spell_02.ogg` | 서리병 |
| `Weapons/wind_fan.wav` | Battle SFX `swish_3.wav` | 풍뢰선 |
| `Weapons/jangseung.ogg` | Kenney Impact `impactWood_heavy_000.ogg` | 장승진 |
| `Weapons/geumjul.ogg` | Kenney Impact `impactBell_heavy_000.ogg` | 금줄·우두머리 경고 |
| `Weapons/singijeon.ogg` | 80 RPG `spell_fire_01.ogg` | 신기전 |
| `Combat/hit_soft_1.ogg` | Kenney Impact `impactSoft_medium_000.ogg` | 일반 피격 |
| `Combat/hit_critical.ogg` | Kenney Impact `impactPunch_heavy_000.ogg` | 치명타 |
| `Combat/boss_defeat.ogg` | Kenney Impact `impactMetal_heavy_000.ogg` | 우두머리 격파 |
| `Combat/player_hurt_1.ogg` | Kenney Impact `impactSoft_medium_001.ogg` | 플레이어 피격 변형 1 |
| `Combat/player_hurt_2.ogg` | Kenney Impact `impactSoft_medium_002.ogg` | 플레이어 피격 변형 2 |
| `Combat/player_defeat.ogg` | 80 RPG `creature_hurt_02.ogg` | 플레이어 패배 |
| `Combat/elite_defeat.ogg` | 80 RPG `creature_die_01.ogg` | 정예 몬스터 격파 |
| `Combat/boss_slam.ogg` | Kenney Impact `impactMetal_heavy_003.ogg` | 우두머리 내려찍기 |
| `Combat/boss_charge.wav` | Battle SFX `swish_4.wav` | 우두머리 돌진 |
| `Combat/boss_volley.ogg` | 80 RPG `spell_fire_07.ogg` | 우두머리 투사체 연사 |
| `Events/wave_warning.ogg` | Kenney Impact `impactBell_heavy_001.ogg` | 웨이브 경고 |
| `Events/elite_appear.ogg` | 80 RPG `creature_roar_01.ogg` | 정예 몬스터 출현 |
| `Events/treasure_appear.ogg` | Kenney Impact `impactWood_medium_004.ogg` | 보물상자 출현 |
| `Events/treasure_open.ogg` | Kenney RPG `handleCoins.ogg` | 보물상자 개봉 |
| `UI/pause_open.ogg` | Kenney UI `switch2.ogg` | 일시정지 메뉴 열기 |
| `UI/appraisal_tick.ogg` | Kenney UI `switch7.ogg` | 추가옵션 수치 상승 |
| `UI/appraisal_reveal.ogg` | 80 RPG `item_gem_03.ogg` | 추가옵션 등급 공개 |

## 검토했지만 편입하지 않은 후보

- Unity Asset Store `FREE Casual Game SFX Pack`(페이지 ID 54116)은 CC0 표기를 확인했지만, 에셋 스토어 페이지의 GraphQL 오류로 계정 라이브러리 추가가 완료되지 않아 프로젝트에는 편입하지 않았다.
- 현재 빌드는 위 Kenney·OpenGameArt CC0 원본만 사용한다.

## 배경음악 편입 목록

2026-08-07에 OpenGameArt 원본 페이지에서 CC0 표기를 직접 확인하고 아래 6곡을 편입했다. WAV 원본 세 곡은 메타데이터를 제거하고 스테레오·원본 샘플레이트를 유지한 채 OGG Vorbis 품질 5로 변환했다. OGG 원본 세 곡은 재압축하지 않았다.

| Unity 파일 | 원본·제작자 | 용도 | 원본 SHA-256 |
|---|---|---|---|
| `Audio/Music/CC0/lobby_yoiyami.ogg` | `yoiyami_core_theme.wav` · Yoiyami | 로비 | `613D462F5229568AD98DCBE870036CCDF858F5AE33C63386CACE86548809CB60` |
| `Audio/Music/CC0/gwigok_early_asianoriental.ogg` | `asianoriental1.ogg` · Tozan | 귀곡 들판 0~5분 | `172D95262348D020D7D1428046B100AEFBAD0A6CBAA93EF87EFAFEB8ADD107D0` |
| `Audio/Music/CC0/gwigok_mid_frozen_desert.ogg` | `Frozen Desert.ogg` · Dizzy Crow | 귀곡 들판 5~10분 | `62DDC8D2A52A94FA42DDAAB48A01157F9B6B2A84A726C3090BE222C9D944B949` |
| `Audio/Music/CC0/gwigok_late_hope.ogg` | `hope_orchestral_battle_music_bpm165.ogg` · MintoDog | 귀곡 들판 10~15분 | `1615903236286AF59D14B4D71DA3FC2518A3091ECDD8F162A50215B9B3D0F320` |
| `Audio/Music/CC0/midboss_determined_pursuit.ogg` | `determined_pursuit_loop.wav` · Emma_MA | 중간보스 | `E4F3BE098B50213B56A60AEFE60FFFAE79CD9F7C9008088F93C187EF1BBC856B` |
| `Audio/Music/CC0/finalboss_epic_battle.ogg` | `Juhani Junkala - Epic Boss Battle [Seamlessly Looping].wav` · SubspaceAudio/Juhani Junkala | 최종보스 | `35F75B4381DFBB053992876F7DFC567D9FD959D61B73954D5B1CD519753E7DF1` |
| `Audio/Music/CC0/dokkaebi_pass_oriented.ogg` | `Oriented.ogg` · yd | 도깨비 고갯길 | `D088A0E4D768ADEDD4228B4C5C78A2018972721AA03DEEE3A5B6ECB866E2D3A2` |
| `Audio/Music/CC0/moonlit_tomb_creepy_loop.ogg` | `creepyloop-v2.ogg` · epb9000 | 월식 고분 | `609BD62E824D77DF3D35AF26064BFD824CB6FCE95CB9637C467FB80B717D6050` |

원본 페이지:

- https://opengameart.org/content/yoiyami-core-theme-%E2%80%93-deep-blue-ambient-piano
- https://opengameart.org/content/asianoriental1
- https://opengameart.org/content/frozen-desert-112
- https://opengameart.org/content/hopeorchestral-battle-music
- https://opengameart.org/content/determined-pursuit-epic-orchestra-loop
- https://opengameart.org/content/boss-battle-music
- https://opengameart.org/content/oriented
- https://opengameart.org/content/creepy-ambient-loop
