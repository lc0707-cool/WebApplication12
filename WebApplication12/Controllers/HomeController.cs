using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApplication12.Models;
using Microsoft.Data.SqlClient;

namespace WebApplication12.Controllers
{
    public class HomeController : Controller
    {
        // 資料庫連線字串
        string CS = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\美芳\OneDrive\文件\store.mdf;Integrated Security=True;Connect Timeout=30;Encrypt=True";

        // 全域宣告資安風險對照字典
        Dictionary<string, string> MyRisk = new Dictionary<string, string>();

        // 建構子：初始化資安風險項目
        public HomeController()
        {
            MyRisk.Add("X1", "資料外洩");
            MyRisk.Add("X2", "當機");
            MyRisk.Add("X3", "資料竄改");
            MyRisk.Add("X4", "中毒");
            MyRisk.Add("X0", "風險尚未評估"); // 預設初始值
        }

        // ==========================================
        // 1. 產品列表 (只顯示 IsDelete = 0 或 Null 的正常資產)
        // ==========================================
        public IActionResult Index()
        {
            ViewBag.RiskDictionary = MyRisk;
            List<Product> MyProduct = new List<Product>();
            using (SqlConnection X = new SqlConnection(CS))
            {
                X.Open();
                string G = "select Id, Item, Risk from [Table] where IsDelete = 0 or IsDelete is null";
                SqlCommand Q = new SqlCommand(G, X);
                SqlDataReader R = Q.ExecuteReader();
                while (R.Read())
                {
                    int id = Convert.ToInt16(R["Id"]);
                    string item = R["Item"].ToString();
                    string dbRisk = R["Risk"] != DBNull.Value ? R["Risk"].ToString() : "X0";

                    // 利用分割字串與字典進行風險代碼翻譯
                    string translatedRisk = TranslateRiskCode(dbRisk);

                    MyProduct.Add(new Product(id, item, translatedRisk));
                }
            }
            return View(MyProduct);
        }

        // ==========================================
        // 2. 新增產品功能 (基層日常資產錄入)
        // ==========================================
        public IActionResult Privacy2()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddItem(int AddId, string AddProduct)
        {
            using (SqlConnection X = new SqlConnection(CS))
            {
                X.Open();

                // 【防呆第一步】先去資料庫查詢這個 Id 是不是已經有人用了，並順便查出它的刪除狀態
                string checkQuery = "select IsDelete from [Table] where Id = @Id";
                SqlCommand checkCmd = new SqlCommand(checkQuery, X);
                checkCmd.Parameters.AddWithValue("@Id", AddId);

                object result = checkCmd.ExecuteScalar(); // 讀取單一欄位結果

                if (result != null) // 代表這個 Id 已經存在於資料庫中了！
                {
                    int isDeleteStatus = result != DBNull.Value ? Convert.ToInt32(result) : 0;

                    if (isDeleteStatus == 1)
                    {
                        // 情況 B：在回收桶裡
                        TempData["Result"] = $"⚠️ 錯誤：編號 {AddId} 已經存在於「資源回收桶」中！請先至回收桶進行復原。";
                    }
                    else
                    {
                        // 情況 A：是正常的產品
                        TempData["Result"] = $"❌ 錯誤：編號 {AddId} 已被其他產品佔用，請使用其他編號！";
                    }

                    // 帶有錯誤訊息導回首頁，絕對不閃退！
                    return RedirectToAction("Index");
                }

                // 【安全通過】如果沒重複，才安心執行 Insert
                try
                {
                    string insertQuery = "insert into [Table] (Id, Item, Risk, IsDelete) values (@Id, @Item, 'X0', 0)";
                    SqlCommand insertCmd = new SqlCommand(insertQuery, X);
                    insertCmd.Parameters.AddWithValue("@Id", AddId);
                    insertCmd.Parameters.AddWithValue("@Item", AddProduct);
                    insertCmd.ExecuteNonQuery();

                    TempData["Result"] = $"【作業層成功】已成功建立新資產編號 {AddId}：{AddProduct}";
                }
                catch (Exception ex)
                {
                    // 萬一有其他突發的資料庫錯誤，用 try-catch 死死攔截，確保系統安全
                    TempData["Result"] = $"⚠️ 系統異常：資料庫寫入失敗。原因：{ex.Message}";
                }
            }

            return RedirectToAction("Index");
        }

        // ==========================================
        // 3. 低階作業即時面板 (動態 Checkbox 防錯畫面)
        // ==========================================
        public IActionResult Privacy3()
        {
            // 將全域字典傳遞給前端，用以動態渲染 Checkbox 方塊
            ViewBag.RiskDictionary = MyRisk;
            return View();
        }

