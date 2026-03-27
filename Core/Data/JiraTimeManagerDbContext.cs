using Jira_Time_Manager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Jira_Time_Manager.Core.Data;

public partial class JiraTimeManagerDbContext : DbContext
{
    public JiraTimeManagerDbContext()
    {
    }

    public JiraTimeManagerDbContext(DbContextOptions<JiraTimeManagerDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Manager> Managers { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<WorkLog> WorkLogs { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.ClientId).HasName("PK__clients__BF21A424BD0E2369");

            entity.ToTable("clients");

            entity.HasIndex(e => e.ClientName, "UQ__clients__9ADC3B74CF593258").IsUnique();

            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.ClientName)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("client_name");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.EmployeeId).HasName("PK__employee__C52E0BA84CC76967");

            entity.ToTable("employees");

            entity.HasIndex(e => e.StaffNo, "UQ__employee__1962B212E1DA41FD").IsUnique();

            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");

            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.ManagerId).HasColumnName("manager_id");
            entity.Property(e => e.StaffNo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("staff_no");
            entity.Property(e => e.TeamId).HasColumnName("team_id");

            entity.HasOne(d => d.Manager).WithMany(p => p.Employees)
                .HasForeignKey(d => d.ManagerId)
                .HasConstraintName("fk_employee_manager");

            entity.HasOne(d => d.Team).WithMany(p => p.Employees)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__employees__team___2E1BDC42");
        });

        modelBuilder.Entity<Manager>(entity =>
        {
            entity.HasKey(e => e.ManagerId).HasName("PK__managers__5A6073FC27DD79E3");

            entity.ToTable("managers");

            entity.HasIndex(e => e.EmployeeId, "UQ__managers__C52E0BA92CD9B289").IsUnique();

            entity.Property(e => e.ManagerId).HasColumnName("manager_id");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");

            entity.HasOne(d => d.Employee).WithOne(p => p.ManagerNavigation)
                .HasForeignKey<Manager>(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__managers__employ__31EC6D26");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId).HasName("PK__projects__BC799E1F4DFC6553");

            entity.ToTable("projects");

            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.ProjectName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("project_name");

            entity.HasOne(d => d.Client).WithMany(p => p.Projects)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__projects__client__267ABA7A");
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.TeamId).HasName("PK__teams__F82DEDBC6E6EF83A");

            entity.ToTable("teams");

            entity.HasIndex(e => e.TeamName, "UQ__teams__29E35E0C050CAE3F").IsUnique();

            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.TeamName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("team_name");
        });

        modelBuilder.Entity<WorkLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__work_log__9E2397E072F2F7AC");

            entity.ToTable("work_logs");

            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.Comment)
                .IsUnicode(false)
                .HasColumnName("comment");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.Hours)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("hours");
            entity.Property(e => e.IsApproved)
                .HasDefaultValue(false)
                .HasColumnName("is_approved");
            entity.Property(e => e.LogDate).HasColumnName("log_date");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ReferenceNumber)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("reference_number");
            entity.Property(e => e.WorkCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("work_code");

            entity.HasOne(d => d.Employee).WithMany(p => p.WorkLogs)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__work_logs__emplo__36B12243");

            entity.HasOne(d => d.Project).WithMany(p => p.WorkLogs)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__work_logs__proje__37A5467C");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
