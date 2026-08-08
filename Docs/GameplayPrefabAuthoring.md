# 게임플레이 Prefab 꾸미기 안내

이 문서는 Unity를 처음 사용하는 사람도 플레이어, 몬스터, 체력바, 경험치·엽전·자석의 **모양과 배치만 안전하게 수정**할 수 있도록 만든 안내서입니다.

실제 `Gameplay` 씬은 게임을 시작할 때 전투 오브젝트를 생성합니다. 그래서 Scene 탭에서 플레이어와 몬스터가 미리 보이지 않는 것이 정상입니다. 외형을 편집할 때는 실제 게임 로직이 들어 있는 `Gameplay` 씬 대신, 아래의 **Visual Preview** 또는 각 Prefab을 사용합니다.

## 가장 먼저 할 일

Unity 위쪽 메뉴에서 다음 항목을 한 번 실행합니다.

`JoseonHunter > Gameplay Editing > Create or Validate Visual Prefabs`

이 메뉴는 빠진 Prefab과 연결 정보를 만들거나 검사합니다. 이미 정상적으로 꾸며 놓은 Prefab은 새것으로 덮어쓰지 않습니다.

그다음 아래 메뉴를 선택합니다.

`JoseonHunter > Gameplay Editing > Open Visual Preview`

그러면 플레이어, 일반 몬스터, 큰 몬스터, 체력·보호막 바, 경험치·엽전·자석을 한 화면에서 비교할 수 있는 편집용 Preview 씬이 열립니다.

Preview 씬의 실제 경로는 다음과 같습니다.

`Assets/JoseonHunter/Scenes/GameplayVisualPreview.unity`

이 씬은 외형을 확인하기 위한 작업실이며 실제 게임 빌드에는 포함되지 않습니다.

Production Prefab을 수정했는데 Preview에 이전 배치나 Override가 남아 있다면 다음 메뉴로 Preview만 다시 만들 수 있습니다.

`JoseonHunter > Gameplay Editing > Rebuild Visual Preview From Production Prefabs`

이 메뉴는 Preview 씬과 `GameplayAuthoringPreview.prefab`의 구성·Override만 Production Prefab 기준으로 새로 만듭니다. `PlayerVisual.prefab`, `EnemyVisual.prefab`, 체력·보호막 바, 경험치·엽전·자석 같은 **실제 게임용 Production Prefab은 덮어쓰거나 되돌리지 않습니다.** 따라서 Preview 배치가 꼬였을 때만 사용하면 됩니다.

## Prefab을 여는 방법

Project 창에서 다음 폴더를 펼칩니다.

`Assets/JoseonHunter/Prefabs/Gameplay`

편집하려는 `.prefab` 파일을 더블 클릭하면 **Prefab Mode**가 열립니다. Scene 창 위쪽에 Prefab 이름과 뒤로 가기 화살표가 보이면 Prefab Mode에 들어온 것입니다.

주요 Prefab은 다음과 같습니다.

- `PlayerVisual.prefab`: 플레이어 외형과 플레이어 체력바 기준 위치
- `EnemyVisual.prefab`: 일반·정예·중간 보스·보스가 공통으로 사용하는 기본 외형
- `WorldHealthBar.prefab`: 월드 체력바의 배경과 채움
- `WorldShieldBar.prefab`: 월드 보호막 바의 배경과 채움
- `ExperiencePickup.prefab`: 경험치 획득물
- `YeopjeonPickup.prefab`: 엽전 획득물
- `MagnetPickup.prefab`: 자석 획득물
- `GameplayAuthoringPreview.prefab`: Preview 씬에서 여러 항목을 함께 보여 주는 구성용 Prefab

## 저장과 Apply의 차이

### Prefab Mode에서 수정했을 때

Prefab 파일 자체를 편집하고 있으므로 `Ctrl+S`만 누르면 저장됩니다. **Apply는 필요 없습니다.**

가장 권장하는 작업 방식입니다.

1. Project 창에서 Prefab을 더블 클릭합니다.
2. 위치나 크기를 수정합니다.
3. `Ctrl+S`로 저장합니다.
4. Prefab Mode의 뒤로 가기 화살표로 나옵니다.
5. Visual Preview 또는 Play Mode에서 확인합니다.

### Preview 씬의 Prefab 인스턴스를 수정했을 때

Scene 안의 인스턴스를 직접 수정하면 그 변경은 **Override**가 됩니다. 이 상태에서는 Preview 씬에만 변화가 남고 실제 생성되는 게임 오브젝트에는 적용되지 않을 수 있습니다.

