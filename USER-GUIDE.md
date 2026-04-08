# 🥛 Raj Dairy Pro v2.1 - Complete User Guide

## App Kya Hai?
Raj Dairy Pro ek **complete dairy management application** hai jo dairy ke sabhi kaam digital kar deti hai - milk collection, farmer hisab-kitab, ghee production, orders, subscriptions, expenses, payments - sab kuch ek jagah se manage ho jata hai.

---

## 🔗 App URLs

| URL | Kaun Use Karega | Kaam |
|-----|-----------------|------|
| `rajdairy.onrender.com` | **Admin (Dairy Owner)** | Full admin panel - sab kuch manage karein |
| `rajdairy.onrender.com/Public/Login` | **Partner / Farmer** | Apna hisab dekhein (read-only) |
| `rajdairy.onrender.com/Public/TrackOrder` | **Koi bhi** | Order number se delivery track karein |

### Default Admin Login:
- **Username:** `admin`
- **Password:** `rajdairy123`
- *(Login ke baad Settings > Change Password se badal lein)*

---

## 📱 Mobile App Install (PWA)
1. Phone mein Chrome se `rajdairy.onrender.com` kholein
2. **"Install App"** ya **"Add to Home Screen"** option aayega
3. Install karein - Home screen pe **Raj Dairy** icon aa jayega
4. Ab app ki tarah use karein - full screen mein khulegi

---

## 📋 Modules & Features

### 1. 🏠 Dashboard
- **Aaj ka summary** - kitna milk aaya, kitna paisa, kitne orders
- **7-day chart** - hafta bhar ka milk collection graph
- **Milk type distribution** - Cow / Buffalo / Mixed ka ratio
- **Quick action buttons** - ek click mein naya collection, order banao
- **Recent activity** - haal ke orders aur collections

### 2. 🥛 Milk Collection
#### Quick Entry (⚡ Sabse Important Feature)
- **Farmer dropdown** se naam select karo - mobile, milk type auto-fill
- Sirf **3 numbers** daalo: Quantity, Fat%, SNF%
- **Rate auto-calculate** hota hai rate chart se
- **Total auto-calculate** hota hai
- Save karte hi **WhatsApp receipt** farmer ke number pe ready!
- **Today's summary** side mein dikhta hai

#### Milk Collection List
- Sabhi collections ka record
- **Search** farmer name, milk type, date range se
- **Summary cards** - total quantity, amount, avg fat, avg snf

### 3. 💰 Milk Rate Chart
- Fat% aur SNF% ke base pe rate set karein
- **Cow / Buffalo / Mixed** - alag-alag rates
- Jab bhi milk collection hoti hai, rate yahan se auto-pick hota hai

### 4. 📦 Dairy Products
- Sabhi products manage karein - Milk, Dahi, Paneer, Butter, Ghee, Cream, Chaach, Khoya, Cheese, Lassi, Ice Cream
- **Stock tracking** - kitna stock hai
- **Expiry tracking** - kab expire hoga, alert aata hai
- **Price management**

### 5. 🔥 Ghee Production
- **Batch tracking** - har batch ka record
- Milk used vs Ghee produced
- **Yield rate auto-calculate**
- Quality grades - Premium, Standard, Economy
- **Stock management**

### 6. 👥 Partners (Farmers & Buyers)
#### Partner Add Karein
- Naam, Mobile, Address, Type (Supplier/Buyer/Both)
- **Unique Access Code** auto-generate hota hai (e.g. RD0001)
- Yeh code partner ko dein - woh apna hisab dekh sakta hai

#### Partner Ledger (Hisab-Kitab)
- Har partner ka **complete hisab** - kitna diya, kitna liya, kitna baaki
- **Transactions** - Milk, Ghee, Paneer, Cash - sab track hota hai
- **Balance** - kitna dena hai, kitna lena hai
- **WhatsApp pe hisab bhejein** - ek click mein

#### Monthly Summary
- Farmer ka **mahine ka poora hisab** - day-by-day table
- Total milk, total amount, avg fat, avg snf, collection days
- Paid vs Balance Due
- **Print** ya **WhatsApp pe statement** bhejein

