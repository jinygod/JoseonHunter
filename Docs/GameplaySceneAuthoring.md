# Gameplay 씬 작업 안내

이 문서는 Unity에서 실제 전투 씬의 시작 위치, 카메라, 필드 미리보기와 외형 Prefab을 안전하게 편집하는 방법을 설명합니다. `Gameplay` 씬은 **시작 구성은 씬에 저장**하고, 많은 수가 필요한 전투 오브젝트는 **실행 중에 생성**하는 혼합 구조입니다.

## Gameplay 씬 열기

다음 둘 중 하나로 실제 Gameplay 씬을 엽니다.

- Project 창에서 `Assets/JoseonHunter/Scenes/Gameplay.unity`를 더블 클릭합니다.
- Unity 메뉴에서 `JoseonHunter > Gameplay Editing > Open Authored Gameplay Scene`을 선택합니다.

열린 Scene View에서는 씬에 저장된 `Han Yeonhwa`를 선택해 이동하거나 크기를 조정할 수 있습니다. 이것은 다음 Play Mode의 시작 상태에 반영됩니다. 캐릭터의 실제 이동 시뮬레이션과 카메라 추적은 Play Mode에서만 실행됩니다.

## 시작 위치 바꾸기

Hierarchy에서 다음 경로를 엽니다.

`FirstPlayable/RuntimeObjects/Han Yeonhwa`

`Han Yeonhwa`를 선택한 뒤 Scene View의 이동 도구 또는 Inspector의 Transform으로 위치를 바꾸고 씬을 저장합니다. 저장한 위치가 다음 Play Mode의 시작 위치이며, 재시작해도 그 저장된 포즈로 복원됩니다.

`Han Yeonhwa`는 연결된 `PlayerVisual` Prefab 인스턴스이므로, 이름을 바꾸거나 다른 Prefab으로 교체하지 마세요. 외형 자체를 고칠 때는 아래 Prefab 작업 방식을 사용합니다.

## 씬에 계속 남는 것과 실행 중에 바뀌는 것

다음은 씬에 작성되어 재시작 후에도 같은 오브젝트로 남는 **안정적인 구성**입니다.

```text
Gameplay
├─ Main Camera
├─ FirstPlayable
│  ├─ FlatField
│  │  ├─ Authoring Preview
│  │  └─ Runtime Battlefield
│  ├─ RuntimeObjects
│  │  └─ Han Yeonhwa
│  ├─ RuntimeSystems
│  └─ Spawn Guides
├─ First Playable UI
└─ EventSystem
```

반대로 `RuntimeObjects`의 `Han Yeonhwa` 이외 자식과 `RuntimeSystems`의 내용은 **한 판에만 필요한(reset-scoped) 내용**입니다. 적, 투사체, 장판·함정, 보물, 획득물과 풀·표현 도우미는 시작과 재시작 때 정리되거나 다시 만들어집니다.

이들을 씬에 미리 많이 배치하지 않는 이유는 웨이브 수, 스테이지, 플레이어 위치에 따라 수와 위치가 계속 바뀌고, 풀링으로 재사용해야 하기 때문입니다. 따라서 적·투사체·획득물의 실제 동작은 Play Mode에서 확인하세요.

## 필드: Authoring Preview와 Runtime Battlefield

`FirstPlayable/FlatField/Authoring Preview`는 편집할 때 필드의 타일과 분위기를 볼 수 있도록 둔 3×3 미리보기입니다. Play Mode가 시작되면 숨겨집니다.

`FirstPlayable/FlatField/Runtime Battlefield` 아래에는 현재 스테이지에 맞는 전장 청크, 장식, 경계가 실행 중에 만들어집니다. Runtime Battlefield의 자식은 저장해서 편집 대상으로 삼지 마세요. 필드의 안정적인 부모인 `FlatField` 자체는 유지됩니다.

`Spawn Guides`는 Scene View에서 스폰 위치를 이해하기 위한 안내입니다. 실제 적 스폰은 움직이는 카메라의 화면 가장자리와 스테이지 규칙을 사용하므로, 고정된 월드 좌표 안내를 게임 규칙으로 바꾸지는 않습니다.

