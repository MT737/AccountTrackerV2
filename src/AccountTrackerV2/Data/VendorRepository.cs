﻿using AccountTrackerV2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountTrackerV2.Interfaces;

namespace AccountTrackerV2.Data
{
    public class VendorRepository : BaseRepository<Vendor>, IVendorRepository
    {
        public VendorRepository(AccountTrackerV2Context context) : base(context)
        {
        }

        /// <summary>
        /// Returns the Vendor entity associated to the passed vendor ID.
        /// </summary>
        /// <param name="id">Int: VendorID associated to the desired vendor.</param>
        /// <returns>Vendor entity with the passed VendorID.</returns>
        public Vendor Get(int id)
        {
            var vendor = Context.Vendors.AsQueryable();

            return vendor
                .Where(v => v.VendorID == id)
                .SingleOrDefault();
        }

        /// <summary>
        /// Returns an IList of vendor entities.
        /// </summary>
        /// <param name="userID">String: UserID for which to get a list of vendors.</param>
        /// <returns>IList of vendor entities.</returns>
        public IList<Vendor> GetList(string userID)
        {
            return Context.Vendors
                .Where(v => v.UserID == userID)
                .OrderBy(v => v.Name)
                .ToList();
        }

        /// <summary>
        /// Returns the number of vendors in the DB.
        /// </summary>
        /// <returns>Int: integer representing the number of vendors in the DB.</returns>
        public int GetCount()
        {
            return Context.Vendors.Count();
        }

        /// <summary>
        /// Calculates the amount of user spending at a given vendor.
        /// </summary>
        /// <param name="vendorID">Int: VendorID for which to determine total spending.</param>
        /// <param name="userID">String: UserID for which to determine total spending.</param>
        /// <returns>Decimal: amount of user spending with the given vendor.</returns>
        public decimal GetAmount(int vendorID, string userID)
        {
            return Context.Transactions
                .Where(t => t.VendorID == vendorID && t.UserID == userID)
                .ToList().Sum(t => t.Amount);
        }

        /// <summary>
        /// Indicates if the user owns the specified vendor.
        /// </summary>
        /// <param name="vendorId">Int: VendorID of the specified vendor.</param>
        /// <param name="userID">String: User's ID.</param>
        /// <returns></returns>
        public bool UserOwnsVendor(int vendorId, string userID)
        {
            return Context.Vendors.Where(v => v.UserID == userID && v.VendorID == vendorId).Any();
        }

        /// <summary>
        /// Determines if the vendor name currently exits in the DB.
        /// </summary>
        /// <param name="vendor">Vendor: vendor entity containing the vendor name to test for existence.</param>
        /// <param name="userID">String: Used to filter the vendor name search to just those owned by the user.</param>
        /// <returns>Bool: True if the vendor name already exists in the DB. False otherwise.</returns>
        public bool NameExists(Vendor vendor, string userID)
        {
            return Context.Vendors
                .Where(v => v.UserID == userID && v.Name.ToLower() == vendor.Name.ToLower() && v.VendorID != vendor.VendorID)
                .Any();
        }

        /// <summary>
        /// Returns the VendorID containing the passed vendor name.
        /// </summary>
        /// <param name="name">String: Vendor name for which to retrieve a VendorID.</param>
        /// <returns>Int: VendorID associated to the passed vendor name.</returns>
        public int GetID(string name)
        {
            return Context.Vendors
                .Where(v => v.Name == name)
                .SingleOrDefault().VendorID;
        }

        /// <summary>
        /// Replaces all Transactions table instances of the aborbedID with the absorbingID.
        /// </summary>
        /// <param name="absorbedID">Int: the vendorID of the vendor being absorbed(deleted).</param>
        /// <param name="absorbingID">Int: the vendorID of the vendor absorbing the absorbed VendorID.</param>
        /// <param name="userID">String: UserID of the owner of the vendors being adjusted.</param>
        public void Absorption(int absorbedID, int absorbingID, string userID)
        {
            //TODO: this works for a small database, but for large scale, this method should be updated to perform a bulk update.
            IQueryable<Transaction> vendorsToUpdate = Context.Transactions
                .Where(v => v.VendorID == absorbedID && v.UserID == userID);

            foreach (Transaction transaction in vendorsToUpdate)
            {
                transaction.VendorID = absorbingID;
            }
            Context.SaveChanges();
        }

    }
}