### 7. 💵 Transactions
- **Maal Diya** (Given) - jab aapne partner ko kuch diya
- **Maal Liya** (Received) - jab partner ne aapko kuch diya
- Items: Milk, Ghee, Paneer, Curd, Butter, Cream, Cash, Other
- **Payment Status**: Paid / Pending / Partial
- Jab milk collection hoti hai toh **auto-transaction** ban jaata hai

### 8. 📋 Orders
- **Order create** karein - partner, product, quantity, rate, delivery date
- **Order number** auto-generate: ORD20260408XXXX
- **Status pipeline**: Pending → Confirmed → Out for Delivery → Delivered
- **One-click status update** buttons
- **Invoice** - professional printable invoice
- Partner apne portal se **order track** kar sakta hai

### 9. 📅 Subscriptions
- **Daily / Weekly / Alternate day** delivery subscriptions
- Products: Cow Milk, Buffalo Milk, Ghee, Paneer, etc.
- **Delivery slots**: Morning / Evening / Both
- **Pause / Resume / Cancel** subscription
- **Monthly revenue estimate** auto-calculate

### 10. 💳 Farmer Payments
- **Sabhi farmers ka pending balance** ek page pe
- **Quick Pay** - amount daalo, mode select karo (Cash/UPI/Bank), pay karo
- **UPI redirect** - PhonePe / Google Pay / Paytm direct khulega
- Auto-transaction create hota hai

### 11. 💸 Expenses
- Sabhi kharchon ka record - Bijli, Labour, Transport, Packaging, Equipment, Rent, Chara, Repair, Mobile, Taxes
- **Payment mode** - Cash, UPI, Bank Transfer, Cheque
- **Date filter** with total

### 12. 📊 Profit & Loss Report
- **Income breakdown** - Milk sales, Ghee sales, Product sales, Other
- **Expense breakdown** - Category-wise with percentage bars
- **Net Profit/Loss** with margin %
- **WhatsApp pe share** karein

### 13. 📅 Daily Report
- Aaj ka complete summary
- Milk collected, revenue, expenses, net profit
- Farmer count, avg fat%, avg snf%
- **WhatsApp pe share** karein

### 14. 📈 Analytics
- **6-month charts** - milk collection & revenue
- **Weekly milk trend**
- **Ghee production chart**
- **Product distribution** (doughnut chart)
- **Top 10 farmers** ranking
- **Growth %** indicators

### 15. 🏆 Farmer Leaderboard
- **Top 3 Podium** - Gold/Silver/Bronze
- Full ranking - total milk, amount, avg fat%, SNF%, collection days
- **Progress bars** - visual comparison
- **Month filter**

### 16. ✅ Farmer Attendance
- Aaj kaun aaya, kaun nahi
- **Morning / Evening** shift wise
- **Streak** - kitne consecutive din aaya
- **7-day heatmap** - green = present, red = absent
- Quick add button for absent farmers

### 17. 🧮 Rate Calculator
- Fat% aur SNF% daalo - **turant rate milega**
- Milk type select karo (Cow/Buffalo/Mixed)
- Quantity daalo - total amount bhi milega
- **Current rate chart** reference mein dikhta hai
- Field mein use karne ke liye perfect tool

### 18. 📢 WhatsApp Broadcast
- **Ek saath sabko message** bhejein
- **6 ready templates** - Rate Change, Payment, Holiday, Collection Time, Festival, New Product
- **Send to** - Sabhi / Suppliers / Buyers / Selected
- **Live preview** - WhatsApp style
- Bulk WhatsApp links generate hote hain

### 19. 🚚 Delivery Route
- **Area-wise order grouping**
- Date filter - kisi bhi din ka route
- Har area mein order details, customer, phone
- **Direct call button**

### 20. 🔔 Notifications
- **Auto alerts** - low ghee stock, expiring products, expired products
- **Bell icon** with unread count
- Mark read / Mark all read
- **30-second auto-refresh**

### 21. 🌙 Dark Mode
- Navbar mein **moon icon** click karein
- Poora app dark ho jayega
- Setting remember rehti hai

### 22. 👤 Admin Profile
- **Photo upload** (Google Sheets mein save rehti hai)
- Dairy stats summary
- Overall revenue & collection data

### 23. ⚙️ Settings
- **Change Username / Password**
- **QR Code upload** - payment ke liye
- **UPI ID set** karein - partners ko dikhega
- App info & Google Sheet link
- Data status (active/inactive products & rates)

