# KoreanPatch

Spark in the Dark 한글패치입니다. 게임 파일을 바꾸지 않고, 게임이 실행될 때 한글 번역과 한글 글꼴을 적용합니다.

[SparkModLoader]((https://github.com/backchohyo/spark-in-the-dark-modloader)) 가 필요합니다.

## 설치

1. `release/Mods` 폴더의 내용(`KoreanPatch.dll` 과 `KoreanPatch` 폴더)을 게임 폴더의 `Mods` 폴더에 넣습니다.
2. 게임 설정의 언어를 **English** 로 둡니다. 한글은 영어 자리에 표시됩니다.

예전 방식 한글패치(`sharedassets0.assets`, `level0` 교체)를 쓰고 있었다면 스팀 "게임 파일 무결성 검사"로 원본을 복구한 뒤 사용하세요.

## 기능

- 게임 번역 파일 11종(대사, 책, 메모, 꿈, 기억 등)을 한글로 교체
- 메인 메뉴의 고정 문구(제작진 화면 등) 번역
- 게임 업데이트로 새 문장이 생기면 그 문장만 영어로 나오고 나머지는 한글 유지
- 게임 글꼴 교체: [모드] > 한글패치 [설정]에서 선택 (다시 시작하면 적용)
  - 워헤이븐 (기본값)
  - 명조 (마루 부리 · 바탕)
  - 맑은 고딕
  - 게임 기본 글꼴 + 한글만 대체
  - `fonts` 폴더에 넣은 글꼴

  고른 글꼴이 없으면 명조 → 맑은 고딕 순서로 대신 씁니다. 글꼴 파일은 `Mods\KoreanPatch\fonts` → 내 계정 글꼴 → Windows 글꼴 폴더 순서로 찾습니다. 글꼴은 "모든 사용자용으로 설치"해야 가장 확실하게 인식됩니다.

## 번역 수정

번역은 `data` 폴더(설치 후에는 `Mods\KoreanPatch`)의 `*-en.txt` 파일에 있습니다. 게임 원본과 같은 XML 형식이라 메모장으로 고칠 수 있고, 게임을 다시 켜면 반영됩니다.

## 빌드

```
dotnet build -c Release
```

게임 폴더의 `Spark in the Dark_Data\Managed` 와 `Mods\ModLoader.dll` 을 참조합니다. 게임이 기본 경로가 아니면 `-p:GameDir="..."` 로 지정합니다.
