using AccountTrackerV2.Models;
using System.Collections.Generic;

namespace AccountTrackerV2.Interfaces
{
    public interface IVendorRepository : IBaseRepository<Vendor>
    {
        void Absorption(int absorbedID, int absorbingID, string userID);
        Vendor Get(int id, string userID);
        int GetCount(string userID);
        bool UserOwnsVendor(int vendorID, string userID);
        int GetID(string name, string userID);
        IList<Vendor> GetList(string userID);
        bool NameExists(Vendor vendor, string userID);
        void CreateDefaults(string userID);
        bool DefaultsExist(string userID);
        decimal GetAmount(int vendorID, string userID);
    }
}