        // 【Ajax API】供前端即時非同步查詢單一資產的風險代碼
        [HttpGet]
        public string GetRiskAjax(int id)
        {
            using (SqlConnection X = new SqlConnection(CS))
            {
                X.Open();
                string G = "select Risk from [Table] where Id=@id and (IsDelete = 0 or IsDelete is null)";
                SqlCommand Q = new SqlCommand(G, X);
                Q.Parameters.AddWithValue("@id", id);
                SqlDataReader R = Q.ExecuteReader();
                if (R.Read())
                {
                    return R["Risk"] != DBNull.Value ? R["Risk"].ToString() : "X0";
                }
            }
            Response.StatusCode = 404; // 查無此正常資產時拋出錯誤碼
            return "NotFound";
        }

        // 【Ajax API】接收前端勾選組合而成的代碼串，並無刷新即時寫入資料庫
        [HttpPost]
        public string UpdateRiskAjax(int id, string risk)
        {
            using (SqlConnection X = new SqlConnection(CS))
            {
                X.Open();
                string G = "update [Table] set Risk=@risk where Id=@id";
                SqlCommand Q = new SqlCommand(G, X);
                Q.Parameters.AddWithValue("@id", id);
                Q.Parameters.AddWithValue("@risk", risk ?? "X0");
                Q.ExecuteNonQuery();
            }
            return $"【作業層通知】產品 ID {id} 的資安風險項目已即時同步更新至資料庫！";
        }

        // ==========================================
        // 4. 微軟資源回收桶功能 (軟刪除、復原、抹除)
        // ==========================================

        // 資源回收桶畫面：只抓取 IsDelete = 1 (已被刪除) 的資料
        public IActionResult Trash()
        {
            List<Product> DeletedProducts = new List<Product>();
            using (SqlConnection X = new SqlConnection(CS))
            {
                X.Open();
                string G = "select Id, Item, Risk from [Table] where IsDelete = 1";
                SqlCommand Q = new SqlCommand(G, X);
                SqlDataReader R = Q.ExecuteReader();
                while (R.Read())
                {
                    DeletedProducts.Add(new Product(
                        Convert.ToInt16(R["Id"]),
                        R["Item"].ToString(),
                        R["Risk"].ToString()
                    ));
                }
            }
            return View(DeletedProducts);
        }

        // 【Ajax 動作】產品列表點擊刪除時 -> 觸發軟刪除 (IsDelete 改為 1)
        [HttpPost]
        public string SoftDeleteAjax(int id)
        {
            using (SqlConnection X = new SqlConnection(CS))
            {
                X.Open();
                string G = "update [Table] set IsDelete = 1 where Id = @id";
                SqlCommand Q = new SqlCommand(G, X);
                Q.Parameters.AddWithValue("@id", id);
                Q.ExecuteNonQuery();
            }
            return "品項已成功移至資源回收桶。";
        }

        // 【Ajax 動作】資源回收桶點擊復原時 -> (IsDelete 改回 0)
        [HttpPost]
        public string RecoverItemAjax(int id)
        {
            using (SqlConnection X = new SqlConnection(CS))
            {
                X.Open();
                string G = "update [Table] set IsDelete = 0 where Id = @id";
                SqlCommand Q = new SqlCommand(G, X);
                Q.Parameters.AddWithValue("@id", id);
                Q.ExecuteNonQuery();
            }
            return "資產資料已成功復原至正常產品列表！";
        }

        // 【Ajax 動作】資源回收桶點擊永久刪除時 -> 徹底從資料庫 DELETE
        [HttpPost]
        public string RealDeleteAjax(int id)
        {
            using (SqlConnection X = new SqlConnection(CS))
            {
                X.Open();
                string G = "delete from [Table] where Id = @id";
                SqlCommand Q = new SqlCommand(G, X);
                Q.Parameters.AddWithValue("@id", id);
                Q.ExecuteNonQuery();
            }
            return "警報：該資產已從系統資料庫中永久抹除！";
        }

        // ==========================================
        // 5. 系統預設自帶 Action
        // ==========================================
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // 私有輔助方法：將資料庫的多重代碼轉化成中文說明
        private string TranslateRiskCode(string dbRisk)
        {
            string[] riskunit = dbRisk.Split(';');
            string translatedRisk = string.Empty;
            for (int i = 0; i < riskunit.Length; i++)
            {
                string key = riskunit[i].Trim();
                if (MyRisk.ContainsKey(key))
                    translatedRisk += (translatedRisk == string.Empty ? "" : ", ") + MyRisk[key];
                else
                    translatedRisk += (translatedRisk == string.Empty ? "" : ", ") + key;
            }
            return translatedRisk;
        }
    }
}

?