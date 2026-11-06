"""Make short evidence previews from real offscreen frames, without changing game art."""
from pathlib import Path
from PIL import Image
import wave
import json,re
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
version=re.search(r'const string Version = "([^"]+)"',(ROOT/'Assets/Rift/Runtime/RiftData.cs').read_text(encoding='utf-8')).group(1)
folder=ROOT/('BuildLogs/ExperienceV'+version.replace('.',''))
report=json.loads((folder/'report.json').read_text(encoding='utf-8'))
assert report['version']==version and report['passed']
started=(folder/'report.json').stat().st_mtime-report['realSeconds']-1
for prefix in ('one','two'):
    files=sorted(p for p in folder.glob(prefix+'-*.png') if p.stat().st_mtime>=started)
    assert len(files)>=15
    frames=[]
    for path in files:
        with Image.open(path) as image:
            frames.append(image.convert('RGB').resize((960,540),Image.Resampling.LANCZOS))
    frames[0].save(folder/('gameplay-'+('1x' if prefix=='one' else '2x')+'.gif'),save_all=True,append_images=frames[1:],duration=100,loop=0,optimize=False)

audio=ROOT/('BuildLogs/Audio-v'+version)
parts=[]
for kind in (0,7,8,9,10,4):
    path=next(audio.glob(f'{kind:02d}-*.wav'))
    with wave.open(str(path),'rb') as stream:
        assert stream.getframerate()==22050 and stream.getnchannels()==1
        parts.append(np.frombuffer(stream.readframes(stream.getnframes()),dtype='<i2'))
        parts.append(np.zeros(int(22050*.24),dtype='<i2'))
with wave.open(str(audio/'Cue-preview.wav'),'wb') as stream:
    stream.setnchannels(1);stream.setsampwidth(2);stream.setframerate(22050);stream.writeframes(np.concatenate(parts).tobytes())
print('Created live gameplay sample GIFs and an unaltered cue-clip sequence.')
