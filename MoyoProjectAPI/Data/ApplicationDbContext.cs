﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MoyoProjectAPI.Data
{
    
    namespace ProductAPI.Data
    {
        public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
        {
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options): base(options)
            {

            }

            public DbSet<Product> Products { get; set; }
            public DbSet<EditProduct> EditProducts { get; set; }

        }

    
    }

}
