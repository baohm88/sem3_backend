CREATE DATABASE  IF NOT EXISTS `mvp_platform` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `mvp_platform`;
-- MySQL dump 10.13  Distrib 8.0.38, for macos14 (x86_64)
--
-- Host: localhost    Database: mvp_platform
-- ------------------------------------------------------
-- Server version	8.4.3

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `__EFMigrationsHistory`
--

DROP TABLE IF EXISTS `__EFMigrationsHistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__EFMigrationsHistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__EFMigrationsHistory`
--

LOCK TABLES `__EFMigrationsHistory` WRITE;
/*!40000 ALTER TABLE `__EFMigrationsHistory` DISABLE KEYS */;
INSERT INTO `__EFMigrationsHistory` VALUES ('20250914104255_Baseline','9.0.0'),('20250914115835_Baseline','9.0.0'),('20250914115957_AddCompanyIdToReviews','9.0.0');
/*!40000 ALTER TABLE `__EFMigrationsHistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `advertisements`
--

DROP TABLE IF EXISTS `advertisements`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `advertisements` (
  `id` char(24) NOT NULL,
  `company_id` char(24) NOT NULL,
  `title` varchar(200) NOT NULL,
  `description` text,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_ads_company` (`company_id`),
  CONSTRAINT `fk_ads_company` FOREIGN KEY (`company_id`) REFERENCES `companies` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `advertisements`
--

LOCK TABLES `advertisements` WRITE;
/*!40000 ALTER TABLE `advertisements` DISABLE KEYS */;
/*!40000 ALTER TABLE `advertisements` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `companies`
--

DROP TABLE IF EXISTS `companies`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `companies` (
  `id` char(24) NOT NULL,
  `owner_user_id` char(24) NOT NULL,
  `name` varchar(200) NOT NULL,
  `description` text,
  `img_url` varchar(500) DEFAULT NULL,
  `rating` decimal(3,2) DEFAULT '0.00',
  `membership` enum('Free','Basic','Premium') NOT NULL DEFAULT 'Free',
  `membership_expires_at` datetime DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_companies_owner` (`owner_user_id`),
  CONSTRAINT `fk_companies_owner` FOREIGN KEY (`owner_user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `companies`
--

LOCK TABLES `companies` WRITE;
/*!40000 ALTER TABLE `companies` DISABLE KEYS */;
INSERT INTO `companies` VALUES ('68b000000000000000000001','68a000000000000000000002','AutoPro Co','Transportation services','https://thegioibodam.vn/wp-content/uploads/2020/09/so-dien-thoai-tong-dai-taxi-da-nang-600x400.jpg',0.00,'Basic','2025-09-30 15:00:09',1,'2025-08-30 15:00:09','2025-09-01 20:18:30'),('68b000000000000000000002','68a000000000000000000003','Bee Logistics','Delivery & riders','https://cdn2.fptshop.com.vn/unsafe/1920x0/filters:format(webp):quality(75)/2024_1_3_638398793527236287_taxi-dien-vinfast.jpg',0.00,'Premium','2025-09-30 15:00:09',1,'2025-08-30 15:00:09','2025-09-01 20:18:06'),('68b000000000000000000003','68a000000000000000000004','CQueen Services','On-demand helpers','https://cdn2.fptshop.com.vn/unsafe/1920x0/filters:format(webp):quality(75)/2023_12_15_638382272535011012_taxi-bien-hoa-thumnb.jpg',0.00,'Free','2025-09-30 15:00:09',1,'2025-08-30 15:00:09','2025-09-01 20:17:18'),('a65495c801844884810f60e6','315f97b5d5c14954be07a9ca','ABC Cabs updated','This is ABC Cabs, operating in most cities in Vietnam.\nBest price - premium quality','https://console.kr-asia.com/wp-content/uploads/2021/04/Grab-feat-img.png',0.00,'Premium','2025-11-29 15:11:58',1,'2025-08-31 09:09:12','2025-09-12 13:26:59'),('ced018add36a40d9bd6af9fa','5d45071d4f12451aa2fa5c2f','Company A','This is a test','https://cdn2.fptshop.com.vn/unsafe/1920x0/filters:format(webp):quality(75)/2023_12_14_638381503815348762_taxi-quang-ngai.jpg',0.00,'Free',NULL,1,'2025-09-09 13:13:04','2025-09-09 13:21:56');
/*!40000 ALTER TABLE `companies` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `company_driver_relations`
--

DROP TABLE IF EXISTS `company_driver_relations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `company_driver_relations` (
  `id` char(24) NOT NULL,
  `company_id` char(24) NOT NULL,
  `driver_user_id` char(24) NOT NULL,
  `base_salary_cents` int NOT NULL DEFAULT '0',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_company_driver` (`company_id`,`driver_user_id`),
  KEY `fk_cdr_driver` (`driver_user_id`),
  CONSTRAINT `fk_cdr_company` FOREIGN KEY (`company_id`) REFERENCES `companies` (`id`),
  CONSTRAINT `fk_cdr_driver` FOREIGN KEY (`driver_user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `company_driver_relations`
--

LOCK TABLES `company_driver_relations` WRITE;
/*!40000 ALTER TABLE `company_driver_relations` DISABLE KEYS */;
INSERT INTO `company_driver_relations` VALUES ('41fe3ab6e4f64711a3bed240','ced018add36a40d9bd6af9fa','68a000000000000000000010',150000,'2025-09-09 13:37:47','2025-09-09 13:37:47'),('8e8cc2ae73214b40a7e71518','a65495c801844884810f60e6','126090faa35e4b76a1a0fe4a',0,'2025-09-12 16:27:43','2025-09-12 16:27:43'),('eea645b28e2b40d8bfdf0a83','a65495c801844884810f60e6','68a000000000000000000005',150000,'2025-09-05 16:52:14','2025-09-05 16:52:14');
/*!40000 ALTER TABLE `company_driver_relations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `deactivations`
--

DROP TABLE IF EXISTS `deactivations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `deactivations` (
  `id` char(24) NOT NULL,
  `actor_user_id` char(24) NOT NULL,
  `target_type` enum('User','Company','Service','Advertisement','Order') NOT NULL,
  `target_id` char(24) NOT NULL,
  `reason_code` varchar(100) NOT NULL,
  `reason_note` text,
  `expires_at` datetime DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `deactivations`
--

LOCK TABLES `deactivations` WRITE;
/*!40000 ALTER TABLE `deactivations` DISABLE KEYS */;
/*!40000 ALTER TABLE `deactivations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `driver_profiles`
--

DROP TABLE IF EXISTS `driver_profiles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `driver_profiles` (
  `id` char(24) NOT NULL,
  `user_id` char(24) NOT NULL,
  `full_name` varchar(200) NOT NULL,
  `email` varchar(255) DEFAULT NULL,
  `phone` varchar(40) DEFAULT NULL,
  `bio` text,
  `img_url` varchar(500) DEFAULT NULL,
  `rating` decimal(3,2) DEFAULT '0.00',
  `skills` json DEFAULT NULL,
  `location` varchar(200) DEFAULT NULL,
  `is_available` tinyint(1) NOT NULL DEFAULT '1',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `user_id` (`user_id`),
  CONSTRAINT `fk_driver_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `driver_profiles`
--

LOCK TABLES `driver_profiles` WRITE;
/*!40000 ALTER TABLE `driver_profiles` DISABLE KEYS */;
INSERT INTO `driver_profiles` VALUES ('59f26210d651477c88121a0f','126090faa35e4b76a1a0fe4a','John Doe updated','d1@mycabs.com','123456789','This is a test updated','https://saigonbanme.vn/wp-content/uploads/2024/12/bo-99-anh-avatar-dep-cho-con-gai-ngau-chat-nhat-viet-nam.jpg',0.00,'[\"fast\", \"reliabble\"]','Hanoi',1,'2025-08-31 08:31:02','2025-09-13 10:08:05'),('68c000000000000000000001','68a000000000000000000005','Bill Gates','d7@mycabs.com','0123450001','Bio 1','https://cdn2.fptshop.com.vn/unsafe/1920x0/filters:format(webp):quality(75)/avatar_nam_anime_10_1b906dde27.jpg',4.50,'[\"fast\", \"safe\"]','Hanoi',1,'2025-08-30 15:00:09','2025-09-13 10:08:05'),('68c000000000000000000002','68a000000000000000000006','Nguyễn Văn A','d2@mycabs.com','0123450002','Bio 2','https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRxztj0qhf9FB4DSGIwWS5Fd32ihZq-yFpRLg&s',4.10,'[\"friendly\"]','Hanoi',1,'2025-08-30 15:00:09','2025-09-13 10:08:05'),('68c000000000000000000003','68a000000000000000000007','Driver 3','d3@mycabs.com','0123450003','Bio 3','https://cdn-media.sforum.vn/storage/app/media/THANHAN/2/2a/avatar-dep-84.jpg',4.20,'[\"motorbike\"]','HCM',1,'2025-08-30 15:00:09','2025-09-13 10:08:05'),('68c000000000000000000004','68a000000000000000000008','Mike Pompeo','d4@mycabs.com','0123450004','Bio 4','https://bayotech.vn/wp-content/uploads/2025/04/avatar-dep-2.jpg',4.00,'[\"car\"]','HCM',1,'2025-08-30 15:00:09','2025-09-13 10:08:05'),('68c000000000000000000005','68a000000000000000000009','Captain America','d5@mycabs.com','0123450005','Bio 5','https://jbagy.me/wp-content/uploads/2025/03/anh-avatar-dep-vo-tri-3.jpg',3.90,'[\"truck\"]','Danang',1,'2025-08-30 15:00:09','2025-09-13 10:08:05'),('68c000000000000000000006','68a000000000000000000010','Meo Meo','d6@mycabs.com','0123450006','Bio 6','https://haycafe.vn/wp-content/uploads/2021/11/Anh-avatar-dep-chat-lam-hinh-dai-dien-600x600.jpg',4.30,'[\"careful\"]','Hue',1,'2025-08-30 15:00:09','2025-09-13 10:08:05');
/*!40000 ALTER TABLE `driver_profiles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `invites`
--

DROP TABLE IF EXISTS `invites`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `invites` (
  `id` char(24) NOT NULL,
  `company_id` char(24) NOT NULL,
  `driver_user_id` char(24) NOT NULL,
  `base_salary_cents` int NOT NULL DEFAULT '0',
  `status` enum('Pending','Accepted','Rejected','Cancelled','Expired') NOT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `expires_at` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_inv_company` (`company_id`),
  KEY `fk_inv_driver` (`driver_user_id`),
  CONSTRAINT `fk_inv_company` FOREIGN KEY (`company_id`) REFERENCES `companies` (`id`),
  CONSTRAINT `fk_inv_driver` FOREIGN KEY (`driver_user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `invites`
--

LOCK TABLES `invites` WRITE;
/*!40000 ALTER TABLE `invites` DISABLE KEYS */;
INSERT INTO `invites` VALUES ('2cce233c36b54ac6abc41b0f','a65495c801844884810f60e6','68a000000000000000000010',150000,'Pending','2025-09-07 09:56:43','2025-09-08 16:56:00'),('5a99a58ceb484287bf3d1c86','a65495c801844884810f60e6','68a000000000000000000005',150000,'Rejected','2025-09-02 07:29:35',NULL),('5dfc969cd3514e168967e3cc','a65495c801844884810f60e6','68a000000000000000000005',1500000,'Cancelled','2025-09-02 09:56:50','2025-09-09 16:56:00'),('8898fe0dd48f4e40a841abfa','a65495c801844884810f60e6','68a000000000000000000005',100000,'Cancelled','2025-09-02 08:58:19','2025-09-02 15:00:00'),('9e4b276b74bb455c9f361c15','a65495c801844884810f60e6','68a000000000000000000005',1500000,'Cancelled','2025-09-02 10:08:12',NULL),('a2714ccb3097427fa51b0983','a65495c801844884810f60e6','68a000000000000000000006',150000,'Rejected','2025-09-02 13:25:44','2025-09-03 20:25:00'),('c1a5618be65e4605a6874003','68b000000000000000000002','68a000000000000000000010',120000,'Rejected','2025-09-07 16:18:51','2025-09-08 23:18:00'),('c3c1b23b56c947fb8287d8d7','a65495c801844884810f60e6','68a000000000000000000005',1000000,'Cancelled','2025-09-02 07:22:38',NULL),('c3f842363fd143128959d8df','a65495c801844884810f60e6','68a000000000000000000010',200000,'Cancelled','2025-09-02 07:29:45',NULL),('cca0558127f6470e99364344','a65495c801844884810f60e6','68a000000000000000000005',0,'Cancelled','2025-09-02 07:21:59',NULL),('d612e84c0ad74c94bec3a74b','a65495c801844884810f60e6','68a000000000000000000005',150000,'Accepted','2025-09-05 16:47:20','2025-09-13 00:47:00'),('e0e11cf3e757416ca253b7a4','ced018add36a40d9bd6af9fa','68a000000000000000000010',150000,'Accepted','2025-09-09 13:32:38','2025-09-11 20:32:00');
/*!40000 ALTER TABLE `invites` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `job_applications`
--

DROP TABLE IF EXISTS `job_applications`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `job_applications` (
  `id` char(24) NOT NULL,
  `company_id` char(24) NOT NULL,
  `driver_user_id` char(24) NOT NULL,
  `status` enum('Applied','Accepted','Rejected','Expired','Cancelled') NOT NULL DEFAULT 'Applied',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `expires_at` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_ja_company` (`company_id`),
  KEY `fk_ja_driver` (`driver_user_id`),
  CONSTRAINT `fk_ja_company` FOREIGN KEY (`company_id`) REFERENCES `companies` (`id`),
  CONSTRAINT `fk_ja_driver` FOREIGN KEY (`driver_user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `job_applications`
--

LOCK TABLES `job_applications` WRITE;
/*!40000 ALTER TABLE `job_applications` DISABLE KEYS */;
INSERT INTO `job_applications` VALUES ('03653a4d4de5446b98296a5d','a65495c801844884810f60e6','126090faa35e4b76a1a0fe4a','Cancelled','2025-09-02 01:38:51',NULL),('049b0e77650e4ba6986a566e','68b000000000000000000002','68a000000000000000000006','Applied','2025-09-07 16:17:21',NULL),('051a9c66fbaa4512b8c8f86b','68b000000000000000000002','126090faa35e4b76a1a0fe4a','Cancelled','2025-09-02 01:20:56',NULL),('1e108fc6465249e1b40307c1','68b000000000000000000002','68a000000000000000000006','Cancelled','2025-09-07 16:06:14',NULL),('232b86b1fcee4cdaa94f619e','68b000000000000000000001','126090faa35e4b76a1a0fe4a','Applied','2025-09-02 01:38:53',NULL),('242030ce69c24169985ee253','a65495c801844884810f60e6','68a000000000000000000006','Cancelled','2025-09-07 16:11:03',NULL),('2a089a7704d6420d91f8a936','a65495c801844884810f60e6','126090faa35e4b76a1a0fe4a','Accepted','2025-09-09 13:26:46',NULL),('3f219842518b439484797814','68b000000000000000000002','68a000000000000000000005','Cancelled','2025-09-02 06:45:36',NULL),('3f97e7467c64486f90e1802f','a65495c801844884810f60e6','68a000000000000000000008','Rejected','2025-09-07 16:46:41',NULL),('51851e8006a14c938b706488','68b000000000000000000002','126090faa35e4b76a1a0fe4a','Cancelled','2025-09-01 18:22:21',NULL),('68081af2be2d47e78ae60b5f','68b000000000000000000002','68a000000000000000000006','Cancelled','2025-09-07 16:11:47',NULL),('6b1440ccbf0c484897ce8d6c','68b000000000000000000002','68a000000000000000000006','Cancelled','2025-09-07 16:10:45',NULL),('721dc374f02046669ef3a3c7','a65495c801844884810f60e6','68a000000000000000000005','Cancelled','2025-09-02 11:02:01',NULL),('74543a330d414878a4fafcf6','a65495c801844884810f60e6','126090faa35e4b76a1a0fe4a','Cancelled','2025-09-02 01:36:14',NULL),('90a94a121fd447c6b2443260','a65495c801844884810f60e6','68a000000000000000000006','Cancelled','2025-09-07 16:10:33',NULL),('9d28a420cd5b4ecf86ff8472','68b000000000000000000002','68a000000000000000000006','Cancelled','2025-09-07 16:16:00',NULL),('ab0c510cb2804e6298ba4962','68b000000000000000000002','68a000000000000000000005','Cancelled','2025-09-02 05:56:41',NULL),('b14d22e79a4442ae89c67cf9','68b000000000000000000002','126090faa35e4b76a1a0fe4a','Cancelled','2025-09-02 01:38:49',NULL),('b79317e319ad4747bd77d11c','a65495c801844884810f60e6','68a000000000000000000008','Cancelled','2025-09-07 16:46:27',NULL),('b8eeff5e1a634142a3d4d48d','a65495c801844884810f60e6','68a000000000000000000006','Applied','2025-09-07 16:16:03',NULL),('b926e35824e64fbfb3d739d9','a65495c801844884810f60e6','126090faa35e4b76a1a0fe4a','Cancelled','2025-09-02 01:34:06',NULL),('ce618fcb6a5d4505988f57c8','68b000000000000000000002','68a000000000000000000006','Cancelled','2025-09-07 16:12:10',NULL),('ddad23a308d44c2da106df57','68b000000000000000000002','126090faa35e4b76a1a0fe4a','Cancelled','2025-09-01 18:28:17',NULL),('de3fa63aa55f4164b907128b','68b000000000000000000002','68a000000000000000000005','Cancelled','2025-09-02 11:09:04',NULL),('e915d822fad94ac792e039d5','68b000000000000000000001','126090faa35e4b76a1a0fe4a','Cancelled','2025-09-02 01:21:02',NULL),('f54bb4b77d8f4eec87ef3a69','a65495c801844884810f60e6','126090faa35e4b76a1a0fe4a','Cancelled','2025-09-02 01:21:00',NULL),('fa203500e66642a4a93caefe','ced018add36a40d9bd6af9fa','126090faa35e4b76a1a0fe4a','Applied','2025-09-09 13:24:50',NULL);
/*!40000 ALTER TABLE `job_applications` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `orders`
--

DROP TABLE IF EXISTS `orders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `orders` (
  `id` char(24) NOT NULL,
  `rider_user_id` char(24) NOT NULL,
  `company_id` char(24) NOT NULL,
  `service_id` char(24) NOT NULL,
  `status` enum('Pending','InProgress','Completed','Cancelled') NOT NULL DEFAULT 'Pending',
  `price_cents` int NOT NULL DEFAULT '0',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_orders_rider` (`rider_user_id`),
  KEY `fk_orders_company` (`company_id`),
  KEY `fk_orders_service` (`service_id`),
  CONSTRAINT `fk_orders_company` FOREIGN KEY (`company_id`) REFERENCES `companies` (`id`),
  CONSTRAINT `fk_orders_rider` FOREIGN KEY (`rider_user_id`) REFERENCES `users` (`id`),
  CONSTRAINT `fk_orders_service` FOREIGN KEY (`service_id`) REFERENCES `services` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `orders`
--

LOCK TABLES `orders` WRITE;
/*!40000 ALTER TABLE `orders` DISABLE KEYS */;
INSERT INTO `orders` VALUES ('70o000000000000000000001','68a000000000000000000011','68b000000000000000000001','68e000000000000000000001','Completed',150000,'2025-08-30 15:00:09','2025-08-30 15:00:09'),('70o000000000000000000002','68a000000000000000000012','68b000000000000000000001','68e000000000000000000002','Completed',50000,'2025-08-30 15:00:09','2025-08-30 15:00:09'),('70o000000000000000000003','68a000000000000000000011','68b000000000000000000002','68e000000000000000000003','Pending',200000,'2025-08-30 15:00:09','2025-08-30 15:00:09'),('70o000000000000000000004','68a000000000000000000013','68b000000000000000000002','68e000000000000000000004','InProgress',350000,'2025-08-30 15:00:09','2025-08-30 15:00:09'),('70o000000000000000000005','68a000000000000000000014','68b000000000000000000003','68e000000000000000000005','Cancelled',100000,'2025-08-30 15:00:09','2025-08-30 15:00:09'),('70o000000000000000000006','68a000000000000000000015','68b000000000000000000002','68e000000000000000000003','Completed',200000,'2025-08-30 15:00:09','2025-08-30 15:00:09'),('70o000000000000000000007','68a000000000000000000014','68b000000000000000000001','68e000000000000000000002','Completed',50000,'2025-08-30 15:00:09','2025-08-30 15:00:09'),('70o000000000000000000008','68a000000000000000000013','68b000000000000000000002','68e000000000000000000004','Pending',350000,'2025-08-30 15:00:09','2025-08-30 15:00:09'),('70o000000000000000000009','68a000000000000000000012','68b000000000000000000003','68e000000000000000000005','Pending',100000,'2025-08-30 15:00:09','2025-08-30 15:00:09'),('70o000000000000000000010','68a000000000000000000015','68b000000000000000000001','68e000000000000000000001','Completed',150000,'2025-08-30 15:00:09','2025-08-30 15:00:09');
/*!40000 ALTER TABLE `orders` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `reviews`
--

DROP TABLE IF EXISTS `reviews`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `reviews` (
  `id` char(24) NOT NULL,
  `order_id` varchar(255) DEFAULT NULL,
  `rider_user_id` char(24) NOT NULL,
  `rating` int NOT NULL,
  `comment` text,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `company_id` varchar(255) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `order_id` (`order_id`),
  KEY `fk_reviews_rider` (`rider_user_id`),
  KEY `IX_reviews_company_id` (`company_id`),
  CONSTRAINT `FK_reviews_companies_company_id` FOREIGN KEY (`company_id`) REFERENCES `companies` (`id`) ON DELETE CASCADE,
  CONSTRAINT `FK_reviews_orders_order_id` FOREIGN KEY (`order_id`) REFERENCES `orders` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_reviews_rider` FOREIGN KEY (`rider_user_id`) REFERENCES `users` (`id`),
  CONSTRAINT `reviews_chk_1` CHECK ((`rating` between 1 and 5))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `reviews`
--

LOCK TABLES `reviews` WRITE;
/*!40000 ALTER TABLE `reviews` DISABLE KEYS */;
INSERT INTO `reviews` VALUES ('16570730cbf74776955e102a',NULL,'56a5fb12b45e47c9a66eb2d5',4,'this is a test','2025-09-14 12:47:48','a65495c801844884810f60e6'),('44f4fb1e9ac64c989a649a33',NULL,'56a5fb12b45e47c9a66eb2d5',5,'adfasdf','2025-09-14 12:39:35','a65495c801844884810f60e6'),('5c52baddd0544b03bb589a49',NULL,'56a5fb12b45e47c9a66eb2d5',5,'đafdafsdf','2025-09-14 12:39:47','a65495c801844884810f60e6'),('71r000000000000000000001','70o000000000000000000001','68a000000000000000000011',5,'Great ride!','2025-08-30 15:00:09','68b000000000000000000001'),('71r000000000000000000002','70o000000000000000000002','68a000000000000000000012',4,'Good motorbike!','2025-08-30 15:00:09','68b000000000000000000001'),('71r000000000000000000006','70o000000000000000000006','68a000000000000000000015',5,'Fast delivery!','2025-08-30 15:00:09','68b000000000000000000002'),('71r000000000000000000007','70o000000000000000000007','68a000000000000000000014',4,'Nice driver!','2025-08-30 15:00:09','68b000000000000000000001'),('71r000000000000000000010','70o000000000000000000010','68a000000000000000000015',5,'Comfortable car!','2025-08-30 15:00:09','68b000000000000000000001'),('8a804518bd1a45c0a08b82ea',NULL,'126090faa35e4b76a1a0fe4a',5,'dafdfasdf','2025-09-14 12:52:09','a65495c801844884810f60e6'),('9a2e236e509249b9bf6946e5',NULL,'315f97b5d5c14954be07a9ca',4,'this is a good company','2025-09-14 12:50:55','a65495c801844884810f60e6');
/*!40000 ALTER TABLE `reviews` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `rider_profiles`
--

DROP TABLE IF EXISTS `rider_profiles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `rider_profiles` (
  `id` char(24) NOT NULL,
  `user_id` char(24) NOT NULL,
  `full_name` varchar(200) NOT NULL,
  `phone` varchar(40) DEFAULT NULL,
  `img_url` varchar(500) DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `user_id` (`user_id`),
  CONSTRAINT `fk_rider_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `rider_profiles`
--

LOCK TABLES `rider_profiles` WRITE;
/*!40000 ALTER TABLE `rider_profiles` DISABLE KEYS */;
INSERT INTO `rider_profiles` VALUES ('543d23dbe0064c8bbd934ce1','3efdd3845ffe42d5be0e6b45','Rider 2',NULL,'https://yt3.googleusercontent.com/OXbxyxi7XaDta1HS8rAUWzgLcegQxXf4clltpIUE3qCzuO3LxFhRqqatphRP788cVqYiRWWKPXQ=s900-c-k-c0x00ffffff-no-rj','2025-09-01 17:40:04','2025-09-01 17:40:04'),('68d000000000000000000001','68a000000000000000000011','Rider 1','0991111111',NULL,'2025-08-30 15:00:09','2025-08-30 15:00:09'),('68d000000000000000000002','68a000000000000000000012','Rider 2','0992222222',NULL,'2025-08-30 15:00:09','2025-08-30 15:00:09'),('68d000000000000000000003','68a000000000000000000013','Rider 3','0993333333',NULL,'2025-08-30 15:00:09','2025-08-30 15:00:09'),('b99259eac33941ef88033d19','56a5fb12b45e47c9a66eb2d5','John Doe',NULL,NULL,'2025-08-31 08:50:11','2025-08-31 08:50:11');
/*!40000 ALTER TABLE `rider_profiles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `services`
--

DROP TABLE IF EXISTS `services`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `services` (
  `id` char(24) NOT NULL,
  `company_id` char(24) NOT NULL,
  `title` varchar(200) NOT NULL,
  `description` text,
  `img_url` varchar(1024) DEFAULT NULL,
  `price_cents` int NOT NULL DEFAULT '0',
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `fk_services_company` (`company_id`),
  CONSTRAINT `fk_services_company` FOREIGN KEY (`company_id`) REFERENCES `companies` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `services`
--

LOCK TABLES `services` WRITE;
/*!40000 ALTER TABLE `services` DISABLE KEYS */;
INSERT INTO `services` VALUES ('264a15b2c83b43f1a42091a6','ced018add36a40d9bd6af9fa','4-seater airport pickup','this is a test','https://cdn2.fptshop.com.vn/unsafe/1920x0/filters:format(webp):quality(75)/2023_12_14_638381503815348762_taxi-quang-ngai.jpg',100000,1,'2025-09-09 13:18:32','2025-09-09 13:18:47'),('643ab1d8c15a439386aade11','a65495c801844884810f60e6','7-seater schook pickup','fadfas fd à dà đầfdadfadf ad fadfdafdsf',NULL,1200000,1,'2025-09-01 08:19:56','2025-09-01 08:21:08'),('68e000000000000000000001','68b000000000000000000001','Car ride','Ride with car',NULL,150000,1,'2025-08-30 15:00:09','2025-08-30 15:00:09'),('68e000000000000000000002','68b000000000000000000001','Motor ride','Ride with motorbike',NULL,50000,1,'2025-08-30 15:00:09','2025-08-30 15:00:09'),('68e000000000000000000003','68b000000000000000000002','Express delivery','Fast delivery',NULL,200000,1,'2025-08-30 15:00:09','2025-08-30 15:00:09'),('68e000000000000000000004','68b000000000000000000002','Cargo delivery','Heavy items',NULL,350000,1,'2025-08-30 15:00:09','2025-08-30 15:00:09'),('68e000000000000000000005','68b000000000000000000003','Home helper','Basic tasks',NULL,100000,1,'2025-08-30 15:00:09','2025-08-30 15:00:09'),('712900fa6e2e4a75a8f34fa7','a65495c801844884810f60e6','4 chỗ - Hà Nội - Nội Bài updated','Xe điện Vinfast + điều hoà + máy lạnh','https://upload.wikimedia.org/wikipedia/commons/f/f1/Lamborghini_Urus_SE_DSC_8524.jpg',150000,1,'2025-08-31 13:41:38','2025-09-07 03:40:35'),('df46e059a56a4500b7186890','a65495c801844884810f60e6','4-seater Airport Pickup','asbadsf dfdafasdfsadf dafdfasdfsdf dafdfasdf',NULL,1200,0,'2025-08-31 14:27:42','2025-08-31 14:28:49');
/*!40000 ALTER TABLE `services` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `transactions`
--

DROP TABLE IF EXISTS `transactions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `transactions` (
  `id` char(24) NOT NULL,
  `from_wallet_id` char(24) DEFAULT NULL,
  `to_wallet_id` char(24) DEFAULT NULL,
  `amount_cents` int NOT NULL,
  `status` enum('Pending','Completed','Failed') NOT NULL DEFAULT 'Pending',
  `type` tinyint NOT NULL DEFAULT '0',
  `ref_id` varchar(255) DEFAULT NULL,
  `meta_json` json DEFAULT NULL,
  `idempotency_key` varchar(100) DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_idempotency` (`idempotency_key`),
  KEY `fk_tx_from` (`from_wallet_id`),
  KEY `fk_tx_to` (`to_wallet_id`),
  CONSTRAINT `fk_tx_from` FOREIGN KEY (`from_wallet_id`) REFERENCES `wallets` (`id`),
  CONSTRAINT `fk_tx_to` FOREIGN KEY (`to_wallet_id`) REFERENCES `wallets` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `transactions`
--

LOCK TABLES `transactions` WRITE;
/*!40000 ALTER TABLE `transactions` DISABLE KEYS */;
INSERT INTO `transactions` VALUES ('042680b840284b9096e5d7f9','69w000000000000000000005',NULL,20000,'Completed',5,'68a000000000000000000005','{\"method\": \"manual\", \"driverUserId\": \"68a000000000000000000005\"}','eb4a91dc-6c5e-4f48-86c9-354cc3229d4f','2025-09-05 17:02:31'),('0ddd9a2812254e5eaab51010','9be8d44bd3bb458bb83a0636',NULL,10000,'Completed',5,'126090faa35e4b76a1a0fe4a','{\"method\": \"manual\", \"driverUserId\": \"126090faa35e4b76a1a0fe4a\"}','505fdd51-ea36-4169-9dfa-a8e819b1b1c9','2025-09-07 13:33:36'),('20a46021f9664a4b8abd8bb3',NULL,'9be8d44bd3bb458bb83a0636',200000,'Completed',0,'126090faa35e4b76a1a0fe4a','{\"source\": \"manual\", \"driverUserId\": \"126090faa35e4b76a1a0fe4a\"}','200f227b-3de6-4f12-a865-680295fdd93e','2025-09-01 12:47:34'),('21301612783649a59f7ee584','9be8d44bd3bb458bb83a0636',NULL,10000,'Completed',5,'126090faa35e4b76a1a0fe4a','{\"method\": \"manual\", \"driverUserId\": \"126090faa35e4b76a1a0fe4a\"}','e1d8004d-1ea7-49af-83db-150e5d6fceca','2025-09-01 11:25:56'),('28ba5e7958e24d8db68ff713',NULL,'bc20b1e2c14248e9ae80bc03',500000,'Completed',0,NULL,NULL,'3f956eec-ae1f-4eb3-b05c-7d24d4ae780b','2025-08-31 12:50:49'),('301213f9f2ae409d90ac6a9d','bc20b1e2c14248e9ae80bc03','69w000000000000000000001',3000,'Completed',2,'a65495c801844884810f60e6','{\"plan\": \"Premium\"}','pay-membership-a65495c801844884810f60e6-Premium-2025-09-01T02:21:49.003Z','2025-09-01 02:21:49'),('3021c3e52d2f4cabb638887d',NULL,'9be8d44bd3bb458bb83a0636',100000000,'Completed',0,'126090faa35e4b76a1a0fe4a','{\"source\": \"manual\", \"driverUserId\": \"126090faa35e4b76a1a0fe4a\"}','94e54961-c06b-4783-b0df-06519ee67d73','2025-09-07 13:33:10'),('32bada75f4f84321acd500da','9be8d44bd3bb458bb83a0636',NULL,10000,'Completed',0,NULL,NULL,'992dff0d-c447-454f-8c79-edcc9422303a','2025-09-01 11:21:46'),('34b5dc5efce1441cbcde3f99','bc20b1e2c14248e9ae80bc03',NULL,20000,'Completed',5,'a65495c801844884810f60e6','{\"method\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','8de536cb-c7a9-4c05-bd31-f2cf3c52b4cd','2025-09-07 11:18:57'),('3910f2cb650e4bb3be05b4c2',NULL,'69w000000000000000000010',1000000,'Completed',0,'68a000000000000000000010','{\"source\": \"manual\", \"driverUserId\": \"68a000000000000000000010\"}','28313010-5fd7-4c2f-886f-f6463ca20357','2025-09-02 02:54:52'),('452b4793de98495a9354ff29',NULL,'bc20b1e2c14248e9ae80bc03',10000000,'Completed',0,NULL,NULL,'8b8592e8-87ff-4db2-9ed2-b83ea1fb7cb7','2025-08-31 12:50:33'),('4b3a35f984e846b3b18dc26d',NULL,'69w000000000000000000010',244400,'Completed',0,'68a000000000000000000010','{\"source\": \"manual\", \"driverUserId\": \"68a000000000000000000010\"}','593e48ce-6f88-4f6b-9436-d1210040119b','2025-09-02 02:54:58'),('4d32a4dd0ba44c02913f5088','bc20b1e2c14248e9ae80bc03','69w000000000000000000001',3000,'Completed',2,'a65495c801844884810f60e6','{\"plan\": \"Premium\"}','pay-membership-a65495c801844884810f60e6-Premium-2025-08-31T16:41:05.202Z','2025-08-31 16:41:06'),('596b61bddfd64b8fb7d261e0',NULL,'69w000000000000000000005',1500000,'Completed',0,'68a000000000000000000005','{\"source\": \"manual\", \"driverUserId\": \"68a000000000000000000005\"}','0c24dff8-9b0e-4fca-9449-ab8031bc33f5','2025-09-02 08:56:24'),('63f452835b034b2fad5a84d7','bc20b1e2c14248e9ae80bc03',NULL,30000,'Completed',5,'a65495c801844884810f60e6','{\"method\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','b1aa276b-3d51-4349-8a79-3a774e56c5c1','2025-09-05 16:59:03'),('63fe0d3b93eb4222a7cebc51','bc20b1e2c14248e9ae80bc03',NULL,10000,'Completed',5,'a65495c801844884810f60e6','{\"method\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','11d40fff-dd46-4ea5-9542-712cc5a91a11','2025-09-01 10:39:33'),('646428ebbeb14ac6914775a9',NULL,'bc20b1e2c14248e9ae80bc03',2000000,'Completed',0,NULL,NULL,'topup-a65495c801844884810f60e6-1756655978565','2025-08-31 15:59:39'),('6e52621757ac466bb8f9cd7d',NULL,'bc20b1e2c14248e9ae80bc03',100000,'Completed',0,NULL,NULL,'19abfa97-29d6-4b41-b9bb-b411bd8539df','2025-08-31 12:51:31'),('7080869fbb84495a84160541',NULL,'bc20b1e2c14248e9ae80bc03',600000,'Completed',0,NULL,NULL,'topup-a65495c801844884810f60e6-1756656036474','2025-08-31 16:00:37'),('783b01829fdc4d3a84c2eba7','bc20b1e2c14248e9ae80bc03',NULL,50000,'Completed',5,'a65495c801844884810f60e6','{\"method\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','e0213426-5709-47b5-b333-bdfc451626b2','2025-09-01 10:39:18'),('7c2c0849154641cebd826303',NULL,'bc20b1e2c14248e9ae80bc03',2000,'Completed',0,'a65495c801844884810f60e6','{\"source\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','topup-a65495c801844884810f60e6-1756714715189','2025-09-01 08:18:35'),('82e25f4ecb484e95878fcd37','9be8d44bd3bb458bb83a0636',NULL,120000,'Completed',5,'126090faa35e4b76a1a0fe4a','{\"method\": \"manual\", \"driverUserId\": \"126090faa35e4b76a1a0fe4a\"}','896de208-5190-467d-a716-262cf9424214','2025-09-07 13:33:30'),('8a2f1fc9011a4af89f256568',NULL,'bc20b1e2c14248e9ae80bc03',1000000000,'Completed',0,NULL,NULL,'topup-a65495c801844884810f60e6-1756655088850','2025-08-31 15:44:49'),('8edbe7ef9acd4825bbd7d713',NULL,'bc20b1e2c14248e9ae80bc03',10000,'Completed',0,'a65495c801844884810f60e6','{\"source\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','topup-a65495c801844884810f60e6-1756715418821','2025-09-01 08:30:19'),('8fa602aeaa394ab4a2d9b05c','9be8d44bd3bb458bb83a0636',NULL,10000,'Completed',0,NULL,NULL,'76803696-d489-44a1-9ab0-843d6cdb2c45','2025-09-01 11:13:33'),('9032fb6109aa40f4b549f1fb',NULL,'69w000000000000000000005',10000,'Completed',0,'68a000000000000000000005','{\"source\": \"manual\", \"driverUserId\": \"68a000000000000000000005\"}','d808aa44-7842-431d-9c78-6331b18ddb8a','2025-09-05 17:02:11'),('94944ad9dd104b7f954d9945',NULL,'9be8d44bd3bb458bb83a0636',300000,'Completed',0,'126090faa35e4b76a1a0fe4a','{\"source\": \"manual\", \"driverUserId\": \"126090faa35e4b76a1a0fe4a\"}','2013f471-ab35-43c0-b856-228a826f2ead','2025-09-01 12:47:44'),('95b28dcd323746b3b9bcedd3','bc20b1e2c14248e9ae80bc03',NULL,3000,'Completed',0,NULL,NULL,'pay-membership-a65495c801844884810f60e6-Premium-2025-08-31T15:11:58.081Z','2025-08-31 15:11:58'),('98041bf6fc104f958f8b3f10',NULL,'69w000000000000000000009',10000000,'Completed',0,'68a000000000000000000009','{\"source\": \"manual\", \"driverUserId\": \"68a000000000000000000009\"}','e8fb6b29-94ab-42de-b244-842e1da0eba4','2025-09-02 02:54:26'),('9aa4c4e1900a4af6af2b8c22',NULL,'69w000000000000000000006',100,'Completed',0,'68a000000000000000000006','{\"source\": \"manual\", \"driverUserId\": \"68a000000000000000000006\"}','47a821eb-f10e-405e-b525-04a9bcf1a08a','2025-09-02 02:49:59'),('9c5a614d831843ae85ab982c',NULL,'eacfd2b5af834d938ad39328',5000000,'Completed',0,'ced018add36a40d9bd6af9fa','{\"source\": \"manual\", \"companyId\": \"ced018add36a40d9bd6af9fa\"}','bbe1b416-4d93-4296-b4bd-098b95c338c6','2025-09-09 13:16:57'),('9f722e71e3b54ec5a5b33e2d',NULL,'bc20b1e2c14248e9ae80bc03',3000,'Completed',0,'a65495c801844884810f60e6','{\"source\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','topup-a65495c801844884810f60e6-1756693227719','2025-09-01 02:20:28'),('9fcb1f56c8a34f5abeaa55f7','bc20b1e2c14248e9ae80bc03','69w000000000000000000005',15000,'Completed',1,'68a000000000000000000005','{\"note\": \"Lương 2025-09\", \"period\": \"2025-09\", \"companyId\": \"a65495c801844884810f60e6\", \"driverUserId\": \"68a000000000000000000005\"}','salary:a65495c801844884810f60e6:68a000000000000000000005:2025-09:15000','2025-09-07 09:20:19'),('a159ec0d34df4d42ac310bfc',NULL,'bc20b1e2c14248e9ae80bc03',10000,'Completed',0,'a65495c801844884810f60e6','{\"source\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','381fa730-fac2-4b74-a4bf-8707318678e7','2025-09-01 10:28:50'),('a27e80e3d199464a88365469',NULL,'9be8d44bd3bb458bb83a0636',100000,'Completed',0,'126090faa35e4b76a1a0fe4a','{\"source\": \"manual\", \"driverUserId\": \"126090faa35e4b76a1a0fe4a\"}','bcec9cae-a22c-440e-acf6-4b21c84f99d9','2025-09-01 11:13:21'),('a8346787cf37490caa315314',NULL,'bc20b1e2c14248e9ae80bc03',1000,'Completed',0,'a65495c801844884810f60e6','{\"source\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','topup-a65495c801844884810f60e6-1756693947383','2025-09-01 02:32:27'),('ac2019b822864b18a94f8538',NULL,'bc20b1e2c14248e9ae80bc03',10000,'Completed',0,'a65495c801844884810f60e6','{\"source\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','topup-a65495c801844884810f60e6-1756715411179','2025-09-01 08:30:11'),('acb63b6db6ae48f18a1eb7e1',NULL,'bc20b1e2c14248e9ae80bc03',500000,'Completed',0,NULL,NULL,'topup-a65495c801844884810f60e6-1756655999453','2025-08-31 16:00:00'),('b062bd6d79a1422c824cc218',NULL,'9be8d44bd3bb458bb83a0636',200000,'Completed',0,'126090faa35e4b76a1a0fe4a','{\"source\": \"manual\", \"driverUserId\": \"126090faa35e4b76a1a0fe4a\"}','1a0badd7-95b1-42bc-9d11-e705edda58e2','2025-09-02 02:49:36'),('b0a14829c097439295c6cab5',NULL,'bc20b1e2c14248e9ae80bc03',60000,'Completed',0,'a65495c801844884810f60e6','{\"source\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','topup-a65495c801844884810f60e6-1756693218687','2025-09-01 02:20:19'),('c4f46be6a57e4faeb5a2223d',NULL,'bc20b1e2c14248e9ae80bc03',60000,'Completed',0,NULL,NULL,'topup-a65495c801844884810f60e6-1756656059705','2025-08-31 16:01:00'),('cb102b0802154ebeb7f870c8',NULL,'bc20b1e2c14248e9ae80bc03',1000,'Completed',0,'a65495c801844884810f60e6','{\"source\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','topup-a65495c801844884810f60e6-1756693233665','2025-09-01 02:20:34'),('e073dfa2b447400088ec77d1',NULL,'bc20b1e2c14248e9ae80bc03',2000000,'Completed',0,NULL,NULL,'topup-a65495c801844884810f60e6-1756655594489','2025-08-31 15:53:15'),('e4e386fbcdcb4c7d96f9fcdf',NULL,'bc20b1e2c14248e9ae80bc03',2000,'Completed',0,'a65495c801844884810f60e6','{\"source\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','topup-a65495c801844884810f60e6-1756711713058','2025-09-01 07:28:33'),('e616372ceeb8413c8e01c132','9be8d44bd3bb458bb83a0636',NULL,10000,'Completed',0,NULL,NULL,'edd940ca-2a70-486c-823d-da320ebdbfb2','2025-09-01 11:13:50'),('e8b5b63a8f344ad38b83f376','bc20b1e2c14248e9ae80bc03','69w000000000000000000005',150000,'Completed',1,'68a000000000000000000005','{\"note\": \"Lương 2025-09\", \"period\": \"2025-09\", \"companyId\": \"a65495c801844884810f60e6\", \"driverUserId\": \"68a000000000000000000005\"}','salary:a65495c801844884810f60e6:68a000000000000000000005:2025-09:150000','2025-09-07 09:20:51'),('ecfc91f82ecc46f08a6d1472',NULL,'bc20b1e2c14248e9ae80bc03',100000,'Completed',0,'a65495c801844884810f60e6','{\"source\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','0a08dfcc-4593-4d14-892e-c742213463f8','2025-09-07 10:47:45'),('ed3ca4ca7769414b83e04013','eacfd2b5af834d938ad39328',NULL,50000,'Completed',5,'ced018add36a40d9bd6af9fa','{\"method\": \"manual\", \"companyId\": \"ced018add36a40d9bd6af9fa\"}','52704394-5199-4ebf-b859-a6754f112d9d','2025-09-09 13:17:15'),('efcfcfc225b743fe8b9e57dc',NULL,'bc20b1e2c14248e9ae80bc03',6000,'Completed',0,'a65495c801844884810f60e6','{\"source\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','000d68ea-9748-4c14-8f1d-f0f2a27e9a0f','2025-09-07 10:47:53'),('f11a05b6d3bc4913bcba3aef','bc20b1e2c14248e9ae80bc03',NULL,20000,'Completed',5,'a65495c801844884810f60e6','{\"method\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','4733c5ec-445a-4dfe-ae38-82ee76020bed','2025-09-01 10:40:04'),('f691420fbd64436689c5ad6f',NULL,'bc20b1e2c14248e9ae80bc03',10000,'Completed',0,'a65495c801844884810f60e6','{\"source\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','df9cf1d5-2960-4a7e-819c-6bad2d79ce65','2025-09-05 16:58:05'),('fbad76910d3a45459cf89d7f',NULL,'bc20b1e2c14248e9ae80bc03',20000,'Completed',0,'a65495c801844884810f60e6','{\"source\": \"manual\", \"companyId\": \"a65495c801844884810f60e6\"}','cb5a2cff-5f20-4be3-9539-9e522ea58504','2025-09-01 10:40:43');
/*!40000 ALTER TABLE `transactions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `id` char(24) NOT NULL,
  `email` varchar(255) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `role` enum('Admin','Company','Driver','Rider') NOT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `email` (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES ('126090faa35e4b76a1a0fe4a','d1@mycabs.com','$2a$11$.Wuse0X8agLhnxx1TahUhuciPUDOt2fREWfATtNT3wIup.GmDVWcG','Driver',1,'2025-08-31 08:31:02','2025-08-31 08:31:02'),('315f97b5d5c14954be07a9ca','c1@mycabs.com','$2a$11$5uH/IDJNxUmiunSSrMSMDeafR8KXZjFkgceRb8H1C4H0axeVE0Ove','Company',1,'2025-08-31 09:09:12','2025-08-31 09:09:12'),('3efdd3845ffe42d5be0e6b45','r2@mycabs.com','$2a$11$3ceOoEeAbDVZ63nFaxYf7.1wseVN083gUqgQ1509byEXgCA8GQz4y','Rider',1,'2025-09-01 17:40:04','2025-09-01 17:40:04'),('56a5fb12b45e47c9a66eb2d5','r1@mycabs.com','$2a$11$oubAxpRZBkqI5xtuZwdkuORHujMX11/fW4zDedWAXQ.yTiNCr1JPG','Rider',0,'2025-08-31 08:50:11','2025-09-14 21:56:44'),('5d45071d4f12451aa2fa5c2f','c5@mycabs.com','$2a$11$5g.SA7zTDnTYGCGg.MDvo.0qGuizF2hZj20eHdwXnoa2puWWEX79q','Company',0,'2025-09-09 13:13:04','2025-09-14 22:00:34'),('68a000000000000000000001','admin@mycabs.com','$2a$10$abcdefghijklmnopqrstuuuuuuuuuuuuuuuuuuuuuuuuuuu','Admin',1,'2025-08-30 15:00:09','2025-09-14 20:10:56'),('68a000000000000000000002','c2@mycabs.com','$2a$10$abcdefghijklmnopqrstuuuuuuuuuuuuuuuuuuuuuuuuuuu','Company',1,'2025-08-30 15:00:09','2025-09-02 09:45:15'),('68a000000000000000000003','c3@mycabs.com','$2a$10$abcdefghijklmnopqrstuuuuuuuuuuuuuuuuuuuuuuuuuuu','Company',1,'2025-08-30 15:00:09','2025-09-02 09:45:24'),('68a000000000000000000004','c4@mycabs.com','$2a$10$abcdefghijklmnopqrstuuuuuuuuuuuuuuuuuuuuuuuuuuu','Company',1,'2025-08-30 15:00:09','2025-09-02 09:45:34'),('68a000000000000000000005','d7@mycabs.com','$2a$10$abcdefghijklmnopqrstuuuuuuuuuuuuuuuuuuuuuuuuuuu','Driver',1,'2025-08-30 15:00:09','2025-09-02 09:45:59'),('68a000000000000000000006','d2@mycabs.com','$2a$10$abcdefghijklmnopqrstuuuuuuuuuuuuuuuuuuuuuuuuuuu','Driver',1,'2025-08-30 15:00:09','2025-09-02 09:43:49'),('68a000000000000000000007','d3@mycabs.com','$2a$10$abcdefghijklmnopqrstuuuuuuuuuuuuuuuuuuuuuuuuuuu','Driver',1,'2025-08-30 15:00:09','2025-09-02 09:43:57'),('68a000000000000000000008','d4@mycabs.com','$2a$10$abcdefghijklmnopqrstuuuuuuuuuuuuuuuuuuuuuuuuuuu','Driver',1,'2025-08-30 15:00:09','2025-09-02 09:44:07'),('68a000000000000000000009','d5@mycabs.com','$2a$10$abcdefghijklmnopqrstuuuuuuuuuuuuuuuuuuuuuuuuuuu','Driver',1,'2025-08-30 15:00:09','2025-09-02 09:44:15'),('68a000000000000000000010','d6@mycabs.com','$2a$10$abcdefghijklmnopqrstuuuuuuuuuuuuuuuuuuuuuuuuuuu','Driver',1,'2025-08-30 15:00:09','2025-09-02 09:44:47'),('68a000000000000000000011','r3@mycabs.com','$2a$10$abcdefghijklmnopqrstuuuuuuuuuuuuuuuuuuuuuuuuuuu','Rider',1,'2025-08-30 15:00:09','2025-09-02 09:46:24'),('68a000000000000000000012','r4@mycabs.com','$2a$10$abcdefghijklmnopqrstuuuuuuuuuuuuuuuuuuuuuuuuuuu','Rider',1,'2025-08-30 15:00:09','2025-09-02 09:46:30'),('68a000000000000000000013','r5@mycabs.com','$2a$10$abcdefghijklmnopqrstuuuuuuuuuuuuuuuuuuuuuuuuuuu','Rider',1,'2025-08-30 15:00:09','2025-09-02 09:46:46'),('68a000000000000000000014','r6@mycabs.com','$2a$10$abcdefghijklmnopqrstuuuuuuuuuuuuuuuuuuuuuuuuuuu','Rider',1,'2025-08-30 15:00:09','2025-09-02 09:46:55'),('68a000000000000000000015','r7@mycabs.com','$2a$10$abcdefghijklmnopqrstuuuuuuuuuuuuuuuuuuuuuuuuuuu','Rider',1,'2025-08-30 15:00:09','2025-09-02 09:47:05');
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `wallets`
--

DROP TABLE IF EXISTS `wallets`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `wallets` (
  `id` char(24) NOT NULL,
  `owner_type` enum('Company','Driver','Rider','Admin') NOT NULL,
  `owner_ref_id` char(24) NOT NULL,
  `balance_cents` int NOT NULL DEFAULT '0',
  `low_balance_threshold` int NOT NULL DEFAULT '10000',
  `updated_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_wallet_owner` (`owner_type`,`owner_ref_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `wallets`
--

LOCK TABLES `wallets` WRITE;
/*!40000 ALTER TABLE `wallets` DISABLE KEYS */;
INSERT INTO `wallets` VALUES ('69w000000000000000000001','Admin','68a000000000000000000001',0,10000,'2025-08-30 15:00:09'),('69w000000000000000000002','Company','68b000000000000000000001',1000000,10000,'2025-08-30 15:00:09'),('69w000000000000000000003','Company','68b000000000000000000002',2000000,10000,'2025-08-30 15:00:09'),('69w000000000000000000004','Company','68b000000000000000000003',50000,10000,'2025-08-30 15:00:09'),('69w000000000000000000005','Driver','68a000000000000000000005',1655000,10000,'2025-09-07 09:20:51'),('69w000000000000000000006','Driver','68a000000000000000000006',100,10000,'2025-09-02 02:49:59'),('69w000000000000000000007','Driver','68a000000000000000000007',0,10000,'2025-08-30 15:00:09'),('69w000000000000000000008','Driver','68a000000000000000000008',0,10000,'2025-08-30 15:00:09'),('69w000000000000000000009','Driver','68a000000000000000000009',10000000,10000,'2025-09-02 02:54:26'),('69w000000000000000000010','Driver','68a000000000000000000010',1244400,10000,'2025-09-02 02:54:58'),('69w000000000000000000011','Rider','68a000000000000000000011',1000000,10000,'2025-08-30 15:00:09'),('69w000000000000000000012','Rider','68a000000000000000000012',1000000,10000,'2025-08-30 15:00:09'),('69w000000000000000000013','Rider','68a000000000000000000013',1000000,10000,'2025-08-30 15:00:09'),('69w000000000000000000014','Rider','68a000000000000000000014',1000000,10000,'2025-08-30 15:00:09'),('69w000000000000000000015','Rider','68a000000000000000000015',1000000,10000,'2025-08-30 15:00:09'),('9be8d44bd3bb458bb83a0636','Driver','126090faa35e4b76a1a0fe4a',100640000,10000,'2025-09-07 13:33:36'),('bc20b1e2c14248e9ae80bc03','Company','a65495c801844884810f60e6',1016691000,10000,'2025-09-07 11:18:57'),('eacfd2b5af834d938ad39328','Company','ced018add36a40d9bd6af9fa',4950000,10000,'2025-09-09 13:17:15');
/*!40000 ALTER TABLE `wallets` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2025-09-14 22:05:24
