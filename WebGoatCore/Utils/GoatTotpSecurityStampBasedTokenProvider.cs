using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OtpNet;
using WebGoatCore.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace WebGoatCore.Utils
{
    public class GoatTotpSecurityStampBasedTokenProvider<TUser> : TotpSecurityStampBasedTokenProvider<TUser>
    where TUser : class
    {
        private readonly NorthwindContext _db;
        
        public DataProtectionTokenProviderOptions Options { get; }
        
        public IDataProtector Protector { get; }

        public string Name => Options.Name;

        public GoatTotpSecurityStampBasedTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<GoatTotpSecurityStampBasedTokenProviderOptions> options,
            NorthwindContext db)
        {
            if (dataProtectionProvider == null)
                throw new ArgumentNullException(nameof(dataProtectionProvider));
            Options = options?.Value ?? new DataProtectionTokenProviderOptions();
            Protector = dataProtectionProvider.CreateProtector(Name ?? "DataProtectorTokenProvider");

            _db = db;
        }

        public override Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
        {
            return Task.FromResult(false);
        }

        public override async Task<string> GetUserModifierAsync(string purpose, UserManager<TUser> manager, TUser user)
        {
            var email = await manager.GetEmailAsync(user);
            return "GoatTotpToken:" + purpose + ":" + email;
        }

        public override async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
        {
            var secretKey = await manager.CreateSecurityTokenAsync(user);
            var totp = new Totp(secretKey, mode: OtpHashMode.Sha512, step: 180, totpSize:8);
            var totpCode = totp.ComputeTotp(DateTime.UtcNow);
            return totpCode; 
        }

        public override async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
        {
            var secretKey = await manager.CreateSecurityTokenAsync(user);
            var totp = new Totp(secretKey, mode: OtpHashMode.Sha512, step: 180, totpSize: 8);
            bool verifiedToken = totp.VerifyTotp(DateTime.UtcNow, token, out long timeWindowUsed, VerificationWindow.RfcSpecifiedNetworkDelay);
            return verifiedToken;

        }

    }

    public class GoatTotpSecurityStampBasedTokenProviderOptions : DataProtectionTokenProviderOptions { }
}
