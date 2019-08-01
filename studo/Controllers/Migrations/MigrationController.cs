﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using studo.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using studo.Models.Responses.Migrations;
using Microsoft.AspNetCore.Authorization;
using studo.Services.Configure;

namespace studo.Controllers.Migrations
{
    [Produces("application/json")]
    [Route("api/migrations")]
    public class MigrationController : Controller
    {
        private readonly ILogger<MigrationController> logger;
        private readonly DatabaseContext dbContext;

        public MigrationController(ILogger<MigrationController> logger, DatabaseContext dbContext)
        {
            this.logger = logger;
            this.dbContext = dbContext;
        }

        [Authorize(Roles = RolesConstants.Admin)]
        public async Task<ActionResult<MigrationsView>> GetLastMigrationId()
        {
            MigrationsView migrationsView = new MigrationsView();

            migrationsView.Migrations = dbContext.Database.GetMigrations();
            migrationsView.PendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            migrationsView.AppliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
            return Ok(migrationsView);
        }
    }
}

