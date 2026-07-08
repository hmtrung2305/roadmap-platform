namespace RoadmapPlatform.Application.DTOs.AuthProviders
{
    /// <summary>
    /// Represents the linking status of a login method for the current user.
    /// </summary>
    /// <remarks>
    /// This DTO is used to show which authentication providers are available,
    /// whether they are already linked to the current account, and what actions
    /// the user can perform on each provider.
    ///
    /// Example providers can include:
    /// - local
    /// - google
    /// - github
    /// .
    /// </remarks>
    public class LoginMethodStatusDto
    {
        /// <summary>
        /// Gets or sets the internal provider key.
        /// </summary>
        /// <remarks>
        /// This value should be stable and used by frontend logic or API routes.
        ///
        /// Example values:
        /// - local
        /// - google
        /// - github
        /// .
        /// </remarks>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user-friendly provider name.
        /// </summary>
        /// <remarks>
        /// This value is intended for display in the UI.
        ///
        /// Example values:
        /// - Email and password
        /// - Google
        /// - GitHub
        /// .
        /// </remarks>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this login method is linked
        /// to the current user's account.
        /// </summary>
        public bool IsLinked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user is allowed to unlink
        /// this login method.
        /// </summary>
        /// <remarks>
        /// This should usually be false when unlinking the provider would leave
        /// the user with no remaining login method.
        /// </remarks>
        public bool CanUnlink { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this login method still requires
        /// email verification.
        /// </summary>
        /// <remarks>
        /// This is mainly useful for local email/password login when the email
        /// has been linked but not verified yet.
        /// </remarks>
        public bool RequiresVerification { get; set; }
    }
}