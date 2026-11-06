"""Review current UI, presentation timing, SFX and strategic pacing; never certify subjective fun."""
from pathlib import Path
import json
import statistics
import wave
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
def read(name):return json.loads((ROOT/name).read_text(encoding='utf-8-sig'))
model=read('BuildLogs/model-validation.json');build=read('BuildLogs/build-result.json');version=model['version']
assert model['passed'] and build['version']==version and build['result']=='Succeeded'
checks={c['name']:c['passed'] for c in model['checks']}
required=(
    'A ready perfect counter is offered in its actual timing window',
    'Low mana never displays a false counter instruction',
    'A shield on cooldown offers its real unavailable state',
    'A charged ultimate is offered when the counter is unavailable',
    'Heavy strikes blades and spells use three distinct hit styles',
    'Ordinary hits cannot preempt a warning or ultimate voice',
    'A warning can preempt a lower priority active sound',
    'An unused voice is used before interrupting an active sound',
    'Simultaneous effects have a bounded shared mix gain',
    'The three hit audio clips have different attack samples',
    'Stun suspends the windup pose together with its attack clock',
    'A suspended attack lands during its actual strike pose',
)+tuple(f'Synthesized cue is audible finite and unclipped {i}' for i in range(12))
assert all(checks.get(c,False) for c in required)
runtime={'twelve-hero-details-fit-backed-strip','summon-cooldown-has-progress-indicator','critical-toast-survives-ordinary-notices','active-controls-stay-on-screen','unavailable-counter-never-shows-press-E'}
resolutions=[]
for name,size in (('PlayerSmoke',(1600,900)),('PlayerSmoke1280',(1280,720))):
    r=read(f'BuildLogs/{name}/report.json')
    assert r['version']==version and r['passed'] and r['feedbackPassed'] and set(r['feedbackChecks'])==runtime
    assert (r['screenWidth'],r['screenHeight'])==size and r['completedCampaigns']==2
    assert (ROOT/f'BuildLogs/{name}/report.json').stat().st_mtime>=(ROOT/'BuildLogs/build-result.json').stat().st_mtime
    for capture in ('26-readable-deck.png','27-counter-unavailable.png'):assert capture in r['screenshots']
    resolutions.append({'width':size[0],'height':size[1],'feedbackChecks':len(runtime),'allRuntimeChecks':len(runtime)+len(r['contentChecks'])+len(r['convenienceChecks'])+len(r['workshopChecks']),'captures':len(r['screenshots'])})

proof_folder='BuildLogs/ExperienceV'+version.replace('.','')
before=read('BuildLogs/ExperienceBefore/report.json');after=read(proof_folder+'/report.json')
timing_finding=read('BuildLogs/ExperienceAfter/report.json')
assert timing_finding['animationHits']==162 and timing_finding['animationMismatches']==14
assert before['passed'] and before['version']=='0.7.0'
assert after['version']==version and after['passed'] and not after['errors'] and after['realSeconds']>=33
assert after['animationHits']>=20 and after['animationMismatches']==0 and after['audioHits']>=20
assert (ROOT/proof_folder/'report.json').stat().st_mtime>=(ROOT/'BuildLogs/build-result.json').stat().st_mtime
def presentation(r):
    result={}
    for key in ('effects','largeEffects','numbers','overlayArea'):
        a=sorted(s[key] for s in r['samples']);result[key]={'max':round(max(a),4),'p95':round(a[int(len(a)*.95)],4)}
    frames=sorted(s['frameMs'] for s in r['samples'] if 3<s['realTime']<11 or 19<s['realTime']<26)
    result['nonCaptureFrameP95Ms']=round(frames[int(len(frames)*.95)],3)
    return result
previous,current=presentation(before),presentation(after)
assert current['effects']['max']<=42 and current['numbers']['max']<=24
assert current['overlayArea']['p95']<previous['overlayArea']['p95'] and current['largeEffects']['max']<previous['largeEffects']['max']

