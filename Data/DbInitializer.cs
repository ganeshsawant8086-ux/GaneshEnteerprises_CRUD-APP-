using GaneshEnterprises.Models;
using Microsoft.EntityFrameworkCore;

namespace GaneshEnterprises.Data
{
    public static class DbInitializer
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                // Attempt to apply migrations or ensure database tables are created
                try
                {
                    context.Database.Migrate();
                    logger.LogInformation("Database migration verified and applied successfully.");
                }
                catch (Exception migrateEx)
                {
                    logger.LogWarning(migrateEx, "Migrate() failed; attempting EnsureCreated() fallback.");
                    context.Database.EnsureCreated();
                }

                SeedProductsAndData(context, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing or seeding the database.");
            }
        }

        public static void SeedProductsAndData(ApplicationDbContext context, ILogger? logger = null)
        {
            var catalog = GetStandardCatalog();

            // 1. Seed or Update Products (ensure 20 per category: Small, Medium, Large)
            foreach (var standardItem in catalog)
            {
                var existing = context.Products.FirstOrDefault(p => p.Name == standardItem.Name);
                if (existing != null)
                {
                    // Update pricing, description, stock if missing
                    if (existing.CustomerPrice <= 0) existing.CustomerPrice = standardItem.CustomerPrice;
                    if (existing.RetailerPrice <= 0) existing.RetailerPrice = standardItem.RetailerPrice;
                    if (existing.Price <= 0) existing.Price = standardItem.CustomerPrice;
                    if (string.IsNullOrWhiteSpace(existing.Description)) existing.Description = standardItem.Description;
                    if (existing.StockQuantity <= 0) existing.StockQuantity = standardItem.StockQuantity;
                    existing.IsAvailable = true;
                }
                else
                {
                    context.Products.Add(new Product
                    {
                        Name = standardItem.Name,
                        Description = standardItem.Description,
                        BagSize = standardItem.BagSize,
                        CustomerPrice = standardItem.CustomerPrice,
                        RetailerPrice = standardItem.RetailerPrice,
                        Price = standardItem.CustomerPrice,
                        StockQuantity = standardItem.StockQuantity,
                        IsAvailable = true,
                    });
                }
            }

            context.SaveChanges();
            logger?.LogInformation("Verified product catalog: at least 20 products per category (Small, Medium, Large) with images.");

            // 2. Ensure Retailers exist
            if (!context.Retailers.Any())
            {
                context.Retailers.AddRange(
                    new Retailer
                    {
                        BusinessName = "Omkar Packaging & Traders",
                        OwnerName = "Suresh Patil",
                        Email = "omkarpack@gmail.com",
                        Password = "Retailer@123",
                        Phone = "9822012345",
                        GstNumber = "27AABCO1234F1Z1",
                        Address = "Shop 14, APMC Market Yard",
                        City = "Pune",
                        State = "Maharashtra",
                        Pincode = "411037",
                        IsActive = true
                    },
                    new Retailer
                    {
                        BusinessName = "Metro Super Bazar Suppliers",
                        OwnerName = "Rajesh Mehta",
                        Email = "metroretailers@gmail.com",
                        Password = "Retailer@123",
                        Phone = "9890123456",
                        GstNumber = "27AABCM5678F1Z5",
                        Address = "G-22, Commercial Complex, Gultekdi",
                        City = "Pune",
                        State = "Maharashtra",
                        Pincode = "411037",
                        IsActive = true
                    }
                );
                context.SaveChanges();
            }

            // 3. Ensure sample Orders exist
            if (!context.Orders.Any())
            {
                var prodSmall = context.Products.FirstOrDefault(p => p.BagSize == BagSize.Small);
                var prodMed = context.Products.FirstOrDefault(p => p.BagSize == BagSize.Medium);
                var prodLarge = context.Products.FirstOrDefault(p => p.BagSize == BagSize.Large);
                var retailer = context.Retailers.FirstOrDefault();

                if (prodMed != null)
                {
                    var order1 = new Order
                    {
                        OrderNumber = "GE-202610-001",
                        CustomerName = "Metro Retail Mart",
                        ContactNumber = "9876543210",
                        ProductId = prodMed.Id,
                        Quantity = 2000,
                        OrderDate = DateTime.Now.AddDays(-6),
                        Status = "Delivered",
                        OrderType = "PublicCustomer",
                        UnitPrice = prodMed.CustomerPrice,
                        TotalAmount = prodMed.CustomerPrice * 2000
                    };
                    context.Orders.Add(order1);
                    context.SaveChanges();

                    context.OrderTrackings.Add(new OrderTracking
                    {
                        OrderId = order1.Id,
                        TrackingNumber = "TRK-GE-10001",
                        CurrentStatus = "Delivered",
                        CurrentLocation = "Delivered at Destination Warehouse (Pune)",
                        CarrierName = "Ganesh Fast Logistics",
                        EstimatedDelivery = DateTime.Now.AddDays(-4),
                        UpdatedAt = DateTime.Now.AddDays(-4),
                        Notes = "Consignment handed over to receiver."
                    });
                }

                if (prodSmall != null)
                {
                    var order2 = new Order
                    {
                        OrderNumber = "GE-202610-002",
                        CustomerName = "City Chemist Wholesale",
                        ContactNumber = "9823456789",
                        ProductId = prodSmall.Id,
                        Quantity = 5000,
                        OrderDate = DateTime.Now.AddDays(-3),
                        Status = "In Transit",
                        OrderType = "RetailerWholesale",
                        RetailerId = retailer?.Id,
                        UnitPrice = prodSmall.RetailerPrice,
                        TotalAmount = prodSmall.RetailerPrice * 5000
                    };
                    context.Orders.Add(order2);
                    context.SaveChanges();

                    context.OrderTrackings.Add(new OrderTracking
                    {
                        OrderId = order2.Id,
                        TrackingNumber = "TRK-GE-10002",
                        CurrentStatus = "In Transit",
                        CurrentLocation = "Hub Dispatch Center - Shirwal Toll Plaza",
                        CarrierName = "Maharashtra Express Cargo",
                        EstimatedDelivery = DateTime.Now.AddDays(1),
                        UpdatedAt = DateTime.Now.AddHours(-3),
                        Notes = "Vehicle en-route to delivery terminal."
                    });
                }

                if (prodLarge != null)
                {
                    var order3 = new Order
                    {
                        OrderNumber = "GE-202610-003",
                        CustomerName = "Apex Garments & Fabrics",
                        ContactNumber = "9811223344",
                        ProductId = prodLarge.Id,
                        Quantity = 1500,
                        OrderDate = DateTime.Now.AddDays(-1),
                        Status = "Processing",
                        OrderType = "PublicCustomer",
                        UnitPrice = prodLarge.CustomerPrice,
                        TotalAmount = prodLarge.CustomerPrice * 1500
                    };
                    context.Orders.Add(order3);
                    context.SaveChanges();

                    context.OrderTrackings.Add(new OrderTracking
                    {
                        OrderId = order3.Id,
                        TrackingNumber = "TRK-GE-10003",
                        CurrentStatus = "Order Placed",
                        CurrentLocation = "Factory Floor Production Line #2, Pune",
                        CarrierName = "Ganesh Factory Logistics",
                        EstimatedDelivery = DateTime.Now.AddDays(2),
                        UpdatedAt = DateTime.Now.AddHours(-1),
                        Notes = "Bag extrusion and bundle boxing in progress."
                    });
                }

                context.SaveChanges();
            }
        }

        public static List<Product> GetStandardCatalog()
        {
            var list = new List<Product>();

            // =========================================================================
            // CATEGORY 1: SMALL BAGS (20 Products with Images)
            // =========================================================================
            list.Add(new Product
            {
                Name = "Small LDPE Carry Bag (10x12)",
                Description = "Durable lightweight Low-Density Polyethylene carry bag with punch handle. Ideal for pharmacy, cosmetics, and small retail takeaway goods.",
                BagSize = BagSize.Small,
                CustomerPrice = 1.50m,
                RetailerPrice = 1.10m,
                StockQuantity = 25000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small HDPE Grocery Pouch (8x10)",
                Description = "High-Density Polyethylene pouch bag designed for provisions, dry goods, pulses, and hardware items. High tensile strength.",
                BagSize = BagSize.Small,
                CustomerPrice = 1.10m,
                RetailerPrice = 0.80m,
                StockQuantity = 35000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small Clear Zip-Lock Resealable Pouch (6x8)",
                Description = "50-Micron crystal clear transparent zip-lock pouch. Air-tight, moisture-proof seal for jewelry, seeds, and electronics components.",
                BagSize = BagSize.Small,
                CustomerPrice = 1.80m,
                RetailerPrice = 1.30m,
                StockQuantity = 18000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small Pharmacy Rx Dispensing Bag (7x9)",
                Description = "Medical-grade green printed HDPE bag with Rx emblem for chemist counters, diagnostic reports, and clinical prescriptions.",
                BagSize = BagSize.Small,
                CustomerPrice = 1.25m,
                RetailerPrice = 0.90m,
                StockQuantity = 30000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small Glossy Black Boutique Bag (9x11)",
                Description = "Glossy finish virgin polymer D-cut carry bag in jet black. Perfect for luxury perfume, cosmetics, and watch showrooms.",
                BagSize = BagSize.Small,
                CustomerPrice = 2.20m,
                RetailerPrice = 1.65m,
                StockQuantity = 14000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small Soft Loop Handle Gift Bag (10x12)",
                Description = "Royal blue premium loop-handle shopping bag with heat-sealed joints for jewelry, mobile phone accessories, and gifts.",
                BagSize = BagSize.Small,
                CustomerPrice = 2.50m,
                RetailerPrice = 1.85m,
                StockQuantity = 12000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small Silver Foil Stand-Up Barrier Pouch (6x9)",
                Description = "Three-layer metallic barrier foil pouch for dry fruits, tea leaves, spices, and confectionery. Resealable zip closure.",
                BagSize = BagSize.Small,
                CustomerPrice = 3.20m,
                RetailerPrice = 2.40m,
                StockQuantity = 15000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small Anti-Static Pink ESD Component Bag (5x7)",
                Description = "Dissipative polyethylene pouch engineered for microchips, printed circuit boards, and sensitive electronics.",
                BagSize = BagSize.Small,
                CustomerPrice = 1.90m,
                RetailerPrice = 1.40m,
                StockQuantity = 22000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small 100% Biodegradable Cornstarch Bag (8x10)",
                Description = "CPCB certified compostable eco-bag made from natural corn starch. Breaks down naturally, non-toxic and eco-friendly.",
                BagSize = BagSize.Small,
                CustomerPrice = 2.40m,
                RetailerPrice = 1.80m,
                StockQuantity = 20000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small Nursery Plant Poly Bag (6x8)",
                Description = "UV-stabilized heavy black nursery seedling bag with pre-punched drainage holes for horticulture and plant saplings.",
                BagSize = BagSize.Small,
                CustomerPrice = 0.95m,
                RetailerPrice = 0.70m,
                StockQuantity = 40000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small Velvet-Finish Gold D-Cut Bag (7x9)",
                Description = "Golden matte velvet touch designer plastic pouch for festive gifts, dry fruits, and bridal souvenir packaging.",
                BagSize = BagSize.Small,
                CustomerPrice = 2.80m,
                RetailerPrice = 2.10m,
                StockQuantity = 10000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small Bakery & Confectionery Pouch (7x10)",
                Description = "Food-grade grease-resistant clear poly pouch for cookies, bread rolls, muffins, and bakery savories.",
                BagSize = BagSize.Small,
                CustomerPrice = 1.40m,
                RetailerPrice = 1.00m,
                StockQuantity = 28000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small Courier Tamper-Proof Flyer (8x10)",
                Description = "Co-extruded 60 micron security mailing bag with hot-melt permanent adhesive strip and POD jacket for document shipping.",
                BagSize = BagSize.Small,
                CustomerPrice = 2.00m,
                RetailerPrice = 1.50m,
                StockQuantity = 26000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small Heavy-Duty Hardware Bolt Pouch (6x9)",
                Description = "100-Micron puncture-resistant thick LDPE pouch designed to hold screws, nuts, bolts, and metallic hardware without tearing.",
                BagSize = BagSize.Small,
                CustomerPrice = 1.75m,
                RetailerPrice = 1.30m,
                StockQuantity = 19000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small Bubble-Lined Protective Pouch (7x9)",
                Description = "Self-sealing air bubble padded poly pouch for impact-free shipping of lenses, spectacles, and mobile spares.",
                BagSize = BagSize.Small,
                CustomerPrice = 2.60m,
                RetailerPrice = 1.95m,
                StockQuantity = 16000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small BOPP Transparent Display Bag (8x11)",
                Description = "Biaxially Oriented Polypropylene ultra-glossy clear bag with peel-and-seal tape for greeting cards and hosiery.",
                BagSize = BagSize.Small,
                CustomerPrice = 1.60m,
                RetailerPrice = 1.20m,
                StockQuantity = 32000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small Oxo-Degradable Sanitary Bag (7x10)",
                Description = "Discreet opaque pink disposal bag with tie handles for hygienic waste disposal in hotels, offices, and restrooms.",
                BagSize = BagSize.Small,
                CustomerPrice = 1.35m,
                RetailerPrice = 0.95m,
                StockQuantity = 24000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small Gusseted Bottom Spice Pouch (6x8)",
                Description = "Multi-layer barrier pouch with expandable bottom gusset. Stands upright on retail spice racks with airtight top seal.",
                BagSize = BagSize.Small,
                CustomerPrice = 1.95m,
                RetailerPrice = 1.45m,
                StockQuantity = 21000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small Printed Thank You D-Cut Bag (9x12)",
                Description = "Bright sunny yellow customer appreciation bag with punch handle. High customer recall for apparel and retail cash counters.",
                BagSize = BagSize.Small,
                CustomerPrice = 2.10m,
                RetailerPrice = 1.55m,
                StockQuantity = 25000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Small Vacuum Sealer Food Pouch (8x10)",
                Description = "Textured embossed food-grade nylon/polyethylene vacuum pouch for sous-vide cooking, cheese, and deep-freeze preservation.",
                BagSize = BagSize.Small,
                CustomerPrice = 2.75m,
                RetailerPrice = 2.05m,
                StockQuantity = 17000,
                IsAvailable = true,
            });

            // =========================================================================
            // CATEGORY 2: MEDIUM BAGS (20 Products with Images)
            // =========================================================================
            list.Add(new Product
            {
                Name = "Medium D-Cut Shopping Bag (14x16)",
                Description = "Standard commercial D-cut shopping bag with high tensile strength for apparel stores, supermarkets, and general retail.",
                BagSize = BagSize.Medium,
                CustomerPrice = 2.60m,
                RetailerPrice = 1.95m,
                StockQuantity = 20000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Loop-Handle Carry Bag (12x15)",
                Description = "Comfortable soft loop handle plastic bag offering excellent load bearing for departmental stores and textile shops.",
                BagSize = BagSize.Medium,
                CustomerPrice = 2.40m,
                RetailerPrice = 1.80m,
                StockQuantity = 18000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Supermarket W-Cut Vest Bag (13x19)",
                Description = "Economical high-capacity T-shirt vest carry bag with side gusset for grocery stores, vegetables, and supermarkets.",
                BagSize = BagSize.Medium,
                CustomerPrice = 2.10m,
                RetailerPrice = 1.55m,
                StockQuantity = 35000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Frosted Translucent Carry Bag (14x17)",
                Description = "Premium frosted matte finish carry bag with reinforced oval punch handle. Water-resistant and tear-resistant.",
                BagSize = BagSize.Medium,
                CustomerPrice = 2.90m,
                RetailerPrice = 2.15m,
                StockQuantity = 16000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Apparel Poly Bag with Flap (14x18)",
                Description = "High clarity poly bag with self-adhesive resealable strip and air-release punch hole for shirts, kurtas, and garments.",
                BagSize = BagSize.Medium,
                CustomerPrice = 2.30m,
                RetailerPrice = 1.70m,
                StockQuantity = 28000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Tamper-Evident Courier Bag (12x16)",
                Description = "Security courier bag with permanent hot-melt closure, water-resistant co-ex film, and external documents pouch.",
                BagSize = BagSize.Medium,
                CustomerPrice = 3.10m,
                RetailerPrice = 2.30m,
                StockQuantity = 24000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Sapphire Blue Boutique Bag (15x18)",
                Description = "Eye-catching sapphire blue glossy finish bag with heavy-duty handle. Designed for footwear and apparel boutiques.",
                BagSize = BagSize.Medium,
                CustomerPrice = 3.40m,
                RetailerPrice = 2.50m,
                StockQuantity = 15000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Compostable Cornstarch Carrier (13x17)",
                Description = "Eco-friendly 100% biodegradable shopping bag complying with ISO 17088 standards. Sturdy and compostable.",
                BagSize = BagSize.Medium,
                CustomerPrice = 3.60m,
                RetailerPrice = 2.70m,
                StockQuantity = 19000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Bright Orange D-Cut Bag (14x16)",
                Description = "High-visibility vibrant orange shopping bag with reinforced top header for trade fairs, events, and brand promotions.",
                BagSize = BagSize.Medium,
                CustomerPrice = 2.75m,
                RetailerPrice = 2.05m,
                StockQuantity = 22000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Gusseted Grocery Tote (12x16)",
                Description = "Broad expandable side and bottom gusset bag capable of carrying bulky cereal boxes, oil packs, and grocery jars.",
                BagSize = BagSize.Medium,
                CustomerPrice = 2.85m,
                RetailerPrice = 2.10m,
                StockQuantity = 21000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Footwear Box Carrier Bag (15x19)",
                Description = "Specifically sized to carry shoe boxes, sandals, and sports gear comfortably without tilting or tearing.",
                BagSize = BagSize.Medium,
                CustomerPrice = 3.25m,
                RetailerPrice = 2.45m,
                StockQuantity = 17000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Anti-Fog Vegetable Pouch (12x17)",
                Description = "Micro-perforated bag with anti-condensation coating keeps vegetables, greens, and fruits fresh during refrigeration.",
                BagSize = BagSize.Medium,
                CustomerPrice = 2.20m,
                RetailerPrice = 1.60m,
                StockQuantity = 30000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Metallic Silver D-Cut Bag (14x17)",
                Description = "Elegant silver metallic sheen polymer bag with double-welded seams for gift items, watches, and electronics.",
                BagSize = BagSize.Medium,
                CustomerPrice = 3.50m,
                RetailerPrice = 2.60m,
                StockQuantity = 14000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Stand-Up Window Zipper Pouch (11x15)",
                Description = "Multi-layer packaging pouch with clear front display window, tear notch, and heavy zipper lock for snacks and cereals.",
                BagSize = BagSize.Medium,
                CustomerPrice = 3.80m,
                RetailerPrice = 2.85m,
                StockQuantity = 13000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Biohazard Medical Waste Bag (14x18)",
                Description = "Yellow clinical waste bag with universal biohazard symbol for hospitals, dental clinics, and pathology laboratories.",
                BagSize = BagSize.Medium,
                CustomerPrice = 2.95m,
                RetailerPrice = 2.20m,
                StockQuantity = 20000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Water-Resistant Document Bag (12x15)",
                Description = "Heavy gauge transparent folder bag with snap button closure for files, catalogs, school books, and test sheets.",
                BagSize = BagSize.Medium,
                CustomerPrice = 3.30m,
                RetailerPrice = 2.45m,
                StockQuantity = 16000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Padded Bubble Mailer Envelope (13x17)",
                Description = "Kraft golden padded bubble bag providing 360-degree shock protection for books, mobile phones, and cosmetics.",
                BagSize = BagSize.Medium,
                CustomerPrice = 3.90m,
                RetailerPrice = 2.90m,
                StockQuantity = 15000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Deep-Freeze Cold Storage Bag (12x16)",
                Description = "Cold-resistant 80-micron polymer film retains elasticity at sub-zero temperatures. For ice cream, seafood, and frozen foods.",
                BagSize = BagSize.Medium,
                CustomerPrice = 2.70m,
                RetailerPrice = 2.00m,
                StockQuantity = 25000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Matte Black Electronics Bag (13x16)",
                Description = "Premium matte black anti-scratch bag with reinforced die-cut handle for gadgets, earphones, and tech stores.",
                BagSize = BagSize.Medium,
                CustomerPrice = 3.15m,
                RetailerPrice = 2.35m,
                StockQuantity = 18000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Medium Saree Packing Crystal Bag (14x18)",
                Description = "Crystal clear BOPP bag with high gloss for sarees, dress materials, and formal shirts. Dust-proof and wrinkle-free.",
                BagSize = BagSize.Medium,
                CustomerPrice = 2.50m,
                RetailerPrice = 1.85m,
                StockQuantity = 27000,
                IsAvailable = true,
            });

            // =========================================================================
            // CATEGORY 3: LARGE BAGS (20 Products with Images)
            // =========================================================================
            list.Add(new Product
            {
                Name = "Large Heavy-Duty Carrier Bag (18x24)",
                Description = "Heavy-gauge reinforced plastic carrier bag engineered for bulk clothing, textiles, and wholesale merchandise.",
                BagSize = BagSize.Large,
                CustomerPrice = 4.80m,
                RetailerPrice = 3.60m,
                StockQuantity = 15000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Industrial Packaging Liner (24x30)",
                Description = "Extra-large heavy-duty plastic packaging bag used for industrial liners, parcel dispatch, and bulk supply storage.",
                BagSize = BagSize.Large,
                CustomerPrice = 6.50m,
                RetailerPrice = 4.90m,
                StockQuantity = 10000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large D-Cut Textile & Fabric Bag (20x26)",
                Description = "Royale purple heavy-duty plastic shopping bag for wholesale cloth markets, curtains, and denim jeans batches.",
                BagSize = BagSize.Large,
                CustomerPrice = 5.40m,
                RetailerPrice = 4.05m,
                StockQuantity = 12000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Milky White Gusset Suit Bag (18x28)",
                Description = "Milky opaque polyethylene suit and coat bag with expandable side gusset and hanger slot for laundry and dry-cleaners.",
                BagSize = BagSize.Large,
                CustomerPrice = 5.90m,
                RetailerPrice = 4.40m,
                StockQuantity = 11000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Emerald Department Store Bag (19x25)",
                Description = "Heavy-duty loop handle shopping bag in deep emerald green for department stores, hypermarkets, and homeware retail.",
                BagSize = BagSize.Large,
                CustomerPrice = 5.20m,
                RetailerPrice = 3.90m,
                StockQuantity = 14000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Extra-Tough Warehouse Parcel Sack (26x36)",
                Description = "100-Micron thick co-extruded black poly sack for warehouse bundling, freight transit, and export packing.",
                BagSize = BagSize.Large,
                CustomerPrice = 8.50m,
                RetailerPrice = 6.40m,
                StockQuantity = 8000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Tamper-Proof Bulk Courier Bag (24x32)",
                Description = "Industrial grade security bag with serial barcode area and void tamper tape for bulk dispatch to logistics hubs.",
                BagSize = BagSize.Large,
                CustomerPrice = 7.80m,
                RetailerPrice = 5.85m,
                StockQuantity = 9000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Red Clinical Waste Liner (24x30)",
                Description = "Heavy gauge puncture-resistant red hazardous waste liner with biohazard warnings for general hospital wards.",
                BagSize = BagSize.Large,
                CustomerPrice = 6.20m,
                RetailerPrice = 4.65m,
                StockQuantity = 13000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Heavy Garbage Trash Bag on Roll (30x37)",
                Description = "40-Micron black heavy trash can liner on perforated roll for commercial kitchens, hotels, and municipal waste handling.",
                BagSize = BagSize.Large,
                CustomerPrice = 5.00m,
                RetailerPrice = 3.75m,
                StockQuantity = 20000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Woven Poly Grain & Seed Sack (20x32)",
                Description = "Heavy woven polymer sack with laminated inner moisture shield for rice, pulses, flour, and agricultural seeds.",
                BagSize = BagSize.Large,
                CustomerPrice = 7.20m,
                RetailerPrice = 5.40m,
                StockQuantity = 12000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Blanket & Quilt Storage Bag (24x30)",
                Description = "Heavy transparent storage bag with zippered top closure and nylon piping for blankets, quilts, and winter wear.",
                BagSize = BagSize.Large,
                CustomerPrice = 9.50m,
                RetailerPrice = 7.10m,
                StockQuantity = 7500,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Furniture Dust Cover Poly Sheet Bag (36x48)",
                Description = "Giant 100-micron clear poly bag for protecting mattresses, armchairs, and home appliances during transit or renovation.",
                BagSize = BagSize.Large,
                CustomerPrice = 12.00m,
                RetailerPrice = 9.00m,
                StockQuantity = 5000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Fertilizer & Agro Chemical Sack (22x34)",
                Description = "Multi-layer barrier bag resistant to corrosive fertilizers, pesticide powders, and farm soil supplements.",
                BagSize = BagSize.Large,
                CustomerPrice = 8.20m,
                RetailerPrice = 6.15m,
                StockQuantity = 10000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Blue Catering Food Service Liner (28x36)",
                Description = "Food-grade virgin blue LDPE liner bag for catering cauldrons, bakery flour bins, and raw food wholesale processing.",
                BagSize = BagSize.Large,
                CustomerPrice = 6.80m,
                RetailerPrice = 5.10m,
                StockQuantity = 11000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Compostable Yard & Lawn Bag (24x32)",
                Description = "Heavy-duty 100% compostable garden waste bag for leaves, grass clippings, and municipal green composting.",
                BagSize = BagSize.Large,
                CustomerPrice = 7.50m,
                RetailerPrice = 5.60m,
                StockQuantity = 9500,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Golden Yellow Bulk Carry Bag (22x28)",
                Description = "Extra-wide gusset shopping bag with reinforced punch handle for wholesale spice and grain retail counters.",
                BagSize = BagSize.Large,
                CustomerPrice = 5.80m,
                RetailerPrice = 4.35m,
                StockQuantity = 14000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Anti-Corrosion VCI Machinery Bag (30x42)",
                Description = "Volatile Corrosion Inhibitor silver film bag protects steel machinery, automotive parts, and tools from rusting.",
                BagSize = BagSize.Large,
                CustomerPrice = 11.50m,
                RetailerPrice = 8.60m,
                StockQuantity = 6000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Round-Bottom Drum Liner (32x45)",
                Description = "Specialized circular bottom seal liner prevents creases and leaks inside standard 200-liter industrial storage drums.",
                BagSize = BagSize.Large,
                CustomerPrice = 9.00m,
                RetailerPrice = 6.75m,
                StockQuantity = 8500,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Construction Debris Heavy Sack (24x36)",
                Description = "Super-duty 150-micron poly woven sack engineered to handle rubble, gravel, tiles, and heavy site construction debris.",
                BagSize = BagSize.Large,
                CustomerPrice = 10.50m,
                RetailerPrice = 7.90m,
                StockQuantity = 7000,
                IsAvailable = true,
            });
            list.Add(new Product
            {
                Name = "Large Luggage Rain Protection Wrapper (28x40)",
                Description = "Heavy stretchable water-resistant protective wrapper bag for travel luggage, backpacks, and cargo cartons in monsoon.",
                BagSize = BagSize.Large,
                CustomerPrice = 6.90m,
                RetailerPrice = 5.20m,
                StockQuantity = 13000,
                IsAvailable = true,
            });

            return list;
        }
    }
}

