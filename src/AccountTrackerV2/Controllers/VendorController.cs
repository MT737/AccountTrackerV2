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

            IList<Vendor> vendors = new List<Vendor>();
            vendors = _vendorRepository.GetList(userID);

            return View(vendors);
        }

        public IActionResult Add()
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            Vendor vendor = new Vendor();

            //TODO: Refactor to remove need to pass userID.
            vendor.UserID = userID;
            return View(vendor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(Vendor vendor)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            //Don't allow users to add a default vendor. Shouldn't be allowed by the UI, but making sure here.
            vendor.IsDefault = false;

            if (vendor.Name != null)
            {
                //TODO: Test trying to add a vendor by navigating directly to the page without input.            
                
                ValidateVendor(vendor, userID);

                if (ModelState.IsValid)
                {
                    //Add vendor to the DB
                    _vendorRepository.Add(vendor);

                    TempData["Message"] = "Vendor successfully added.";

                    return RedirectToAction("Index");
                }
            }

            //TODO: Refactor to remove the need to pass userID
            vendor.UserID = userID;
            return View(vendor);
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

            //Don't allow users to edit default vendors.
            Vendor vendor = _vendorRepository.Get((int)id);
            if (!vendor.IsDefault)
            {
                //TODO: Refactor to avoid having to pass UserID.
                vendor.UserID = userID;
                return View(vendor);
            }

            TempData["Message"] = "Adjustment of default vendor is not allow.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Vendor vendor)
        {
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (vendor.Name != null)
            {

                //TODO: Account for the possibility that the user passes a vendor ID that is default?

                //Confirm the user owns the vendor
                if (!_vendorRepository.UserOwnsVendor(vendor.VendorID, userID))
                {
                    return NotFound();
                }

                if (!vendor.IsDefault)
                {

                    //Validate vendor
                    ValidateVendor(vendor, userID);

                    if (ModelState.IsValid)
                    {
                        vendor.UserID = userID;

                        //Update the Vendor in the DB
                        _vendorRepository.Update(vendor);

                        TempData["Message"] = "Vendor successfully updated.";

                        return RedirectToAction("Index");
                    }
                }                
            }

            return View(vendor);
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

            //Don't allow users to delete a default vendor
            //TODO: Refactor for DI?
            Vendor vendorToDelete = _vendorRepository.Get((int)id);
            if (!vendorToDelete.IsDefault)
            {
                //Instantiate a vm to hold vendor to delete, vendor select list, and vendor to absorb.
                ApplicationViewModel vm = new ApplicationViewModel();
                vm.VendorOfInterest = vendorToDelete;
                vm.AbsorptionVendor = new Vendor();
                vm.VendorSelectList = vm.InitVendorSelectList(_vendorRepository, userID);

                //TODO: Refactor to remove the need to pass UserID
                vm.VendorOfInterest.UserID = userID;
                vm.AbsorptionVendor.UserID = userID;

                return View(vm);
            }

            TempData["Message"] = "Deleting default vendor is not allowed.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(ApplicationViewModel vm)
        {
            bool errorMessageSet = false;
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Check for absorption vendor selection.
            if (vm.AbsorptionVendor.VendorID != 0)
            {
                //Make sure the user owns both the absorbed and absorbing vendors
                if (!_vendorRepository.UserOwnsVendor(vm.VendorOfInterest.VendorID, userID)
                    || !_vendorRepository.UserOwnsVendor(vm.AbsorptionVendor.VendorID, userID))
                {
                    //TODO: Perhaps a more specific message to the user?
                    return NotFound();
                }

                Vendor absorbedVendor = _vendorRepository.Get(vm.VendorOfInterest.VendorID);
                Vendor absorbingVendor = _vendorRepository.Get(vm.AbsorptionVendor.VendorID);

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

            ApplicationViewModel failureStateVM = new ApplicationViewModel();

            //TODO: It's possible that the client could adjust the vendor of interest vendor ID to a vendor not owned before posting, which would not be caught by now.
            //TODO: Not a huge deal, as the absorption process will catch this, but it could allow users to see other's vendors.
            failureStateVM.VendorOfInterest = _vendorRepository.Get(vm.VendorOfInterest.VendorID);
            failureStateVM.VendorSelectList = failureStateVM.InitVendorSelectList(_vendorRepository, userID);
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

                ModelState.AddModelError("Vendor", message);
            }
        }

        private void ValidateVendor(Vendor vendor, string userID)
        {
            if (_vendorRepository.NameExists(vendor, userID))
            {
                ModelState.AddModelError("Name", "The provided vendor name already exists.");
            }
        }
    }
}