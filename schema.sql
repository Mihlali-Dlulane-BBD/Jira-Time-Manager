USE master;
GO

IF EXISTS(SELECT * FROM sys.databases WHERE name='JiraTimeManagerDB')
BEGIN
    ALTER DATABASE JiraTimeManagerDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE JiraTimeManagerDB;
END
GO

CREATE DATABASE JiraTimeManagerDB;
GO

USE JiraTimeManagerDB;
GO

-- 1. Clients Table 
CREATE TABLE clients (
    client_id INT PRIMARY KEY IDENTITY(1,1),
    client_name VARCHAR(150) NOT NULL UNIQUE
);

-- 2. Projects Table 
CREATE TABLE projects (
    project_id INT PRIMARY KEY IDENTITY(1,1),
    client_id INT NOT NULL,
    project_name VARCHAR(100) NOT NULL,
    FOREIGN KEY (client_id) REFERENCES clients(client_id)
);

-- 3. Teams Table
CREATE TABLE teams (
    team_id INT PRIMARY KEY IDENTITY(1,1),
    team_name VARCHAR(100) NOT NULL UNIQUE
);

-- 4. Employees Table 
CREATE TABLE employees (
    employee_id INT PRIMARY KEY IDENTITY(1,1),
    staff_no VARCHAR(50) NOT NULL UNIQUE, 
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    team_id INT NOT NULL,
    manager_id INT NULL, 
    FOREIGN KEY (team_id) REFERENCES teams(team_id)
);

-- 5. Managers Table 
CREATE TABLE managers (
    manager_id INT PRIMARY KEY IDENTITY(1,1),
    employee_id INT NOT NULL UNIQUE, 
    FOREIGN KEY (employee_id) REFERENCES employees(employee_id)
);


ALTER TABLE employees
ADD CONSTRAINT fk_employee_manager
FOREIGN KEY (manager_id) REFERENCES managers(manager_id);

-- 6. Import Batches Table 
CREATE TABLE import_batches (
    import_batch_id INT IDENTITY(1,1) PRIMARY KEY,
    FileName NVARCHAR(MAX) NOT NULL,
    import_date DATETIME2 NOT NULL,
    Status NVARCHAR(50) NOT NULL
);

-- 7. Work Logs Table 
CREATE TABLE work_logs (
    log_id INT PRIMARY KEY IDENTITY(1,1),
    employee_id INT NOT NULL,
    project_id INT NOT NULL,
    import_batch_id INT NULL,
    description VARCHAR(255) NOT NULL,   
    work_code VARCHAR(50) NOT NULL,        
    comment VARCHAR(MAX),                  
    reference_number VARCHAR(100),        
    log_date DATE NOT NULL,               
    hours DECIMAL(5,2) NOT NULL,          
    is_approved BIT DEFAULT 0,
    FOREIGN KEY (employee_id) REFERENCES employees(employee_id),
    FOREIGN KEY (project_id) REFERENCES projects(project_id),
    CONSTRAINT FK_WorkLogs_ImportBatches_ImportBatchId 
        FOREIGN KEY (import_batch_id) REFERENCES import_batches(import_batch_id)
        ON DELETE SET NULL
);
GO