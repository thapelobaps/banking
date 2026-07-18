# Appwrite migration: South African registration

This branch replaces the previous US-oriented registration model with a South African registration model. Apply these Appwrite schema changes before testing new registrations.

## User collection

Keep these existing attributes:

- `userId` — string, required, indexed/unique where appropriate
- `email` — email or string, required
- `firstName` — string, required
- `lastName` — string, required
- `address1` — string, required
- `city` — string, required
- `postalCode` — string, required
- `dateOfBirth` — string, required; the application stores ISO `YYYY-MM-DD`

Add these attributes:

- `mobileNumber` — string, required, maximum 16 characters; stored in `+27XXXXXXXXX` format
- `suburb` — string, required, maximum 60 characters
- `province` — string, required, maximum 30 characters
- `country` — string, required, maximum 30 characters; value is `South Africa`
- `termsAcceptedAt` — datetime or ISO string, required
- `privacyAcceptedAt` — datetime or ISO string, required

Remove or make optional before deployment:

- `state`
- `ssn`

Do not replace `ssn` with a South African ID number or passport number in the basic registration collection. Identity verification belongs in a later secured KYC workflow with separate access controls, retention rules and audit logging.

## Bank collection

Keep these attributes:

- `userId`
- `bankId`
- `accountId`
- `accountNumber`
- `bankName`
- `balance`
- `currency`
- `linkedAt`

Add:

- `branchCode` — string, required, maximum 6 characters

Remove or make optional:

- `routingNumber`

The current demo account uses `ZAR`, a South African branch code and clearly labelled mock data. It does not represent a live bank connection.

## Existing users

Existing users should continue to sign in because profile loading does not require the new properties at runtime. Before making new attributes required, backfill existing documents where necessary:

- Set `country` to `South Africa` for South African users.
- Populate `province`, `suburb` and `mobileNumber` from verified user information only.
- Do not copy old US `state` or `ssn` values into the new model.
- Record consent timestamps only when valid consent evidence exists. Do not invent historical consent.

## Safe deployment order

1. Rotate the exposed Appwrite API key and update local/deployment secrets.
2. Add the new optional attributes.
3. Deploy the code to a non-production environment.
4. Test sign-in with an existing user.
5. Test a new South African registration.
6. Backfill verified existing-user data.
7. Make required fields mandatory only after backfill is complete.
8. Remove the deprecated `state`, `ssn` and `routingNumber` attributes after confirming no production code reads them.

## Manual registration checks

- State, US state codes, SSN and five-digit ZIP rules are absent.
- All nine South African provinces are available.
- `0821234567` and `+27821234567` normalise to `+27821234567`.
- Invalid mobile numbers are rejected.
- Postal code accepts exactly four digits.
- Date of birth accepts `DD/MM/YYYY` and stores ISO `YYYY-MM-DD`.
- Password confirmation is required and mismatches are rejected.
- Terms and privacy acknowledgement are required.
- Duplicate email errors are user-safe.
- Successful registration creates the Appwrite account, user profile, demo bank account and session.
- No ID number, passport number, SSN or real banking credentials are requested.