runs=read(f'BuildLogs/experience-model-v{version}.json')['runs'];baseline=read('BuildLogs/experience-model-v0.7.0.json')['runs']
assert len(runs)==48 and runs==baseline, 'Neutral equipment must retain the baseline combat outcomes'
policies={}
for policy in ('human-0.8','human-1.2','human-no-tactics','solo'):
    data=[r for r in runs if r['policy'].startswith('solo-')] if policy=='solo' else [r for r in runs if r['policy']==policy]
    policies[policy]={'runs':len(data),'wins':sum(r['phase']=='Victory' for r in data),'meanSeconds':round(statistics.mean(r['seconds'] for r in data),2),'firstContactMean':round(statistics.mean(r['firstHit'] for r in data),2),'lowestBaseHealth':round(min(r['minBase'] for r in data),2),'stagesOver120Seconds':sum(s>120 for r in data for s in r['stageSeconds'])}
assert policies['human-0.8']['runs']==policies['human-1.2']['runs']==8
assert policies['solo']['runs']==24 and len({r['policy'] for r in runs if r['policy'].startswith('solo-')})==12
assert all(policies[p]['wins']>=6 for p in ('human-0.8','human-1.2'))

audio=[]
for path in sorted((ROOT/f'BuildLogs/Audio-v{version}').glob('[0-9][0-9]-*.wav')):
    with wave.open(str(path),'rb') as stream:
        samples=np.frombuffer(stream.readframes(stream.getnframes()),dtype='<i2').astype(float)/32767
        assert len(samples)>2000 and np.isfinite(samples).all() and np.max(np.abs(samples))<.9
        audio.append({'file':path.name,'peak':round(float(np.max(np.abs(samples))),4),'rms':round(float(np.sqrt(np.mean(samples*samples))),4)})
