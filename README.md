# לוח אירועים משפחתי – השרת (ASP.NET Core 8)

מערכת לתיאום אירועים משפחתיים: משפחות, אירועים, הזמנות ואישורי הגעה, עדכונים, קבצים מצורפים ומשימות.
אפשר להתנדב למשימות, והתזכורות נשלחות במייל.
הקליינט (React) נמצא במאגר נפרד: `family-client`.

**המשאב המוגבל** במערכת הוא משימה פנויה באירוע (למשל „להביא מיחם”). כמה בני משפחה עשויים ללחוץ „אני לוקח/ת” באותו רגע,
אבל רק אחד מהם יקבל אותה. השאר מקבלים `409 Conflict` ולא נוצרת הקצאה כפולה (פירוט בהמשך).

---

## ארכיטקטורה

ארבע שכבות. כל שכבה מכירה רק את השכבות שמתחתיה:

```
my-project.API      Controllers, Middleware, JWT, Swagger, NLog, הגדרות, אתחול מסד הנתונים
      │
my-project.Service  לוגיקה עסקית, הרשאות, AutoMapper, JWT, שליחת מיילים, תזכורות
      │
my-project.Data     AppDbContext, Fluent API, Repositories, UnitOfWork, Migrations, Seed
      │
my-project.Core     Entities, DTOs, Enums, Interfaces, Exceptions (בלי חבילות חיצוניות)

my-project.Tests    xUnit + Moq + SQLite בזיכרון + בדיקות HTTP מקצה לקצה
```

- **Repository + Unit of Work:** כל Repository עובד על ה־DbContext של הבקשה, ו־`IUnitOfWork.SaveChangesAsync` שומר הכול יחד.
  ה־UnitOfWork גם מתרגם שגיאות של EF לחריגות של המערכת:
  - התנגשות מקביליות הופכת ל־`ConcurrencyConflictException`.
  - ערך כפול (PostgreSQL 23505) הופך ל־`DuplicateValueException`.
- **ServiceResult:** כישלון צפוי (לא נמצא, אין הרשאה, התנגשות, קלט לא תקין) מוחזר כתוצאה ולא נזרק כחריגה.
  הבקר ממיר אותו לקוד HTTP (200/201/204/400/401/403/404/409) עם גוף `ErrorResponse` אחיד.
- **הרשאות:** כל בדיקות ההרשאה ברמת המשפחה מרוכזות ב־`FamilyAccessService`:
  - חבר משפחה רואה רק את המשפחות שלו.
  - מנהל משפחה, ומארגן האירוע כל עוד הוא חבר במשפחה, יכולים לנהל את האירוע.
  - `SystemAdmin` מוגדר ב־JWT ומשמש לנתיבי `api/admin`.
- **Middleware**, לפי הסדר:
  1. `ExceptionHandling`: כל חריגה שלא טופלה הופכת ל־`ErrorResponse`, בלי לחשוף stack trace.
  2. `CorrelationId`: כותרת `X-Correlation-ID` בכל תגובה ובכל שורת לוג.
  3. `RequestLogging`
  4. HTTPS ו־HSTS (בפרודקשן)
  5. CORS
  6. Authentication
  7. Authorization

## טכנולוגיות

| תחום | חבילה / כלי |
|---|---|
| שרת | ASP.NET Core 8 Web API |
| מסד נתונים | PostgreSQL (Neon) · Npgsql.EntityFrameworkCore.PostgreSQL 8 · EF Core 8 (Code First, Migrations) |
| אימות | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) · גיבוב סיסמאות PBKDF2 (Identity PasswordHasher) |
| מיפוי | AutoMapper 14 |
| לוגים | NLog (`my-project.API/logs`) |
| מיילים | MailKit (SMTP) – כבוי כברירת מחדל |
| תיעוד API | Swagger (Swashbuckle) – בסביבת Development |
| בדיקות | xUnit · Moq · Microsoft.EntityFrameworkCore.Sqlite · Microsoft.AspNetCore.Mvc.Testing |

---

## התקנה והרצה מקומית

דרישות: .NET 8 SDK, Visual Studio 2022 (או `dotnet` CLI), וגישה למסד PostgreSQL (מקומי או Neon).

### 1. סודות (User Secrets – לא נכנסים ל־Git)

הסודות נשמרים רק ב־User Secrets של `my-project.API`. ב־Visual Studio עושים קליק ימני על הפרויקט ובוחרים „Manage User Secrets”, או מריצים מהטרמינל:

