using AccountTrackerV2.Interfaces;
using AccountTrackerV2.Models;
using AccountTrackerV2.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;
using X.PagedList;

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

        public IActionResult Index(string sortOrder, string currentFilter, string searchName, int? page)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.SortOrder = sortOrder;
            ViewBag.NameSortParam = String.IsNullOrEmpty(sortOrder) ? "Category" : "";

            //TODO: Add VMs to services DI?
            EntityViewModel vm = new EntityViewModel();
            vm.Categories = _categoryRepository.GetList(userID);

            //If categories list is 0, then this is likely a new user. Fill the default categories.
            if (vm.Categories.Count == 0)
            {
                _categoryRepository.CreateDefaults(userID);
                vm.Categories = _categoryRepository.GetList(userID);
            }

            //If user searched for a new name, then start pagenation over
            if (searchName != null)
            {
                page = 1;
            }
            else
            {
                searchName = currentFilter;
            }

            ViewBag.CurrentFilter = searchName;

            //If search name entered, filter on search name.
            if (searchName != null)
            {
                vm.Categories = vm.Categories.Where(c => c.Name == searchName).ToList();
            }

            //Sort as instructed
            switch (sortOrder)
            {
                case "Category":
                    vm.Categories = vm.Categories.OrderByDescending(c => c.Name).ToList();
                    break;

                default:
                    vm.Categories = vm.Categories.OrderBy(c => c.Name).ToList();
                    break;
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            ViewBag.SinglePageCategory = vm.Categories.ToPagedList(pageNumber, pageSize);
            return View();
        }

        public IActionResult Add()
        {            
            //Refactor for DI?
            EntityViewModel vm = new EntityViewModel();
            vm.EntityOfInterest = new EntityViewModel.Entity();
            
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(EntityViewModel vm)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        
            if (vm.EntityOfInterest.Name != null)
            {
                ValidateCategory(vm, userID);

                if (ModelState.IsValid)
                {
                    //Convert VMCategory to DBCategory
                    Category category = new Category
                    {
                        UserID = userID,
                        Name = vm.EntityOfInterest.Name,
                        IsDisplayed = vm.EntityOfInterest.IsDisplayed,
                        IsDefault = false //User's cannot create default categories.
                    };
                    
                    //Add the category to the DB
                    _categoryRepository.Add(category);

                    TempData["Message"] = "Category successfully added.";

                    return RedirectToAction("Index");
                }
            }

            return View(vm);
        }

        public IActionResult Edit(int? id)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (id == null)
            {
                return BadRequest();
            }

            //Confirm the user owns the category
            if (!_categoryRepository.UserOwnsCategory((int)id, userID))
            {
                return NotFound();
            }

            //Get the category data.
            Category category = _categoryRepository.Get((int)id, userID);
            
            //Don't allow users to edit a default category. Should be prevented by the UI, but confirming here. 
            if (!category.IsDefault)
            {
                //Convert the DBCategory to VMCategory
                EntityViewModel vm = new EntityViewModel();
                vm.EntityOfInterest = new EntityViewModel.Entity
                {
                    EntityID = category.CategoryID,
                    Name = category.Name,
                    IsDisplayed = category.IsDisplayed
                };                

                return View(vm);
            }

            TempData["Message"] = "Adjustment of default categories is not allowed.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(EntityViewModel vm)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (vm.EntityOfInterest.Name != null)
            {
                                
                //Confirm the user owns the category
                if (!_categoryRepository.UserOwnsCategory(vm.EntityOfInterest.EntityID, userID))
                {
                    return NotFound();
                }

                //Account for the possibility that the client finds a way to pass category ID that is default.
                //Can I do this without reaching back to the DB?
                if (!_categoryRepository.IsDefault(vm.EntityOfInterest.EntityID, userID))
                {

                    //Validate the category
                    ValidateCategory(vm, userID);

                    if (ModelState.IsValid)
                    {
                        //Convert the VMCategory to DBCategory
                        Category category = new Category
                        {
                            CategoryID = vm.EntityOfInterest.EntityID,
                            UserID = userID,
                            Name = vm.EntityOfInterest.Name,
                            IsDisplayed = vm.EntityOfInterest.IsDisplayed,
                            IsDefault = false //User's cannot edit default categories.
                        };
                        
                        //Update the category in the DB
                        _categoryRepository.Update(category);

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

            //Get the category data
            Category categoryToDelete = _categoryRepository.Get((int)id, userID);

            //Don't allow users to delete a default category.            
            if (!categoryToDelete.IsDefault)
            {
                //Convert the DBCategory to a VMCategory

                EntityViewModel vm = new EntityViewModel();
                vm.EntityOfInterest = new EntityViewModel.Entity
                {
                    EntityID = categoryToDelete.CategoryID,
                    Name = categoryToDelete.Name,
                    IsDisplayed = categoryToDelete.IsDisplayed
                };

                //Instantiate an absorption category
                vm.AbsorptionEntity = new EntityViewModel.Entity();

                //Fill the category select list with user owned categories.
                vm.CategorySelectList = vm.InitCategorySelectList(_categoryRepository, userID);

                return View(vm);
            }

            TempData["Message"] = "Deleting default categories is not allowed.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(EntityViewModel vm)
        {
            bool errorMessageSet = false;
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Check for absorption category selection.
            if (vm.AbsorptionEntity.EntityID != 0)
            {
                //Make sure user owns both the absorbed and absorbing categories
                if (!_categoryRepository.UserOwnsCategory(vm.EntityOfInterest.EntityID, userID) 
                    || !_categoryRepository.UserOwnsCategory(vm.AbsorptionEntity.EntityID, userID))
                {
                    //TODO: Perhaps a more specific message to the user?
                    return NotFound();
                }

                //Convert the VMCategories to DBCategories
                Category absorbedCategory = _categoryRepository.Get(vm.EntityOfInterest.EntityID, userID);
                Category absorbingCategory = _categoryRepository.Get(vm.AbsorptionEntity.EntityID, userID);

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

            EntityViewModel failureStateVM = new EntityViewModel();         

            failureStateVM.EntityOfInterest = vm.EntityOfInterest;
            failureStateVM.CategorySelectList = failureStateVM.InitCategorySelectList(_categoryRepository, userID);
            return View(failureStateVM);
        }

        private void SetErrorMessage(EntityViewModel vm, string message, bool messageSet)
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

        private void ValidateCategory(EntityViewModel vm, string userID)
        {
            if (_categoryRepository.NameExists(vm, userID))
            {
                ModelState.AddModelError("category.Name", "The provided category name already exists.");
            }
        }
    }
}