assert len(audio)==12
for name in ('gameplay-1x.gif','gameplay-2x.gif'):assert (ROOT/proof_folder/name).stat().st_size>10000
result={'passed':True,'version':version,'feedbackModelChecks':len(required),'resolutions':resolutions,'proofFolder':proof_folder,'normalTimeObservation':{'seconds':round(after['realSeconds'],2),'firstHit':round(after['firstHit'],3),'damageEvents':after['hitEvents'],'attackPoseSamples':after['animationHits'],'attackPoseMismatches':after['animationMismatches'],'audioHitPlaybacks':after['audioHits'],'before':previous,'after':current},'preTimingFixFinding':{'report':'BuildLogs/ExperienceAfter/report.json','samples':162,'mismatches':14},'strategyScenarios':len(runs),'policies':policies,'combatOutcomesUnchanged':True,'audioClips':audio,'judgement':{'ui':'reviewed and repaired','impact':'timing and presentation verified','fun':'strategic differences observed; expert base pressure remains low; human enjoyment is unproven'}}
(ROOT/'BuildLogs/experience-review.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
solo=policies['solo'];balanced=policies['human-0.8'];slower=policies['human-1.2']
document=f'''# UI·타격감·재미 점검 · v{version}

현재 판단은 **기본 조작 흐름은 사용 가능하고, 타격 표현에서 실제 문제를 찾아 수정했으며, 전술 선택의 효과는 존재한다**입니다. 숙련 편성이 거점을 전혀 위협받지 않는 구간이 많아 긴장감은 약합니다. 자동 승리율을 재미의 증명으로 해석하지 않았습니다.

## 확인한 문제와 수정

| 관찰 | 수정 | 현재 증거 |
|---|---|---|
| 캐릭터 설명이 밝은 바닥에 섞이고 글자가 작음 | 진한 설명 배경, 18→20 크기, 두 줄로 정보 정리, 카드 준비 막대 | 두 해상도에서 12명 모두 실제 호버·문구 잘림 검사, 출격 재사용 막대 |
| 결계를 쓸 수 없는데 ‘지금 E!’ 표시 | 영력·재사용·궁극기 상태에 맞춘 안내와 준비됐을 때만 강조 | 실제 이른 E 뒤 재사용 중 예고 캡처, 부족 영력·R 대안 모델 검사 |
| 일반 알림이 중요한 보스 안내를 덮음 | 알림 우선순위, 중요 경고 배경, 안내·알림 한 자리 사용 | 실제 우선순위 검사와 보스 예고 화면 |
| 큰 원형 효과가 캐릭터와 위험 범위를 덮음 | 충돌 지점의 짧은 주먹·검격·술법 효과, 궁극기·결계 크기 조절, 효과 42·숫자 24 상한 | 일반 속도 전후 표본과 혼잡 전투 캡처 |
| 경직 중 모션은 끝났는데 나중에 피해가 들어옴 | 경직 중 공격 모션과 준비 시간을 함께 정지, 술법은 준비 충전 후 피해 시점에 짧은 광선 | 수정 전 14 / 162 불일치 → 최종 **{after['animationMismatches']} / {after['animationHits']}**, 전용 경직 회귀 검사 |
| 유닛이 뒤쪽 적을 칠 때 방향과 겹침 순서가 어색함 | 실제 이동·공격 방향, 발 위치 순서로 표시, 피격 시 짧은 반동 | 실제 전투 프레임 시각 검토 |
| 모든 공격이 비슷한 소리이며 치유음이 타격음을 막을 수 있음 | 둔탁한 타격·검격·술법 구분, 독립 치유 간격, 경고·반격 우선 음성, 신호 중 음악 낮춤 | 실제 타격음 경로 **{after['audioHits']}회**, 합성 클립 12종의 PCM·우선순위·합산 음량 검사 |

## UI 점검 범위

홈·명단·훈련·출격 예약·길게 누르기·카드 전환·현재 편성·일시정지·보상 비교/새로고침·저장/이어하기·결과·최근 기록·도전 보상·같은 전장 재도전을 확인했습니다. 실제 버튼/포인터 이벤트와 별도 QA 프로필을 사용했습니다. 키보드 매핑은 소스를 확인했으며 OS 키보드 입력은 사용하지 않았습니다.

**1600×900·1280×720**, 각각 기존 편의 13 + 훈련/저장 7 + UI 5 + 공방 16 = **41개 시나리오**, 양 진영 6경계 완주, 오류 0. 모델 검사는 **{len(model['checks'])}개**입니다. 현재 두 해상도 캡처는 총 **{sum(r['captures'] for r in resolutions)}개**입니다.

## 일반 속도의 타격 표현

전후 각각 약 34초, 실제 Update의 1배속 17초·2배속 17초를 관찰했습니다. 이 과정은 모델 시간을 직접 건너뛰지 않고 0.8초마다 버튼을 선택했습니다. 최종 첫 실제 피해는 **{after['firstHit']:.2f}초**였고 피해 신호 {after['hitEvents']}건을 관측했습니다. 경직·연타·광역 피해를 포함해 조사 가능한 공격 모션 {after['animationHits']}건의 시점이 일치했습니다.

| 지표 | v0.7.0 관찰 | 최종 관찰 |
|---|---:|---:|
| 동시 효과 최대 | {previous['effects']['max']} | {current['effects']['max']} |
| 300px보다 큰 사각 효과 동시 최대 | {previous['largeEffects']['max']} | {current['largeEffects']['max']} |
| 피해·치유 숫자 동시 최대 | {previous['numbers']['max']} | {current['numbers']['max']} |
| 효과 면적 지표의 95백분위 | {previous['overlayArea']['p95']:.4f} | {current['overlayArea']['p95']:.4f} |
| 캡처하지 않은 구간 프레임 95백분위 | {previous['nonCaptureFrameP95Ms']:.3f}ms | {current['nonCaptureFrameP95Ms']:.3f}ms |

면적 지표는 효과 사각형의 면적×알파×잔여 수명을 전투 영역으로 나눈 합계이며 실제 가려진 픽셀 비율이 아닙니다. 숨김 실행과 PNG 저장은 프레임 시간을 바꾸므로 캡처 구간을 제외한 값을 따로 집계했습니다. 이 결과는 해당 PC·해상도의 관찰이며 모든 PC의 성능 보장은 아닙니다.

게임 PCM 클립 12종은 유한한 값·시작 파형·실제 에너지·잘림 없음을 확인했습니다. 치유가 타격 채널을 막지 않으며 일반음이 경고·반격을 빼앗지 않습니다. 동시 음원의 합산 계수를 제한하고 음악을 낮춥니다. QA 프로세스는 소리 출력만 0으로 두고 실제 AudioSource 재생을 실행했습니다. 출력 장치에서의 청취 평가와 사람의 체감 평가는 이번 자동 검증에 포함하지 않았습니다.

## 재미와 난도 판단

기존 40회 실험에 더해 **48회**를 조사했습니다. 균형 편성은 한 번에 한 입력만 선택했고 두 진영·4개 시드씩을 사용했습니다. 단일 인물 반복은 열두 명 모두·2개 시드씩 시험했습니다. 단일 인물 정책도 진격·궁극기·영맥을 사용하므로 단순히 보조 버튼을 금지한 비교가 아닙니다.

| 입력 정책 | 완주 | 평균 종료 시간 | 관찰 |
|---|---:|---:|---|
| 균형 편성 · 0.8초 간격 | {balanced['wins']} / {balanced['runs']} | {balanced['meanSeconds']:.1f}초 | 편성·성장·전술을 선택하면 진행 가능 |
| 균형 편성 · 1.2초 간격 | {slower['wins']} / {slower['runs']} | {slower['meanSeconds']:.1f}초 | 초당 여러 번 입력을 요구하지 않음 |
| 균형 편성 · 전술 미사용 | {policies['human-no-tactics']['wins']} / 8 | {policies['human-no-tactics']['meanSeconds']:.1f}초 | 진격·궁극기·결계의 효과가 큼 |
| 한 인물 반복 · 12명×2회 | {solo['wins']} / {solo['runs']} | {solo['meanSeconds']:.1f}초 | 인물별 편차가 크고 정체가 길어지는 경우 존재 |

평균 종료 시간에는 패배도 포함합니다. 승리 횟수는 자동 정책의 결과이며 이용자 승률이 아닙니다. 균형 편성의 두 입력 간격 모두 최저 거점 체력이 1000이었습니다. **잘 구성한 편성에 대한 압박이 약하고, 일부 단일 인물 전략도 완주하므로 적 전술과 후반 위협의 역할은 더 다듬을 여지가 있습니다.** 조합·중앙 제단·0.65초 반격·세 종류 보상·시드별 대장은 선택의 재료를 제공하지만 장기적인 ‘계속 하고 싶은 재미’는 이 실험으로 확정할 수 없습니다. 16초 합성 음악 반복과 제한된 공격 포즈 역시 완성도 면에서 남은 한계입니다.

특성·부적을 비운 기본 준비의 48개 전략 시나리오는 v0.7.0과 전투 결과가 같습니다. 특성·부적을 장착한 추가 48회와 실제 구매 흐름은 [공방 점검](WORKSHOP.md)에 별도로 기록했습니다. 사람의 첫 플레이에서 목표 이해·경고 인지·지루해지는 시점·재도전 의사를 확인하는 것이 다음 평가에 유용합니다.

## 증거와 미리보기

- `BuildLogs/experience-review.json`, `experience-model-v{version}.json`, `experience-model-v0.7.0.json`
- `BuildLogs/ExperienceBefore/`, `{proof_folder}/`: 원본 캡처와 실제 Update 표본
- `BuildLogs/ExperienceAfter/report.json`: 경직 모션 수정 전에 14 / 162 불일치를 발견한 중간 진단. 현재 합격 근거는 `{proof_folder}/report.json`입니다.
- `{proof_folder}/gameplay-1x.gif`, `gameplay-2x.gif`: 원본 화면의 약 10fps 짧은 미리보기
- `BuildLogs/Audio-v{version}/Cue-preview.wav`: 타격 → 검격 → 술법 → 정밀 반격 → 경고 → 궁극기 순서의 실제 게임 클립
- `BuildLogs/PlayerSmoke*/26-readable-deck.png`, `27-counter-unavailable.png`, `13-counter-window.png`

PC 창 활성화·실제 마우스·키보드 조작 없이 숨김 Unity 실행, 게임 내부 이벤트와 오프스크린 렌더링으로 점검했습니다. 원작별 6명·총 12명, 기존 의상·신규 원화와 출정 저장 형식을 유지합니다.
'''
(ROOT/'Docs/EXPERIENCE.md').write_text(document,encoding='utf-8')
print(json.dumps({'passed':True,'version':version,'modelChecks':len(model['checks']),'uiChecksPerResolution':41,'strategyScenarios':len(runs),'attackPoseMismatches':after['animationMismatches'],'policies':policies},ensure_ascii=False,indent=2))
