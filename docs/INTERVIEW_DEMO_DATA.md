# Interview demo dataset

The development seed builds a connected marketplace story rather than isolated
rows. It covers public discovery, recommendations, social proof, cart and
checkout states, buyer/seller history, library wallets and payouts, and book
moderation.

## Safety and enablement

The dataset is allowed only when `ASPNETCORE_ENVIRONMENT=Development`.
It is disabled in every committed appsettings file. Enable it explicitly in a
dedicated local `.env` file:

```dotenv
DemoData__Enabled=true
```

Startup fails if that setting is enabled outside Development. Keep it disabled
when a Development process points at shared, staging, or production-like data.

Migrations, categories, and the configuration-driven bootstrap administrator
remain separate from demo data. The demo graph is written in one transaction
under a PostgreSQL advisory lock and can be run repeatedly without duplicating
its stable scenarios.

For a repeatable rehearsal, point `ConnectionStrings__DefaultConnection` at a
dedicated database such as `quraaa_interview_demo`. Reruns preserve state changes
made while demonstrating the system; recreate only that dedicated database when
you need the exact pristine matrix again.

## Demo credentials

These credentials are public development data. Never reuse them for a real
account or deployment.

| Persona                  | Login identifier       | Password        | Notes                                                             |
| ------------------------ | ---------------------- | --------------- | ----------------------------------------------------------------- |
| Main buyer               | `+963912345678`        | `User@12345`    | Interests, three locations, favorites, reviews, cart, and history |
| User seller              | `+963912345679`        | `User@12345`    | Own listings, sell history, and an expired buyer order            |
| Pending-checkout buyer   | `+963912345680`        | `User@12345`    | A cart locked in `PendingPayment`                                 |
| Retry buyer              | `+963912345682`        | `User@12345`    | Declined payment with the cart reopened                           |
| Cancelled-order buyer    | `+963912345683`        | `User@12345`    | Cancelled checkout with released stock                            |
| Processing-order buyer   | `+963912345681`        | `User@12345`    | Paid for the user seller's physical book                          |
| Demo super admin         | `+963990000001`        | `Admin@12345`   | Identity roles `Admin` and `SuperAdmin`                           |
| Demo moderator           | `+963990000002`        | `Admin@12345`   | Ordinary `Admin` used by moderation records                       |
| First library dashboard  | `info.lib1@quraaa.com` | `Library@12345` | Approved, active demo wallet, 70% share                           |
| Second library dashboard | `info.lib2@quraaa.com` | `Library@12345` | Approved, wallet onboarding incomplete, 60% share                 |
| Third library dashboard  | `info.lib3@quraaa.com` | `Library@12345` | Approved, no wallet, 55% share                                    |

User login is `POST /api/auth/login`. Library dashboard login is
`POST /api/auth/library/login`. Admin login still uses the real password-plus-OTP
flow; the seed does not bypass OTP security or create a fixed OTP.

All 102 library-owner phone identities use `User@12345`. Their phones follow
`+963930000001` through `+963930000102`, while dashboard emails follow
`info.lib1@quraaa.com` through `info.lib102@quraaa.com`.

## Fresh-database scenario matrix

| Area       | Seeded story                                                                                                                                                        |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Profiles   | Category interests, eight named saved locations, default-location selection, and a masked Visa sample for the main buyer                                            |
| Libraries  | 75 approved, 25 pending review, one rejected, and one awaiting email verification; active/incomplete/missing wallet examples                                        |
| Catalog    | 10 curated books, nine rich authors, edited book version history, French/Arabic/English coverage, plus 60 pagination books and the packaged ebook                   |
| Listings   | Multiple libraries competing on one book, library digital/physical inventory, a user seller, and `Active`, `OutOfStock`, `Sold`, and `Removed` states               |
| Engagement | 10 favorites, 12 ratings, 12 Arabic/English comments, and enough purchase facts for recommendations and popularity ranking                                          |
| Moderation | `Pending`, `InReview`, `Resolved`, and `Rejected` reports; one flagged-visible book and one hidden-for-review book listed by two libraries                          |
| Commerce   | Seven orders covering pending checkout, failed payment, buyer cancellation, expiry, paid processing, completed digital, and completed mixed orders                  |
| History    | Order-linked purchase snapshots plus historical purchases for buy history, rating eligibility, popular books, private ebook streaming, and AI purchase authorization |
| Payouts    | Safe terminal examples: one `Paid`, one `Failed`, and one `NoAmountDue` payout                                                                                      |

