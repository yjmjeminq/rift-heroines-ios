"""Package only this new game's source and portable Windows player."""
from pathlib import Path
import hashlib
import json
import re
import zipfile

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Builds" / "Artifacts"
OUT.mkdir(parents=True, exist_ok=True)
VERSION = re.search(r'const string Version = "([^"]+)"', (ROOT / "Assets/Rift/Runtime/RiftData.cs").read_text(encoding="utf-8")).group(1)

def digest(path):
    sha = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            sha.update(block)
    return sha.hexdigest()

build = json.loads((ROOT / "BuildLogs" / "build-result.json").read_text(encoding="utf-8"))
model = json.loads((ROOT / "BuildLogs" / "model-validation.json").read_text(encoding="utf-8"))
smoke = json.loads((ROOT / "BuildLogs" / "PlayerSmoke" / "report.json").read_text(encoding="utf-8"))
roster = json.loads((ROOT / "BuildLogs" / "roster-audit.json").read_text(encoding="utf-8"))
engagement = json.loads((ROOT / "BuildLogs" / "engagement-review.json").read_text(encoding="utf-8"))
usability = json.loads((ROOT / "BuildLogs" / "usability-review.json").read_text(encoding="utf-8"))
compact = json.loads((ROOT / "BuildLogs/PlayerSmoke1280/report.json").read_text(encoding="utf-8"))
content = json.loads((ROOT / "BuildLogs/content-review.json").read_text(encoding="utf-8"))
experience = json.loads((ROOT / "BuildLogs/experience-review.json").read_text(encoding="utf-8"))
workshop = json.loads((ROOT / "BuildLogs/workshop-review.json").read_text(encoding="utf-8"))
identities = json.loads((ROOT / "Assets/Rift/Resources/Rift/canonical-roster.json").read_text(encoding="utf-8"))["characters"]
assert build["result"] == "Succeeded" and build["errors"] == 0
assert model["passed"] and smoke["passed"]
assert roster["passed"] and roster["canonicalOnly"]
assert engagement["passed"] and engagement["version"] == VERSION and engagement["scenarios"] == 40
assert usability["passed"] and usability["version"] == VERSION and len(usability["resolutions"]) == 2
assert smoke["conveniencePassed"] and compact["passed"] and compact["conveniencePassed"] and compact["version"] == VERSION
assert smoke["completedCampaigns"] == compact["completedCampaigns"] == 2
assert content["passed"] and content["version"] == VERSION and content["newModelChecks"] == 33
assert smoke["contentPassed"] and compact["contentPassed"]
assert experience["passed"] and experience["version"]==VERSION and experience["strategyScenarios"]==48
assert smoke["feedbackPassed"] and compact["feedbackPassed"]
assert experience["normalTimeObservation"]["attackPoseMismatches"]==0
assert workshop["passed"] and workshop["version"]==VERSION and workshop["catalogueItems"]==41 and workshop["equippedCampaigns"]==48
assert smoke["workshopPassed"] and compact["workshopPassed"]
assert (ROOT / "BuildLogs/PlayerSmoke/report.json").stat().st_mtime >= (ROOT / "BuildLogs/build-result.json").stat().st_mtime
assert build["version"] == model["version"] == smoke["version"] == roster["version"] == VERSION
assert model["roster"] == smoke["roster"] == [c["displayName"] for c in identities]
assert set(smoke["observedHeroIds"]) == {c["id"] for c in identities}
assert smoke["campaignFinalStage"] == 6 and smoke["campaignFinalPhase"] == "Victory"