실제 Prefab에도 반영하려면 Inspector 위쪽의 `Overrides`를 열고 `Apply All`을 눌러야 합니다. 다만 실수를 줄이려면 Preview에서는 비교만 하고, 수정은 해당 Prefab을 더블 클릭해서 Prefab Mode에서 하는 편이 안전합니다.

### Play Mode에서 수정했을 때

상단 재생 버튼이 파란색인 Play Mode에서 Inspector 값을 바꾸면 게임을 멈추는 순간 대부분 원래 값으로 돌아갑니다. Play Mode에서는 값을 시험만 하고, 마음에 드는 값을 적어 둔 뒤 재생을 멈추고 Prefab Mode에서 다시 입력해야 합니다.

## 플레이어 외형 수정

`PlayerVisual.prefab`의 기본 구조는 다음과 같습니다.

```text
PlayerVisual
├─ Soft Shadow
├─ Silhouette Outline
├─ Player Aura
├─ Visual Pivot
└─ HealthBarAnchor
```

주로 수정해도 되는 항목은 다음과 같습니다.

- `Soft Shadow`의 위치와 크기: 발밑 그림자 정리
- `Silhouette Outline`의 위치와 크기: 외곽선 정렬
- `Player Aura`의 위치와 크기: 플레이어 표시 범위 조정
- `HealthBarAnchor`의 Local Position: 플레이어 체력바 위치 조정

다음 항목은 수정할 수 있지만 **소폭만 변경하고 반드시 Play Mode에서 충돌을 확인**해야 합니다.

- `Visual Pivot`의 Local Position: 캐릭터 그림을 위·아래·좌우로 미세 조정
- `Visual Pivot` 또는 Body SpriteRenderer가 있는 Transform의 Scale: 캐릭터 그림의 기본 크기 조정

이 프로젝트의 픽셀 충돌은 화면에 보이는 Sprite와 Transform 계산을 함께 사용합니다. `Visual Pivot`이나 Body의 위치·크기를 크게 바꾸면 보이는 그림뿐 아니라 공격 또는 피격 판정의 기준 계산에도 영향을 줄 수 있습니다. 한 번에 크게 바꾸지 말고 작은 값으로 조정한 뒤 실제 전투에서 몬스터와 부딪히고 공격을 맞히는 장면까지 확인하세요. 외형만 안전하게 다듬고 싶다면 먼저 그림자, 외곽선, 오라, 체력바 Anchor부터 조정하는 편이 좋습니다.

플레이 중 사용할 캐릭터 Sprite, 바라보는 방향, 정렬 순서, 피격·사망·이동 연출, 역할별 크기는 런타임에서 넣습니다. Prefab의 Sprite 칸을 바꾸더라도 실제 전투에서는 선택된 캐릭터 Sprite로 교체될 수 있습니다.

루트 `PlayerVisual`의 이름이나 아래의 필수 자식 이름은 바꾸지 마세요. 자식을 복제하여 같은 역할의 Renderer를 두 개 만들면 캐릭터가 겹쳐 보이거나 외곽선이 두꺼워질 수 있습니다.

## 몬스터 외형 수정

`EnemyVisual.prefab`은 여러 몬스터가 함께 사용하는 기본 틀입니다.

```text
EnemyVisual
├─ Soft Shadow
├─ Silhouette Outline
├─ Visual Pivot
├─ HealthBarAnchor
└─ ShieldBarAnchor
```

조정 항목의 구분은 플레이어와 같습니다. 그림자, 외곽선, 체력바·보호막 바의 기준 위치는 외형용으로 조정할 수 있고, `Visual Pivot`과 Body는 아래 주의사항에 따라 다뤄야 합니다.

몬스터 종류별 Sprite, 일반·정예·중간 보스·보스 구분, 보스의 큰 크기, 색상, 체력, 보호막, 이동과 공격은 런타임 데이터입니다. `EnemyVisual.prefab`의 루트 크기는 역할별 크기 보정과 겹치므로 바꾸지 마세요. 보이는 그림의 기본 크기를 꼭 바꿔야 한다면 `Visual Pivot` 또는 Body 쪽에서 소폭 조정하고 아래 확인 절차를 따릅니다.

