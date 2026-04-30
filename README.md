# Fintech Wallet System

A learning project building a digital wallet platform with bank account linking, UPI-style VPA transfers, and double-entry ledger accounting.

## What's the Idea?

Building a fintech wallet where users can:
- Create an account and complete basic KYC
- Link their bank account
- Generate a UPI-like VPA address for receiving payments
- Transfer money peer-to-peer and withdraw to their bank
- See transaction history with audit trails

Think simplified BHIM/PhonePe for learning purposes.

## What I've Built So Far

### Core Architecture
- **Clean separation**: Core (entities/enums), Infrastructure (data/migrations), API (endpoints to come)
- **.NET 8** with Entity Framework Core for data access
- **SQL Server** for persistence (LocalDB for dev)

### Data Model
- **User Management**: Basic user registration with email/phone uniqueness
  - One user → one wallet (by design for now)
  - One user → one VPA address
  - One user → one linked bank account
  
- **Wallet & Balance**: 
  - Decimal(18,2) for financial accuracy
  - Wallet status tracking (Active, Inactive, etc.)
  - Soft deletes for data retention

- **Transactions**:
  - Flexible architecture: Wallet-to-Wallet, Wallet-to-Bank, Bank-to-Wallet transfers
  - Idempotency keys to prevent duplicate charges
  - Transaction status workflow (Initiated → Completed/Failed)
  - Check constraints ensuring valid source/destination combinations

- **Ledger System** (Double-Entry Bookkeeping):
  - Every transaction creates ledger entries
  - Debit/Credit tracking for audit trail
  - Balance snapshots per entry
  - Immutable once created

- **Security & Compliance**:
  - Password and Payment PIN hashed (never plaintext)
  - KYC workflow with government ID verification
  - Account masking (only last 4 digits shown)
  - Verification flags for bank accounts
  - Soft deletes and audit timestamps

- **Simulated Bank Service**:
  - Separate microservice for bank account simulation
  - Bank and BankAccount entities with transaction history
  - Acts as external system for testing transfers

### Database Schema (Created ✅)
Users (1) → (1) Wallet Users (1) → (1) UserCredentials Users (1) → (1) UserKyc Users (1) → (1) LinkedBankAccount Wallets (1) → (1) Vpa Wallets (1) ← → (M) Transactions (as source/destination) Transactions (1) → (M) LedgerEntries


All relationships are configured with proper constraints and cascade rules.

## What's Working
- ✅ Complete database schema with EF Core migrations
- ✅ All entity relationships properly configured (no shadow properties!)
- ✅ Financial constraints (decimal precision, check constraints)
- ✅ Audit trail setup (CreatedAt, UpdatedAt, soft deletes)
- ✅ Optimistic concurrency (RowVersion) on critical entities
- ✅ Simulated bank system ready

## What's Next (Immediate)

### Phase 1: Core Services
- [ ] **UserService**: Register user, hash password/PIN
- [ ] **WalletService**: Create wallet on registration, manage balance
- [ ] **VpaService**: Generate unique VPA addresses
- [ ] **BankLinkingService**: Simulate bank account verification
- [ ] **TransactionService**: Orchestrate transfers with ledger entries

### Phase 2: API Endpoints
- [ ] User registration & login
- [ ] Wallet balance queries
- [ ] Initiate P2P transfer
- [ ] Initiate withdrawal to bank
- [ ] Transaction history

### Phase 3: Polish
- [ ] Input validation & error handling
- [ ] Logging & monitoring
- [ ] Unit tests for services
- [ ] API documentation

## Why This Approach?

**One-to-One by Design**: Currently restricting to one wallet per user, one VPA per wallet. This simplifies the initial build while the schema is flexible enough to scale later (just remove unique constraints and adjust relationships).

**Double-Entry Ledger**: Even for a learning project, implementing proper accounting practices now saves refactoring later. Every rupee movement is traceable.

**Idempotency Keys**: Payment systems must handle retries. Built this in from day one to avoid duplicate charges when requests timeout.

**Separate Bank Service**: Keeps the wallet and bank concerns isolated, simulating a real distributed system.

## Tech Stack
- **.NET 8**
- **Entity Framework Core** (Code-first migrations)
- **SQL Server** (LocalDB for development)
- **C# 12**

## Running It
Prerequisites
- Visual Studio 2022 or VS Code with .NET 8
- SQL Server LocalDB
Setup
1. Clone repo
2. Open WalletSystem.sln
3. Package Manager Console: Update-Database
4. Run WalletSystem.API

For SimulatedBank separately:

dotnet run --project SimulatedBank



## Current Limitations (Intentional)
- No authentication/authorization yet (focusing on business logic first)
- One wallet per user (will expand)
- No real bank integration (using simulator)
- No actual payment gateway (mocking transfers)
- No API documentation yet (endpoints TBD)

## Notes for Future Me
- When scaling to multiple wallets per user, update the unique indexes
- Consider adding a WalletType enum if building different wallet categories
- Think about transaction fees/commissions architecture
- Rate limiting will be important for production
- Add comprehensive logging before going live

---

**Status**: Early development 
