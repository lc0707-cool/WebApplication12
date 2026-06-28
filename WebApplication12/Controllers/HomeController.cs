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
            LoadRisksFromDatabase();
        }

        // 建立一個私有方法，專門用來跟新表同步
        private void LoadRisksFromDatabase()
        {
            MyRisk.Clear(); // 先清空舊資料
            using (SqlConnection X = new SqlConnection(CS))
            {
                X.Open();
                // 讀取我們剛剛建立的新資料表
                string G = "select RiskCode, RiskName from [RiskDefinition]";
                SqlCommand Q = new SqlCommand(G, X);
                SqlDataReader R = Q.ExecuteReader();
                while (R.Read())
                {
                    MyRisk.Add(R["RiskCode"].ToString().Trim(), R["RiskName"].ToString().Trim());
                }
            }
            // 防呆：確保預設值一定存在
            if (!MyRisk.ContainsKey("X0")) MyRisk.Add("X0", "風險尚未評估");
        }

        // 3. 【全新功能】供使用者在前端動態輸入新風險（如：X5 自訂風險）
        [HttpPost]
        public IActionResult AddCustomRisk(string newRiskName)
        {
            if (string.IsNullOrEmpty(newRiskName)) return RedirectToAction("Index");

            using (SqlConnection X = new SqlConnection(CS))
            {
                X.Open();

                // 自動計算下一個 X 代碼（例如目前有 5 筆， count 完就是 5，剛好當作新代碼 X5）
                string getCountQuery = "select count(*) from [RiskDefinition] where RiskCode != 'X0'";
                SqlCommand countCmd = new SqlCommand(getCountQuery, X);
                int currentCount = Convert.ToInt32(countCmd.ExecuteScalar());
                string nextCode = "X" + (currentCount + 1); // 自動產生 X5, X6...

                // 寫入資料庫的風險定義表
                string insertQuery = "insert into [RiskDefinition] (RiskCode, RiskName) values (@Code, @Name)";
                SqlCommand insertCmd = new SqlCommand(insertQuery, X);
                insertCmd.Parameters.AddWithValue("@Code", nextCode);
                insertCmd.Parameters.AddWithValue("@Name", newRiskName);
                insertCmd.ExecuteNonQuery();

                TempData["Result"] = $"【管理層通知】已成功動態新增全新的資安風險代碼 {nextCode}：{newRiskName}";
            }

            // 重新載入記憶體字典，確保接下來的翻譯（TranslateRiskCode）能立刻認得 X5
            LoadRisksFromDatabase();

            return RedirectToAction("Index");
        }

        // ==========================================
        // 1. 產品列表 (只顯示 IsDelete = 0 或 Null 的正常資產)
        // ==========================================
        // ==========================================
        // 1. 產品矩陣首頁 (基層主動查看)
        // ==========================================
        public IActionResult Index()
        {
            List<Product> list = new List<Product>();

            // 🎯 修正點 1：SQL 語法必須將新增的三個欄位撈出來 (AssetType, RiskLevel, Status)
            string G = "select Id, Item, Risk, IsDelete, AssetType, RiskLevel, Status from [Table] where IsDelete = 0";

            using (SqlConnection X = new SqlConnection(CS))
            {
                X.Open();
                SqlCommand Q = new SqlCommand(G, X);
                SqlDataReader R = Q.ExecuteReader();
                while (R.Read())
                {
                    // 🎯 修正点 2：改用物件初始設定式，並加入 DBNull 防呆
                    Product p = new Product
                    {
                        Id = Convert.ToInt32(R["Id"]),
                        Item = R["Item"].ToString(),
                        // 維持你原本的風險代碼中文轉換
                        Risk = TranslateRiskCode(R["Risk"].ToString()),
                        IsDelete = Convert.ToInt32(R["IsDelete"]),

                        // 🚀 將資料庫欄位內容精準指派給 Model 屬性
                        AssetType = R["AssetType"] != DBNull.Value ? R["AssetType"].ToString() : "未分類",
                        RiskLevel = R["RiskLevel"] != DBNull.Value ? R["RiskLevel"].ToString() : "中",
                        Status = R["Status"] != DBNull.Value ? R["Status"].ToString() : "未處理"
                    };

                    list.Add(p);
                }
            }

            // 將目前的風險對照字典傳給前端 (畫面的勾選面板需要用到)
            ViewBag.RiskDictionary = MyRisk;

            return View(list);
        }

        // ==========================================
        // 2. 新增產品功能 (基層日常資產錄入)
        // ==========================================
        // ==========================================
        // 2. 新增產品功能 (基層主動需求導向資產錄入)
        // ==========================================
        public IActionResult Privacy2()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddItem(string AddProduct, string AddAssetType)
        {
            // 【防呆】確保使用者真的有輸入資產名稱
            if (string.IsNullOrWhiteSpace(AddProduct))
            {
                TempData["Result"] = "❌ 錯誤：資產名稱不能為空白！";
                return RedirectToAction("Index");
            }

            using (SqlConnection X = new SqlConnection(CS))
            {
                X.Open();

                // 【安全執行 Insert】
                // 因為 Id 是自動遞增 (IDENTITY)，我們在 INSERT 語法中直接「移除 Id 欄位與 @Id 參數」
                // 同時，我們無縫注入擴充的三大功能性欄位
                // 【安全執行 Insert】
                try
                {
                    // 🎯 1. SQL 語法乾乾淨淨，全部用 @ 參數代替
                    string insertQuery = @"INSERT INTO [Table] 
                          (Item, Risk, IsDelete, AssetType, RiskLevel, Status) 
                          VALUES 
                          (@Item, 'X0', 0, @AssetType, @RiskLevel, @Status)";

                    SqlCommand insertCmd = new SqlCommand(insertQuery, X);

                    // 🎯 2. 在這裡整齊地指派參數，注意每行結尾都是分號
                    insertCmd.Parameters.AddWithValue("@Item", AddProduct.Trim());
                    insertCmd.Parameters.AddWithValue("@AssetType", !string.IsNullOrEmpty(AddAssetType) ? AddAssetType : "未分類");
                    insertCmd.Parameters.AddWithValue("@RiskLevel", "中");
                    insertCmd.Parameters.AddWithValue("@Status", "未處理");

                    insertCmd.ExecuteNonQuery();

                    TempData["Result"] = $"【系統通知】全新資產「{AddProduct}」已成功錄入，系統已自動配置初始狀態。";
                }
                catch (Exception ex)
                {
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

        [HttpGet]
        public IActionResult GetRiskAjax1(int id)
        {
            string query = "SELECT Item, Risk, RiskLevel, Status FROM [Table] WHERE Id = @Id AND IsDelete = 0";

            using (SqlConnection X = new SqlConnection(CS))
            {
                X.Open();
                SqlCommand cmd = new SqlCommand(query, X);
                cmd.Parameters.AddWithValue("@Id", id);

                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        // 🎯 核心修正：打包成 JSON 物件回傳，讓前端能抓到 Item 名稱、RiskLevel 與 Status
                        var result = new
                        {
                            item = dr["Item"].ToString(),
                            risk = dr["Risk"].ToString(),
                            riskLevel = dr["RiskLevel"] != DBNull.Value ? dr["RiskLevel"].ToString() : "中",
                            status = dr["Status"] != DBNull.Value ? dr["Status"].ToString() : "未處理"
                        };
                        return Json(result);
                    }
                }
            }

            // 如果找不到，回傳 404 狀態碼，讓前端 JavaScript 跑進 error 區塊
            return NotFound();
        }

        // 【Ajax API】接收前端勾選組合而成的代碼串，並無刷新即時寫入資料庫
        [HttpPost]
        public IActionResult UpdateRiskAjax(int id, string risk, string riskLevel, string status)
        {
            // 🎯 核心修正：SQL 同時更新 Risk、RiskLevel、Status 三個欄位
            string query = "UPDATE [Table] SET Risk = @Risk, RiskLevel = @RiskLevel, Status = @Status WHERE Id = @Id";

            using (SqlConnection X = new SqlConnection(CS))
            {
                X.Open();
                SqlCommand cmd = new SqlCommand(query, X);
                cmd.Parameters.AddWithValue("@Risk", risk);
                cmd.Parameters.AddWithValue("@RiskLevel", !string.IsNullOrEmpty(riskLevel) ? riskLevel : "中");
                cmd.Parameters.AddWithValue("@Status", !string.IsNullOrEmpty(status) ? status : "未處理");
                cmd.Parameters.AddWithValue("@Id", id);

                cmd.ExecuteNonQuery();
            }

            return Content($"✅ 【決策同步成功】資產編號 {id} 的風險範疇、等級與處置狀態已即時發布至動態資料庫中。");
        }

        // ==========================================
        // 4. 微軟資源回收桶功能 (軟刪除、復原、抹除)
        // ==========================================
        // ==========================================
        // 3. 資訊資產 - 資源回收桶畫面：只抓取 IsDelete = 1 (已被移至回收桶) 的資料
        // ==========================================
        public IActionResult Trash()
        {
            List<Product> DeletedProducts = new List<Product>();

            // 🎯 修正點 1：使用完整欄位的 SQL 語法
            string query = "SELECT Id, Item, Risk, IsDelete, AssetType, RiskLevel, Status FROM [Table] WHERE IsDelete = 1";

            using (SqlConnection X = new SqlConnection(CS))
            {
                X.Open();

                // 🎯 修正點 2：確保 SqlCommand 綁定的是新語法變數 'query'，而不是舊的 'G'
                SqlCommand Q = new SqlCommand(query, X);
                SqlDataReader R = Q.ExecuteReader();

                while (R.Read())
                {
                    // 🎯 修正點 3：改用物件初始設定式，完美注入 7 個功能欄位並進行 DBNull 防呆
                    Product p = new Product
                    {
                        Id = Convert.ToInt32(R["Id"]),
                        Item = R["Item"].ToString(),
                        // 這裡一樣可以呼叫你的風險代碼轉換方法，讓回收桶顯示中文說明
                        Risk = TranslateRiskCode(R["Risk"].ToString()),
                        IsDelete = Convert.ToInt32(R["IsDelete"]),
                        AssetType = R["AssetType"] != DBNull.Value ? R["AssetType"].ToString() : "未分類",
                        RiskLevel = R["RiskLevel"] != DBNull.Value ? R["RiskLevel"].ToString() : "中",
                        Status = R["Status"] != DBNull.Value ? R["Status"].ToString() : "未處理"
                    };

                    DeletedProducts.Add(p);
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
            return "已成功移至資源回收桶。";
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
            return "該項目已成功復原！";
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
            return "該項目已從資料庫移除！";
        }

        // ==========================================
        // 6. 中階主管決策專用 API (主副分明架構中的「管理輔助層」)
        // ==========================================
        //[HttpGet]
        //public IActionResult GetRiskStatsAjax()
        //{
        //    // 建立一個字典，用來記錄每種風險被勾選的總次數
        //    Dictionary<string, int> RiskCounts = new Dictionary<string, int>();
        //    foreach (var key in MyRisk.Keys)
        //    {
        //        if (key != "X0") RiskCounts.Add(MyRisk[key], 0); // 預設初始化為 0 次，排除未評估
        //    }

        //    using (SqlConnection X = new SqlConnection(CS))
        //    {
        //        X.Open();
        //        // 只統計沒有被刪除的正常資產
        //        string G = "select Risk from [Table] where IsDelete = 0 or IsDelete is null";
        //        SqlCommand Q = new SqlCommand(G, X);
        //        SqlDataReader R = Q.ExecuteReader();

        //        while (R.Read())
        //        {
        //            if (R["Risk"] != DBNull.Value)
        //            {
        //                string dbRisk = R["Risk"].ToString();
        //                // 拆開多重代碼 (例如 "X1;X2" -> ["X1", "X2"])
        //                string[] codes = dbRisk.Split(';');
        //                foreach (string code in codes)
        //                {
        //                    string cleanCode = code.Trim();
        //                    if (MyRisk.ContainsKey(cleanCode) && cleanCode != "X0")
        //                    {
        //                        string chineseName = MyRisk[cleanCode];
        //                        RiskCounts[chineseName]++; // 該風險項目的計數器 +1
        //                    }
        //                }
        //            }
        //        }
        //    }



        //    // 將統計結果轉為 JSON 格式，回傳給前端的 Chart.js 畫圖
        //    // 資料格式會像這樣： { "資料外洩": 5, "當機": 12, "資料竄改": 2 }
        //    return Json(RiskCounts);
        //}


        // ==========================================
        // 7. 中階主管專用：風險項目管理維護頁面
        // ==========================================
        [HttpGet]
        public IActionResult RiskManage()
        {
            // 確保每次進來都是最新從資料庫撈出來的資料
            LoadRisksFromDatabase();

            // 把目前的風險字典傳給前端，讓主管一目了然現有架構
            ViewBag.RiskDictionary = MyRisk;

            return View();
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