// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SCJ.Booking.MVC.Data;

namespace SCJ.Booking.MVC.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("00000000000000_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("SCJ.Booking.MVC.Models.BookingHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<long>("ContainerId");

                    b.Property<string>("SmGovUserGuid")
                        .IsRequired()
                        .HasMaxLength(32);

                    b.Property<DateTime>("Timestamp");

                    b.HasKey("Id");

                    b.HasIndex("SmGovUserGuid", "Timestamp")
                        .IsUnique();

                    b.ToTable("BookingHistory");
                });
#pragma warning restore 612, 618
        }
    }
}
