namespace my_project.Service.Common;

// User-facing error messages returned in ServiceResult (Hebrew)
internal static class ErrorMessages
{
    // Families and permissions
    public const string FamilyNotFound = "המשפחה לא נמצאה.";
    public const string NotFamilyMember = "אינך חבר במשפחה זו.";
    public const string FamilyManagerOnly = "פעולה זו מותרת למנהל המשפחה בלבד.";
    public const string EventManagerOnly = "פעולה זו מותרת למארגן האירוע או למנהל המשפחה בלבד.";
    public const string JoinCodeNotFound = "קוד ההצטרפות שגוי.";
    public const string AlreadyMember = "כבר קיימת חברות במשפחה זו.";
    public const string MemberNotFound = "המשתמש אינו חבר במשפחה זו.";
    public const string LastManager = "לא ניתן להסיר או להוריד את המנהל האחרון של המשפחה.";
    public const string ConcurrentChangeTryAgain = "הנתונים עודכנו על ידי משתמש אחר באותו רגע. נסו שוב.";

    // Users and authentication
    public const string UserNotFound = "המשתמש לא נמצא.";
    public const string CannotViewUser = "אין לך הרשאה לצפות במשתמש זה.";
    public const string EmailTaken = "כתובת האימייל כבר רשומה במערכת.";
    public const string InvalidCredentials = "האימייל או הסיסמה שגויים.";

    // Events
    public const string EventNotFound = "האירוע לא נמצא.";
    public const string EventCancelled = "האירוע בוטל ולא ניתן לבצע עליו פעולה זו.";
    public const string StartInPast = "מועד ההתחלה חייב להיות בעתיד.";
    public const string EndBeforeStart = "מועד הסיום חייב להיות אחרי מועד ההתחלה.";
    public const string InviteesNotMembers = "ניתן להזמין רק חברי משפחה.";
    public const string EventChangedByOther = "האירוע עודכן על ידי משתמש אחר. יש לטעון אותו מחדש ולנסות שוב.";

    // Participants and updates
    public const string NotInvited = "לא הוזמנת לאירוע זה.";
    public const string PendingIsNotAnswer = "יש לבחור תשובה: מגיע, לא מגיע או אולי.";
    public const string CannotRemoveOrganizer = "לא ניתן להסיר את מארגן האירוע מרשימת המשתתפים.";
    public const string ParticipantNotFound = "המשתתף לא נמצא באירוע.";
    public const string UpdateNotFound = "העדכון לא נמצא.";

    // Tasks
    public const string TaskNotFound = "המשימה לא נמצאה.";
    public const string TaskAlreadyTaken = "המשימה כבר נלקחה.";
    public const string TaskTakenConcurrently = "מישהו אחר לקח את המשימה הזו הרגע. נסו משימה אחרת.";
    public const string TaskChangedByOther = "המשימה עודכנה על ידי משתמש אחר. יש לטעון אותה מחדש ולנסות שוב.";
    public const string VolunteerSelfOnly = "ניתן להתנדב למשימה רק עבור עצמך.";
    public const string AssigneeNotMember = "ניתן לשייך משימה רק לחבר משפחה.";
    public const string TaskAssigneeOrManagerOnly = "פעולה זו מותרת לאחראי על המשימה, למארגן האירוע או למנהל המשפחה בלבד.";
    public const string TaskNotAssigned = "למשימה אין אחראי.";
    public const string OpenStatusViaUnassign = "כדי להחזיר משימה למצב פתוח יש להסיר את האחראי.";

    // Attachments
    public const string AttachmentNotFound = "הקובץ לא נמצא.";
    public const string FileEmpty = "הקובץ ריק.";
    public const string FileTooLarge = "גודל הקובץ חורג מהמותר ({0}MB).";
    public const string FileTypeNotAllowed = "סוג הקובץ אינו נתמך. סוגים מותרים: {0}.";
    public const string FileContentMismatch = "תוכן הקובץ אינו תואם לסוג הקובץ ({0}).";
    public const string AttachmentDeleteForbidden = "רק מי שהעלה את הקובץ, מארגן האירוע או מנהל המשפחה יכולים למחוק אותו.";

    // System admin
    public const string AdminOnly = "פעולה זו מותרת למנהלת המערכת בלבד.";
    public const string CannotChangeOwnAdminAccount = "לא ניתן לחסום את עצמך או להסיר את הרשאות הניהול שלך.";
}