## 카메라 작업

`Main Camera`를 선택하면 Inspector에서 투영 방식, Orthographic Size, 배경색, Clear Flags, 시작 위치를 확인하고 편집할 수 있습니다. 작성된 Gameplay 구성이 정상일 때 이 Inspector 값이 시작 카메라의 기준이며 초기화 과정에서 덮어쓰지 않습니다.

Play Mode에서는 카메라가 플레이어를 부드럽게 따라갑니다. 즉, Scene View에서 정한 값은 시작 프레이밍이고, 전투 중 카메라가 플레이어 이동에 맞춰 움직이는 것은 정상입니다. 카메라가 플레이어의 위치를 완전히 따라가지 않는 것처럼 보이면, 이동 방향을 조금 앞서 보는 효과와 부드러운 추적 때문입니다.

## 플레이어·적·월드 바 외형 편집

다음 Prefab은 Project 창의 `Assets/JoseonHunter/Prefabs/Gameplay`에 있습니다.

- `PlayerVisual.prefab`
- `EnemyVisual.prefab`
- `WorldHealthBar.prefab`
- `WorldShieldBar.prefab`

가장 안전한 방법은 Prefab을 더블 클릭해 **Prefab Mode**에서 수정하고 `Ctrl+S`로 저장하는 것입니다. Prefab 파일 자체를 편집한 경우에는 Apply가 필요 없습니다.

Gameplay 씬 또는 Preview 씬 안의 Prefab 인스턴스를 수정했다면 그 수정은 Override입니다. Inspector의 `Overrides`에서 필요한 변경을 확인한 뒤 `Apply All`로 원본 Prefab에 적용해야 실제 런타임 생성물에도 반영됩니다. 인스턴스 변경이 필요하지 않다면 Prefab Mode에서 직접 편집하는 편이 안전합니다.

월드 바의 위치는 `PlayerVisual` 또는 `EnemyVisual`의 `HealthBarAnchor`/`ShieldBarAnchor`에서 조정하고, 바의 모양과 크기는 `WorldHealthBar.prefab`과 `WorldShieldBar.prefab`에서 조정합니다. 필수 스크립트와 참조를 삭제하거나 이름을 임의로 바꾸지 마세요.

외형을 한 화면에서 비교하려면 `JoseonHunter > Gameplay Editing > Open Visual Preview`를 사용합니다. Visual Preview는 외형 확인용이며, 실제 시작 위치를 정하는 씬은 `Gameplay`입니다.

## Play Mode 변경은 임시입니다

Play 버튼이 켜진 상태에서 Scene View나 Inspector로 바꾼 값은 Play Mode를 끄면 되돌아갑니다. 지속할 변경은 다음 중 하나로 처리하세요.

1. Play Mode를 끈 뒤 `Gameplay` 씬의 안정적인 오브젝트를 편집하고 저장합니다.
2. 원본 Prefab을 Prefab Mode에서 편집하고 저장합니다.
3. 씬의 Prefab 인스턴스 Override를 의도적으로 유지할 경우 `Overrides > Apply All`로 원본 Prefab에 적용합니다.

## 복구와 검사

구성이 빠졌거나 참조가 의심되면 Unity 메뉴에서 다음을 실행합니다.

`JoseonHunter > Gameplay Editing > Create or Validate Authored Gameplay Scene`

이 명령은 빠진 안정적 오브젝트와 연결을 만들거나 검사하지만, 정상적인 사용자 위치와 연결된 Prefab은 보존합니다. 단, 열려 있는 `Gameplay` 씬에 저장하지 않은 변경이 있으면 수정 전에 거부합니다. 이는 작업 중인 변경을 덮어쓰지 않기 위한 보호 장치입니다.

이 경우에는 먼저 변경을 저장하거나 되돌린 뒤 다시 실행하세요. Prefab 자체의 연결을 검사하려면 별도로 `JoseonHunter > Gameplay Editing > Create or Validate Visual Prefabs`를 사용합니다.
