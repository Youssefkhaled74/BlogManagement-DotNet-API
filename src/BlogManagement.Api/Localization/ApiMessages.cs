using System.Globalization;
using System.Text.RegularExpressions;

namespace BlogManagement.Api.Localization;

public static class ApiMessages
{
    private static readonly IReadOnlyDictionary<string, string> Arabic = new Dictionary<string, string>
    {
        ["Resource not found"] = "السجل غير موجود",
        ["Unauthorized"] = "يلزم تسجيل الدخول",
        ["Forbidden"] = "ليست لديك صلاحية",
        ["Database conflict"] = "تعارض في البيانات",
        ["Invalid operation"] = "عملية غير مسموحة",
        ["Unexpected server error"] = "خطأ غير متوقع في الخادم",
        ["An unexpected error occurred."] = "حدث خطأ غير متوقع.",
        ["The operation conflicts with existing database data."] = "تتعارض العملية مع بيانات موجودة بالفعل.",
        ["Invalid credentials."] = "البريد الإلكتروني أو كلمة المرور غير صحيحة.",
        ["Account is suspended or inactive."] = "الحساب موقوف أو غير نشط.",
        ["Email already in use."] = "البريد الإلكتروني مستخدم بالفعل.",
        ["Username already in use."] = "اسم المستخدم مستخدم بالفعل.",
        ["Invalid refresh token."] = "رمز تجديد الجلسة غير صالح.",
        ["Refresh token expired or revoked."] = "انتهت صلاحية رمز تجديد الجلسة أو تم إلغاؤه.",
        ["User not found."] = "المستخدم غير موجود.",
        ["Current password is incorrect."] = "كلمة المرور الحالية غير صحيحة.",
        ["The new password must be different from the current password."] = "يجب أن تختلف كلمة المرور الجديدة عن الحالية.",
        ["You cannot deactivate your own account."] = "لا يمكنك إيقاف حسابك.",
        ["You cannot change your own roles."] = "لا يمكنك تغيير أدوارك.",
        ["One or more roles do not exist."] = "أحد الأدوار المحددة غير موجود.",
        ["Category not found."] = "التصنيف غير موجود.",
        ["Category name already exists."] = "اسم التصنيف موجود بالفعل.",
        ["Cannot delete a category that contains blogs."] = "لا يمكن حذف تصنيف يحتوي على مقالات.",
        ["Blog not found."] = "المقال غير موجود.",
        ["A pending or published blog cannot be edited."] = "لا يمكن تعديل مقال بانتظار المراجعة أو منشور.",
        ["A pending or published blog cannot be deleted."] = "لا يمكن حذف مقال بانتظار المراجعة أو منشور.",
        ["Only the author can submit this blog."] = "كاتب المقال فقط يمكنه إرساله للمراجعة.",
        ["Only draft or rejected blogs can be submitted."] = "يمكن إرسال المسودات أو المقالات المرفوضة فقط للمراجعة.",
        ["Only pending blogs can be reviewed."] = "يمكن مراجعة المقالات التي تنتظر الموافقة فقط.",
        ["Only approved blogs can be published."] = "يمكن نشر المقالات التي تمت الموافقة عليها فقط.",
        ["Only published blogs can be unpublished."] = "يمكن إلغاء نشر المقالات المنشورة فقط.",
        ["Selected category does not exist."] = "التصنيف المحدد غير موجود.",
        ["You cannot manage another user's blog."] = "لا يمكنك تعديل مقال مستخدم آخر.",
        ["You can only manage your own blogs."] = "يمكنك إدارة مقالاتك فقط.",
        ["Role not found."] = "الدور غير موجود.",
        ["Role name already exists."] = "اسم الدور موجود بالفعل.",
        ["Employee not found."] = "الموظف غير موجود.",
        ["Employee number already exists."] = "رقم الموظف موجود بالفعل.",
        ["User is already linked to another employee."] = "المستخدم مرتبط بموظف آخر بالفعل.",
        ["Linked user not found."] = "المستخدم المرتبط غير موجود.",
        ["Choose an image smaller than 5 MB."] = "اختر صورة بحجم أقل من 5 ميجابايت.",
        ["Image exceeds 5 MB."] = "حجم الصورة يتجاوز 5 ميجابايت.",
        ["Only PNG, JPEG, and WebP images are supported."] = "يُسمح بصور PNG وJPEG وWebP فقط.",
        ["Validation failed"] = "تحقق من البيانات المدخلة.",
        ["System roles cannot be renamed."] = "لا يمكن تغيير أسماء أدوار النظام.",
        ["The Admin role must keep every system permission."] = "يجب أن يحتفظ دور المسؤول بجميع صلاحيات النظام.",
        ["System roles cannot be deleted."] = "لا يمكن حذف أدوار النظام.",
        ["Cannot delete a role assigned to users."] = "لا يمكن حذف دور مرتبط بمستخدمين.",
        ["Selected user does not exist."] = "المستخدم المحدد غير موجود.",
        ["Selected user is already linked to an employee."] = "المستخدم المحدد مرتبط بموظف بالفعل.",
        ["Please sign in to continue."] = "سجّل الدخول للمتابعة.",
        ["Authenticated user identifier is missing."] = "جلسة الدخول غير صالحة. سجّل الدخول مرة أخرى.",
        ["Jwt:Issuer is missing."] = "إعداد جهة إصدار تسجيل الدخول غير موجود في الخادم.",
        ["Jwt:Audience is missing."] = "إعداد جمهور تسجيل الدخول غير موجود في الخادم.",
        ["Jwt:Key is missing."] = "مفتاح توقيع تسجيل الدخول غير موجود في الخادم.",
        ["Jwt:Key must be at least 32 bytes."] = "مفتاح توقيع تسجيل الدخول في الخادم يجب أن يكون ٣٢ بايت على الأقل.",
        ["You do not have permission to perform this action."] = "ليس لديك صلاحية لتنفيذ هذه العملية.",
    };

