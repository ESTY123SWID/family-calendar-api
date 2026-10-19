# פריסה לענן – Render (חינמי) + PostgreSQL ב־Neon

> לפי מסמך הדרישות, הפריסה לענן **אינה חובה**. חובה רק שהמערכת תעלה ותרוץ מקומית בלי התערבות ידנית במסד הנתונים.
> המסמך מתאר את הפריסה שתוכננה: השרת כ־Web Service (Docker) ב־Render, הקליינט כ־Static Site ב־Render, ומסד הנתונים ב־Neon.
> **שום דבר עדיין לא פורסם.** אין בקובץ הזה אף סוד אמיתי – כל `<...>` הוא מקום לערך שמזינים רק בלוח הבקרה של Render.

## מבנה המאגרים

| מאגר | תוכן | שירות ב־Render |
|---|---|---|
| השרת | תיקיית הפתרון: `my-project.slnx`, `Dockerfile`, `my-project.API/Core/Data/Service/Tests` | Web Service, Runtime: **Docker** |
| הקליינט | התיקייה `family-client` (מאגר נפרד) | **Static Site** |

## מה כבר מוכן בקוד

| נושא | איך |
|---|---|
| סודות | אף סוד לא נמצא בקבצים. הכול מגיע ממשתני סביבה (טבלה בהמשך) |
| Docker | `Dockerfile` בשורש מאגר השרת. מאזין ב־HTTP על פורט **8080** (`ASPNETCORE_HTTP_PORTS=8080`), `ASPNETCORE_ENVIRONMENT=Production` |
| HTTPS ו־Proxy | Render מסיים את ה־HTTPS ומעביר HTTP לקונטיינר. `Hosting__BehindProxy=true` (כבר מוגדר ב־Dockerfile) מפעיל `UseForwardedHeaders`, כך שהשרת יודע שהבקשה המקורית הייתה HTTPS. `UseHsts` פועל מחוץ ל־Development |
| Migrations | `Database:MigrateOnStartup=true` (ב־`appsettings.json`): בכל עלייה מוחלות רק Migrations שעוד לא הוחלו – בטוח להריץ שוב |
| מנהל מערכת | `SeedAdmin__Email` / `SeedAdmin__Password` יוצרים אותו בעלייה הראשונה. משתמש קיים לא משתנה |
| CORS | רק הכתובות ב־`Cors__AllowedOrigins__N`. בלעדיהן הדפדפן חוסם את הקליינט |
| Swagger | רק ב־Development – לא נחשף בפרודקשן |
| לוגים | NLog כותב לקונסול (מופיע ב־Logs של Render) ולקובץ. בפרודקשן: Information ומעלה (Debug רק ב־Development) |
| מיילים | כבויים (`Email:Enabled=false`) – נכתבים ללוג בלבד |

---

## 1. מסד הנתונים (Neon)

1. בלוח הבקרה של Neon: **Connect** → מעתיקים את פרטי החיבור של ה־branch הראשי.
   מומלץ להשתמש בכתובת **הישירה** (בלי `-pooler` בשם השרת), כי ה־Migrations רצות בעלייה.
2. Neon נותן כתובת בפורמט URI (`postgresql://user:password@host/db?sslmode=require`).
   **Npgsql לא מקבל URI** – יש להמיר לפורמט key=value:

   ```
   Host=<host>;Database=<db>;Username=<user>;Password=<password>;SSL Mode=Require
   ```

3. ה־Migrations יוחלו על ידי השרת עצמו בעלייה הראשונה ב־Render (אין צורך להריץ `Update-Database` מהמחשב,
   שממנו החיבור נחסם בעבר בשגיאה `Received unknown response H for SSLRequest` – סינון רשת מקומי).
   **אין למחוק או ליצור מחדש את המסד.** אם המסד כבר מכיל טבלאות שלא נוצרו מה־Migrations, יש לבדוק לפני העלייה.

## 2. השרת – Web Service ב־Render

**New → Web Service** → מחברים את מאגר השרת:

| שדה | ערך |
|---|---|
| Runtime / Language | `Docker` |
| Root Directory | ריק (שורש המאגר) |
| Dockerfile Path | `./Dockerfile` |
| Docker Build Context Directory | `.` |
| Instance Type | Free |
| Health Check Path | **ריק**. אין בשרת נתיב GET פתוח (כל ה־API דורש התחברות ומחזיר 401), ובלי נתיב Render בודק רק שהפורט פתוח |
| Auto-Deploy | לבחירתכם |

### משתני סביבה (Environment)

ב־.NET, `__` (שני קווים תחתונים) מחליף את `:` בשם ההגדרה. כל השמות נבדקו מול הקוד.

| משתנה | ערך | נקרא ב־ |
|---|---|---|
| `PORT` | `8080` – אומר ל־Render לאיזה פורט להעביר את הבקשות (תואם ל־`ASPNETCORE_HTTP_PORTS` ב־Dockerfile) | Render |
| `ConnectionStrings__Default` | מחרוזת ה־Neon בפורמט key=value (סעיף 1) | `Program.cs` |
| `Jwt__Key` | מחרוזת אקראית חדשה, **32 תווים לפחות** (אחרת השרת לא עולה). לא אותו מפתח של הפיתוח. אפשר ללחוץ Generate ב־Render | `JwtSettings` |
| `SeedAdmin__Email` | אימייל מנהל המערכת הראשון | `DatabaseInitializer` |
| `SeedAdmin__Password` | סיסמה חזקה, 8 תווים לפחות | `DatabaseInitializer` |
| `SeedAdmin__FullName` | אופציונלי (ברירת מחדל: „מנהל מערכת”) | `DatabaseInitializer` |
| `Cors__AllowedOrigins__0` | כתובת הקליינט, למשל `https://<client-name>.onrender.com` – **בלי `/` בסוף** | `AddClientCors` |

