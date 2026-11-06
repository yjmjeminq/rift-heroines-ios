"""Audit convenience, recovery and persistent goals against both native UI runs."""
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[1]
def read(name):
    return json.loads((ROOT / name).read_text(encoding="utf-8-sig"))

model=read("BuildLogs/model-validation.json")
build=read("BuildLogs/build-result.json")
version=model["version"]
assert model["passed"] and build["result"]=="Succeeded" and build["version"]==version
checks={c["name"]:c["passed"] for c in model["checks"]}
required_model=(
    "An unaffordable summon becomes a single reservation without spending mana",
    "A reservation deploys exactly once when normal resources are ready",
    "Pressing a queued card cancels before readiness and cannot requeue on release",
    "Holding a card repeats legal summons and stops on release",
    "Reserved spending waits during a live boss telegraph",
    "Quitting before choosing resumes the exact rerolled reward screen",
    "An unused reroll keeps played-hero preferences across a reward-screen restart",
    "A resumed pending reward advances once and keeps its free awakening",
    "A damaged primary save recovers the valid backup including pending rewards",
    "Saving after recovery preserves the healthy backup and new progress",
    "Old profiles keep currency and upgrades while gaining new record fields",
    "Battle speed preference survives profile saving",
    "Cost previews use the actual rounded summon cost",
    "Cooldown previews respect the actual minimum",
)
assert all(checks.get(name,False) for name in required_model)
assert sum(name.startswith("Run rewards and records settle exactly once") and passed for name,passed in checks.items())==8
assert sum(name.startswith("Completed challenges survive saving without duplicate claims") and passed for name,passed in checks.items())==8
required_runtime={"queued-once","queue-page-and-cancel","hold-and-release","focus-loss-pauses","inventory-preserves-pause","reward-inventory-return","pending-reward-cold-continue","records-and-claim-once","layout-bounds","both-faction-records","same-field-new-receipt","two-faction-goal-saved","owned-relic-inventory"}
resolutions=[]
for folder,dimensions in (("PlayerSmoke",(1600,900)),("PlayerSmoke1280",(1280,720))):
    report=read(f"BuildLogs/{folder}/report.json")
    profile=read(f"BuildLogs/{folder}/profile.json")
    assert report["version"]==version and report["passed"] and report["conveniencePassed"] and not report["errors"]
    assert (report["screenWidth"],report["screenHeight"])==dimensions
    assert set(report["convenienceChecks"])==required_runtime and report["completedCampaigns"]==2
    assert (ROOT/f"BuildLogs/{folder}/report.json").stat().st_mtime >= (ROOT/"BuildLogs/build-result.json").stat().st_mtime
    assert profile["factionWins"]==[1,1] and len(profile["history"])==2
    assert len({r["id"] for r in profile["history"]})==2
    assert set(profile["claimedChallenges"])=={"first","factions"} and profile["shards"]+profile["workshopSpent"]==236
    assert report["workshopPassed"] and report["workshopEarned"]==236 and report["workshopSpent"]==profile["workshopSpent"]==230
    assert len(profile["deployments"])==12 and all(n>0 for n in profile["deployments"])
    assert len(report["observedHeroIds"])==12 and len(report["observedBonds"])==6
    resolutions.append({"width":dimensions[0],"height":dimensions[1],"screenshots":len(report["screenshots"]),"convenienceChecks":len(report["convenienceChecks"]),"campaigns":2,"profileHistory":2,"claimBalance":profile["shards"],"workshopSpent":profile["workshopSpent"]})

