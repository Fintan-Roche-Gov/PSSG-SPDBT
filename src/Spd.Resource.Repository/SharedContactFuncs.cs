using Spd.Utilities.Dataverse;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;
using ContactEntity = Spd.Utilities.Dataverse.EarlyBound.Contact;
using DataverseContext = Spd.Utilities.Dataverse.EarlyBound.DataverseContext;

namespace Spd.Resource.Repository;
internal static class SharedContactFuncs
{
    public static async Task<ContactEntity?> CreateContact(this DataverseContext context,
        ContactEntity contact,
        Spd_Identity? identity,
        IEnumerable<Spd_Alias> aliases,
        CancellationToken ct)
    {
        context.AddObject(contact);
        if (identity != null)
            identity.Spd_ContactId = contact.ToEntityReference();
        if (aliases != null && aliases.Any())
        {
            foreach (var alias in aliases)
            {
                if (AliasExists(context, alias, contact) == null)
                {
                    context.AddObject(alias);
                    // associate alias to contact
                    alias.Spd_ContactId = contact.ToEntityReference();
                }
            }
        }
        return contact;
    }

    public static async Task<ContactEntity?> UpdateContact(this DataverseContext context,
        ContactEntity existingContact,
        ContactEntity newContact,
        Spd_Identity? identity,
        IEnumerable<Spd_Alias> aliases,
        CancellationToken ct,
        bool isPartialSave = false)
    {
        if (!NameIsSame(existingContact, newContact) && !isPartialSave)
        {
            //put old name to spd_alias
            Spd_Alias newAlias = new Spd_Alias
            {
                Spd_FirstName = existingContact.FirstName,
                Spd_Surname = existingContact.LastName,
                Spd_MiddleName1 = existingContact.Spd_MiddleName1,
                Spd_MiddleName2 = existingContact.Spd_MiddleName2,
                Spd_Source = Spd_Alias_Spd_Source.UserEntered,
            };
            if (AliasExists(context, newAlias, existingContact) == null)
            {
                context.AddObject(newAlias);
                // associate alias to contact
                newAlias.Spd_ContactId = existingContact.ToEntityReference();
            }
        }

        //when we found address is different, need to put current address to previous address.
        if (newContact.Address1_Country != null && newContact.Address1_Line1 != null && !MailingAddressIsSame(existingContact, newContact) && !isPartialSave)
        {
            //put old address to spd_address
            Spd_Address newAddress = CreateAddrFromContact(existingContact, true);
            if (AddressExists(context, newAddress, existingContact) == null)
            {
                context.AddObject(newAddress);
                // associate address to contact
                newAddress.Spd_Contact = existingContact.ToEntityReference();
            }
        }

        //when we found residential address is different, need to put current address to previous address.
        if (newContact.Address2_Country != null && newContact.Address2_Line1 != null && !ResidentialAddressIsSame(existingContact, newContact) && !isPartialSave)
        {
            //put old address to spd_address
            Spd_Address newAddress = CreateAddrFromContact(existingContact, false);
            if (newAddress.Spd_Address1 != null && newAddress.Spd_Country != null && AddressExists(context, newAddress, existingContact) == null)
            {
                context.AddObject(newAddress);
                // associate address to contact
                newAddress.Spd_Contact = existingContact.ToEntityReference();
            }
        }
        //update current contact
        existingContact = UpdateExistingContact(existingContact, newContact);
        context.UpdateObject(existingContact);

        if (aliases.Any())
        {
            foreach (var alias in aliases)
            {
                if (AliasExists(context, alias, existingContact) == null)
                {
                    context.AddObject(alias);
                    // associate alias to contact
                    alias.Spd_ContactId = existingContact.ToEntityReference();
                }
            }
        }

        return existingContact;
    }

