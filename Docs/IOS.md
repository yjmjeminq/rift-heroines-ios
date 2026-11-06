# iPhone 시험 빌드 · 0.9.0 (1)

- 앱: Rift Heroines / `com.moonbridge.riftheroines`
- 최소 iOS 15.0, ARM64 실기기, iPhone / iPad, 양쪽 가로 회전.
- 출력: `RiftHeroines-iPhone-unsigned-v0.9.0.ipa`. iLoader에서 본인의 설치 서명으로 서명하여 사용합니다.
- Windows PC의 창·마우스·키보드를 조작하지 않고 숨김 Unity 배치 빌드와 GitHub의 Mac 빌더를 사용합니다.

## 터치 화면

16:9 전장을 노치·홈 인디케이터를 제외한 `Screen.safeArea` 안에 맞춥니다. 가로 회전 시 위치와 크기를 다시 계산합니다. 화면을 길게 늘리거나 전장을 잘라내지 않습니다.

출격 카드는 한 번 터치 / 길게 누르기를 유지합니다. `동료 정보 · 각성`은 전투를 잠시 멈추고 큰 동료 선택·각성·출격 버튼을 제공합니다. 상단 경계 제목은 큰 일시정지 메뉴로 이어져 배속·소리·흔들림·편성·거점 복귀를 조작합니다. 공방은 카드 전체를 터치하면 큰 설명·구매·수집 목표 화면을 열고, 캐릭터 그림을 터치하면 12명 선택 화면을 엽니다. 잠긴 상품은 돈이 부족하면 구매가 비활성화되며 구매 직후 저장합니다.

## 재현

```powershell
./Tools/build.ps1
./Tools/mobile-smoke.ps1
./Tools/export-ios.ps1
python Tools/IOSBuild.py prepare --project Builds/iOS-Xcode --archive Builds/Artifacts/RiftHeroines-ios-export-v0.9.0.zip
```

Releases에 입력 ZIP을 업로드한 뒤 `Build Rift Heroines iPhone IPA` 워크플로를 수동 실행합니다. `source_tag`에 릴리스 태그, `source_sha256`에 준비 과정에서 출력한 SHA256을 넣습니다. Mac은 Xcode 16.4 / iPhoneOS SDK로 `CODE_SIGNING_ALLOWED=NO`를 지정하여 컴파일하고 `Payload/<app>.app`를 패키징합니다. 인증서·프로비저닝 프로파일은 저장소에 넣지 않습니다. [워크플로](../.github/workflows/ios-build.yml).

## 검증 범위

`Tools/mobile-smoke.ps1`는 숨김 Windows 플레이어에서 아이폰 화면 비율과 노치 여백을 적용하고 Unity 내부 포인터 이벤트를 발생시킵니다. 양 진영 출격, 전투 정지·재개, 각성, 공방 인물 선택·목표·구매·디스크 저장과 새 출정 적용을 검사합니다. 테스트 저장은 `BuildLogs`에 격리됩니다.

`Tools/IOSBuild.py`는 입력 ZIP과 개별 파일 해시, Mach-O의 ARM64 / iOS 실기기 플랫폼, 앱 식별자·버전·가로 방향, 실행 권한, 게임 데이터 일치와 서명 부재를 검사합니다. Mac 빌드 보고서와 입력 검증은 같은 릴리스에 첨부됩니다. 내려받은 IPA도 로컬에서 같은 검증기를 다시 통과시켜야 합니다.

이 자동 검증은 실제 아이폰에서의 설치·플레이 검증을 대신하지 않습니다. 이번 파일은 사용자가 iLoader로 설치하여 확인하는 첫 실기기 시험용입니다.

2026-11-06 로컬 검증: 모델 465개 검사와 8개 캠페인 통과. 2532×1170 / 안전 영역 (132, 63, 2268, 1107)의 숨김 플레이어에서 양 진영 출격, 큰 각성·일시정지 메뉴, 12번째 동료 선택, 카드 전체 터치, 목표·구매 저장, 잔액 부족 차단, 다음 출정 특성 적용과 화면 경계 검사를 통과했습니다. 새로 생성한 UI도 오프스크린 렌더링 후 Unity GraphicRaycaster로 버튼 가림 여부를 확인하고 내부 포인터 이벤트를 보냅니다.

추가로 2048×1447의 넓은 화면에서도 같은 터치 검사를 통과했고, 기존 1600×900 플레이어의 양 진영 2회 완주 및 편의성·훈련·타격 UI·공방 검증도 통과했습니다. Xcode 내보내기는 오류 0개로 완료됐습니다. Unity 내부 GUI 셰이더의 Windows D3D11 경고 2개는 내보내기 로그에 남아 있습니다.

## 완성된 시험판

[IPA 다운로드](https://github.com/yjmjeminq/rift-heroines-ios/releases/download/v0.9.0-ios.1/RiftHeroines-iPhone-unsigned-v0.9.0.ipa) · [릴리스와 검증 보고서](https://github.com/yjmjeminq/rift-heroines-ios/releases/tag/v0.9.0-ios.1) · [성공한 Mac 빌드](https://github.com/yjmjeminq/rift-heroines-ios/actions/runs/42155560229).

IPA는 64,429,010바이트이며 SHA256은 `4e219295766b37d569827e53ba1050b58fc8737c9e00dea0c10be0cfece4de0b`입니다. 소스 태그 `v0.9.0-ios.1`은 `fd7ebc61cf720af7773f9004e831e4f8474d90fb` 커밋을 가리킵니다. Mac 빌드 성공 후 내려받은 파일의 GitHub 자산 해시, 체크섬 파일, ARM64 iOS 실기기 실행 파일 두 개, 버전 0.9.0(1), 가로 방향, 실행 권한과 Unity 게임 데이터 8개의 일치 여부를 다시 확인했습니다.

로컬 파일은 `Builds/Artifacts/RiftHeroines-iPhone-unsigned-v0.9.0.ipa`이고 같은 폴더에 설치 안내와 검증 보고서를 보관합니다. 실기기 설치·플레이 여부는 아직 검증되지 않았습니다.
