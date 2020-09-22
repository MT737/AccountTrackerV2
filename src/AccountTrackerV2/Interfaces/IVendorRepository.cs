using AccountTrackerV2.Models;
using System.Collections.Generic;

namespace AccountTrackerV2.Interfaces
{
    public interface IVendorRepository : IBaseRepository<Vendor>
    {
        void Absorption(int absorbedID, int absorbingID, string userID);
        Vendor Get(int id);
        decimal GetAmount(int vendorID, string userID);
        bool UserOwnsVendor(int vendorID, string userID);
        int GetCount();
        int GetID(string name);
        IList<Vendor> GetList(string userID);
        bool NameExists(Vendor vendor, string userID);
    }
}