    public static string Translate(string value)
    {
        if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "ar") return value;
        if (Arabic.TryGetValue(value, out var translated)) return translated;
        if (value.StartsWith("Unknown permissions: ", StringComparison.Ordinal))
            return "صلاحيات غير معروفة: " + value["Unknown permissions: ".Length..];
        if (value.StartsWith("Default ", StringComparison.Ordinal) && value.EndsWith(" role is not configured.", StringComparison.Ordinal))
            return "الدور الافتراضي غير مُعدّ في النظام. شغّل تهيئة الأدوار أولًا.";
        return value;
    }

    public static string Validation(string field, string value)
    {
        if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "ar") return value;
        var label = field.Split('.').Last();
        label = label.ToLowerInvariant() switch
        {
            "username" => "اسم المستخدم", "email" => "البريد الإلكتروني", "password" => "كلمة المرور",
            "title" => "العنوان", "content" => "المحتوى", "name" => "الاسم", "description" => "الوصف",
            "firstname" => "الاسم الأول", "lastname" => "اسم العائلة", "currentpassword" => "كلمة المرور الحالية",
            "newpassword" => "كلمة المرور الجديدة", "categoryid" => "التصنيف", "file" => "الصورة", _ => label
        };
        if (value.Contains("required", StringComparison.OrdinalIgnoreCase)) return $"حقل {label} مطلوب.";
        if (value.Contains("e-mail", StringComparison.OrdinalIgnoreCase) || value.Contains("email", StringComparison.OrdinalIgnoreCase)) return "أدخل بريدًا إلكترونيًا صحيحًا.";
        var number = Regex.Match(value, @"\d+");
        if (number.Success && value.Contains("minimum length", StringComparison.OrdinalIgnoreCase)) return $"يجب ألا يقل طول {label} عن {number.Value} أحرف.";
        if (number.Success && value.Contains("maximum length", StringComparison.OrdinalIgnoreCase)) return $"يجب ألا يزيد طول {label} عن {number.Value} أحرف.";
        return $"أدخل قيمة صحيحة لحقل {label}.";
    }
}
