# Network Team Project (BackTogether)

유니티(Unity)를 이용해 제작한 2D 멀티플레이어 협동 게임 프로젝트입니다.

## 📁 주요 구성 및 씬 (Scenes)
* **메인 및 로비**: Main, Lobby, NLobby
* **챕터 씬**: Chapter 1 ~ 6 (Nchapter 포함), Ex 씬

## 💻 주요 기능 및 담당 역할
이 프로젝트는 `_Sub` 폴더 내에 각 팀원별로 기능을 나누어 작업했습니다.

### 1. 네트워크 및 기믹 (Bae)
* **네트워크 연동**: Mirror, Steamworks, Epic Online Services(EOS)를 활용한 로비 및 방 생성 기능
* **플랫폼 및 장애물(Gimmick)**: 
  * 발판(통나무, 거울 발판, 시소, 패트롤 발판 등)
  * 버튼(다중 버튼, 브릿지 버튼, 스파이크 버튼)
  * 특수 환경(블리자드 존, 중력 역전, 강풍, 동기화 점프, 시야 제한)

### 2. UI (Choi)
* **메뉴 및 로비 UI**: 로비(Lobby), 챕터 선택 창, 팝업, 일시정지(Pause) 메뉴 구현
* **리소스 적용**: 폰트, 버튼 스프라이트, 패널 등 각종 UI 에셋 적용

### 3. 플레이어 모듈 (Seo)
* **입력 및 조작**: Unity New Input System을 활용한 키보드 및 게임패드 액션
* **캐릭터 액션**: 이동(Movement), 점프, 플래시라이트, 넉백, 리스폰
* **애니메이션 및 물리**: 캐릭터 애니메이터 제어 및 Physics 상호작용

### 4. 레벨 디자인 및 사운드 (Sin)
* **타일 및 배경**: RuleTile 및 각종 타일맵을 이용한 챕터별 레벨 디자인 구현
* **사운드**: 배경음악(BGM) 및 효과음(SFX) 관리 및 적용

## 🛠 사용 기술
* **Engine**: Unity
* **Network**: Mirror, Steamworks.NET, Epic Online Services
* **Input**: Unity New Input System