```powershell
dotnet user-secrets set "ConnectionStrings:Default" "Host=<host>;Database=<db>;Username=<user>;Password=<password>;SSL Mode=Require" --project my-project.API
dotnet user-secrets set "Jwt:Key" "<מחרוזת אקראית של 32 תווים לפחות>" --project my-project.API

# מנהל המערכת הראשון (נוצר אוטומטית בעלייה הראשונה; לא נשמר בקוד)
dotnet user-secrets set "SeedAdmin:Email" "<admin email>" --project my-project.API
dotnet user-secrets set "SeedAdmin:Password" "<סיסמה חזקה, 8 תווים לפחות>" --project my-project.API
dotnet user-secrets set "SeedAdmin:FullName" "מנהל מערכת" --project my-project.API
```

### 2. הרצה

```powershell
dotnet run --project my-project.API --launch-profile http
```

- Swagger: http://localhost:5248/swagger
- בעלייה הראשונה השרת מריץ לבד את ה־Migrations, כולל נתוני ההדגמה, ויוצר את מנהל המערכת.
  לכן אין צורך בשום פעולה ידנית במסד הנתונים (ראו בהמשך).

### 3. הקליינט

ההוראות נמצאות ב־`family-client/README.md`. מריצים `npm install` ואז `npm run dev:api`, וגולשים ל־http://localhost:5173.

---

## מסד הנתונים: Migrations ו־Seed

| Migration | תוכן |
|---|---|
| `20261018192305_createDB` | כל הטבלאות, המפתחות, האינדקסים והאילוצים |
| `20261019031211_SeedData` | נתוני הדגמה (HasData), ואיפוס מוני ה־Id אחרי השורות שנוספו |

**החלה אוטומטית (ברירת המחדל):** ב־`appsettings.json` מוגדר `"Database": { "MigrateOnStartup": true }`.
בעלייה, השרת מחיל רק Migrations שעדיין לא הוחלו. מה שכבר הוחל מדולג, ולכן הרצה חוזרת לא יוצרת כפילויות.

**החלה ידנית (אפשרות נוספת):** ב־Package Manager Console בוחרים Default project: `my-project.Data`, ופרויקט ההפעלה הוא `my-project.API`:

```powershell
Update-Database
```

**יצירת Migration חדשה:** `Add-Migration <Name>`, עם אותן הגדרות.

### נתוני ההדגמה

לכל משתמשי ההדגמה יש אותה סיסמה: **`Demo1234!`**. זו סיסמת הדגמה בלבד, ובקוד נשמר רק ה־hash שלה.

| משתמש | אימייל | תפקיד |
|---|---|---|
| שרה לוי | `sara@family.demo` | מנהלת „משפחת לוי”, מארגנת אירועים |
| דוד לוי | `david@family.demo` | חבר בשתי המשפחות, מארגן יום הולדת |
| משה לוי | `moshe@family.demo` | חבר משפחה |
| נועה לוי | `noa@family.demo` | חברת משפחה |
| יעל פרידמן | `yael@family.demo` | חברת משפחה |
| רבקה כהן | `rivka@family.demo` | מנהלת „משפחת כהן” (לא רואה את משפחת לוי – בידוד בין משפחות) |
| מנהל מערכת | מתוך `SeedAdmin:Email` | `SystemAdmin` – נוצר מההגדרות בלבד |

הנתונים כוללים 2 משפחות (קודי הצטרפות `LEVI-4821` ו־`COHEN-3307`) ו־4 אירועים ב־2027, שאחד מהם בוטל.
יש גם אישורי הגעה, עדכונים ו־9 משימות, מהן 5 פנויות להתנדבות.

**מנהל המערכת** לא נמצא ב־Seed. `DatabaseInitializer` יוצר אותו בעלייה מתוך `SeedAdmin:*`,
שנקראים מ־User Secrets או ממשתני סביבה: `SeedAdmin__Email` ו־`SeedAdmin__Password`.
- אם המשתמש כבר קיים, הוא נשאר כמו שהוא.
- אם לא הוגדר `SeedAdmin`, לא נוצר מנהל, ונכתבת על כך שורה בלוג.

---

## מקביליות (Optimistic Concurrency) – איך זה עובד

לטבלאות `Events` ו־`FamilyTasks` יש עמודת `Version` (Guid) שמוגדרת `IsConcurrencyToken`.
`AppDbContext.SaveChanges` מחליף אותה ב־Guid חדש בכל עדכון.

1. שני בני משפחה טוענים את אותה משימה פנויה, ושניהם רואים את אותו `Version`.
2. הראשון שומר. EF שולח `UPDATE "FamilyTasks" SET ... WHERE "Id" = @id AND "Version" = @old`. שורה אחת מתעדכנת, ונוצר `Version` חדש.
3. השני שולח את אותה פקודה עם ה־`Version` הישן, והיא מעדכנת 0 שורות. EF זורק `DbUpdateConcurrencyException`.
   ה־UnitOfWork הופך אותה ל־`ConcurrencyConflictException`, וה־`TaskService` מחזיר `409 Conflict` עם ההודעה „מישהו אחר לקח את המשימה הזו הרגע”.