player = OUT / f"RiftHeroines-v{VERSION}-Windows.zip"
with zipfile.ZipFile(player, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
    for path in sorted((ROOT / "Builds" / "Windows").rglob("*")):
        if path.is_file():
            archive.write(path, "RiftHeroines/" + path.relative_to(ROOT / "Builds" / "Windows").as_posix())
    archive.write(ROOT / "BuildLogs/PlayerSmoke/00-twelve-roster.png", "RiftHeroines/Characters.png")
    archive.write(ROOT / "Docs/ENGAGEMENT.md", "RiftHeroines/PlaytestReview.md")
    archive.write(ROOT / "Docs/USABILITY.md", "RiftHeroines/ConvenienceReview.md")
    archive.write(ROOT / "Docs/CONTENT.md", "RiftHeroines/TrainingAndStages.md")
    archive.write(ROOT / "Docs/EXPERIENCE.md", "RiftHeroines/UIAndImpactReview.md")
    archive.write(ROOT / "Docs/WORKSHOP.md", "RiftHeroines/WorkshopGuide.md")
    archive.write(ROOT / "BuildLogs/PlayerSmoke/30-trait-unlocked.png", "RiftHeroines/WorkshopPreview.png")
    archive.write(ROOT / experience["proofFolder"] / "gameplay-1x.gif", "RiftHeroines/GameplayPreview.gif")
    archive.write(ROOT / f"BuildLogs/Audio-v{VERSION}/Cue-preview.wav", "RiftHeroines/SoundPreview.wav")
    archive.writestr("RiftHeroines/Play.cmd", '@echo off\ncd /d "%~dp0"\nstart "Rift Heroines" "RiftHeroines.exe"\n')
    archive.writestr("RiftHeroines/README-ko.txt", f"월야의 서약 · RIFT HEROINES {VERSION}\n\n압축을 전부 푼 뒤 Play.cmd 또는 RiftHeroines.exe를 실행하세요.\nData/DLL/MonoBleedingEdge 폴더를 함께 유지하세요.\n\n1–6 출격 / 길게 누르면 연속 출격 / 부족하면 1회 예약\nBackspace 예약 취소 / B 편성·서약 보기 / C 기본·영입 카드 전환\nQ 진격 / E 결계 / R 궁극기 / U 영맥 강화\n보상 화면 1–3 선택 / R 새로고침\nSpace 일시정지 / Tab 배속 / F11 전체 화면 / Esc 창 닫기\n\n나루토 6명: 사쿠라·츠나데·히나타·이노·테마리·코난\n블리치 6명: 란기쿠·요루이치·우노하나·루키아·오리히메·소이폰\n전원 성인 모습·8등신 비키니 · 6경계 캠페인\n홈의 직접 배우기 · 훈련: 조합→Q→E 반격→R, 기존 출정 보존\n전장 3곳 · 바뀌는 대장 · 전열/후열/지원 소집의 세 비기\nUI 가독성·출격 준비 막대·실제 자원에 맞춘 경고\n경직 중 모션·판정 동기화 · 타격/검격/술법 효과와 음향 구분\n\n인연 공방: 특성 24종·출정 부적 3종·별빛 연출 12종·액자 2종\n조각으로 영구 해금 · 무료 교체 · 원하는 항목 수집 목표 지정\n장착 변경은 새 출정부터 적용, 이어하기는 기존 준비 유지\n\n홈의 기록·도전: 도전 6종·12명 출격 도감·최근 6회·진영별 최고 기록\n같은 전장 재도전으로 기록에 도전!\n전투 중단은 구간 시작부터, 보상 전 종료는 같은 선택지부터 재개\n저장 백업 복구 · 창 전환 자동 일시정지 · 배속 저장\n\n짝이 함께 살아 있으면 생존 조합 활성!\n중앙 제단 점령으로 자원과 돌파 버프 획득!\n보스 비기 마지막 0.65초의 금빛 예고에 E로 절묘한 반격!\n동료 영입·서약 13종·무료 각성·영구 성장\n")

source = OUT / f"RiftHeroines-v{VERSION}-UnitySource.zip"
with zipfile.ZipFile(source, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
    for name in ("Assets", "Packages", "ProjectSettings", "Docs", "Tools"):
        for path in sorted((ROOT / name).rglob("*")):
            if path.is_file() and "__pycache__" not in path.parts:
                archive.write(path, "rift-heroines-unity/" + path.relative_to(ROOT).as_posix())
    for name in ("README.md", ".gitignore", "게임 실행.cmd", "BuildLogs/model-validation.json", "BuildLogs/build-result.json", "BuildLogs/roster-audit.json", "BuildLogs/PlayerSmoke/report.json", "BuildLogs/PlayerSmoke/profile.json", "BuildLogs/PlayerSmoke1280/report.json", "BuildLogs/PlayerSmoke1280/profile.json", "BuildLogs/usability-review.json", "BuildLogs/engagement-review.json", "BuildLogs/content-review.json", "BuildLogs/playtest-v0.4.0.json", f"BuildLogs/playtest-v{VERSION}.json"):
        archive.write(ROOT / name, "rift-heroines-unity/" + name)
    for name in smoke["screenshots"]:
        archive.write(ROOT / "BuildLogs/PlayerSmoke" / name, "rift-heroines-unity/BuildLogs/PlayerSmoke/" + name)
    for name in compact["screenshots"]:
        archive.write(ROOT / "BuildLogs/PlayerSmoke1280" / name, "rift-heroines-unity/BuildLogs/PlayerSmoke1280/" + name)
    for name in ("BuildLogs/experience-review.json","BuildLogs/experience-model-v0.7.0.json",f"BuildLogs/experience-model-v{VERSION}.json","BuildLogs/ExperienceAfter/report.json","BuildLogs/workshop-review.json",f"BuildLogs/workshop-playtest-v{VERSION}.json"):
        archive.write(ROOT/name,"rift-heroines-unity/"+name)
    for folder in ("BuildLogs/ExperienceBefore",experience["proofFolder"]):
        for name in ("report.json","opening.png","combat-1x.png","combat-2x.png"):
            rel=f"{folder}/{name}";archive.write(ROOT/rel,"rift-heroines-unity/"+rel)
    for name in ("gameplay-1x.gif","gameplay-2x.gif"):
        rel=experience["proofFolder"]+"/"+name;archive.write(ROOT/rel,"rift-heroines-unity/"+rel)
    for path in sorted((ROOT/f"BuildLogs/Audio-v{VERSION}").glob("*.wav")):
        archive.write(path,"rift-heroines-unity/"+path.relative_to(ROOT).as_posix())

for path in (player, source):
    with zipfile.ZipFile(path) as archive:
        assert archive.testzip() is None

manifest = {
    "version": VERSION,
    "unity": build["unity"],
    "modelChecks": len(model["checks"]),
    "campaignWins": sum(c["phase"] == "Victory" for c in model["campaigns"]),
    "playerSmokePassed": smoke["passed"],
    "canonicalOnly": roster["canonicalOnly"],
    "charactersPerFranchise": 6,
    "activeCharacters": roster["activeCount"],
    "animationFrames": roster["frames"],
    "playtestScenarios": engagement["scenarios"],
    "runtimePerfectGuards": smoke["perfectGuards"],
    "runtimeShrineCaptures": smoke["shrineCaptures"],
    "convenienceScenarios": usability["runtimeScenarios"],
    "testedResolutions": [[r["width"],r["height"]] for r in usability["resolutions"]],
    "persistentChallenges": usability["persistedGoals"],
    "trainingSteps": content["practiceSteps"],
    "contentScenarios": content["runtimeScenarios"],
    "environments": 3,
    "bossTechniqueTypes": 3,
    "feedbackModelChecks": experience["feedbackModelChecks"],
    "strategyReviewScenarios": experience["strategyScenarios"],
    "attackPoseMismatches": experience["normalTimeObservation"]["attackPoseMismatches"],
    "uiScenariosPerResolution": experience["resolutions"][0]["allRuntimeChecks"],
    "workshopItems": workshop["catalogueItems"],
    "workshopTraits": workshop["traits"],
    "workshopAmulets": workshop["amulets"],
    "workshopCosmetics": workshop["heroStyles"]+workshop["portraitFrames"],
    "equippedCampaigns": workshop["equippedCampaigns"],
    "roster": [c["canonicalName"] for c in identities],
    "files": [{"name": p.name, "bytes": p.stat().st_size, "sha256": digest(p)} for p in (player, source)],
}
(OUT / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
print(json.dumps(manifest, ensure_ascii=False, indent=2))
