using System.Collections.Generic;

namespace Platform.AgentReports.API.Models.Response
{
    public class ProductResponse
    {
        public ProductResponse()
        {
            ProductTypes = new List<ProductType>();
        }
        public List<ProductType> ProductTypes { get; set; }
    }

    public class ProductType
    {
        public ProductType()
        {
            Products = new List<ProductModel>();
        }
        public int ProductTypeId { get; set; }
        public string ProductTypeName { get; set; }
        public List<ProductModel> Products { get; set; }
    }

    public class ProductModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; }

    }
}