4. בעריכת אירוע או משימה, הקליינט שולח את ה־`Version` שראה (`UpdateTaskRequest.Version`), והוא נכנס לתנאי ה־WHERE.
   אם מישהו אחר שמר בינתיים, מתקבל 409, והקליינט מבקש לטעון מחדש.

אין נעילות במסד הנתונים, ושום בקשה לא ממתינה לאחרת. ההתנגשות מתגלה רק ברגע השמירה.

---

## בדיקות

```powershell
dotnet test
```

| קבוצה | מה נבדק |
|---|---|
| `Services/TaskServiceTests` (Moq) | התנדבות, משימה תפוסה (409), התנגשות מקביליות (409), 404, חבר שמשבץ אחר (403), משתמש ממשפחה אחרת (403), שינוי סטטוס רק על ידי המשובץ |
| `Services/EventServiceTests` (Moq) | ולידציה של תאריכים ומוזמנים, הרשאת מארגן, 409 בעריכה, ביטול |
| `Services/FamilyAccessServiceTests` (Moq) | חברות, מנהל, מארגן שעזב, 404 מול 403 |
| `Services/ParticipantServiceTests` (Moq) | אישור הגעה, איפוס מלווים, ולידציות, בידוד |
| `Services/AuthServiceTests` | הרשמה (hash, אימייל כפול), התחברות (סיסמה שגויה, משתמש חסום), תפקיד ב־JWT, מפתח קצר נדחה |
| `Data/TaskConcurrencyTests` (SQLite) | **שני DbContext נפרדים** על אותה משימה: הראשון מצליח והשני מקבל 409; תרגום החריגה ב־UnitOfWork; Version ישן נדחה |
| `Data/SeedDataTests` (SQLite) | ה־Seed נטען, סיסמת ההדגמה עובדת, אין מנהל מערכת ב־Seed, ה־Seed קבוע |
| `Api/ApiTests` (HTTP) | ה־API האמיתי בזיכרון: 401/403/404/400/409, תפקיד אדמין, מנהל מערכת מההגדרות, בידוד משפחות, `X-Correlation-ID` |
| `Data/PostgresMigrationTests` | מחיל את ה־Migrations על PostgreSQL אמיתי. רץ רק אם הוגדר `TEST_POSTGRES_CONNECTION` (למסד בדיקות **ריק**), ואחרת מדווח Skipped |

---

## הגדרות ואבטחה

- `appsettings.json` לא מכיל אף סוד. מחרוזת החיבור, `Jwt:Key`, `SeedAdmin:Password` וסיסמת ה־SMTP מגיעים מ־User Secrets בפיתוח, וממשתני סביבה בפרודקשן.
- ההגדרות נבדקות בעלייה (`ValidateOnStart`), וחסר בהן עוצר את השרת עם הודעה ברורה. לדוגמה: מפתח JWT קצר מ־32 בתים.
- `Email:Enabled` כבוי כברירת מחדל. במצב הזה המיילים נכתבים ללוג בלבד, ואין שליחה אמיתית עד שמגדירים SMTP ומפעילים את ההגדרה.
- קבצים מצורפים: יש סיומות מותרות ובדיקה של תוכן הקובץ (חתימה). הגודל מוגבל ל־5MB, הקבצים נשמרים בשם אקראי, ונשלחים עם `nosniff`.
- לוגים (NLog): לקונסול ולקובץ `my-project.API/logs/app-<date>.log`. ברמות Debug (רק ב־Development), Information, Warning ו־Error.
  לא נכתבים ללוג סיסמאות, Tokens, מחרוזות חיבור או גוף הבקשה.

---

## JWT ותפקידים

- `POST /api/auth/register` ו־`POST /api/auth/login` מחזירים JWT (HS256, תוקף 60 דקות, בלי Refresh Token).
  הקליינט שולח אותו בכותרת `Authorization: Bearer <token>`. משתמש חסום לא יכול להתחבר.
- ב־Token יש את מזהה המשתמש ואת **תפקיד המערכת** שלו (`Role`). יש שני סוגי תפקידים:

| תפקיד | סוג | מה מותר |
|---|---|---|
| `User` | תפקיד מערכת | כל משתמש רשום: יוצר משפחה או מצטרף בקוד, ורואה רק את המשפחות שלו |
| `Member` | תפקיד במשפחה | צפייה, אישור הגעה, התנדבות למשימות, עדכונים וקבצים |
| `Manager` | תפקיד במשפחה | בנוסף: ניהול בני המשפחה, קוד ההצטרפות ואירועי המשפחה |
| `SystemAdmin` | תפקיד מערכת | ממשק הניהול: `api/admin/*` |

