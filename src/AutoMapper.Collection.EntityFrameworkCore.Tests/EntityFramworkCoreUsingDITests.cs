﻿using System;
using System.Linq;
using AutoMapper.EntityFrameworkCore;
using AutoMapper.EquivalencyExpression;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AutoMapper.Collection.EntityFrameworkCore.Tests
{
    public class EntityFramworkCoreUsingDITests : IDisposable
    {
        private readonly IServiceScope _serviceScope;

        public EntityFramworkCoreUsingDITests()
        {
            var services = new ServiceCollection();
            services.AddEntityFrameworkInMemoryDatabase()
                .AddDbContext<DB>(options => options.UseInMemoryDatabase("EfTestDatabase" + Guid.NewGuid()));
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            Mapper.Reset();
            Mapper.Initialize(x =>
            {
                x.AddCollectionMappers();
                x.CreateMap<ThingDto, Thing>().ReverseMap();
                x.SetGeneratePropertyMaps(new GenerateEntityFrameworkCorePrimaryKeyPropertyMapsDependencyInjection<DB>(serviceProvider));
            });

            _serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        }

        [Fact]
        public void Should_Persist_To_Update()
        {
            var db = _serviceScope.ServiceProvider.GetRequiredService<DB>();
            db.Things.Add(new Thing { Title = "Test2" });
            db.Things.Add(new Thing { Title = "Test3" });
            db.Things.Add(new Thing { Title = "Test4" });
            db.SaveChanges();

            Assert.Equal(3, db.Things.Count());

            var item = db.Things.First();

            db.Things.Persist().InsertOrUpdate(new ThingDto { ID = item.ID, Title = "Test" });
            Assert.Equal(1, db.ChangeTracker.Entries<Thing>().Count(x => x.State == EntityState.Modified));

            Assert.Equal(3, db.Things.Count());

            db.Things.First(x => x.ID == item.ID).Title.Should().Be("Test");
        }

        [Fact]
        public void Should_Persist_To_Insert()
        {
            var db = _serviceScope.ServiceProvider.GetRequiredService<DB>();
            db.Things.Add(new Thing { Title = "Test2" });
            db.Things.Add(new Thing { Title = "Test3" });
            db.Things.Add(new Thing { Title = "Test4" });
            db.SaveChanges();

            Assert.Equal(3, db.Things.Count());

            db.Things.Persist().InsertOrUpdate(new ThingDto { Title = "Test" });
            Assert.Equal(3, db.Things.Count());
            Assert.Equal(1, db.ChangeTracker.Entries<Thing>().Count(x => x.State == EntityState.Added));

            db.SaveChanges();

            Assert.Equal(4, db.Things.Count());

            db.Things.OrderByDescending(x => x.ID).First().Title.Should().Be("Test");
        }

        public void Dispose()
        {
            _serviceScope?.Dispose();
        }

        public class DB : DbContext
        {
            public DB(DbContextOptions dbContextOptions)
                : base(dbContextOptions)
            {
            }

            public DbSet<Thing> Things { get; set; }
        }

        public class Thing
        {
            public int ID { get; set; }
            public string Title { get; set; }
            public override string ToString() { return Title; }
        }

        public class ThingDto
        {
            public int ID { get; set; }
            public string Title { get; set; }
        }
    }
}
