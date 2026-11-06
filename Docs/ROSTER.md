# 현재 캐릭터 확인표 · v0.9.0

현재 게임에는 **나루토 6명과 블리치 6명, 총 12명**이 등장합니다. 선택한 작품의 6명은 기본 출격, 상대 작품의 6명은 구간 보상의 영입 대상입니다. 적 자동 편성과 각성 대장도 같은 명단을 사용합니다.

| 진영 | 게임 표시 | 원작 인물 | 공식 확인 출처 |
|---|---|---|---|
| 나루토 | 사쿠라 | 하루노 사쿠라 · 春野サクラ | [NARUTO 공식 사쿠라 소개](https://naruto-official.com/en/news/01_1743) |
| 나루토 | 츠나데 | 츠나데 · 綱手 | [NARUTO 공식 인물별 월페이퍼](https://naruto-official.com/en/special/wallpaper) |
| 나루토 | 히나타 | 휴우가 히나타 · 日向ヒナタ | [NARUTO 공식 인물별 월페이퍼](https://naruto-official.com/en/special/wallpaper) |
| 나루토 | 이노 | 야마나카 이노 · 山中いの | [NARUTO 공식 인물별 월페이퍼](https://naruto-official.com/en/special/wallpaper) |
| 나루토 | 테마리 | 테마리 · テマリ | [NARUTO 공식 인물별 월페이퍼](https://naruto-official.com/en/special/wallpaper) |
| 나루토 | 코난 | 코난 · 小南 | [NARUTO 공식 인물별 월페이퍼](https://naruto-official.com/en/special/wallpaper) |
| 블리치 | 란기쿠 | 마츠모토 란기쿠 · 松本乱菊 | [공식 Rebirth of Souls 캐릭터 소개](https://bleach-ros.bn-ent.net/character/?chara=rangiku-matsumoto) |
| 블리치 | 요루이치 | 시호인 요루이치 · 四楓院夜一 | [공식 Rebirth of Souls 캐릭터 소개](https://bleach-ros.bn-ent.net/character/?chara=yoruichi-shihoin) |
| 블리치 | 우노하나 | 우노하나 레츠 · 卯ノ花烈 | [공식 Brave Souls 캐릭터 소개](https://www.bleach-bravesouls.com/character/unohana.html) |
| 블리치 | 루키아 | 쿠치키 루키아 · 朽木ルキア | [공식 Brave Souls 캐릭터 소개](https://www.bleach-bravesouls.com/character/rukia.html) |
| 블리치 | 오리히메 | 이노우에 오리히메 · 井上織姫 | [공식 Brave Souls 캐릭터 소개](https://www.bleach-bravesouls.com/character/orihime.html) |
| 블리치 | 소이폰 | 소이폰 · 砕蜂 | [공식 Brave Souls 캐릭터 소개](https://www.bleach-bravesouls.com/character/soifon.html) |

블리치 여섯 인물은 [Brave Souls 공식 전체 명단](https://www.bleach-bravesouls.com/character/)에서도 확인했습니다. 확인일은 2026-11-05입니다.

게임에서 이름뿐 아니라 인물의 특징도 맞췄습니다. 사쿠라는 분홍 머리와 괴력, 츠나데는 금발·이마 표식과 의료 인술, 히나타는 백안과 유권, 란기쿠는 주황 머리와 회묘의 재, 요루이치는 짙은 피부·보라 포니테일과 순홍, 우노하나는 긴 앞쪽 땋은 머리·검술과 치유를 사용합니다.

추가 인물은 이노의 금발 포니테일과 심전신, 테마리의 네 갈래 묶음머리와 대형 부채, 코난의 파란 머리·종이꽃과 종이 인술, 루키아의 검은 단발과 수백설, 오리히메의 주황 머리·꽃핀과 순순육화, 소이폰의 검은 머리·두 갈래 땋기와 작봉을 반영했습니다. 사쿠라·히나타·이노·테마리는 보루토의 성인 시점, 루키아·오리히메는 완결 이후 성인 시점으로 제작했습니다.

출처는 원작 인물 확인용입니다. 의상, 게임 내 병종·수치·연출은 사용자가 요청한 성인 모습·8등신·비키니형 의상과 디펜스 규칙으로 재구성했습니다.

실행 시 사용하는 명단은 `Assets/Rift/Resources/Rift/canonical-roster.json`입니다. `RiftRosterCatalog`가 게임 정의와 인물명·진영·공식 출처와 작품별 6명 편성을 대조합니다. `Tools/validate_roster.py`는 현재 코드, PNG, 애니메이션 레이아웃, 제작 프롬프트까지 같은 열두 인물인지 검사합니다.

교체된 초안 자료는 Unity 에셋 밖 `ReferenceArchive/`에 보관하며 현재 실행본과 소스 ZIP에는 넣지 않습니다.
