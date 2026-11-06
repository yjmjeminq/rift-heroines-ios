"""Reconcile actual earned currency, permanent ownership and equipped combat effects."""
from pathlib import Path
import json,re,statistics

ROOT=Path(__file__).resolve().parents[1]
def read(path):return json.loads((ROOT/path).read_text(encoding='utf-8-sig'))
model=read('BuildLogs/model-validation.json');build=read('BuildLogs/build-result.json');version=model['version']
assert model['passed'] and build['result']=='Succeeded' and build['version']==version
validation=(ROOT/'Assets/Rift/Editor/RiftWorkshopValidation.cs').read_text(encoding='utf-8')
prefixes=re.findall(r'Check\("([^"]+)"',validation)
new_checks=[c for c in model['checks'] if any(c['name'].startswith(prefix) for prefix in prefixes)]
assert len(new_checks)==212 and all(c['passed'] for c in new_checks)
assert sum(c['name'].startswith('Workshop purchase charges once equips and survives serialization') for c in new_checks)==41
assert sum(c['name'].startswith('Trait boss damage is applied in real combat') for c in new_checks)==24
source=(ROOT/'Assets/Rift/Runtime/RiftWorkshop.cs').read_text(encoding='utf-8')
trait_block=source.split('HeroTrait[] Traits={',1)[1].split('\n        };',1)[0]
traits=[{'name':name,'description':desc.replace('\\n',' / ')} for name,desc in re.findall(r'new HeroTrait\("([^"]+)","([^"]+)"\)',trait_block)]
assert len(traits)==24
roster=read('Assets/Rift/Resources/Rift/canonical-roster.json')['characters']
runtime={
    'result-collection-goal-persists','result-shop-return-never-duplicates-payout','all-twelve-hero-specializations-visible',
    'failed-purchase-write-rolls-back-currency','failed-purchase-write-preserves-disk-save','earned-shards-buy-and-equip-a-real-trait',
    'repeat-purchase-cannot-spend-again','owned-traits-switch-and-reset-without-cost','amulet-purchase-uses-earned-currency',
    'cosmetic-preview-is-free-without-unlocking','style-frame-and-loadout-survive-disk-saving','shop-control-bounds-and-readable-text',
    'saved-expedition-keeps-its-original-equipment','new-expedition-applies-trait-amulet-and-cosmetics','post-purchase-goal-shows-real-remaining-currency',
    'purchased-loadout-plays-with-synchronized-live-impacts',
}
resolutions=[]
for folder,dimensions in (('PlayerSmoke',(1600,900)),('PlayerSmoke1280',(1280,720))):
    report=read(f'BuildLogs/{folder}/report.json');profile=read(f'BuildLogs/{folder}/profile.json')
    assert report['version']==version and report['passed'] and report['workshopPassed'] and not report['errors']
    assert set(report['workshopChecks'])==runtime and (report['screenWidth'],report['screenHeight'])==dimensions
    assert report['workshopEarned']==236 and report['workshopSpent']==profile['workshopSpent']==230 and profile['shards']==6
    assert profile['healthUpgrade']==profile['manaUpgrade']==0 and len(profile['history'])==2
    assert profile['traitUnlocks']==[2]+[0]*11 and profile['equippedTraits']==[2]+[0]*11
    assert profile['styleUnlocks']==profile['equippedStyles']==[1]+[0]*11
    assert profile['amuletUnlocks']==profile['equippedAmulet']==1 and profile['frameUnlocks']==profile['equippedFrame']==1
    assert profile['workshopGoal']==100
    checkpoint=profile['checkpoint']['loadout']
    assert checkpoint['traits']==profile['equippedTraits'] and checkpoint['styles']==profile['equippedStyles'] and checkpoint['amulet']==checkpoint['frame']==1
    assert report['workshopAttackSamples']>=5 and report['workshopPoseMismatches']==0 and report['workshopEffectPeak']<=42
    assert (ROOT/f'BuildLogs/{folder}/report.json').stat().st_mtime >= (ROOT/'BuildLogs/build-result.json').stat().st_mtime
    for capture in ('28-workshop-goal.png','29-result-collection-goal.png','30-trait-unlocked.png','31-amulet-choices.png','32-cosmetic-preview.png','33-cosmetics-equipped.png','34-equipped-expedition.png','35-equipped-live-combat.png'):
        assert capture in report['screenshots'] and (ROOT/f'BuildLogs/{folder}/{capture}').is_file()
    resolutions.append({'width':dimensions[0],'height':dimensions[1],'scenarios':len(runtime),'earned':236,'spent':230,'balance':6,'liveAttackSamples':report['workshopAttackSamples'],'livePoseMismatches':report['workshopPoseMismatches'],'effectPeak':report['workshopEffectPeak']})
