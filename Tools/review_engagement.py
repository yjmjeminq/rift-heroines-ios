"""Compare pacing evidence and verify the shipped engagement mechanics together."""
from pathlib import Path
import json
import statistics

ROOT = Path(__file__).resolve().parents[1]

def read(name):
    return json.loads((ROOT / name).read_text(encoding="utf-8-sig"))

model = read("BuildLogs/model-validation.json")
build = read("BuildLogs/build-result.json")
smoke = read("BuildLogs/PlayerSmoke/report.json")
version = model["version"]
current = read(f"BuildLogs/playtest-v{version}.json")
baseline = read("BuildLogs/playtest-v0.4.0.json")
assert model["passed"] and smoke["passed"] and build["result"] == "Succeeded"
assert version == smoke["version"] == build["version"] == current["version"]
assert (ROOT / "BuildLogs/PlayerSmoke/report.json").stat().st_mtime >= (ROOT / "BuildLogs/build-result.json").stat().st_mtime

def summarize(runs):
    return {
        "runs": len(runs), "wins": sum(r["phase"] == "Victory" for r in runs),
        "meanSeconds": round(statistics.mean(r["seconds"] for r in runs), 2),
        "stagesOver150Seconds": sum(s["seconds"] > 150 for r in runs for s in r["stages"]),
        "perfectGuards": sum(r.get("perfectGuards", 0) for r in runs),
        "shrineCaptures": sum(r.get("shrineCaptures", 0) for r in runs),
        "freeAwakens": sum(r.get("freeAwakens", 0) for r in runs),
        "chainBursts": sum(r.get("chainBursts", 0) for r in runs),
    }

policies = ("recruit", "relic", "mixed", "no-tactics", "legacy")
assert len(current["runs"]) == 40
summaries = {}
for policy in policies:
    runs = [r for r in current["runs"] if r["policy"] == policy]
    assert len(runs) == 8 and {r["faction"] for r in runs} == {0, 1}
    summaries[policy] = summarize(runs)
    if policy in ("recruit", "relic", "mixed"):
        for faction in (0, 1):
            assert sum(r["phase"] == "Victory" for r in runs if r["faction"] == faction) >= 2

old = summarize([r for r in baseline["runs"] if r["policy"] == "recruit"])
new = summaries["legacy"]
required = (
    "Melee fighters pass supports stopped outside melee range",
    "A live Sakura/Tsunade pair strengthens actual attack damage",
    "Yoruichi/Soifon's haste bond produces more real attacks",
    "Holding the central shrine grants resources and a timed offensive buff",
    "A last-moment shield interrupts the boss and exposes its weakness",
    "A clear offers distinct recruit, relic and free-awakening decisions",
    "Boss healing is capped to prevent an unbounded recovery stalemate",
)
checks = {c["name"]: c["passed"] for c in model["checks"]}
assert all(checks.get(name, False) for name in required)
assert smoke["perfectGuards"] > 0 and smoke["shrineCaptures"] > 0 and smoke["freeAwakens"] > 0
assert set(smoke["pickedRewardKinds"]) == {"recruit", "relic", "awakening"}
assert len(smoke["observedBonds"]) == 6
assert {"10-tactics-guide.png", "11-shrine-control.png", "12-perfect-guard.png", "13-counter-window.png"}.issubset(smoke["screenshots"])
assert new["wins"] >= old["wins"] and new["meanSeconds"] < old["meanSeconds"]