The exact totals can be higher on a previously populated database because the
seeder preserves non-demo data and older seed rows.

## Useful fresh-database records

These catalog IDs are deterministic on a fresh demo database. On an existing
database, the seeder can safely reuse a matching natural catalog record while
the demo listing and order identifiers remain deterministic.

| Record               | ID                                     | What it demonstrates                                                |
| -------------------- | -------------------------------------- | ------------------------------------------------------------------- |
| `Dune`               | `77777777-7777-7777-7777-777777777705` | Popular digital/physical book, comments, ratings, purchase/streaming |
| `Clean Code`         | `77777777-7777-7777-7777-777777777704` | Two competing libraries and version history                         |
| `ثلاثية غرناطة`      | `77777777-7777-7777-7777-777777777701` | Arabic discovery, reviews, and completed physical fulfillment       |
| Flagged book         | `77777777-7777-7777-7777-777777777708` | Visible book with a pending report                                  |
| Hidden book          | `77777777-7777-7777-7777-777777777709` | Global catalog filtering and the admin/library moderation queues    |
| Dune digital listing | `88888888-8888-8888-8888-888888888804` | Packaged `books/book1.pdf` purchase and in-app streaming            |
| User-sold listing    | `88888888-8888-8888-8888-888888888809` | Sold stock and a paid item in the seller processing queue           |

The seven order stories use the visible order numbers `DEMO-1001` through
`DEMO-1007`, in this order: completed digital, completed mixed, processing user
sale, pending checkout, failed payment, cancelled, and expired.

## Suggested interview walkthrough

1. Open public discovery with `GET /api/books/home-catalog`, then show
   `GET /api/listings/{id}/details` for the Dune digital listing. The response
   has seller details, rating summary, and recent comments.
2. Log in as the main buyer. Show `GET /api/profile/me`,
   `GET /api/profile/locations`, and `GET /api/books/recommended` with
   `Accept-Language: en` or `ar`.
3. Compare `GET /api/books/most-popular` and favorites, then show the rating
   summary in listing details beside `GET /api/books/{bookId}/comments`.
4. Show the main buyer's active cart with `GET /api/cart/me`, then buyer order
   states through `GET /api/orders/me` and purchase history through
   `GET /api/purchases/me/buy-history`.
5. Use the completed digital purchase's `GET /api/purchases/{purchaseId}/stream`
   route to explain why a Stripe redirect is not payment proof and why the
   purchase stores an immutable file snapshot.
6. Log in as the user seller and call `GET /api/seller/orders` plus
   `GET /api/purchases/me/sell-history` to show the paid physical item.
7. Log in to the first library dashboard and show its inventory, wallet, payout
   history, seller queue, and reports concerning books it lists.
8. Complete admin OTP login, then show the report queue, the hidden book,
   author activation, library approval variants, and book version history.

## Intentionally not seeded

The demo does not create OTPs, refresh tokens, registration/password-reset
challenges, processed webhook inbox rows, fake FCM devices, ready notification
outboxes, or due payouts. Those records are transient security/provider state;
seeding them could send messages or call Firebase, SMTP, or Stripe. The pending
order deliberately has no fabricated provider session: its buyer can either
cancel it locally or create a real test checkout session through the recovery
route. Every seeded payout is terminal, and all listing-publication domain
events are cleared before persistence.

The private demo file is `Quraaa.API/storage/books/book1.pdf`. Keep it in place
to demonstrate the authorized in-app streaming route.
