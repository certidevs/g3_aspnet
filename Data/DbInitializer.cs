using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using G3AspNet.Models;
using G3AspNet.Models.Enum;

namespace G3AspNet.Data;

/// <summary>Recrea la base de datos desde las entidades y prepara roles y cuentas demo.</summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // Proyecto de clase sin migraciones: la base se borra y se vuelve a crear
        // desde las entidades en cada arranque (como create-drop con H2 en Spring).
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var configuration = services.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue("SeedData:Enabled", true))
        {
            return;
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, RoleNames.User);
        await EnsureRoleAsync(roleManager, RoleNames.Admin);
        await EnsureRoleAsync(roleManager, RoleNames.Customer);
        await EnsureRoleAsync(roleManager, RoleNames.Mechanic);

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUserAsync(
            userManager,
            username: "admin",
            email: "admin@g3.local",
            displayName: "Administrador",
            password: "Admin123!",
            role: RoleNames.Admin);

        await EnsureUserAsync(
            userManager,
            username: "user",
            email: "user@g3.local",
            displayName: "Usuario de demo",
            password: "User123!",
            role: RoleNames.User);

        await EnsureUserAsync(
            userManager,
            username: "customer",
            email: "customer@g3.local",
            displayName: "Cliente Demo",
            password: "Customer123!",
            role: RoleNames.Customer);

        await EnsureUserAsync(
            userManager,
            username: "mechanic",
            email: "mechanic@g3.local",
            displayName: "Mecánico Demo",
            password: "Mechanic123!",
            role: RoleNames.Mechanic);

        // Más clientes y mecánicos para que los vehículos y las reparaciones tengan varios dueños y mecánicos.
        await EnsureUserAsync(
            userManager,
            username: "customer2",
            email: "customer2@g3.local",
            displayName: "Lucía Fernández",
            password: "Customer123!",
            role: RoleNames.Customer);

        await EnsureUserAsync(
            userManager,
            username: "customer3",
            email: "customer3@g3.local",
            displayName: "Marcos Ruiz",
            password: "Customer123!",
            role: RoleNames.Customer);

        await EnsureUserAsync(
            userManager,
            username: "mechanic2",
            email: "mechanic2@g3.local",
            displayName: "Raúl Ortega",
            password: "Mechanic123!",
            role: RoleNames.Mechanic);

        await EnsureUserAsync(
            userManager,
            username: "mechanic3",
            email: "mechanic3@g3.local",
            displayName: "Elena Navarro",
            password: "Mechanic123!",
            role: RoleNames.Mechanic);

        // Orden obligatorio: los vehículos y los recambios van antes que las reparaciones que los usan.
        await EnsureVehiclesAsync(context, userManager);
        await EnsureSparePartsAsync(context);
        await EnsureRepairsAsync(context, userManager);
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo crear el rol {roleName}.");
            }
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string username,
        string email,
        string displayName,
        string password,
        string role)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                DisplayName = displayName,
                EmailConfirmed = true,
                IsActive = true
            };
            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo crear el usuario demo {username}.");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo asignar el rol {role} a {username}.");
            }
        }
    }

    private static async Task EnsureVehiclesAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        if (await context.Vehicles.AnyAsync())
        {
            return;
        }

        var customerUser = await userManager.FindByNameAsync("customer")
                           ?? (await userManager.GetUsersInRoleAsync(RoleNames.Customer)).FirstOrDefault();
        var customer2 = await userManager.FindByNameAsync("customer2");
        var customer3 = await userManager.FindByNameAsync("customer3");

        if (customerUser is null || customer2 is null || customer3 is null)
        {
            return;
        }

        var demoVehicles = new List<Vehicle>
        {
            new Vehicle
            {
                LicensePlate = "1234ABC",
                VIN = "VF1BB050F12345678",
                Brand = "Renault",
                Model = "Clio 1.5 dCi",
                Year = 2018,
                FuelType = FuelType.Diesel,
                Mileage = 87500,
                OwnerId = customerUser.Id
            },
            new Vehicle
            {
                LicensePlate = "5678DEF",
                VIN = "WVWZZZ3CZWE098765",
                Brand = "Volkswagen",
                Model = "Golf 1.5 TSI",
                Year = 2021,
                FuelType = FuelType.Petrol,
                Mileage = 42300,
                OwnerId = customerUser.Id
            },
            new Vehicle
            {
                LicensePlate = "9012GHI",
                VIN = "JTDKN36U001234567",
                Brand = "Toyota",
                Model = "Yaris Hybrid",
                Year = 2023,
                FuelType = FuelType.Hybrid,
                Mileage = 15800,
                OwnerId = customerUser.Id
            },
            new Vehicle
            {
                LicensePlate = "3456JKL",
                VIN = "VSSZZZ5FZKR123456",
                Brand = "Seat",
                Model = "León 2.0 TDI",
                Year = 2019,
                FuelType = FuelType.Diesel,
                Mileage = 112400,
                OwnerId = customer2.Id
            },
            new Vehicle
            {
                LicensePlate = "7890MNP",
                VIN = "WF0FXXWPMFGA12345",
                Brand = "Ford",
                Model = "Focus 1.0 EcoBoost",
                Year = 2016,
                FuelType = FuelType.Petrol,
                Mileage = 134000,
                OwnerId = customer2.Id
            },
            new Vehicle
            {
                LicensePlate = "2468RST",
                VIN = "5YJ3E1EA7NF123456",
                Brand = "Tesla",
                Model = "Model 3",
                Year = 2022,
                FuelType = FuelType.Electric,
                Mileage = 38900,
                OwnerId = customer3.Id
            },
            new Vehicle
            {
                LicensePlate = "1357VWX",
                VIN = "UU1DJF00XL1234567",
                Brand = "Dacia",
                Model = "Sandero GLP",
                Year = 2020,
                FuelType = FuelType.LPG,
                Mileage = 61200,
                OwnerId = customer3.Id
            }
        };

        await context.Vehicles.AddRangeAsync(demoVehicles);
        await context.SaveChangesAsync();
    }

    /// <summary>Recambios del almacén. El stock inicial es el de antes de las reparaciones demo.</summary>
    private static async Task EnsureSparePartsAsync(ApplicationDbContext context)
    {
        if (await context.SpareParts.AnyAsync())
        {
            return;
        }

        context.SpareParts.AddRange(
            new SparePart { Reference = "BOS-0451103", Name = "Filtro de aceite", Brand = "Bosch", UnitPrice = 14.50m, Stock = 20, MinStock = 5 },
            new SparePart { Reference = "BRE-P85020", Name = "Pastillas de freno", Brand = "Brembo", UnitPrice = 45.00m, Stock = 12, MinStock = 4 },
            new SparePart { Reference = "CAS-EDGE5W30-5L", Name = "Aceite 5W30 5L", Brand = "Castrol", UnitPrice = 38.90m, Stock = 15, MinStock = 3 },
            new SparePart { Reference = "ATE-DOT4-1L", Name = "Líquido de frenos DOT4 1L", Brand = "ATE", UnitPrice = 12.90m, Stock = 10, MinStock = 3 },
            new SparePart { Reference = "MAN-C25114", Name = "Filtro de aire", Brand = "Mann-Filter", UnitPrice = 18.75m, Stock = 14, MinStock = 4 },
            new SparePart { Reference = "NGK-ILKAR7B11", Name = "Bujía iridio", Brand = "NGK", UnitPrice = 8.40m, Stock = 24, MinStock = 8 },
            new SparePart { Reference = "MIC-PRIMACY4-2055516", Name = "Neumático 205/55 R16", Brand = "Michelin", UnitPrice = 92.00m, Stock = 16, MinStock = 4 },
            new SparePart { Reference = "PHI-12972PRC1", Name = "Bombilla H7 12V", Brand = "Philips", UnitPrice = 9.50m, Stock = 6, MinStock = 5 },
            new SparePart { Reference = "VAL-RET-3201", Name = "Retrovisor exterior izquierdo", Brand = "Valeo", UnitPrice = 74.30m, Stock = 2, MinStock = 1 }
        );
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Reparaciones demo: todos los estados y tipos, varios mecánicos y vehículos.
    /// Las piezas se añaden con Repair.AddPart, que descuenta el stock de cada recambio
    /// (así el stock final es coherente con las piezas usadas).
    /// </summary>
    private static async Task EnsureRepairsAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        if (await context.Repairs.AnyAsync())
        {
            return;
        }

        var vehicles = await context.Vehicles.ToDictionaryAsync(vehicle => vehicle.LicensePlate);
        var parts = await context.SpareParts.ToDictionaryAsync(sparePart => sparePart.Name);
        var mechanic1 = await userManager.FindByNameAsync("mechanic");
        var mechanic2 = await userManager.FindByNameAsync("mechanic2");
        var mechanic3 = await userManager.FindByNameAsync("mechanic3");

        if (vehicles.Count == 0 || parts.Count == 0 || mechanic1 is null || mechanic2 is null || mechanic3 is null)
        {
            return;
        }

        // NewRepair(vehículo, mecánico, tipo, estado, descripción, días desde la apertura, días desde el cierre, km, horas, tarifa/hora)
        var repairs = new List<Repair>();

        // Renault Clio: dos reparaciones ya terminadas (el precio de la pieza en aquel momento era distinto al actual).
        var r1 = NewRepair(vehicles["1234ABC"], mechanic1, RepairType.OilChange, RepairStatus.Delivered,
            "Cambio de aceite y filtro", 60, 59, 72400, 1.0m, 45m);
        r1.AddPart(parts["Aceite 5W30 5L"], 1, 36.90m);
        r1.AddPart(parts["Filtro de aceite"], 1, 13.90m);
        repairs.Add(r1);

        var r2 = NewRepair(vehicles["1234ABC"], mechanic2, RepairType.BrakeFluidChange, RepairStatus.Completed,
            "Sustitución del líquido de frenos", 10, 9, 87300, 1.5m, 48m);
        r2.AddPart(parts["Líquido de frenos DOT4 1L"], 2);
        repairs.Add(r2);

        // Volkswagen Golf: una revisión entregada y un cambio de neumáticos en curso.
        var r3 = NewRepair(vehicles["5678DEF"], mechanic1, RepairType.GeneralInspection, RepairStatus.Delivered,
            "Revisión de los 40.000 km", 45, 44, 40100, 2.0m, 45m);
        r3.AddPart(parts["Filtro de aire"], 1, 17.90m);
        r3.AddPart(parts["Bujía iridio"], 4);
        repairs.Add(r3);

        var r4 = NewRepair(vehicles["5678DEF"], mechanic3, RepairType.Tires, RepairStatus.InProgress,
            "Cambio de los dos neumáticos delanteros", 1, null, 42300, 1.5m, 50m);
        r4.AddPart(parts["Neumático 205/55 R16"], 2);
        repairs.Add(r4);

        // Toyota Yaris: una reparación recién abierta (sin horas ni piezas todavía) y su primera revisión.
        repairs.Add(NewRepair(vehicles["9012GHI"], mechanic1, RepairType.OilChange, RepairStatus.Pending,
            "Cambio de aceite pendiente de cita", 0, null, 15800, 0m, 45m));

        var r6 = NewRepair(vehicles["9012GHI"], mechanic1, RepairType.GeneralInspection, RepairStatus.Delivered,
            "Primera revisión del vehículo", 120, 119, 7600, 1.0m, 45m);
        r6.AddPart(parts["Filtro de aire"], 1);
        repairs.Add(r6);

        // Seat León: mantenimiento entregado y frenos esperando recambios.
        var r7 = NewRepair(vehicles["3456JKL"], mechanic2, RepairType.OilChange, RepairStatus.Delivered,
            "Cambio de aceite y filtros", 90, 89, 101200, 1.5m, 48m);
        r7.AddPart(parts["Aceite 5W30 5L"], 1, 36.90m);
        r7.AddPart(parts["Filtro de aceite"], 1, 13.90m);
        r7.AddPart(parts["Filtro de aire"], 1, 17.90m);
        repairs.Add(r7);

        var r8 = NewRepair(vehicles["3456JKL"], mechanic2, RepairType.Other, RepairStatus.WaitingParts,
            "Cambio de pastillas y discos de freno; faltan los discos por recibir", 3, null, 112400, 2.5m, 48m);
        r8.AddPart(parts["Pastillas de freno"], 1);
        repairs.Add(r8);

        // Ford Focus: carrocería en curso y pintura pendiente.
        var r9 = NewRepair(vehicles["7890MNP"], mechanic3, RepairType.Bodywork, RepairStatus.InProgress,
            "Sustitución del retrovisor izquierdo y reparación de la aleta", 5, null, 134000, 6.0m, 50m);
        r9.AddPart(parts["Retrovisor exterior izquierdo"], 1);
        repairs.Add(r9);

        repairs.Add(NewRepair(vehicles["7890MNP"], mechanic1, RepairType.Painting, RepairStatus.Pending,
            "Pintado de la aleta delantera derecha", 2, null, 134000, 0m, 50m));

        // Tesla Model 3 y Dacia Sandero.
        var r11 = NewRepair(vehicles["2468RST"], mechanic1, RepairType.GeneralInspection, RepairStatus.Completed,
            "Revisión anual y sustitución del filtro de habitáculo", 7, 6, 38500, 1.5m, 52m);
        r11.AddPart(parts["Filtro de aire"], 1);
        repairs.Add(r11);

        var r12 = NewRepair(vehicles["1357VWX"], mechanic3, RepairType.Tires, RepairStatus.Delivered,
            "Cambio de los cuatro neumáticos", 30, 29, 59800, 2.0m, 45m);
        r12.AddPart(parts["Neumático 205/55 R16"], 4, 88.00m);
        repairs.Add(r12);

        var r13 = NewRepair(vehicles["1357VWX"], mechanic2, RepairType.Other, RepairStatus.Completed,
            "Sustitución de las bombillas de los faros (H7)", 4, 3, 61100, 0.5m, 45m);
        r13.AddPart(parts["Bombilla H7 12V"], 2);
        repairs.Add(r13);

        // Un solo SaveChanges guarda las reparaciones, sus piezas y el stock descontado de los recambios.
        context.Repairs.AddRange(repairs);
        await context.SaveChangesAsync();
    }

    /// <summary>Crea una reparación demo con fechas relativas a hoy. Sin cierre (null) = todavía abierta.</summary>
    private static Repair NewRepair(
        Vehicle vehicle,
        ApplicationUser mechanic,
        RepairType type,
        RepairStatus status,
        string description,
        int openedDaysAgo,
        int? completedDaysAgo,
        int mileageAtEntry,
        decimal laborHours,
        decimal hourlyRate)
    {
        var now = DateTime.UtcNow;
        return new Repair
        {
            VehicleId = vehicle.Id,
            MechanicId = mechanic.Id,
            RepairType = type,
            Status = status,
            Description = description,
            OpenedAt = now.AddDays(-openedDaysAgo),
            CompletedAt = completedDaysAgo is null ? null : now.AddDays(-completedDaysAgo.Value),
            MileageAtEntry = mileageAtEntry,
            LaborHours = laborHours,
            HourlyRate = hourlyRate
        };
    }
}