כבר מוגדרים ב־Dockerfile ואין צורך להוסיף: `ASPNETCORE_ENVIRONMENT=Production`, `ASPNETCORE_HTTP_PORTS=8080`, `Hosting__BehindProxy=true`.

### בדיקה אחרי העלייה (Logs ב־Render)

אמורות להופיע השורות:

- `Applying 2 migration(s): 20261018192305_createDB, 20261019031211_SeedData` (בעלייה הראשונה בלבד; אחר כך `The database is up to date`)
- `System admin <id> was created from SeedAdmin`
- `Now listening on: http://[::]:8080`

אם השרת נופל בעלייה – השגיאה נכתבת ללוג (`The server stopped because of an unhandled exception`). סיבות נפוצות: מחרוזת חיבור חסרה או בפורמט URI, `Jwt__Key` קצר מדי.

## 3. הקליינט – Static Site ב־Render

**New → Static Site** → מחברים את מאגר הקליינט:

| שדה | ערך |
|---|---|
| Root Directory | ריק (שורש המאגר) |
| Build Command | `npm ci && npm run build:api` |
| Publish Directory | `dist` |
| Environment → `VITE_API_BASE_URL` | `https://<server-name>.onrender.com` – **בלי `/` בסוף** |

- `build:api` בונה עם `--mode api`, כלומר הקובץ `.env.api` קובע `VITE_DATA_MODE=api`: **מצב ההדגמה כבוי** בבנייה הזו
  (נבדק: בקוד הבנוי `dataMode` הוא `"api"`, ומשתנה הסביבה גובר על הכתובת המקומית שב־`.env.api`).
- הכתובת נקבעת **בזמן הבנייה**. שינוי של `VITE_API_BASE_URL` מחייב Deploy חדש של הקליינט.
- **Redirects/Rewrites** (בהגדרות האתר): Source `/*`, Destination `/index.html`, Action **Rewrite**.
  בלי זה, רענון בעמוד כמו `/admin/users` או `/events/1` מחזיר 404.

## 4. חיבור הקליינט לשרת (CORS)

1. מעתיקים את כתובת ה־Static Site (למשל `https://<client-name>.onrender.com`).
2. בשרת: `Cors__AllowedOrigins__0` = הכתובת הזו → Save → השרת עולה מחדש.
3. אם יש כתובת נוספת (דומיין מותאם) – `Cors__AllowedOrigins__1`.

## 5. בדיקות אחרי הפרסום (טרם בוצעו)

- [ ] בלוג השרת: 2 ה־Migrations הוחלו בהצלחה על Neon, ומנהל המערכת נוצר.
- [ ] התחברות כמשתמש הדגמה (`sara@family.demo`) – מגיעים ל־`/home`.
- [ ] התחברות כמנהל המערכת – מגיעים ל־`/admin`, רואים סטטיסטיקות, משתמשים ומשפחות.
- [ ] משתמש רגיל שמנסה `/admin` מוחזר ל־`/home`, וקריאה ישירה ל־`/api/admin/stats` מחזירה 403.
- [ ] רענון דף בנתיב פנימי לא מחזיר 404 ולא מנתק.
- [ ] אין שגיאות CORS בקונסול של הדפדפן.
- [ ] התנדבות למשימה משני דפדפנים במקביל – אחד מקבל 409.
- [ ] העלאת קובץ קטן לאירוע (ראו מגבלה על קבצים בהמשך).

---

## מגבלות המסלול החינמי

| מגבלה | המשמעות לפרויקט |
|---|---|
| **שינה אחרי ~15 דקות בלי בקשות** | השרת נכבה כשאין תנועה. |
| **בקשה ראשונה איטית** | אחרי שינה, העלייה (כולל בדיקת Migrations) לוקחת עד כדקה. Neon החינמי גם הוא משהה את המחשוב אחרי חוסר פעילות, ומוסיף עוד כמה שניות. מומלץ „להעיר” את האתר לפני הצגה. |
| **משימות רקע** | `ReminderBackgroundService` רץ רק כשהשרת ער. בזמן שינה **לא נשלחות תזכורות**, והן יישלחו (אם עוד רלוונטיות) רק בעלייה הבאה. בנוסף המיילים כבויים (`Email:Enabled=false`). |
| **דיסק זמני** | הדיסק של השירות נמחק בכל Deploy, הפעלה מחדש או שינה. |

### ⚠️ קבצים מצורפים נשמרים בדיסק המקומי – בעיה שיש לפתור לפני שימוש קבוע

הקבצים המצורפים לאירועים (תמונות ומסמכים) נשמרים בתיקייה `uploads` של השרת (`LocalFileStorage`).
ב־Render החינמי **הם יימחקו** בפריסה הבאה או אחרי שינה, בעוד שהרשומות שלהם במסד יישארו – כלומר הקישורים יחזירו שגיאה.
להצגה/בדיקה זה עובד, אבל לשימוש קבוע צריך אחסון קבוע (מימוש נוסף של `IFileStorage`, למשל שירות אחסון קבצים בענן).
**לא נוסף שירות כזה – זה דורש החלטה ואישור.**

גם קובצי הלוג בתיקייה `logs` נמחקים; את הלוגים קוראים במסך Logs של Render (הקונסול).

## הערת אבטחה – משתמשי ההדגמה בענן

ה־Migration `SeedData` יוצר גם במסד בענן את משתמשי ההדגמה עם הסיסמה הידועה `Demo1234!` (מתועדת ב־README).
לבדיקה ולהגשה זה מכוון. אם האתר ישמש משפחות אמיתיות – יש לחסום את משתמשי ההדגמה ממסך ניהול המשתמשים (`/admin/users`).
