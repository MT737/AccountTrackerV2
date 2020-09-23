using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountTrackerV2.Areas.Identity.Data;
using AccountTrackerV2.Data;
using AccountTrackerV2.Models;
using AccountTrackerV2.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AccountTrackerV2.Interfaces;
using System.Security.Claims;

namespace AccountTrackerV2.Controllers
{
    public class VendorController : Controller
    {
        private IVendorRepository _vendorRepository = null;
        
        public VendorController(IVendorRepository vendorRepository)
        {
            _vendorRepository = vendorRepository;
        }

        public IActionResult Index()
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //TODO: Add the VMs to services for DI?
            EntityViewModel vm = new EntityViewModel();
            vm.Vendors = _vendorRepository.GetList(userID);

            //If vendor list is 0, then this is likely a new user. Fill the default vendor list.
            if (vm.Vendors.Count == 0)
            {
                _vendorRepository.CreateDefaults(userID);
                vm.Vendors = _vendorRepository.GetList(userID);
            }

            return View(vm);
        }

        public IActionResult Add()
        {   EntityViewModel vm = new EntityViewModel();
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
                //TODO: Test trying to add a vendor by navigating directly to the post without input.            
                
                ValidateVendor(vm, userID);

                if (ModelState.IsValid)
                {
                    //Convert VMVendor to DBVendor
                    Vendor vendor = new Vendor
                    {
                        UserID = userID,
                        Name = vm.EntityOfInterest.Name,
                        IsDisplayed = vm.EntityOfInterest.IsDisplayed,
                        IsDefault = false //User's cannot create default vendors.
                    };                    

                    //Add vendor to the DB
                    _vendorRepository.Add(vendor);

                    TempData["Message"] = "Vendor successfully added.";

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

            //Make sure user owns vendor.
            if (!_vendorRepository.UserOwnsVendor((int)id, userID))
            {
                return NotFound();
            }

            //Get the vendor data
            Vendor vendor = _vendorRepository.Get((int)id, userID);
            
            //Don't allow users to edit default vendors.
            if (!vendor.IsDefault)
            {
                //Convert DBVendor to VMVendor
                EntityViewModel vm = new EntityViewModel();
                vm.EntityOfInterest = new EntityViewModel.Entity
                {
                    EntityID = vendor.VendorID,
                    Name = vendor.Name,
                    IsDisplayed = vendor.IsDisplayed
                };

                return View(vm);
            }

            TempData["Message"] = "Adjustment of default vendor is not allow.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(EntityViewModel vm)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (vm.EntityOfInterest.Name != null)
            {

                //Confirm the user owns the vendor
                if (!_vendorRepository.UserOwnsVendor(vm.EntityOfInterest.EntityID, userID))
                {
                    return NotFound();
                }

                //Account for the possibility that the client finds a way to pass a vendor ID that is default.
                //Can I do this without reaching back out to the DB? 
                if (!_vendorRepository.IsDefault(vm.EntityOfInterest.EntityID, userID))
                {

                    //Validate vendor
                    ValidateVendor(vm, userID);
                                        
                    if (ModelState.IsValid)
                    {
                        //Convert the VMVendor to DBVendor
                        Vendor vendor = new Vendor
                        {
                            VendorID = vm.EntityOfInterest.EntityID,
                            UserID = userID,
                            Name = vm.EntityOfInterest.Name,
                            IsDisplayed = vm.EntityOfInterest.IsDisplayed,
                            IsDefault = false //User's cannot edit default vendors.
                        };

                        //Update the Vendor in the DB
                        _vendorRepository.Update(vendor);

                        TempData["Message"] = "Vendor successfully updated.";

                        return RedirectToAction("Index");
                    }
                }                
            }

            return View(vm);
        }

        public IActionResult Delete(int? id)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (id == null)
            {
                return BadRequest();
            }

            //Confirm the user owns the vendor
            if (!_vendorRepository.UserOwnsVendor((int) id, userID))
            {
                return NotFound();
            }

            //Get the vendor data
            Vendor vendorToDelete = _vendorRepository.Get((int)id, userID);
            
            //Don't allow users to delete a default vendor            
            if (!vendorToDelete.IsDefault)
            {
                //Convert the DBVendor to a VMVendor
                EntityViewModel vm = new EntityViewModel();
                vm.EntityOfInterest = new EntityViewModel.Entity
                {
                    EntityID = vendorToDelete.VendorID,
                    Name = vendorToDelete.Name,
                    IsDisplayed = vendorToDelete.IsDisplayed
                };
                
                //Instantiate an absorption VMVendor
                vm.AbsorptionEntity = new EntityViewModel.Entity();

                //Fill the vendor select list with user owned vendors.
                vm.VendorSelectList = vm.InitVendorSelectList(_vendorRepository, userID);
                
                return View(vm);
            }

            TempData["Message"] = "Deleting default vendor is not allowed.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(EntityViewModel vm)
        {
            bool errorMessageSet = false;
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Check for absorption vendor selection.
            if (vm.AbsorptionEntity.EntityID != 0)
            {
                //Make sure the user owns both the absorbed and absorbing vendors
                if (!_vendorRepository.UserOwnsVendor(vm.EntityOfInterest.EntityID, userID)
                    || !_vendorRepository.UserOwnsVendor(vm.AbsorptionEntity.EntityID, userID))
                {
                    //TODO: Perhaps a more specific message to the user?
                    return NotFound();
                }

                //Convert the VMVendors to DBVendors
                Vendor absorbedVendor = _vendorRepository.Get(vm.EntityOfInterest.EntityID, userID);
                Vendor absorbingVendor = _vendorRepository.Get(vm.AbsorptionEntity.EntityID, userID);

                //Ensure that the deleted vendor is not default.
                if (!absorbedVendor.IsDefault)
                {
                    //Make sure the absorbing vendor and the deleting vendor are not the same.
                    if (absorbedVendor.VendorID != absorbingVendor.VendorID)
                    {
                        //Update all transactions that currently point to the vendor being deleted to instead point to the absorbing vendor.
                        _vendorRepository.Absorption(absorbedVendor.VendorID, absorbingVendor.VendorID, userID);

                        //Delete the vendor to be deleted.
                        _vendorRepository.Delete(absorbedVendor.VendorID);

                        TempData["Message"] = "Vendor successfully deleted.";

                        return RedirectToAction("Index");
                    }
                    SetErrorMessage(vm, "Vendor being deleted and vendor absorbing cannot be the same.", errorMessageSet);
                    errorMessageSet = true;
                }
                SetErrorMessage(vm, "Deleting a default vendor is not allowed.", errorMessageSet);
                errorMessageSet = true;
            }
            SetErrorMessage(vm, "You must select a vendor to absorb transactions related to the vendor being deleted.", errorMessageSet);

            EntityViewModel failureStateVM = new EntityViewModel();

            //TODO: It's possible that the client could adjust the vendor of interest vendor ID to a vendor not owned before posting, which would not be caught by now.
            //TODO: Not a huge deal, as the absorption process will catch this, but it could allow users to see unowned vendors.
            //TODO: Current approach of passing back the passed in VendorVM vendor of interest should resolve this.
            failureStateVM.EntityOfInterest = vm.EntityOfInterest;
            failureStateVM.VendorSelectList = failureStateVM.InitVendorSelectList(_vendorRepository, userID);
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

                ModelState.AddModelError("Vendor", message);
            }
        }

        private void ValidateVendor(EntityViewModel vm, string userID)
        {
            if (_vendorRepository.NameExists(vm, userID))
            {
                ModelState.AddModelError("Name", "The provided vendor name already exists.");
            }
        }
    }
}