result={"passed":True,"version":version,"modelChecks":len(model["checks"]),"resolutions":resolutions,"runtimeScenarios":len(required_runtime),"persistedGoals":6,"recentRunLimit":6,"legacyProfileCompatible":True,"rewardSelectionResume":True,"backupRecovery":True}
(ROOT/"BuildLogs/usability-review.json").write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding="utf-8")
document=f"""# 편의성과 반복 플레이 점검 · v{version}

전투의 선택을 유지하면서 반복 클릭, 보상 비교, 중단과 재개, 한 판 이후의 목표를 보강했습니다. 현재 원작 인물 12명과 전투 구조는 유지합니다.

## 확인한 불편과 변경

| 확인한 상황 | 현재 동작 | 검증 |
|---|---|---|
| 영력이 모자라거나 출격 준비 중이면 다시 눌러야 함 | 한 번 출격 예약, 준비되면 1회 출격. 같은 카드·취소 버튼·Backspace로 취소. 카드 페이지가 바뀌어도 인물 유지 | 자원 미소모·1회 지급·취소·진영 페이지 전환·잠긴 동료 거부 |
| 반복 출격에 많은 클릭이 필요함 | 카드나 1–6 키를 길게 누르면 비용·재사용을 지키며 반복. 손을 떼면 정지, 보스 예고에는 자동 지출 대기 | 실제 포인터 누름·놓음, 놓을 때 추가 클릭 억제, 경고 중 영력 보존 |
| 현재 편성과 서약을 보며 보상을 비교하기 어려움 | B로 편성·각성·병력 수·비용·보유 서약 확인. 보상에서 돌아와도 선택지는 유지. 서약은 보유 수와 효과 전후를 표시 | 편성 창 정지·기존 일시정지 복원, 실제 반올림 비용·재사용 하한 미리보기 |
| 보상 전에 종료하면 깬 구간을 다시 해야 함 | 선택지·무료 새로고침 사용·출정 식별을 저장하고 같은 보상에서 재개 | 실제 저장 파일을 다시 읽은 이어하기, 각성 1회 적용·다음 경계 진입 |
| 저장 원본을 읽지 못하면 진행이 사라질 수 있음 | 정상 백업 복구, 손상본 별도 보존, 복구 후에도 정상 백업 유지 | 전용 QA 저장 파일 손상·복구·재저장 검사 |
| 창을 벗어난 사이 전투가 진행됨 | 일반 플레이에서 포커스를 잃으면 일시정지. 연속 출격 중지 | 공통 포커스 처리 경로에서 시간·자원 정지 |
| 한 판 뒤 다음 목표와 기록이 부족함 | 도전 6종, 출격 도감 12명, 최근 6회 출정과 진영별 최고 시간. 완료 보상은 한 번씩 수령 | 양 진영 완주 기록·저장·보상 중복 방지·정확한 잔액 |
| 같은 전장에서 실력을 비교하기 어려움 | 결과 화면에서 새로운 전장 또는 같은 전장 재도전 선택 | 같은 전장 구성·새 출정 식별·기존 보상 중복 없음 |

## 누적 도전

| 도전 | 조건 | 인연 조각 |
|---|---|---:|
| 첫 경계 돌파 | 경계 1회 돌파 | 10 |
| 세 갈래 인연 | 한 전투에 조합 3종 동시 활성 | 20 |
| 월광의 개척자 | 제단 누적 5회 점령 | 20 |
| 찰나의 반격 | 절묘한 결계 누적 3회 | 25 |
| 열두 명의 동료 | 12명 모두 출격 | 30 |
| 경계의 중재자 | 두 진영 각각 완주 | 40 |

기록은 이벤트를 중복 집계하지 않으며 전투 중 주기적으로 저장합니다. 보상 대기와 결과 정산은 즉시 저장합니다. 기존 재화·강화·승리는 유지하며, 새 도전용 세부 기록은 이후 플레이부터 쌓입니다. 같은 전장의 재도전도 별도 출정으로 기록합니다.

## 실제 확인

- 모델 검사 **{len(model['checks'])}개 통과**, 기존 전투 회귀 8개 캠페인 완주. 별도의 전투 정책 40회도 기록합니다.
- **1600×900, 1280×720** 각각 양 진영을 끝까지 완주했습니다. 해상도마다 편의 시나리오 {len(required_runtime)}개와 화면 {resolutions[0]['screenshots']}개를 확인했습니다. 서약을 실제 획득한 뒤 편성 창의 보유 수와 효과도 확인했습니다.
- 각 QA 프로필에는 서로 다른 출정 2회와 양 진영 승리가 남았고, 12명 전원 출격을 기록했습니다.
- 첫 도전 10조각과 양 진영 도전 40조각, 완주 보상 93조각×2로 **236조각**을 벌었습니다. 이후 실제 공방 구매에 **230조각**을 사용하고 **6조각**이 남았습니다. 획득·소비·잔액이 일치하며 중복 수령과 중복 지출은 차단됐습니다. [공방 점검](WORKSHOP.md).
- 화면 검사에서 미영입 안내의 잘림을 발견해 두 줄 안에 들어오도록 수정했습니다. 주요 버튼의 화면 밖 이탈과 도전·미리보기·편성 글자 잘림을 검사했습니다.
- 키보드 조작: 1–6 출격·길게 누르기, B 편성, Backspace 예약 취소, 보상 1–3 선택·R 새로고침, Esc 창 닫기, F11 전체 화면. 1×/2× 설정을 저장합니다.

숨김 실행과 카메라 렌더링으로 확인했으며 PC 창 활성화나 실제 마우스·키보드 조작을 사용하지 않았습니다. 체감 재미와 장기 반복 플레이는 실제 사용자 반응을 보며 조정할 수 있습니다.

증거: `BuildLogs/model-validation.json`, `BuildLogs/usability-review.json`, `BuildLogs/PlayerSmoke/report.json`, `BuildLogs/PlayerSmoke1280/report.json`, 각 폴더의 PNG와 QA 프로필. [기존 전투 구조 점검](ENGAGEMENT.md).
"""
(ROOT/"Docs/USABILITY.md").write_text(document,encoding="utf-8")
print(json.dumps(result,ensure_ascii=False,indent=2))
