using System.Security.Claims;

namespace Okozukai.Api.Middleware;

/// <summary>
/// JWT クレームからユーザーIDを取り出してリクエストコンテキストに設定する。
/// Auth0 / Azure AD B2C / Firebase など、どのプロバイダーでも
/// "sub" クレームが標準ユーザー識別子として使われる。
/// </summary>
public static class UserContextExtensions
{
    /// <summary>認証済みJWTの "sub" クレームからユーザーIDを取得する</summary>
    public static string GetUserId(this HttpContext context)
    {
        var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub");

        return sub ?? throw new UnauthorizedAccessException("User ID (sub claim) not found in token.");
    }
}
