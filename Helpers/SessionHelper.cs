namespace DairyProductApp.Helpers
{
    public static class SessionHelper
    {
        public static string GetUsername(HttpContext context)
            => context.Session.GetString("AdminUsername") ?? "";

        public static string GetRole(HttpContext context)
            => context.Session.GetString("AdminRole") ?? "Admin";

        public static string GetFullName(HttpContext context)
            => context.Session.GetString("AdminFullName") ?? "Admin";

        public static bool IsSuperAdmin(HttpContext context)
            => context.Session.GetString("AdminRole") == "SuperAdmin";
    }
}