playtest=read(f'BuildLogs/workshop-playtest-v{version}.json')
assert playtest['version']==version and len(playtest['runs'])==48
groups=[]
for trait_set in (1,2):
    for amulet in (1,2,3):
        runs=[r for r in playtest['runs'] if r['traitSet']==trait_set and r['amulet']==amulet]
        assert len(runs)==8 and {r['faction'] for r in runs}=={0,1}
        assert all(r['phase']=='Victory' and r['stage']==6 for r in runs)
        groups.append({'traitSet':trait_set,'amulet':amulet,'wins':8,'meanSeconds':round(statistics.mean(r['seconds'] for r in runs),2)})
result={'passed':True,'version':version,'catalogueItems':41,'traits':24,'amulets':3,'heroStyles':12,'portraitFrames':2,'modelChecks':len(new_checks),'runtimeScenarios':len(runtime),'resolutions':resolutions,'equippedCampaigns':48,'campaignGroups':groups,'legacyCompatible':True,'frozenDepartureLoadout':True,'noNativeCurrencyGrants':True,'collectionGoalLoop':True}
(ROOT/'BuildLogs/workshop-review.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
rows='\n'.join(f"| {c['displayName']} | {traits[i*2]['name']} · {traits[i*2]['description']} | {traits[i*2+1]['name']} · {traits[i*2+1]['description']} |" for i,c in enumerate(roster))
groups_text='\n'.join(f"| {g['traitSet']} | {g['amulet']} | {g['wins']}/8 | {g['meanSeconds']:.1f}초 |" for g in groups)
doc=f'''# 인연 공방과 출정 준비 · v{version}

전투에서 얻은 인연 조각을 원하는 특성·부적·외형에 사용하고 다음 출정을 다르게 준비하는 흐름을 추가했습니다. 홈과 전투 결과의 **인연 공방**에서 이용합니다. 기존 캐릭터 12명과 두 영구 강화는 그대로 사용할 수 있습니다.

## 실제 플레이 흐름

1. 공방에서 원하는 항목의 **수집 목표 지정**을 누릅니다. 거점과 결과에 현재 조각/필요 조각이 표시됩니다.
2. 출정과 도전으로 인연 조각을 얻습니다. 완주 기본 보상은 93조각입니다.
3. **해금하고 장착**을 누르면 정확한 비용을 차감하고 영구 보유·장착합니다. 이미 해금한 항목은 무료로 교체합니다.
4. 새 출정에는 준비한 특성·부적·외형이 적용됩니다. 이어하기는 시작할 때 저장한 준비를 유지합니다.

| 수집 항목 | 개수 | 가격 | 장착 규칙 |
|---|---:|---:|---|
| 캐릭터 특성 | 24 · 인물당 2개 | 각각 60조각 | 인물당 1개 또는 기본 특성 |
| 출정 부적 | 3 | 각각 80조각 | 출정 전체에 1개 또는 미장착 |
| 별빛 전투 연출 | 12 · 인물별 | 각각 40조각 | 해당 동료의 타격·소환 색과 빛 파편 |
| 월광/성운 초상화 액자 | 2 | 50/70조각 | 동료 카드의 공통 액자 1개 |

외형은 공방에서 무료로 미리 볼 수 있고 능력치를 바꾸지 않습니다. 해금 전에도 미리보기만 가능하며 비용이나 소유권은 변하지 않습니다. 외형 수집은 이번 버전에서 전투 연출과 액자를 제공합니다.

## 특성의 장점과 대가

| 인물 | 특성 1 | 특성 2 |
|---|---|---|
{rows}

체력·피해·공속·이동·치유·보호막·범위·사거리·경직·밀침·감속·작봉 표식에 실제 적용합니다. 한 인물의 두 특성은 동시에 겹치지 않습니다. 상대 병력에는 아군의 특성을 적용하지 않습니다.

| 출정 부적 | 장점 | 대가 |
|---|---|---|
| 개척의 월석 | 아군 제단 점령 속도 +20% | 제단 공격 버프 12→9초 |
| 되받는 월석 | 절묘한 결계의 각성 +8 | E 보호막 -25% |
| 진격의 월석 | 진격 지속 +2초 | 기본 영력 회복 -0.7/s |

서약과 함께 적용하며 진격/반격 서약 미리보기도 현재 부적을 반영합니다. 특성·부적은 이득과 비용을 비교하는 선택이며 반복 구매로 수치가 계속 쌓이지 않습니다.

## 몰입과 반복 출정

공방은 선택한 동료의 전신·해금 효과·장착 상태·조각 잔액·목표 진행을 한 화면에 보여줍니다. 해금하면 효과음과 완료 문구를 재생하고 다음 출정에 장착합니다. 별빛 연출은 기존 주먹·검격·술법 표현 위에 색과 작은 파편을 더하며, 액자는 전투 카드·도감·편성에 표시합니다.

결과에서 공방을 열고 돌아와도 전투 결과와 지급 재화를 유지합니다. 직접 정한 수집 목표를 거점·결과의 다음 목표로 우선 표시해 **전투 → 조각 획득 → 원하는 해금 → 새 준비로 출정**을 연결합니다. 전투 중 선택 특성, B 편성 창의 부적·특성 수를 확인할 수 있습니다.

## 저장과 검증

- 새 공방 모델 검사 **212개**, 기존 회귀를 포함한 전체 **{len(model['checks'])}개 통과**입니다. 41개 전 항목에 부족 재화·잠금 장착 거부·정확한 1회 차감·재저장 복원을 확인했습니다.
- 24개 특성의 실제 일반/보스 피해·공격 간격·출격 능력치와 치유·추가 광역·사거리·경직·밀침·이동·감속·표식을 검사했습니다.
- 출정 준비와 체크포인트는 깊은 복사로 고정합니다. 공방 변경이 기존 출정에 소급되지 않고 이전 저장은 기본 준비로 이어집니다.
- 두 해상도 **1600×900·1280×720**, 각각 공방 **16개 시나리오**와 기존 기능을 합쳐 **41개 시나리오**를 통과했습니다.
- 실제 두 진영 완주와 도전에서 **236조각**을 벌고 특성 60 + 부적 80 + 별빛 40 + 액자 50 = **230조각**을 지출했습니다. 최종 잔액은 **6조각**이며 보유·장착·목표·새 출정 체크포인트와 함께 디스크에 저장했습니다. QA의 이 흐름에서는 재화를 인위적으로 지급하지 않았습니다.
- 전용 QA 경로에서 쓰기 실패를 만들어 메모리의 구매/차감을 되돌리고 정상 저장 파일이 그대로인지 확인했습니다. 동일 구매 이벤트를 연속 실행해도 추가 차감되지 않습니다.
- 실제 구매한 준비로 일반 속도 12초를 플레이했습니다. 해상도별 조사한 공격 포즈는 {resolutions[0]['liveAttackSamples']} / {resolutions[1]['liveAttackSamples']}건, 불일치는 모두 0이며 효과 수는 상한을 지켰습니다.

특성 두 계열×부적 세 종류×양 진영×네 시드, **48회 모두 6경계 완주**했습니다. 아래는 자동 조작 정책의 결과이며 모든 조합의 난도나 사람의 체감 재미를 보증하지 않습니다.

| 특성 계열 | 부적 번호 | 완주 | 평균 시간 |
|---|---:|---:|---:|
{groups_text}

기본 준비의 기존 40회·추가 전략 48회도 유지해 회귀를 확인합니다. 장기적인 수집 몰입감과 숙련 플레이의 압박은 실제 플레이 반응을 보며 조정할 수 있습니다.

증거: `BuildLogs/workshop-review.json`, `workshop-playtest-v{version}.json`, `model-validation.json`, 두 `PlayerSmoke` 보고서·QA 프로필. 공방/목표/해금/연출/출정 화면은 28–35번 캡처입니다. 실제 PC 창 활성화나 마우스·키보드 제어 없이 숨김 Unity 실행과 내부 UI 이벤트·렌더링을 사용했습니다.
'''
(ROOT/'Docs/WORKSHOP.md').write_text(doc,encoding='utf-8')
print(json.dumps({k:result[k] for k in ('passed','version','catalogueItems','modelChecks','runtimeScenarios','equippedCampaigns')},ensure_ascii=False))
