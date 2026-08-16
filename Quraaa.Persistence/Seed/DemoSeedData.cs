using Quraaa.Domain.User.Enums;

namespace Quraaa.Persistence.Seed;

/// <summary>
/// Stable identifiers and credentials for the opt-in development demo dataset.
/// None of these values are production credentials.
/// </summary>
public static class DemoSeedData
{
    public const string UserPassword = "User@12345";
    public const string LibraryPassword = "Library@12345";
    public const string SuperAdminPassword = "Admin@12345";

    public const string SuperAdminPhoneNumber = "+963990000001";
    public const string MainBuyerPhoneNumber = "+963912345678";
    public const string SellerPhoneNumber = "+963912345679";
    public const string CheckoutBuyerPhoneNumber = "+963912345680";
    public const string ReporterOnePhoneNumber = "+963912345681";
    public const string ReporterTwoPhoneNumber = "+963912345682";
    public const string ReporterThreePhoneNumber = "+963912345683";

    public static readonly Guid SuperAdminUserId = Guid.Parse("33333333-3333-3333-3333-333333333301");
    public static readonly Guid MainBuyerUserId = Guid.Parse("33333333-3333-3333-3333-333333333302");
    public static readonly Guid SellerUserId = Guid.Parse("33333333-3333-3333-3333-333333333303");
    public static readonly Guid CheckoutBuyerUserId = Guid.Parse("33333333-3333-3333-3333-333333333304");
    public static readonly Guid ReporterOneUserId = Guid.Parse("33333333-3333-3333-3333-333333333305");
    public static readonly Guid ReporterTwoUserId = Guid.Parse("33333333-3333-3333-3333-333333333306");
    public static readonly Guid ReporterThreeUserId = Guid.Parse("33333333-3333-3333-3333-333333333307");

    public static IReadOnlyList<DemoUserDefinition> Users { get; } =
    [
        new(
            SuperAdminUserId,
            SuperAdminPhoneNumber,
            SuperAdminPassword,
            "Demo",
            "Super Admin",
            Gender.Male,
            Role.SuperAdmin,
            [Role.SuperAdmin.ToString()]),
        new(
            MainBuyerUserId,
            MainBuyerPhoneNumber,
            UserPassword,
            "Omar",
            "Darwish",
            Gender.Male,
            Role.User,
            [Role.User.ToString()]),
        new(
            SellerUserId,
            SellerPhoneNumber,
            UserPassword,
            "Lina",
            "Haddad",
            Gender.Female,
            Role.User,
            [Role.User.ToString()]),
        new(
            CheckoutBuyerUserId,
            CheckoutBuyerPhoneNumber,
            UserPassword,
            "Kareem",
            "Nasser",
            Gender.Male,
            Role.User,
            [Role.User.ToString()]),
        new(
            ReporterOneUserId,
            ReporterOnePhoneNumber,
            UserPassword,
            "Noor",
            "Mahmoud",
            Gender.Female,
            Role.User,
            [Role.User.ToString()]),
        new(
            ReporterTwoUserId,
            ReporterTwoPhoneNumber,
            UserPassword,
            "Sara",
            "Khalil",
            Gender.Female,
            Role.User,
            [Role.User.ToString()]),
        new(
            ReporterThreeUserId,
            ReporterThreePhoneNumber,
            UserPassword,
            "Yazan",
            "Ali",
            Gender.Male,
            Role.User,
            [Role.User.ToString()]),
    ];

    public static class Authors
    {
        public static readonly Guid RadwaAshour = Guid.Parse("66666666-6666-6666-6666-666666666601");
        public static readonly Guid AhmedKhaledTowfik = Guid.Parse("66666666-6666-6666-6666-666666666602");
        public static readonly Guid IbnKhaldun = Guid.Parse("66666666-6666-6666-6666-666666666603");
        public static readonly Guid RobertMartin = Guid.Parse("66666666-6666-6666-6666-666666666604");
        public static readonly Guid FrankHerbert = Guid.Parse("66666666-6666-6666-6666-666666666605");
        public static readonly Guid JamesClear = Guid.Parse("66666666-6666-6666-6666-666666666606");
        public static readonly Guid AndrewHunt = Guid.Parse("66666666-6666-6666-6666-666666666607");
        public static readonly Guid DemoInactive = Guid.Parse("66666666-6666-6666-6666-666666666608");
        public static readonly Guid AntoineDeSaintExupery = Guid.Parse("66666666-6666-6666-6666-666666666609");
    }

