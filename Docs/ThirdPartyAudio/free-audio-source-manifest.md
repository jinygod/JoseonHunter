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
