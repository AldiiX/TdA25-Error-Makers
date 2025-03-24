# TdA25 - Error Makers

### Tým
- [**AldiiX** - Stanislav Škudrna](https://stanislavskudrna.cz)
- [**DGKuba657** - Jakub Funk](https://github.com/DGKuba657)
- [**Wedralek** - Pavel Andrle](https://github.com/Wedralda)

> Soutěžíme za [Střední Školu EDUCHEM, a.s.](https://educhem.cz)  &nbsp;&nbsp;&nbsp;:))

---

### Jak aplikace vypadala v nominačním kole?
![s1](/assets/1.png)
![s2](/assets/2.png)
![s3](/assets/3.png)
![s4](/assets/4.png)

---

### Použité technologie
- **Frontend**: ![Vue.js](https://img.shields.io/badge/vuejs-%2335495e.svg?style=for-the-badge&logo=vuedotjs&logoColor=%234FC08D)
- **Backend**: ![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white) ![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)
- **Databáze**: ![MySQL](https://img.shields.io/badge/mysql-4479A1.svg?style=for-the-badge&logo=mysql&logoColor=white)
- **Styly**: ![CSS](https://img.shields.io/badge/css3-%231572B6.svg?style=for-the-badge&logo=css3&logoColor=white) ![SASS](https://img.shields.io/badge/SASS-hotpink.svg?style=for-the-badge&logo=SASS&logoColor=white)

---

### Jak spustit aplikaci

1. **Naklonujte repozitář**:
   ```bash
   git clone https://github.com/AldiiX/TdA25-Error-Makers.git
   cd TdA25-Error-Makers
   ```

2. **Instalace dockeru**:
    - Instalace WSL (pokud nemáte):
      ```bash
      $ wsl --install
      ```
    - Instalace Docker Desktop:
        - [Windows](https://docs.docker.com/docker-for-windows/install/)
        - [Mac](https://docs.docker.com/docker-for-mac/install/)
        - [Linux](https://docs.docker.com/engine/install/)
   ####
3. **Spuštění aplikace**:
    - Buildnutí docker kontejneru:
      ```bash
      $ docker build . -t tda25-error-makers
      ```
    - Spuštění docker kontejneru:
      ```bash
      $ docker run --name tda25-error-makers -p 80:80 tda25-error-makers
      ```
    - Vypnutí a smazání kontejneru:
      ```bash
        $ docker stop tda25-error-makers && docker rm tda25-error-makers
      ```

4. **Přístup**:
    - Otevřete prohlížeč a přejděte na `http://localhost:80`.

---

### Pravidla commitování (předpony)
- `FEAT` – přidána nová funkce
- `FIX` – oprava chyby
- `CHORE` – změny nesouvisející s opravou nebo funkcí, které nemodifikují src nebo test soubory (např. aktualizace závislostí)
- `REFACTOR` – refaktorizace kódu, která neopravuje chybu ani nepřidává funkci
- `DOCS` – aktualizace dokumentace, jako je README nebo jiné markdown soubory
- `STYLE` – změny, které neovlivňují význam kódu, obvykle souvisejí s formátováním kódu (např. mezery, chybějící středníky atd.)
- `TEST` – přidání nových nebo oprava stávajících testů
- `PERF` – vylepšení výkonu
- `CI` - změny týkající se kontinuální integrace
- `BUILD` – změny, které ovlivňují systém sestavení nebo externí závislosti
- `REVERT` – návrat k předchozímu commitu