    public static class Books
    {
        public static readonly Guid Granada = Guid.Parse("77777777-7777-7777-7777-777777777701");
        public static readonly Guid Utopia = Guid.Parse("77777777-7777-7777-7777-777777777702");
        public static readonly Guid Muqaddimah = Guid.Parse("77777777-7777-7777-7777-777777777703");
        public static readonly Guid CleanCode = Guid.Parse("77777777-7777-7777-7777-777777777704");
        public static readonly Guid Dune = Guid.Parse("77777777-7777-7777-7777-777777777705");
        public static readonly Guid AtomicHabits = Guid.Parse("77777777-7777-7777-7777-777777777706");
        public static readonly Guid PragmaticProgrammer = Guid.Parse("77777777-7777-7777-7777-777777777707");
        public static readonly Guid ModerationFlagged = Guid.Parse("77777777-7777-7777-7777-777777777708");
        public static readonly Guid ModerationHidden = Guid.Parse("77777777-7777-7777-7777-777777777709");
        public static readonly Guid LePetitPrince = Guid.Parse("77777777-7777-7777-7777-777777777710");
    }

    public static class Listings
    {
        public static readonly Guid GranadaLibrary = Guid.Parse("88888888-8888-8888-8888-888888888801");
        public static readonly Guid CleanCodeLibraryOne = Guid.Parse("88888888-8888-8888-8888-888888888802");
        public static readonly Guid CleanCodeLibraryTwo = Guid.Parse("88888888-8888-8888-8888-888888888803");
        public static readonly Guid DuneDigital = Guid.Parse("88888888-8888-8888-8888-888888888804");
        public static readonly Guid DunePhysical = Guid.Parse("88888888-8888-8888-8888-888888888805");
        public static readonly Guid AtomicHabitsOutOfStock = Guid.Parse("88888888-8888-8888-8888-888888888806");
        public static readonly Guid MuqaddimahRemoved = Guid.Parse("88888888-8888-8888-8888-888888888807");
        public static readonly Guid UtopiaUser = Guid.Parse("88888888-8888-8888-8888-888888888808");
        public static readonly Guid PragmaticProgrammerUser = Guid.Parse("88888888-8888-8888-8888-888888888809");
        public static readonly Guid FlaggedBookLibrary = Guid.Parse("88888888-8888-8888-8888-888888888810");
        public static readonly Guid HiddenBookLibrary = Guid.Parse("88888888-8888-8888-8888-888888888811");
        public static readonly Guid HiddenBookLibraryTwo = Guid.Parse("88888888-8888-8888-8888-888888888812");
        public static readonly Guid LePetitPrinceLibrary = Guid.Parse("88888888-8888-8888-8888-888888888813");
    }

    public static class CheckoutMarkers
    {
        public const string CompletedDigital = "cs_demo_completed_digital_v1";
        public const string CompletedMixed = "cs_demo_completed_mixed_v1";
        public const string ProcessingUserSale = "cs_demo_processing_user_sale_v1";
        public const string PendingCheckout = "cs_demo_pending_checkout_v1";
        public const string FailedPayment = "cs_demo_failed_payment_v1";
        public const string CancelledOrder = "cs_demo_cancelled_order_v1";
        public const string ExpiredOrder = "cs_demo_expired_order_v1";
    }

    public static class OrderNumbers
    {
        public const string CompletedDigital = "DEMO-1001";
        public const string CompletedMixed = "DEMO-1002";
        public const string ProcessingUserSale = "DEMO-1003";
        public const string PendingCheckout = "DEMO-1004";
        public const string FailedPayment = "DEMO-1005";
        public const string CancelledOrder = "DEMO-1006";
        public const string ExpiredOrder = "DEMO-1007";
    }

    public static Guid GetLibraryOwnerUserId(int index) =>
        Guid.Parse($"44444444-4444-4444-4444-{index:X12}");

    public static Guid GetLibraryId(int index) =>
        Guid.Parse($"55555555-5555-5555-5555-{index:X12}");
}

public sealed record DemoUserDefinition(
    Guid Id,
    string PhoneNumber,
    string Password,
    string FirstName,
    string LastName,
    Gender Gender,
    Role DomainRole,
    IReadOnlyCollection<string> IdentityRoles);
