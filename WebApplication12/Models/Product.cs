using System;

namespace WebApplication12.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Item { get; set; }
        public string Risk { get; set; } // 111頁新增的風險欄位

        // 更新三個參數的建構子 (供112頁讀取含有風險的資料使用)
        public Product(int id, string item, string risk)
        {
            Id = id;
            Item = item;
            Risk = risk;
        }
    }
}