﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserIdentityMVC.Models;

namespace UserIdentityMVC.Controllers
{
	public class UserRolesController : Controller
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		
		public UserRolesController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
		{
			_userManager = userManager;
			_roleManager = roleManager;
		}

		public async Task<List<string>> GetUserRoles(ApplicationUser user)
		{
			return new List<string>(await _userManager.GetRolesAsync(user));
		}
	
		// get roles user from database 
		public async Task<IActionResult> Index()
		{
			var users = await _userManager.Users.ToListAsync();
			var userRolesViewModel = new List<UserRolesViewModel>();
			// for each user 
			foreach (ApplicationUser user in users) 
			{
				var currentViewModel = new UserRolesViewModel();
				currentViewModel.UserId = user.Id;
				currentViewModel.Email = user.Email;
				currentViewModel.FirstName = user.FirstName;
				currentViewModel.LastName = user.LastName;
				currentViewModel.Roles = await GetUserRoles(user);
				userRolesViewModel.Add(currentViewModel);
			}
			return View(userRolesViewModel);
		}
		
		[Authorize(Roles = "SuperAdmin")]
		[HttpGet]
		public async Task<IActionResult> Manage(string userId)
		{
			ViewBag.userId = userId;
			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
			{
				ViewBag.ErrorMessage = $"User with Id = {userId} could not found.";
				return NotFound();
			}
			ViewBag.UserName = user.UserName;
			var model = new List<ManageUserRolesViewModel>();
			foreach (var role in _roleManager.Roles.ToList())
			{
				var userRolesViewModel = new ManageUserRolesViewModel
				{
					RoleId = role.Id,
					RoleName = role.Name
				};
				if (await _userManager.IsInRoleAsync(user, role.Name))
				{
					userRolesViewModel.IsSelectedRole = true;
				}
				else
				{
					userRolesViewModel.IsSelectedRole = false;
				}

				model.Add(userRolesViewModel);
			}

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Manage(List<ManageUserRolesViewModel> model, string userId)
		{
			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
			{
				return View();
			}
			var roles = await _userManager.GetRolesAsync(user);
			var result = await _userManager.RemoveFromRolesAsync(user, roles);
			if (!result.Succeeded)
			{
				ModelState.AddModelError("", "Cannot remove user existing roles");
				return View(model);
			}
			result = await _userManager.AddToRolesAsync(user, model.Where(x => x.IsSelectedRole).Select(y => y.RoleName));
			if (!result.Succeeded)
			{
				ModelState.AddModelError("", "Cannot add selected roles to user");
				return View(model);
			}
			return RedirectToAction("Index");
		}
	}
}