다만 몬스터도 `Visual Pivot`과 Body의 Transform이 픽셀 충돌 계산에 영향을 줄 수 있습니다. 이 두 Transform의 위치나 Scale은 안전한 장식 조정 항목이 아니므로 작은 폭으로만 바꾸고, Play Mode에서 일반 몬스터와 큰 몬스터 모두의 공격·피격 판정을 확인하세요.

## 체력바와 보호막 위치·크기 수정

캐릭터에 대한 바의 위치는 캐릭터 Prefab의 Anchor로 조정합니다.

- 플레이어 체력바 위치: `PlayerVisual.prefab > HealthBarAnchor`
- 몬스터 체력바 위치: `EnemyVisual.prefab > HealthBarAnchor`
- 몬스터 보호막 바 위치: `EnemyVisual.prefab > ShieldBarAnchor`

Anchor의 `Local Position Y`를 올리면 바가 위로, 내리면 아래로 이동합니다. 먼저 작은 값으로 조정하고 Preview에서 일반 몬스터와 큰 몬스터를 함께 확인하세요.

Production Prefab의 Anchor를 Prefab Mode에서 저장하면 Override가 없는 Preview 인스턴스에는 변경 위치가 자동으로 반영됩니다. Preview가 열려 있다면 씬을 다시 선택하거나 Prefab Mode에서 빠져나온 뒤 위치를 확인하세요. 그래도 이전 위치가 남아 있다면 Preview 인스턴스에 Override가 생긴 것이므로 `Rebuild Visual Preview From Production Prefabs` 메뉴를 실행하면 Production Prefab은 보존한 채 Preview 배치만 최신 Anchor 위치로 되돌릴 수 있습니다.

바 자체의 두께와 너비는 아래 Prefab에서 수정합니다.

- 체력바: `WorldHealthBar.prefab`
- 보호막 바: `WorldShieldBar.prefab`

두 Prefab에는 `Background`와 `Fill`이 있습니다. `Fill`이 체력 100%인 상태라고 생각하고 전체 너비, 높이, 위치를 맞추면 됩니다. 게임 중에는 현재 체력 비율에 따라 `Fill`의 가로 길이만 줄어듭니다.

다음 항목은 유지해야 합니다.

- `WorldBarView` 컴포넌트
- `Background`와 `Fill` 참조
- `Fill`이 가로로 줄어드는 방향을 결정하는 Transform 구조

`Fill`을 삭제하고 새 오브젝트로 교체했다면 반드시 `WorldBarView`의 Fill 참조에 새 Transform을 다시 연결해야 합니다.

## 경험치·엽전·자석 크기 수정

각 획득물 Prefab을 열고 `PickupVisualView`의 **Base Scale**을 조정합니다.

- 경험치: `ExperiencePickup.prefab`
- 엽전: `YeopjeonPickup.prefab`
- 자석: `MagnetPickup.prefab`

작게 느껴진다면 Base Scale을 조금씩 올린 뒤 Preview와 실제 게임에서 확인합니다. 루트 Transform을 크게 바꾸기보다 Base Scale을 사용하는 것이 런타임의 등급별 크기와 빨려 들어오는 연출을 유지하기 쉽습니다.

게임 중에는 획득물 종류, Sprite, 등급 색상, 경험치 값, 자석에 끌리는 상태가 런타임에서 들어갑니다. 획득 범위와 획득량은 Prefab 크기와 별개이므로 이 문서의 작업으로 바꾸지 않습니다.

`ExperiencePickup.prefab` 루트의 `TrailRenderer`는 경험치가 캐릭터에게 빨려 들어오는 꼬리 연출에 사용됩니다. 삭제하거나 다른 자식으로 옮기지 마세요.

## 절대 삭제하거나 연결을 끊지 말아야 할 것

Prefab을 꾸밀 때 아래 컴포넌트와 참조는 게임 코드와 이어지는 연결점입니다.

- `CombatantVisualView`
  - Visual Pivot
  - Body SpriteRenderer
  - Soft Shadow
  - Silhouette Outline
  - Player Aura(플레이어만)
  - HealthBarAnchor
  - ShieldBarAnchor(몬스터만)
- `WorldBarView`
  - Background
  - Fill
- `PickupVisualView`
  - Visual SpriteRenderer
  - TrailRenderer(경험치)
- `GameplayVisualPrefabLibrary`
  - 일곱 개 생산용 Prefab 참조

다음 스크립트는 외형을 꾸미기 위한 파일이 아니므로 직접 수정하거나 Prefab에서 임의로 떼지 마세요.

