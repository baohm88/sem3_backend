-- MySQL 8 DDL for MVP platform
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS=0;

CREATE DATABASE IF NOT EXISTS mvp_platform CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE mvp_platform;

CREATE TABLE users (
  id            CHAR(24) PRIMARY KEY,
  email         VARCHAR(255) NOT NULL UNIQUE,
  password_hash VARCHAR(255) NOT NULL,
  role          ENUM('Admin','Company','Driver','Rider') NOT NULL,
  is_active     TINYINT(1) NOT NULL DEFAULT 1,
  created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB;

CREATE TABLE companies (
  id            CHAR(24) PRIMARY KEY,
  owner_user_id CHAR(24) NOT NULL,
  name          VARCHAR(200) NOT NULL,
  description   TEXT,
  rating        DECIMAL(3,2) DEFAULT 0.00,
  membership    ENUM('Free','Basic','Premium') NOT NULL DEFAULT 'Free',
  membership_expires_at DATETIME NULL,
  is_active     TINYINT(1) NOT NULL DEFAULT 1,
  created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT fk_companies_owner FOREIGN KEY (owner_user_id) REFERENCES users(id)
) ENGINE=InnoDB;

CREATE TABLE driver_profiles (
  id            CHAR(24) PRIMARY KEY,
  user_id       CHAR(24) NOT NULL UNIQUE,
  full_name     VARCHAR(200) NOT NULL,
  phone         VARCHAR(40),
  bio           TEXT,
  rating        DECIMAL(3,2) DEFAULT 0.00,
  skills        JSON NULL,
  location      VARCHAR(200),
  is_available  TINYINT(1) NOT NULL DEFAULT 1,
  created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT fk_driver_user FOREIGN KEY (user_id) REFERENCES users(id)
) ENGINE=InnoDB;

CREATE TABLE rider_profiles (
  id            CHAR(24) PRIMARY KEY,
  user_id       CHAR(24) NOT NULL UNIQUE,
  full_name     VARCHAR(200) NOT NULL,
  phone         VARCHAR(40),
  created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT fk_rider_user FOREIGN KEY (user_id) REFERENCES users(id)
) ENGINE=InnoDB;

CREATE TABLE services (
  id            CHAR(24) PRIMARY KEY,
  company_id    CHAR(24) NOT NULL,
  title         VARCHAR(200) NOT NULL,
  description   TEXT,
  price_cents   INT NOT NULL DEFAULT 0,
  is_active     TINYINT(1) NOT NULL DEFAULT 1,
  created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT fk_services_company FOREIGN KEY (company_id) REFERENCES companies(id)
) ENGINE=InnoDB;

CREATE TABLE advertisements (
  id            CHAR(24) PRIMARY KEY,
  company_id    CHAR(24) NOT NULL,
  title         VARCHAR(200) NOT NULL,
  description   TEXT,
  is_active     TINYINT(1) NOT NULL DEFAULT 1,
  created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT fk_ads_company FOREIGN KEY (company_id) REFERENCES companies(id)
) ENGINE=InnoDB;

CREATE TABLE company_driver_relations (
  id            CHAR(24) PRIMARY KEY,
  company_id    CHAR(24) NOT NULL,
  driver_user_id CHAR(24) NOT NULL,
  base_salary_cents INT NOT NULL DEFAULT 0,
  created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY uq_company_driver (company_id, driver_user_id),
  CONSTRAINT fk_cdr_company FOREIGN KEY (company_id) REFERENCES companies(id),
  CONSTRAINT fk_cdr_driver FOREIGN KEY (driver_user_id) REFERENCES users(id)
) ENGINE=InnoDB;

CREATE TABLE job_applications (
  id            CHAR(24) PRIMARY KEY,
  company_id    CHAR(24) NOT NULL,
  driver_user_id CHAR(24) NOT NULL,
  status        ENUM('Applied','Accepted','Rejected','Expired') NOT NULL DEFAULT 'Applied',
  created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  expires_at    DATETIME NULL,
  CONSTRAINT fk_ja_company FOREIGN KEY (company_id) REFERENCES companies(id),
  CONSTRAINT fk_ja_driver FOREIGN KEY (driver_user_id) REFERENCES users(id)
) ENGINE=InnoDB;

CREATE TABLE invites (
  id            CHAR(24) PRIMARY KEY,
  company_id    CHAR(24) NOT NULL,
  driver_user_id CHAR(24) NOT NULL,
  base_salary_cents INT NOT NULL DEFAULT 0,
  status        ENUM('Sent','Accepted','Rejected','Expired') NOT NULL DEFAULT 'Sent',
  created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  expires_at    DATETIME NULL,
  CONSTRAINT fk_inv_company FOREIGN KEY (company_id) REFERENCES companies(id),
  CONSTRAINT fk_inv_driver FOREIGN KEY (driver_user_id) REFERENCES users(id)
) ENGINE=InnoDB;

CREATE TABLE orders (
  id            CHAR(24) PRIMARY KEY,
  rider_user_id CHAR(24) NOT NULL,
  company_id    CHAR(24) NOT NULL,
  service_id    CHAR(24) NOT NULL,
  status        ENUM('Pending','InProgress','Completed','Cancelled') NOT NULL DEFAULT 'Pending',
  price_cents   INT NOT NULL DEFAULT 0,
  created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT fk_orders_rider FOREIGN KEY (rider_user_id) REFERENCES users(id),
  CONSTRAINT fk_orders_company FOREIGN KEY (company_id) REFERENCES companies(id),
  CONSTRAINT fk_orders_service FOREIGN KEY (service_id) REFERENCES services(id)
) ENGINE=InnoDB;

CREATE TABLE reviews (
  id            CHAR(24) PRIMARY KEY,
  order_id      CHAR(24) NOT NULL UNIQUE,
  rider_user_id CHAR(24) NOT NULL,
  rating        INT NOT NULL,
  comment       TEXT,
  created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_reviews_order FOREIGN KEY (order_id) REFERENCES orders(id),
  CONSTRAINT fk_reviews_rider FOREIGN KEY (rider_user_id) REFERENCES users(id),
  CHECK (rating BETWEEN 1 AND 5)
) ENGINE=InnoDB;

CREATE TABLE wallets (
  id            CHAR(24) PRIMARY KEY,
  owner_type    ENUM('Company','Driver','Rider','Admin') NOT NULL,
  owner_ref_id  CHAR(24) NOT NULL,
  balance_cents INT NOT NULL DEFAULT 0,
  low_balance_threshold INT NOT NULL DEFAULT 10000,
  updated_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY uq_wallet_owner (owner_type, owner_ref_id)
) ENGINE=InnoDB;

CREATE TABLE transactions (
  id            CHAR(24) PRIMARY KEY,
  from_wallet_id CHAR(24) NULL,
  to_wallet_id   CHAR(24) NULL,
  amount_cents   INT NOT NULL,
  status         ENUM('Pending','Completed','Failed') NOT NULL DEFAULT 'Pending',
  idempotency_key VARCHAR(100) NULL,
  created_at     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_tx_from FOREIGN KEY (from_wallet_id) REFERENCES wallets(id),
  CONSTRAINT fk_tx_to   FOREIGN KEY (to_wallet_id)   REFERENCES wallets(id),
  UNIQUE KEY uq_idempotency (idempotency_key)
) ENGINE=InnoDB;

CREATE TABLE deactivations (
  id            CHAR(24) PRIMARY KEY,
  actor_user_id CHAR(24) NOT NULL,
  target_type   ENUM('User','Company','Service','Advertisement','Order') NOT NULL,
  target_id     CHAR(24) NOT NULL,
  reason_code   VARCHAR(100) NOT NULL,
  reason_note   TEXT,
  expires_at    DATETIME NULL,
  created_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

SET FOREIGN_KEY_CHECKS=1;

ALTER TABLE companies
  ADD COLUMN img_url VARCHAR(500) NULL AFTER description;
  
  ALTER TABLE driver_profiles
  ADD COLUMN img_url VARCHAR(500) NULL AFTER bio;

ALTER TABLE rider_profiles
  ADD COLUMN img_url VARCHAR(500) NULL AFTER phone;
  
ALTER TABLE users
  ADD CONSTRAINT chk_users_email_tld CHECK (LOCATE('.', SUBSTRING_INDEX(email, '@', -1)) > 0);