### 24. 📤 CSV Export (Excel)
- **Milk Collection CSV** - poore mahine ka data
- **Farmer Summary CSV** - sabhi farmers ka balance
- **Expense CSV** - sabhi expenses

---

## 👨‍🌾 Partner Portal (Farmer/Buyer ke liye)

Partner ko yeh link aur access code dein:
```
Link: rajdairy.onrender.com/Public/Login
Code: RD0001 (har partner ka unique code)
```

### Partner ko kya dikhega:
1. **Summary** - Total diya, liya, pending, balance
2. **Milk Collection** - Kab kab kitna milk diya, fat%, SNF%, rate, amount
3. **Orders** - Order status tracking with visual progress bar
4. **Hisab** - Sabhi transactions ki list
5. **Subscriptions** - Active subscriptions
6. **Payment** - QR Code scan / UPI pay buttons (PhonePe, GPay, Paytm)

### Partner kya NAHI kar sakta:
- ❌ Kuch bhi edit/delete nahi kar sakta
- ❌ Admin panel access nahi kar sakta
- ❌ Doosre partner ka data nahi dekh sakta

---

## 📱 Order Tracking (Bina Login)

Kisi ko bhi order track karvana ho:
```
Link: rajdairy.onrender.com/Public/TrackOrder
Order Number: ORD20260408XXXX
```

Visual step tracker dikhega: Pending → Confirmed → Out for Delivery → Delivered

---

## 🔄 Daily Workflow (Roz ka kaam)

### Subah (Morning Shift):
1. **Quick Entry** kholein
2. Farmer select → Qty, Fat, SNF daalo → Save
3. WhatsApp receipt bhejein
4. Repeat for all farmers

### Shaam (Evening Shift):
1. Same process - Quick Entry se add karein

### Month End:
1. **Farmer Payments** - sabka pending balance dekhein
2. Payment karein (Cash/UPI/Bank)
3. **Monthly Summary** - har farmer ko WhatsApp statement bhejein

### Weekly:
1. **Analytics** dekhein - growth track karein
2. **Leaderboard** dekhein - top farmers identify karein
3. **Attendance** check karein - absent farmers follow up

### Daily (Optional):
1. **Daily Report** dekhein aur WhatsApp pe share karein
2. **Delivery Route** se orders deliver karein
3. **Notifications** check karein - stock alerts

---

## 💡 Tips & Tricks

1. **Rate pehle set karein** - MilkRate mein Fat/SNF ranges aur rates daalo, phir milk collection mein rate auto-aayega
2. **Partners pehle add karein** - Quick Entry mein dropdown mein dikhenge
3. **WhatsApp Broadcast** - Rate change hone pe sabko ek saath bata dein
4. **Dark Mode** - raat ko use karein, aankho ko aaraam milega
5. **PWA Install** - phone mein install karein, app ki tarah use karein
6. **CSV Export** - Excel mein data chahiye toh download karein

---

## 🔒 Security

- Admin panel **password protected** hai
- Partner portal **access code** se chalti hai (read-only)
- Forms mein **CSRF protection** hai
- Session **8 ghante** tak active rehti hai

---

## 📊 Data Storage

Saara data **Google Sheets** mein store hota hai:
- **Free** hai, koi cost nahi
- **Real-time** - app se jo bhi karein, sheet mein turant dikhta hai
- **Backup** - Google automatically backup rakhta hai
- **Share** - sheet kisi ko bhi share kar sakte ho

Google Sheet mein yeh tabs hain:
| Tab | Data |
|-----|------|
| MilkCollection | Milk collection records |
| MilkRates | Fat/SNF based rate chart |
| DairyProducts | Products (paneer, dahi, etc.) |
| GheeProducts | Ghee production batches |
| Partners | Farmers & buyers |
| Transactions | Lena-dena records |
| Orders | Order records |
| Subscriptions | Delivery subscriptions |
| Notifications | System alerts |
| Expenses | Kharche |
| Settings | App settings, QR, UPI, photo |

---

## 📞 Support

Koi issue ho toh:
- GitHub: `github.com/rajrevclerx-ops/rajdairy`
- Admin Settings mein Google Sheet ka direct link hai

---

*Raj Dairy Pro v2.1 - Made with ❤️ for Indian Dairy Business*
