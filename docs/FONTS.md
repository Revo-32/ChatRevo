# 포함 폰트와 출처

기존 더잠실체를 포함해 11개 글꼴을 제공한다. 원본 TTF 파일을 수정하지 않고 앱 내부에서 읽으며, 시스템에 설치하거나 실행 중 외부 폰트 CDN에 접속하지 않는다.

| 글꼴 | 공식 배포 출처 | 동봉 이용 조건 |
|---|---|---|
| 더잠실체 | 롯데마트 | The_Jamsil_LICENSE_Korean.pdf |
| Wanted Sans | https://github.com/wanteddev/wanted-sans | wanted/LICENSE.txt |
| Pretendard | https://github.com/orioncactus/pretendard | pretendard/LICENSE.txt |
| SUIT | https://github.com/sun-typeface/SUIT | suit/LICENSE.txt |
| IBM Plex Sans KR | https://github.com/google/fonts/tree/main/ofl/ibmplexsanskr | ibmplexsanskr/LICENSE.txt |
| 나눔고딕 | https://github.com/google/fonts/tree/main/ofl/nanumgothic | nanumgothic/LICENSE.txt |
| 고운돋움 | https://github.com/google/fonts/tree/main/ofl/gowundodum | gowundodum/LICENSE.txt |
| 고운바탕 | https://github.com/google/fonts/tree/main/ofl/gowunbatang | gowunbatang/LICENSE.txt |
| 배민 주아체 | https://github.com/google/fonts/tree/main/ofl/jua | jua/LICENSE.txt |
| 배민 도현체 | https://github.com/google/fonts/tree/main/ofl/dohyeon | dohyeon/LICENSE.txt |
| 한국기계연구원체 | https://www.kimm.re.kr/webfont | kimm/LICENSE.txt |

출처 – 한국기계연구원, kimm.re.kr. Chat Revo는 한국기계연구원의 후원 또는 제휴를 의미하지 않는다.

`scripts/fetch-fonts.py`는 GitHub 배포본의 고정 커밋에서 글꼴과 라이선스를 가져온다. 한국기계연구원체는 공식 다운로드 주소를 이용한다. 파일별 원본 주소와 SHA-256은 `Assets/Fonts/sources.json`에 기록한다. 주아체·도현체·고운돋움의 굵게 표시는 별도 Bold 파일이 없어 렌더러의 굵기 합성을 사용한다.
