# 히토미 스크롤 뷰어
[![GitHub latest release](https://img.shields.io/github/release/kaismic/Hitomi-Scroll-Viewer.svg?logo=github)](https://github.com/kaismic/Hitomi-Scroll-Viewer/releases/latest)
[![GitHub downloads count latest release](https://img.shields.io/github/downloads/kaismic/Hitomi-Scroll-Viewer/latest/total.svg?logo=github)](https://github.com/kaismic/Hitomi-Scroll-Viewer/releases/latest)
[![GitHub downloads count total](https://img.shields.io/github/downloads/kaismic/Hitomi-Scroll-Viewer/total.svg?logo=github)](https://github.com/kaismic/Hitomi-Scroll-Viewer/releases)


여러가지 기능들이 있는 hitomi.la (18+)를 위한 뷰어.

WinUI 3와 C#으로 개발했습니다.

## 미리보기
<div align="center">
    <img src="images/preview1.png" style="width: 50%;">
    <img src="images/preview2.png" style="width: 50%;">
</div>

## 기능들
- 조합 가능한 태그 필터로 검색 링크 생성
- 갤러리 다운로드
- 자동 스크롤
- 읽는 방향 바꾸기

## 설치법
만약에 앱 버전을 2.x.x 에서 2.x.x로 업그레이드 하는 경우라면 2번 순서를 건너뛰어주세요. 그 외 경우에는 모든 순서를 따라서 설치하면 됩니다.
1. 자신의 CPU 아키텍처에 맞는 파일을 [다운로드](https://github.com/kaismic/Hitomi-Scroll-Viewer/releases/latest)후 압축을 풀어주세요.
2. 압축 해제한 파일을 연 다음 보안인증서 열기 (.cer) -> 인증서 설치 -> 로컬 컴퓨터 -> 모든 인증서를 다음 인증소에 저장 -> 찾아보기 -> 신뢰할 수 있는 사용자 -> 다음 -> 마침.
3. MSIX File (.msix) 을 실행
4. 만약에 "You must install .NET Desktop Runtime...", 라는 창이 나오면 그대로 따라서 설치해주세요.

## 사용법 / 조작법
- 검색 페이지:
    - 태그 입력칸에는 한 줄에 태그를 하나씩 입력해주세요.
- 감상 페이지:
    - 마우스를 창 끝 위로 올리면 설정 메뉴가 나옵니다.
    - 스페이스바로 자동 스크롤 시작/정지.
    - `L` 키로 자동 스크롤 중일때 루프 켜기/끄기.
    - 왼쪽/위쪽 화살표와 오른쪽/아래쪽 화살표 또는 마우스 휠로 페이지 바꾸기.
