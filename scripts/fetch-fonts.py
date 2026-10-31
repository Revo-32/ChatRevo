"""Fetch unmodified, pinned upstream fonts and their redistribution notices."""
from pathlib import Path
from urllib.request import urlopen
from concurrent.futures import ThreadPoolExecutor
import hashlib, json, zipfile, io

root = Path(__file__).resolve().parents[1]
dest = root / 'src/ChatRevo.Desktop/Assets/Fonts'
jobs = []
def upstream(repo, commit, folder, files, license_file, label):
    base = f'https://raw.githubusercontent.com/{repo}/{commit}/'
    for name in files:
        jobs.append((base + folder + name, dest / label / name))
    jobs.append((base + license_file, dest / label / 'LICENSE.txt'))

upstream('wanteddev/wanted-sans','02c9b822349c188ada95f9e2d90c2ed18f853235',
         'packages/wanted-sans/fonts/ttf/',['WantedSans-Regular.ttf','WantedSans-Bold.ttf'],'OFL.txt','wanted')
upstream('orioncactus/pretendard','7aeb0698819be2b4097dae8ec8fe6a795e5cf3ae',
         'packages/pretendard/dist/public/static/alternative/',['Pretendard-Regular.ttf','Pretendard-Bold.ttf'],'LICENSE','pretendard')
upstream('sun-typeface/SUIT','55118d981336d8fce005eb62888c12c0568ef7b0',
         'fonts/static/ttf/',['SUIT-Regular.ttf','SUIT-Bold.ttf'],'LICENSE','suit')
for folder, files in {
    'ibmplexsanskr':['IBMPlexSansKR-Regular.ttf','IBMPlexSansKR-Bold.ttf'],
    'nanumgothic':['NanumGothic-Regular.ttf','NanumGothic-Bold.ttf'],
    'gowundodum':['GowunDodum-Regular.ttf'],
    'gowunbatang':['GowunBatang-Regular.ttf','GowunBatang-Bold.ttf'],
    'jua':['Jua-Regular.ttf'],
    'dohyeon':['DoHyeon-Regular.ttf'],
}.items():
    upstream('google/fonts','6c1dbe27dcf7d817d4b62cec033ba77fa8e2a5ec',f'ofl/{folder}/',files,f'ofl/{folder}/OFL.txt',folder)

def fetch(job):
    url, path = job
    data = urlopen(url, timeout=60).read()
    if path.suffix == '.ttf' and data[:4] not in (b'\x00\x01\x00\x00', b'OTTO'):
        raise ValueError('Not a font: ' + url)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)
    return {'file':path.relative_to(dest).as_posix(),'source':url,'sha256':hashlib.sha256(data).hexdigest()}

with ThreadPoolExecutor(max_workers=6) as pool:
    manifest = list(pool.map(fetch, jobs))
url = 'https://www.kimm.re.kr/webfont/file_down/id/50997'
with zipfile.ZipFile(io.BytesIO(urlopen(url,timeout=60).read())) as archive:
    for name in ['KIMM_Light.ttf','KIMM_bold.ttf']:
        data = archive.read(name)
        path = dest / 'kimm' / name
        path.parent.mkdir(parents=True,exist_ok=True)
        path.write_bytes(data)
        manifest.append({'file':f'kimm/{name}','source':url,'sha256':hashlib.sha256(data).hexdigest()})
(dest / 'sources.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(f'Downloaded {len(manifest)} original font/license files')
