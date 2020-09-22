using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AccountTrackerV2.Areas.Identity.Data;
using AccountTrackerV2.Data;
using AccountTrackerV2.Models;
using AccountTrackerV2.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AccountTrackerV2.Interfaces;

namespace AccountTrackerV2.Controllers
{
    /// <summary>
    /// Controller for the "Category" section of the website.
    /// </summary>
    public class CategoryController : Controller
    {
        private ICategoryRepository _categoryRepository = null;
        
        public CategoryController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public IActionResult Index()
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //TODO: Refactor for DI?
            IList<Category> categories = new List<Category>();        
            categories = _categoryRepository.GetList(userID);
            return View(categories);
        }

        public IActionResult Add()
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Refactor for DI?
            ApplicationViewModel vm = new ApplicationViewModel();
            vm.CategoryOfInterest = new Category();
            //TODO: refactor to remove the need to pass userID
            vm.CategoryOfInterest.UserID = userID;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(ApplicationViewModel vm)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Don't allow users to add a default category. 
            vm.CategoryOfInterest.IsDefault = false;

            if (vm.CategoryOfInterest.Name != null)
            {
                ValidateCategory(vm.CategoryOfInterest, userID);

                if (ModelState.IsValid)
                {
                    vm.CategoryOfInterest.UserID = userID;

                    //Add the category to the DB
                    _categoryRepository.Add(vm.CategoryOfInterest);

                    TempData["Message"] = "Category successfully added.";

                    return RedirectToAction("Index");
                }
            }

            return View(vm);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Confirm the user owns the category
            if (!_categoryRepository.UserOwnsCategory((int)id, userID))
            {
                return NotFound();
            }

            //Don't allow users to edit a default category. Should be prevented by the UI, but confirming here. 
            Category category = _categoryRepository.Get((int)id);
            if (!category.IsDefault)
            {
                //TODO: Refactor for DI?
                ApplicationViewModel vm = new ApplicationViewModel();
                vm.CategoryOfInterest = category;
                
                //TODO: Refactor to remove the need to pass user ID to the view.
                vm.CategoryOfInterest.UserID = userID;

                return View(vm);
            }

            TempData["Message"] = "Adjustment of default categories is not allowed.";

            return RedirectToAction("Index");
        }

        //TODO: Don't need VM for this, as only a category object is required. Consider simplifying this and the ViewModel.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ApplicationViewModel vm)
        {
            if (vm.CategoryOfInterest.Name != null)
            {

                var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

                //TODO: Account for the possibility that the user passes a cat ID that is default?

                //Confirm the user owns the category
                if (!_categoryRepository.UserOwnsCategory(vm.CategoryOfInterest.CategoryID, userID))
                {
                    return NotFound();
                }

                if (!_categoryRepository.Get(vm.CategoryOfInterest.CategoryID).IsDefault)
                {

                    //Validate the category
                    ValidateCategory(vm.CategoryOfInterest, userID);

                    if (ModelState.IsValid)
                    {
                        vm.CategoryOfInterest.UserID = userID;

                        //Update the category in the DB
                        _categoryRepository.Update(vm.CategoryOfInterest);

                        TempData["Message"] = "Category successfully updated.";

                        return RedirectToAction("Index");
                    }
                }
                
                TempData["Message"] = "Adjustment of default categories is not allowed.";

                return RedirectToAction("Index");
            }

            return View(vm);
        }

        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Confirm the user owns the category
            if (!_categoryRepository.UserOwnsCategory((int) id, userID))
            {
                return NotFound();
            }

            //Don't allow users to delete a default category.
            //TODO: Refactor for DI?
            Category category = new Category();
            category = _categoryRepository.Get((int)id);

            if (!category.IsDefault)
            {
                ApplicationViewModel vm = new ApplicationViewModel();
                vm.CategoryOfInterest = category;
                vm.AbsorptionCategory = new Category();
                vm.CategorySelectList = vm.InitCategorySelectList(_categoryRepository, userID);

                //TODO: Refactor to remove the need to pass userid
                vm.AbsorptionCategory.UserID = userID;
                vm.CategoryOfInterest.UserID = userID;

                return View(vm);
            }

            TempData["Message"] = "Deleting default categories is not allowed.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(ApplicationViewModel vm)
        {
            bool errorMessageSet = false;
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Check for absorption category selection.
            if (vm.AbsorptionCategory.CategoryID != 0)
            {
                //Make sure user owns both the absorbed and absorbing categories
                if (!_categoryRepository.UserOwnsCategory(vm.CategoryOfInterest.CategoryID, userID) 
                    || !_categoryRepository.UserOwnsCategory(vm.AbsorptionCategory.CategoryID, userID))
                {
                    //TODO: Perhaps a more specific message to the user?
                    return NotFound();
                }

                Category absorbedCategory = _categoryRepository.Get(vm.CategoryOfInterest.CategoryID);
                Category absorbingCategory = _categoryRepository.Get(vm.AbsorptionCategory.CategoryID);

                //Ensure that the deleted category is not default.
                if (!absorbedCategory.IsDefault)
                {
                    //Make sure the absorbing category and the deleting category are not the same.
                    if (absorbedCategory.CategoryID != absorbingCategory.CategoryID)
                    {
                        //Update all transactions that currently point to the category being deleted to instead point to the absorbing category.
                        _categoryRepository.Absorption(absorbedCategory.CategoryID, absorbingCategory.CategoryID, userID);

                        //Delete the category to be deleted.
                        _categoryRepository.Delete(absorbedCategory.CategoryID);

                        TempData["Message"] = "Category successfully deleted.";

                        return RedirectToAction("Index");
                    }
                    SetErrorMessage(vm, "Category being deleted and category absorbing cannot be the same.", errorMessageSet);
                    errorMessageSet = true;
                }
                SetErrorMessage(vm, "Deleting a default category is not allowed.", errorMessageSet);
                errorMessageSet = true;
            }
            SetErrorMessage(vm, "You must select a category to absorb transactions related to the category being deleted.", errorMessageSet);

            ApplicationViewModel failureStateVM = new ApplicationViewModel();
            
            //TODO: It's possible that the client could adjust the category of interest category ID to a category not owned before posting, which would not be caught by now.
            //TODO: Not a huge deal, as the absorption process will catch this, but it could allow users to see other's categories.
            failureStateVM.CategoryOfInterest = _categoryRepository.Get(vm.CategoryOfInterest.CategoryID);
            failureStateVM.CategorySelectList = failureStateVM.InitCategorySelectList(_categoryRepository, userID);
            return View(failureStateVM);
        }

        private void SetErrorMessage(ApplicationViewModel vm, string message, bool messageSet)
        {
            //If there's already an error message, don't do anything.
            if (!messageSet)
            {
                foreach (var modelValue in ModelState.Values)
                {
                    modelValue.Errors.Clear();
                }

                ModelState.AddModelError("Category", message);
            }
        }

        private void ValidateCategory(Category category, string userID)
        {
            if (_categoryRepository.NameExists(category, userID))
            {
                ModelState.AddModelError("category.Name", "The provided category name already exists.");
            }
        }
    }
}