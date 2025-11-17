using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SmartTicketingManagementApp.Data.Entities;

namespace SmartTicketingManagementApp.Data
{
    public partial class AppDbContext : DbContext
    {
        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<helpdesk_ticket> helpdesk_tickets { get; set; } = null!;
        public virtual DbSet<team> teams { get; set; } = null!;
        public virtual DbSet<team_member> team_members { get; set; } = null!;
        public virtual DbSet<ticket> tickets { get; set; } = null!;
        public virtual DbSet<ticket_embedding> ticket_embeddings { get; set; } = null!;
        public virtual DbSet<user> users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<helpdesk_ticket>(entity =>
            {
                entity.HasNoKey();

                entity.ToView("helpdesk_tickets");

                entity.Property(e => e.answer).HasColumnType("character varying");

                entity.Property(e => e.assigned_to).HasColumnType("character varying");

                entity.Property(e => e.created_at).HasColumnType("timestamp without time zone");

                entity.Property(e => e.description).HasColumnType("character varying");

                entity.Property(e => e.priority).HasColumnType("character varying");

                entity.Property(e => e.title).HasColumnType("character varying");

                entity.Property(e => e.user).HasColumnType("character varying");
            });

            modelBuilder.Entity<team>(entity =>
            {
                entity.HasKey(e => e.team_id)
                    .HasName("teams_pkey");

                entity.Property(e => e.team_id).HasColumnType("character varying");

                entity.Property(e => e.team_description).HasColumnType("character varying");

                entity.Property(e => e.team_email_address).HasMaxLength(255);

                entity.Property(e => e.team_name).HasColumnType("character varying");
            });

            modelBuilder.Entity<team_member>(entity =>
            {
                entity.HasKey(e => e.team_member_id)
                    .HasName("team_members_pkey");

                entity.Property(e => e.team_id).HasColumnType("character varying");

                entity.HasOne(d => d.team)
                    .WithMany(p => p.team_members)
                    .HasForeignKey(d => d.team_id)
                    .HasConstraintName("team_members_team_id_fkey");

                entity.HasOne(d => d.user)
                    .WithMany(p => p.team_members)
                    .HasForeignKey(d => d.user_id)
                    .HasConstraintName("team_members_user_id_fkey");
            });

            modelBuilder.Entity<ticket>(entity =>
            {
                entity.HasKey(e => e.ticket_id)
                    .HasName("tickets_pkey");

                entity.HasIndex(e => new { e.assigned_team_id, e.status }, "idx_tickets_assigned_team_status");

                entity.HasIndex(e => e.created_at, "idx_tickets_created_at_desc")
                    .HasSortOrder(new[] { SortOrder.Descending });

                entity.HasIndex(e => new { e.requester_id, e.status }, "idx_tickets_requester_status");

                entity.HasIndex(e => e.status, "idx_tickets_status");

                entity.HasIndex(e => new { e.status, e.created_at }, "idx_tickets_status_created_at")
                    .HasSortOrder(new[] { SortOrder.Ascending, SortOrder.Descending });

                entity.HasIndex(e => new { e.assigned_team_id, e.status }, "ix_tickets_assigned_status");

                entity.HasIndex(e => e.assigned_team_id, "ix_tickets_assigned_team_id");

                entity.HasIndex(e => new { e.assigned_team_id, e.status }, "ix_tickets_assigned_team_status");

                entity.HasIndex(e => e.created_at, "ix_tickets_created_at")
                    .HasSortOrder(new[] { SortOrder.Descending });

                entity.HasIndex(e => e.status, "ix_tickets_status");

                entity.Property(e => e.answer).HasColumnType("character varying");

                entity.Property(e => e.assigned_team_id).HasColumnType("character varying");

                entity.Property(e => e.body).HasColumnType("character varying");

                entity.Property(e => e.created_at).HasColumnType("timestamp without time zone");

                entity.Property(e => e.priority).HasColumnType("character varying");

                entity.Property(e => e.status).HasColumnType("character varying");

                entity.Property(e => e.subject).HasColumnType("character varying");

                entity.Property(e => e.suggested_answer).HasColumnType("character varying");

                entity.Property(e => e.suggested_assigned_team_id).HasColumnType("character varying");

                entity.Property(e => e.tag_1).HasColumnType("character varying");

                entity.Property(e => e.tag_2).HasColumnType("character varying");

                entity.Property(e => e.tag_3).HasColumnType("character varying");

                entity.Property(e => e.tag_4).HasColumnType("character varying");

                entity.Property(e => e.tag_5).HasColumnType("character varying");

                entity.Property(e => e.tag_6).HasColumnType("character varying");

                entity.Property(e => e.tag_7).HasColumnType("character varying");

                entity.Property(e => e.tag_8).HasColumnType("character varying");

                entity.Property(e => e.type).HasColumnType("character varying");

                entity.HasOne(d => d.assigned_team)
                    .WithMany(p => p.tickets)
                    .HasForeignKey(d => d.assigned_team_id)
                    .HasConstraintName("tickets_assigned_team_id_fkey");

                entity.HasOne(d => d.requester)
                    .WithMany(p => p.tickets)
                    .HasForeignKey(d => d.requester_id)
                    .HasConstraintName("tickets_requester_id_fkey");
            });

            modelBuilder.Entity<ticket_embedding>(entity =>
            {
                entity.Property(e => e.chunk_text).HasColumnType("character varying");

                entity.HasOne(d => d.ticket)
                    .WithMany(p => p.ticket_embeddings)
                    .HasForeignKey(d => d.ticket_id)
                    .HasConstraintName("ticket_embeddings_ticket_id_fkey");
            });

            modelBuilder.Entity<user>(entity =>
            {
                entity.HasKey(e => e.user_id)
                    .HasName("users_pkey");

                entity.Property(e => e.email).HasColumnType("character varying");

                entity.Property(e => e.name).HasColumnType("character varying");

                entity.Property(e => e.user_role).HasMaxLength(50);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
