using Microsoft.AspNetCore.Identity;

namespace QuanLyKTX.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            string[] roles = { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            await CreateOrUpdateUser(userManager, "admin", "Admin@123", "Admin");
            await CreateOrUpdateUser(userManager, "user", "User@123", "User");
        }

        private static async Task CreateOrUpdateUser(
            UserManager<IdentityUser> userManager,
            string username,
            string password,
            string role)
        {
            var user = await userManager.FindByNameAsync(username);

            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = username,
                    Email = $"{username}@ktx.local",
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, password);

                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new Exception($"Không thể tạo tài khoản {username}: {errors}");
                }
            }
            else
            {
                user.Email = $"{username}@ktx.local";
                user.EmailConfirmed = true;

                await userManager.UpdateAsync(user);

                var hasPassword = await userManager.HasPasswordAsync(user);

                if (hasPassword)
                {
                    var removePasswordResult = await userManager.RemovePasswordAsync(user);

                    if (!removePasswordResult.Succeeded)
                    {
                        var errors = string.Join(", ", removePasswordResult.Errors.Select(e => e.Description));
                        throw new Exception($"Không thể xóa mật khẩu cũ của {username}: {errors}");
                    }
                }

                var addPasswordResult = await userManager.AddPasswordAsync(user, password);

                if (!addPasswordResult.Succeeded)
                {
                    var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
                    throw new Exception($"Không thể đặt mật khẩu mới cho {username}: {errors}");
                }
            }

            var roles = await userManager.GetRolesAsync(user);

            foreach (var oldRole in roles)
            {
                if (oldRole != role)
                {
                    await userManager.RemoveFromRoleAsync(user, oldRole);
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}