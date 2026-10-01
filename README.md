# BACK TOGETHER

**BACK TOGETHER**는 2~4인이 함께 플레이하는 협동 퍼즐 플랫포머 게임입니다. Unity 엔진을 기반으로 개발되었으며, Mirror와 Epic Online Services(EOS)를 활용하여 Steam 및 STOVE 플랫폼 간의 원활한 크로스플레이 환경을 제공합니다.

## 📅 프로젝트 개요
* **프로젝트명:** BACK TOGETHER
* **장르:** 4인 협동 퍼즐 플랫포머
* **출시 일정:** 2026년 10월 30일 정식 출시
* **타겟 플랫폼:** PC (Steam, STOVE)

## 🎮 핵심 기능 (Key Features)
* **협동 플레이 시스템:** 합체 기믹과 로프 시스템 등 플레이어 간의 긴밀한 팀워크를 요구하는 고유한 메커니즘을 제공합니다.
* **환경 퍼즐:** 다양한 플랫폼과 도전적인 퍼즐 요소를 팀원과 함께 극복해야 합니다.
* **크로스플랫폼 멀티플레이어:** Steam 유저와 STOVE 유저 간의 제약 없는 멀티플레이를 지원합니다.
* **세이브 및 데이터 관리:** 로컬 게임 진행도는 JSON 파일을 통해 관리하며, 업적 및 리더보드 등은 각 플랫폼 API(Steam/STOVE)를 독립적으로 호출하여 처리하는 아키텍처를 적용했습니다.

## 🕸️ 네트워크 및 로비 시스템 (Network & Lobby)
본 프로젝트의 멀티플레이어 로비 시스템은 EOS를 기반으로 다음과 같이 구축되었습니다.

* **Public 룸 (공개 방)**
  * 방 이름 입력 및 플레이할 챕터(1~6)를 설정하여 방을 생성할 수 있습니다.
  * 8개 단위의 룸 리스트 페이징, 이전/다음 페이지 이동, 새로고침 UI를 지원합니다.
  * 방 제목 검색 및 챕터별 필터링 기능을 제공합니다.
  * 방의 최대 인원(MaxMembers)에 도달하지 않은 방에 즉시 접속하는 빠른 입장(Quick Join) 로직을 구현했습니다.
* **Private 룸 (비공개 방)**
  * 6자리 숏코드(Room Code)를 입력하여 지정된 프라이빗 세션에 바로 접속할 수 있습니다.
* **세션 관리 및 플랫폼 연동**
  * EOS Attribute를 사용하여 `ROOM_NAME`, `CHAPTER`, `ROOM_TYPE` 속성을 세션에 저장 및 갱신합니다.
  * Steam 환경 빌드 시 `SteamFriends`, STOVE 환경 빌드 시 `Stove PCSDK`의 파라미터를 연동하여 각 플랫폼의 네이티브 초대 기능을 활용합니다.

## 🛠 기술 스택 (Tech Stack)
* **Game Engine:** Unity
* **Network:** Mirror, Epic Online Services (EOS)
* **Platform SDK:** Steamworks SDK, Stove PCSDK 3.0

## 💻 시스템 권장 사양 (System Requirements)
* **OS (운영체제):** Windows 10 (64-bit)
* **Processor (프로세서):** Intel Core i5 또는 AMD Ryzen 5 이상
* **Memory (메모리):** 4 GB RAM
* **Graphics (그래픽):** NVIDIA GeForce GTX 950 또는 AMD Radeon R7 370 이상
* **DirectX:** Version 11
* **Storage (저장 공간):** 1 GB 사용 가능 공간
* **Network (네트워크):** 초고속 인터넷 연결 (Broadband Internet connection)

## 🔗 커뮤니티 및 지원 (Links)
* **스토어 페이지:** [Steam Store](#) | [STOVE Store](#)
* **공식 Discord:** 함께 게임을 즐길 파티를 구하는 채널(LFG), 버그 제보, 게임 관련 피드백을 남길 수 있는 커뮤니티 채널을 운영 중입니다.
