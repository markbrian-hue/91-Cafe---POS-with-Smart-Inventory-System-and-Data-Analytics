### 📊 Data Analytics & Forecasting
* **Real-Time Dashboard:** View total sales, transaction counts, and average ticket size instantly.
* **"AI" Sales Forecasting:** Uses a statistical moving average model to predict future demand (Rising/Cooling trends).
* **Interactive Charts:** Visualizes hourly traffic, weekly sales patterns, and top-selling products.
* **Date Filtering:** Analyze performance by Today, Yesterday, Last 7 Days, Last 30 Days, or a custom date.

* ## 🛠️ Tech Stack

* **Framework:** ASP.NET Core MVC (.NET 6/8)
* **Database:** MySQL (using Entity Framework Core)
* **Frontend:** HTML5, CSS3, JavaScript, Bootstrap 5
* **Charting:** Chart.js

## ⚙️ Installation & Setup

1.  **Clone the Repository**
    ```bash
    (https://github.com/markbrian-hue/91-Cafe---POS-with-Smart-Inventory-System-and-Data-Analytics.git)
    ```

2.  **Database Setup**
    * Create a MySQL database named `91cafepos`.
    * Update the connection string in `appsettings.json`:
        ```json
        "ConnectionStrings": {
          "DefaultConnection": "Server=localhost;Database=91cafepos;User=root;Password=yourpassword;"
        }
        ```
    * Run the migrations (or import the provided SQL file):
        ```bash
        dotnet ef database update
        ```

3.  **Run the Application**
    ```bash
    dotnet run
    ```
    Access the app at `https://localhost:7048`.
