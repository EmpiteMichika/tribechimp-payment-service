﻿// <auto-generated />
using System;
using Empite.PaymentService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Empite.PaymentService.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20180801082528_addLastSuccessPaymentDate")]
    partial class addLastSuccessPaymentDate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.0-rtm-30799")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Empite.PaymentService.Data.Entity.InvoiceRelated.ConfiguredPaymentGateway", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("GatewayName");

                    b.Property<bool>("IsEnabled");

                    b.HasKey("Id");

                    b.ToTable("ConfiguredPaymentGateways");
                });

            modelBuilder.Entity("Empite.PaymentService.Data.Entity.InvoiceRelated.InvoiceContact", b =>
                {
                    b.Property<string>("UserId")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreatedAt");

                    b.Property<Guid?>("CreatedBy");

                    b.Property<DateTime?>("DeletedAt");

                    b.Property<Guid?>("DeletedBy");

                    b.Property<string>("Email");

                    b.Property<string>("ExternalContactUserId");

                    b.Property<string>("ExternalPrimaryContactId");

                    b.Property<string>("OrganizationId");

                    b.Property<DateTime?>("UpdatedAt");

                    b.Property<Guid?>("UpdatedBy");

                    b.HasKey("UserId");

                    b.HasIndex("ExternalContactUserId");

                    b.ToTable("InvoiceContacts");
                });

            modelBuilder.Entity("Empite.PaymentService.Data.Entity.InvoiceRelated.InvoiceHistory", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreatedAt");

                    b.Property<Guid?>("CreatedBy");

                    b.Property<DateTime?>("DeletedAt");

                    b.Property<Guid?>("DeletedBy");

                    b.Property<string>("InvoiceId");

                    b.Property<string>("InvoiceNumber");

                    b.Property<int>("InvoiceStatus");

                    b.Property<DateTime?>("PaymentRecordedDate");

                    b.Property<string>("PurcheseId");

                    b.Property<DateTime?>("UpdatedAt");

                    b.Property<Guid?>("UpdatedBy");

                    b.HasKey("Id");

                    b.HasIndex("InvoiceId");

                    b.HasIndex("InvoiceStatus");

                    b.HasIndex("PurcheseId");

                    b.ToTable("InvoiceHistories");
                });

            modelBuilder.Entity("Empite.PaymentService.Data.Entity.InvoiceRelated.InvoiceJobQueue", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreatedAt");

                    b.Property<Guid?>("CreatedBy");

                    b.Property<DateTime?>("DeletedAt");

                    b.Property<Guid?>("DeletedBy");

                    b.Property<int>("InvoiceGatewayType");

                    b.Property<bool>("IsSuccess");

                    b.Property<int>("JobType");

                    b.Property<string>("JsonData")
                        .HasColumnType("longtext");

                    b.Property<string>("LastErrorMessage")
                        .HasColumnType("longtext");

                    b.Property<int>("ReTryCount");

                    b.Property<DateTime?>("UpdatedAt");

                    b.Property<Guid?>("UpdatedBy");

                    b.HasKey("Id");

                    b.HasIndex("InvoiceGatewayType");

                    b.ToTable("InvoiceJobQueues");
                });

            modelBuilder.Entity("Empite.PaymentService.Data.Entity.InvoiceRelated.Item", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreatedAt");

                    b.Property<Guid?>("CreatedBy");

                    b.Property<DateTime?>("DeletedAt");

                    b.Property<Guid?>("DeletedBy");

                    b.Property<string>("Description");

                    b.Property<string>("ItemId");

                    b.Property<string>("Name");

                    b.Property<double>("Rate");

                    b.Property<DateTime?>("UpdatedAt");

                    b.Property<Guid?>("UpdatedBy");

                    b.HasKey("Id");

                    b.HasIndex("ItemId");

                    b.ToTable("Items");
                });

            modelBuilder.Entity("Empite.PaymentService.Data.Entity.InvoiceRelated.Item_Purchese", b =>
                {
                    b.Property<string>("RecurringInvoiceId");

                    b.Property<string>("ItemId");

                    b.Property<int>("Qty");

                    b.HasKey("RecurringInvoiceId", "ItemId");

                    b.HasIndex("ItemId");

                    b.ToTable("Item_Purchese");
                });

            modelBuilder.Entity("Empite.PaymentService.Data.Entity.InvoiceRelated.Purchese", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreatedAt");

                    b.Property<Guid?>("CreatedBy");

                    b.Property<DateTime?>("DeletedAt");

                    b.Property<Guid?>("DeletedBy");

                    b.Property<string>("InvoiceContactUserId");

                    b.Property<int>("InvoiceGatewayType");

                    b.Property<string>("InvoiceName");

                    b.Property<int>("InvoiceStatus");

                    b.Property<int>("InvoiceType");

                    b.Property<bool>("IsPaidForThisMonth");

                    b.Property<DateTime>("LastSuccessPayment");

                    b.Property<Guid?>("ReferenceGuid");

                    b.Property<DateTime?>("UpdatedAt");

                    b.Property<Guid?>("UpdatedBy");

                    b.HasKey("Id");

                    b.HasIndex("InvoiceContactUserId");

                    b.HasIndex("InvoiceGatewayType");

                    b.HasIndex("InvoiceStatus");

                    b.HasIndex("InvoiceType");

                    b.HasIndex("ReferenceGuid");

                    b.ToTable("Purcheses");
                });

            modelBuilder.Entity("Empite.PaymentService.Data.Entity.InvoiceRelated.InvoiceHistory", b =>
                {
                    b.HasOne("Empite.PaymentService.Data.Entity.InvoiceRelated.Purchese", "Purchese")
                        .WithMany("InvoiceHistories")
                        .HasForeignKey("PurcheseId");
                });

            modelBuilder.Entity("Empite.PaymentService.Data.Entity.InvoiceRelated.Item_Purchese", b =>
                {
                    b.HasOne("Empite.PaymentService.Data.Entity.InvoiceRelated.Item", "Item")
                        .WithMany("RecurringInvoices")
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Empite.PaymentService.Data.Entity.InvoiceRelated.Purchese", "Purchese")
                        .WithMany("Items")
                        .HasForeignKey("RecurringInvoiceId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Empite.PaymentService.Data.Entity.InvoiceRelated.Purchese", b =>
                {
                    b.HasOne("Empite.PaymentService.Data.Entity.InvoiceRelated.InvoiceContact", "InvoiceContact")
                        .WithMany("Invoices")
                        .HasForeignKey("InvoiceContactUserId");
                });
#pragma warning restore 612, 618
        }
    }
}