- `FirstPlayableController.cs`: 전투 생성과 전체 게임 진행을 연결
- `CombatantVisualRig.cs`: Sprite, 방향, 피격, 사망, 이동 연출을 Prefab에 주입
- `GameplayVisualPrefabLibrary.cs`: 실제 게임이 사용할 Prefab 묶음을 제공
- `CombatantVisualView.cs`: 플레이어·몬스터 자식 참조를 보관
- `WorldBarView.cs`: 체력·보호막 비율 표시를 담당
- `PickupVisualView.cs`: 획득물 Sprite와 기본 크기 연결

스크립트 파일은 다음 폴더에 있습니다.

`Assets/JoseonHunter/Scripts/Runtime/Gameplay`

Prefab에 `Missing (Mono Script)`가 보이거나 위 참조 칸이 `None`이 되었다면 그대로 저장하지 말고 Undo(`Ctrl+Z`) 하세요. 이미 저장했다면 아래 메뉴를 다시 실행하여 무엇이 빠졌는지 검사합니다.

`JoseonHunter > Gameplay Editing > Create or Validate Visual Prefabs`

검사 결과가 계속 잘못되었다고 나오면 Prefab을 더 수정하기 전에 개발 코드 담당자에게 연결 복구를 요청하는 것이 안전합니다.

## Sprite 이미지를 교체할 때

플레이어와 몬스터의 본체 Sprite는 런타임에서 종류에 맞게 바뀝니다. 단순히 `Visual Pivot`의 Sprite Renderer에 이미지를 끌어 넣는 것만으로 모든 몬스터 그림이 교체되지는 않습니다.

Prefab에서 직접 교체하기 좋은 것은 고정 장식에 해당하는 그림자, 외곽선용 기본 형태, 오라처럼 모든 인스턴스가 공유하는 요소입니다. 캐릭터·몬스터 종류별 본체 그림을 바꾸려면 해당 Sprite 목록 또는 모션 라이브러리를 수정해야 하므로 이 문서의 안전한 외형 작업 범위 밖입니다.

경험치·엽전·자석 Sprite도 런타임에서 종류에 맞게 주입됩니다. 크기와 기준 위치는 Prefab에서 조정하되, 실제 Sprite 교체는 연결된 런타임 카탈로그까지 함께 확인해야 합니다.

## 빠른 확인 순서

수정할 때마다 다음 순서로 확인하면 실수를 찾기 쉽습니다.

1. Prefab Mode에서 위치나 크기만 조금 수정합니다.
2. `Ctrl+S`로 저장합니다.
3. `JoseonHunter > Gameplay Editing > Open Visual Preview`를 실행합니다.
4. 플레이어, 일반 몬스터, 큰 몬스터에서 위치가 모두 자연스러운지 봅니다.
5. Console에 빨간 오류나 `Missing` 경고가 없는지 확인합니다.
6. 실제 `Gameplay` 씬을 Play하여 이동, 피격, 사망, 체력바 감소, 획득물 흡수를 확인합니다.
7. Play Mode를 끈 뒤 Prefab 값이 그대로 저장되어 있는지 확인합니다.

## 문제가 생겼을 때

- **Scene 탭에 플레이어가 없다:** 실제 Gameplay 씬은 런타임 생성 방식이라 정상입니다. Visual Preview를 여세요.
- **수정했는데 게임에 반영되지 않는다:** Preview 인스턴스만 수정했는지 확인하고, 실제 Prefab을 Prefab Mode에서 수정하여 저장하세요.
- **Play를 끄니 수정값이 사라졌다:** Play Mode에서 바꾼 값입니다. 재생을 끄고 Prefab Mode에서 다시 입력하세요.
- **캐릭터가 두 겹으로 보인다:** Body, Outline 또는 Shadow 자식을 복제했는지 확인하세요.
- **체력바가 이상한 방향으로 줄어든다:** `WorldBarView`의 Fill 참조와 Fill의 원래 위치를 확인하세요.
- **경험치 꼬리 효과가 사라졌다:** `ExperiencePickup.prefab` 루트의 `TrailRenderer`가 남아 있고 `PickupVisualView`에 연결되어 있는지 확인하세요.
- **Prefab이 없거나 참조가 비었다:** `Create or Validate Visual Prefabs` 메뉴를 실행하고 Console 메시지를 확인하세요.

외형을 바꿀 때의 가장 중요한 원칙은 **Prefab의 위치·크기·기본 계층은 편집하고, 런타임이 넣는 Sprite·상태·게임 수치는 건드리지 않는 것**입니다.