report = {
    "passed": True, "version": version, "modelChecks": len(model["checks"]),
    "scenarios": len(current["runs"]), "policies": summaries,
    "sameActionPolicyComparison": {"before": old, "after": new,
        "meanTimeReductionPercent": round(100 * (1 - new["meanSeconds"] / old["meanSeconds"]), 1)},
    "runtime": {key: smoke[key] for key in ("perfectGuards", "shrineCaptures", "freeAwakens", "observedBonds", "pickedRewardKinds")},
    "limitation": "Automated functional and pacing evidence; subjective enjoyment and long-term retention require human playtesting.",
}
(ROOT / "BuildLogs/engagement-review.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

labels = {"recruit": "영입 중심", "relic": "서약 중심", "mixed": "영입·각성·서약 혼합", "no-tactics": "소환·성장만 사용", "legacy": "이전 조작 정책 유지"}
rows = "\n".join(f"| {labels[p]} | {summaries[p]['wins']}/8 | {summaries[p]['meanSeconds']:.0f}초 | {summaries[p]['perfectGuards']} | {summaries[p]['shrineCaptures']} |" for p in policies)
doc = f"""# 재미 요소 점검과 개선 · v{version}

v0.5.0에서 추가한 전투 구조의 현재 회귀 검증입니다. v0.6.0의 출격·저장·기록 개선은 [편의성 점검](USABILITY.md)에 기록합니다.

## 확인한 문제

이전 버전의 같은 영입 중심 조작 정책에서 8회 중 {old['wins']}회 완주, 평균 {old['meanSeconds']:.0f}초였습니다. 원거리 지원 유닛이 정지하면 같은 줄의 근접 병력이 막혔습니다. 실패한 두 판에는 적이 모두 사라진 뒤에도 아군이 원거리 유닛 뒤에 몰려 거점을 마무리하지 못한 상태가 기록됐습니다. 같은 대상에게 여러 치유가 겹치고, 보상 첫 카드가 영입으로 고정되며, 보스 결계를 정확히 맞출 때의 추가 보상도 없었습니다.

## 적용한 변화

| 경험 | 구현한 규칙 | 확인한 근거 |
|---|---|---|
| 인물 조합을 유지하는 선택 | 6쌍의 인물이 함께 생존하면 공격·공속·영력 효과 활성. 복제 병력으로 중복 활성 불가 | 실피해·실공격 횟수·영력·파트너 사망 검사, HUD 조합 표시 |
| 중앙을 두고 충돌하는 순간 | 12초 후 제단 개방, 이후 40초마다 재개방. 양측 병력으로 쟁탈, 아군 점령은 영력 35·각성 20·12초 공격 +20% | 점령·경합·적 점령·단발 지급·재개방 검사, 실행본 점령 {smoke['shrineCaptures']}회 |
| 보스에게 되받아치는 손맛 | 마지막 0.65초의 E가 비기 차단, 각성 15, 4초간 대장 피해 +35%를 부여 | 이른 결계와 구분, 실제 추가 피해·재사용·저장 검사, 실행본 반격 {smoke['perfectGuards']}회 |
| 다음 판의 다른 성장 경로 | 영입·서약·무료 각성 중 선택. 카드 위치를 섞고 다음 적 전술과 조합 짝을 미리 표시 | 세 보상 UI 선택, 실제 각성 증가, 영입·서약·혼합 정책 반복 완주 |
| 진입이 쉬운 전투 흐름 | 근접 유닛의 지원대 추월, 목표 방향 이동, 전장 경계 유지, 대장 격파 후 거점 결계 해제 | 이동·표적·경계·보스 결계 검사와 동일 조작 정책 비교 |
| 쌓일수록 달라지는 서약 | 13종. 5연쇄 폭발·정밀 반격 강화·제단 점령 강화 추가. 동일 대상 치유 간격 2초, 대장 치유량 3.5% 제한 | 폭발의 실제 피해·중복 처치 방지·회복 겹침·대장 회복량 검사 |

## 반복 플레이 결과

검사 {len(model['checks'])}개와 새 버전 40회 캠페인 실험을 기록했습니다. 각 정책은 양 진영 × 4개 시드입니다. 영입·서약·혼합 24회는 공개 소환·성장·지휘 입력으로 새 규칙을 활용하며, 이전 조작 정책 8회는 기존 소환 순환·이른 결계·궁극기 사용 방식을 그대로 유지합니다. 보상 카드 위치가 바뀌어도 같은 영입 유형을 선택합니다.

| 조작 정책 | 6경계 완주 | 평균 전투 시간 | 절묘한 결계 | 제단 점령 |
|---|---:|---:|---:|---:|
{rows}

같은 이전 조작 정책의 완주는 **{old['wins']}/8 → {new['wins']}/8**, 평균은 **{old['meanSeconds']:.0f}초 → {new['meanSeconds']:.0f}초**, 150초 초과 구간은 **{old['stagesOver150Seconds']}개 → {new['stagesOver150Seconds']}개**였습니다. 이 비교는 이동·치유·시너지·제단·적 전술을 함께 바꾼 통합 결과입니다. 개별 기능의 효과를 분리한 실험은 아닙니다.

실행본은 12명 전원, 조합 {len(smoke['observedBonds'])}종, 제단 점령, 절묘한 결계, 세 종류 보상 선택, 일시정지, 저장·이어하기와 6경계 완주를 확인했습니다. 실제 캡처는 `BuildLogs/PlayerSmoke/`에 있습니다. PC 창 활성화나 마우스·키보드 조작 없이 숨김 카메라 렌더링으로 확인했습니다.

## 다음 플레이에서 살필 점

자동 검사는 규칙의 작동과 정체 구간을 보여 줍니다. 체감 재미와 오래 다시 하고 싶은지는 직접 플레이 자료가 필요합니다. 첫 3분에 조합 짝과 금빛 결계 신호를 이해하는지, 제단 때문에 궁극기 사용 시점을 바꾸는지, 무료 각성과 영입을 실제로 고민하는지를 관찰하면 다음 수정의 우선순위를 정할 수 있습니다. 추후에는 분기 경로와 캐릭터별 도전 목표가 반복 플레이를 확장할 후보입니다.

원본 자료: `BuildLogs/playtest-v0.4.0.json`, `BuildLogs/playtest-v{version}.json`, `BuildLogs/model-validation.json`, `BuildLogs/PlayerSmoke/report.json`, `BuildLogs/engagement-review.json`.
"""
(ROOT / "Docs/ENGAGEMENT.md").write_text(doc, encoding="utf-8")
print(json.dumps({"passed": True, "version": version, "scenarios": len(current["runs"]), "sameActionPolicyComparison": report["sameActionPolicyComparison"]}, ensure_ascii=False, indent=2))