התפקיד במשפחה לא נמצא ב־Token. הוא נבדק בכל בקשה מול המסד (`FamilyAccessService`), כך ששינוי נכנס לתוקף מיד.

## מנהל המערכת (SystemAdmin)

- **יצירה:** החשבון הראשון נוצר רק מההגדרות `SeedAdmin:*` (ראו „התקנה והרצה מקומית”), ולא נמצא בקוד או ב־Seed.
  אפשר למנות מנהלים נוספים מתוך ממשק הניהול.
- **השרת** (`AdminController`, `[Authorize(Roles = "SystemAdmin")]`) – כל משתמש אחר מקבל 403:

| פעולה | נתיב |
|---|---|
| נתונים סטטיסטיים | `GET /api/admin/stats` |
| רשימת משתמשים (חיפוש, מיון, עמודים) | `GET /api/admin/users?search&sortBy&sortDirection&page&pageSize` |
| חסימה / ביטול חסימה / שינוי תפקיד מערכת | `PATCH /api/admin/users/{id}` (אי אפשר לשנות את עצמך – 409) |
| רשימת משפחות (חיפוש, מיון, עמודים) | `GET /api/admin/families?...` |
| פרטי משפחה ובני המשפחה | `GET /api/admin/families/{id}` |
| אירועי המשפחה (קריאה בלבד, עמודים) | `GET /api/admin/families/{id}/events` |
| מחיקת משפחה וכל התוכן שלה | `DELETE /api/admin/families/{id}` |

- **הקליינט:** אחרי התחברות, מנהל מערכת מגיע ל־`/admin` (סקירה), ומשם ל־`/admin/users` ו־`/admin/families`.
  משתמש אחר שמנסה להיכנס מוחזר לאזור האישי. ההסתרה בקליינט היא רק לנוחות – האכיפה היא בשרת.
  ממשק הניהול עובד רק מול השרת האמיתי (מצב API), ולא במצב ההדגמה.

## פרטי התחברות לבודק/ת

- **משתמשי ההדגמה** (מנהלת משפחה, חברי משפחה, משפחה נפרדת): בטבלה „נתוני ההדגמה” למעלה. הסיסמה `Demo1234!` היא סיסמת הדגמה ציבורית בלבד.
- **מנהל המערכת:** הפרטים **לא נמצאים ב־GitHub**.
  - בהרצה מקומית: הבודק/ת מגדיר/ה אימייל וסיסמה משלו/ה ב־User Secrets (`SeedAdmin:Email`, `SeedAdmin:Password`), והחשבון נוצר בעלייה.
  - באתר בענן: הפרטים נמסרים בערוץ פרטי (למשל בטופס ההגשה או בהודעה אישית למורה), ולא בקובץ במאגר.
    מומלץ להשתמש בסיסמה ייעודית שלא משמשת בשום מקום אחר.

## פריסה לענן

השרת מתוכנן ל־Render (Web Service עם ה־`Dockerfile`), הקליינט ל־Render Static Site, ומסד הנתונים ל־PostgreSQL ב־Neon.
ההוראות המלאות, משתני הסביבה והבדיקות אחרי הפרסום: [DEPLOYMENT.md](DEPLOYMENT.md).

## מגבלות ידועות

- **קבצים מצורפים** נשמרים בדיסק של השרת (`uploads`). בענן החינמי הדיסק זמני והקבצים יימחקו. לשימוש קבוע צריך אחסון קבוע.
- **תזכורות** רצות כשירות רקע בתוך השרת. כשהשרת „ישן” (Render החינמי) הן לא רצות, ושליחת מיילים כבויה כברירת מחדל.
- **אין Refresh Token:** אחרי 60 דקות צריך להתחבר מחדש. משתמש שנחסם או שתפקידו שונה ממשיך עם ה־Token הקיים עד שתוקפו פג.
- **AutoMapper 14** – הגרסה האחרונה ברישיון MIT. יש לה אזהרת אבטחה ידועה (NU1903), והיא תועדה ב־`my-project.Service.csproj`.
- **משתמשי ההדגמה** נוצרים בכל מסד חדש (גם בענן) עם סיסמה ידועה.
- **Neon:** החיבור מהמחשב המקומי נחסם בבדיקה האחרונה (סינון רשת). ה־Migrations נבדקו בקוד ובבדיקות SQLite, אבל עדיין לא הוחלו בפועל על Neon.