    private static Spd_Alias? AliasExists(DataverseContext context, Spd_Alias newAlias, ContactEntity contact)
    {
        var matchingAlias = context.Spd_AliasSet.Where(o =>
           o.Spd_FirstName == newAlias.Spd_FirstName &&
           o.Spd_MiddleName1 == newAlias.Spd_MiddleName1 &&
           o.Spd_MiddleName2 == newAlias.Spd_MiddleName2 &&
           o.Spd_Surname == newAlias.Spd_Surname &&
           o.StateCode != Spd_Alias_StateCode.Inactive &&
           o.Spd_ContactId != null && o.Spd_ContactId.Id == contact.ContactId &&
           o.Spd_Source == Spd_Alias_Spd_Source.UserEntered
       ).FirstOrDefault();
        return matchingAlias;
    }

    private static bool NameIsSame(ContactEntity existed, ContactEntity newOne)
    {
        return (string.Equals(existed.FirstName, newOne.FirstName, StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(existed.LastName, newOne.LastName, StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(existed.Spd_MiddleName1, newOne.Spd_MiddleName1, StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(existed.Spd_MiddleName2, newOne.Spd_MiddleName2, StringComparison.InvariantCultureIgnoreCase));
    }

    private static bool MailingAddressIsSame(ContactEntity existingContact, ContactEntity newContact)
    {
        return (string.Equals(existingContact.Address1_Line1, newContact.Address1_Line1, StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(existingContact.Address1_Line2, newContact.Address1_Line2, StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(existingContact.Address1_City, newContact.Address1_City, StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(existingContact.Address1_Country, newContact.Address1_Country, StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(existingContact.Address1_PostalCode, newContact.Address1_PostalCode, StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(existingContact.Address1_StateOrProvince, newContact.Address1_StateOrProvince, StringComparison.InvariantCultureIgnoreCase));
    }

    private static bool ResidentialAddressIsSame(ContactEntity existingContact, ContactEntity newContact)
    {
        return (string.Equals(existingContact.Address2_Line1, newContact.Address2_Line1, StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(existingContact.Address2_Line2, newContact.Address2_Line2, StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(existingContact.Address2_City, newContact.Address2_City, StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(existingContact.Address2_Country, newContact.Address2_Country, StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(existingContact.Address2_PostalCode, newContact.Address2_PostalCode, StringComparison.InvariantCultureIgnoreCase)
            && string.Equals(existingContact.Address2_StateOrProvince, newContact.Address2_StateOrProvince, StringComparison.InvariantCultureIgnoreCase));
    }
    private static Spd_Address? AddressExists(DataverseContext context, Spd_Address newAddr, ContactEntity contact)
    {
        var matchingAddr = context.Spd_AddressSet.Where(o =>
           o.Spd_Address1 == newAddr.Spd_Address1 &&
           o.Spd_Address2 == newAddr.Spd_Address2 &&
           o.Spd_City == newAddr.Spd_City &&
           o.Spd_Country == newAddr.Spd_Country &&
           o.Spd_ProvinceState == newAddr.Spd_ProvinceState &&
           o.StateCode != Spd_Address_StateCode.Inactive &&
           o.Spd_Contact != null && o.Spd_Contact.Id == contact.ContactId &&
           o.Spd_Type == newAddr.Spd_Type
       ).FirstOrDefault();
        return matchingAddr;
    }

    private static Spd_Address CreateAddrFromContact(ContactEntity c, bool isMailing)
    {
        return new Spd_Address
        {
            Spd_Address1 = isMailing ? c.Address1_Line1 : c.Address2_Line1,
            Spd_Address2 = isMailing ? c.Address1_Line2 : c.Address2_Line2,
            Spd_City = isMailing ? c.Address1_City : c.Address2_City,
            Spd_Country = isMailing ? c.Address1_Country : c.Address2_Country,
            Spd_ProvinceState = isMailing ? c.Address1_StateOrProvince : c.Address2_StateOrProvince,
            Spd_PostalCode = isMailing ? c.Address1_PostalCode : c.Address2_PostalCode,
            Spd_Type = isMailing ? Spd_AddressType.Mailing : Spd_AddressType.Physical
        };
    }

    private static ContactEntity UpdateExistingContact(ContactEntity existingContact, ContactEntity newContact)
    {
        if (newContact.Address1_Country != null && newContact.Address1_Line1 != null && newContact.Address1_City != null)
        {
            existingContact.Address1_City = newContact.Address1_City;
            existingContact.Address1_Country = newContact.Address1_Country;
            existingContact.Address1_Line1 = newContact.Address1_Line1;
            existingContact.Address1_Line2 = newContact.Address1_Line2;
            existingContact.Address1_PostalCode = newContact.Address1_PostalCode;
            existingContact.Address1_StateOrProvince = newContact.Address1_StateOrProvince;
        }

        if (newContact.Address2_Country != null && newContact.Address2_Line1 != null && newContact.Address2_City != null)
        {
            existingContact.Address2_City = newContact.Address2_City;
            existingContact.Address2_Country = newContact.Address2_Country;
            existingContact.Address2_Line1 = newContact.Address2_Line1;
            existingContact.Address2_Line2 = newContact.Address2_Line2;
            existingContact.Address2_PostalCode = newContact.Address2_PostalCode;
            existingContact.Address2_StateOrProvince = newContact.Address2_StateOrProvince;
        }

        existingContact.FirstName = newContact.FirstName ?? string.Empty;
        existingContact.LastName = newContact.LastName ?? string.Empty;
        existingContact.Spd_MiddleName1 = newContact.Spd_MiddleName1 ?? string.Empty;
        existingContact.Spd_MiddleName2 = newContact.Spd_MiddleName2 ?? string.Empty;
        existingContact.Spd_Sex = newContact.Spd_Sex ?? existingContact.Spd_Sex;
        existingContact.Birthdate = newContact.Birthdate ?? existingContact.Birthdate;
        existingContact.Telephone1 = newContact.Telephone1 ?? existingContact.Telephone1;
        existingContact.Spd_BcDriversLicense = newContact.Spd_BcDriversLicense ?? existingContact.Spd_BcDriversLicense;
        existingContact.EmailAddress1 = newContact.EmailAddress1 ?? existingContact.EmailAddress1;
        existingContact.Spd_MentalHealthCondition = newContact.Spd_MentalHealthCondition ?? existingContact.Spd_MentalHealthCondition;
        existingContact.Spd_PeaceOfficer = newContact.Spd_PeaceOfficer ?? existingContact.Spd_PeaceOfficer;
        existingContact.Spd_PeaceOfficerOther = newContact.Spd_PeaceOfficerOther ?? existingContact.Spd_PeaceOfficerOther;
        existingContact.Spd_PeaceOfficerStatus = newContact.Spd_PeaceOfficerStatus ?? existingContact.Spd_PeaceOfficerStatus;
        existingContact.Spd_SelfDisclosure = newContact.Spd_SelfDisclosure ?? existingContact.Spd_SelfDisclosure;
        existingContact.Spd_SelfDisclosureDetails = newContact.Spd_SelfDisclosureDetails ?? existingContact.Spd_SelfDisclosureDetails;
        existingContact.Spd_LastLoggedInLicensingPortal = newContact.Spd_LastLoggedInLicensingPortal ?? existingContact.Spd_LastLoggedInLicensingPortal;
        existingContact.Spd_LastLoggedInScreeningPortal = newContact.Spd_LastLoggedInScreeningPortal ?? existingContact.Spd_LastLoggedInScreeningPortal;
        existingContact.Spd_Height = newContact.Spd_Height ?? existingContact.Spd_Height;
        existingContact.Spd_Weight = newContact.Spd_Weight ?? existingContact.Spd_Weight;
        existingContact.Spd_EyeColour = newContact.Spd_EyeColour ?? existingContact.Spd_EyeColour;
        existingContact.Spd_HairColour = newContact.Spd_HairColour ?? existingContact.Spd_HairColour;
        return existingContact;
    }
}
