using System;

namespace WebApplication12.Models
{
    public class Product
    {
        // 1. 欄位屬性
        public int Id { get; set; }
        public string Item { get; set; }
        public string Risk { get; set; }
        public int IsDelete { get; set; }
        public string AssetType { get; set; }
        public string RiskLevel { get; set; }
        public string Status { get; set; }

        // 🎯 建構子 1：無參數空白建構子（防呆用）
        public Product() { }

        // 🎯 建構子 2：回復原本的 3 參數建構子（完美終結 HomeController 舊代碼報錯！）
        public Product(int id, string item, string risk)
        {
            Id = id;
            Item = item;
            Risk = risk;
        }

        // 🎯 建構子 3：全新 7 參數完整功能建構子（供新功能撈取完整資料使用）
        public Product(int id, string item, string risk, int isDelete, string assetType, string riskLevel, string status)
        {
            Id = id;
            Item = item;
            Risk = risk;
            IsDelete = isDelete;
            AssetType = assetType;
            RiskLevel = riskLevel;
            Status = status;
        }
    }
}