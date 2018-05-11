using System.Collections.Generic;
using System.Linq;
using Client.Wpf.Utility;
using Domain.Models;

namespace Client.Wpf.ViewModels
{
    public class UserViewModel
    {
        public UserViewModel(string email, IEnumerable<TypeValueClaim> claims)
        {
            Email = email;
            Claims = claims;
        }
        public string Email { get; }
        public IEnumerable<TypeValueClaim> Claims { get; }

        public bool Is
        {
            get => SessionManager.UserContacts.Any(contact => contact.Email == Email);
        }

        public string FullName => $"{GetClaim(nameof(FirstName))} {GetClaim(nameof(LastName))}";

        public string UserName { 
            get => GetClaim(nameof(UserName));
            set => SetClaim(nameof(UserName), value);
        }

        public string FirstName { 
            get => GetClaim(nameof(FirstName));
            set => SetClaim(nameof(FirstName), value);
        }

        public string LastName { 
            get => GetClaim(nameof(LastName));
            set => SetClaim(nameof(LastName), value);
        }

        public string Picture { 
            get => GetClaim(nameof(Picture));
            set => SetClaim(nameof(Picture), value);
        }

        private string GetClaim(string type)
        {
            return Claims.FirstOrDefault(claim => claim.Type == type)?.Value;
        }
        private void SetClaim(string type, string value)
        {
            var claims = Claims.First(claim => claim.Type == type);
            claims.Value = value;
        }
    }
}