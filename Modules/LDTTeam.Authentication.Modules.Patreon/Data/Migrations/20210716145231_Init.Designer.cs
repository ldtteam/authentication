﻿// <auto-generated />
using System;
using LDTTeam.Authentication.Modules.Patreon.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace LDTTeam.Authentication.Modules.Patreon.Data.Migrations
{
    [DbContext(typeof(PatreonDatabaseContext))]
    [Migration("20210716145231_Init")]
    partial class Init
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.8")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("LDTTeam.Authentication.Modules.Patreon.Data.Models.PatreonMember", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<long>("Lifetime")
                        .HasColumnType("bigint");

                    b.Property<long>("Monthly")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("PatreonMembers");
                });

            modelBuilder.Entity("LDTTeam.Authentication.Modules.Patreon.Data.Models.Token", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("RefreshToken")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Token");
                });
#pragma warning restore 612, 618
        }
    }
}
