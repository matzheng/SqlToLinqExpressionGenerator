using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RequestEngine;
using System.Linq.Expressions;

namespace RequestEngineTest
{
    class Supplier
    {
        public string Name { get; set; }
        public int Status { get; set; }
        public string City { get; set; }
    }

    enum Color { Red, Yellow, Green, Blue, Brown, Black }

    class Detail
    {
        public string Name { get; set; }
        public Color Color { get; set; }
        public float Weight { get; set; }
        public string City { get; set; }
    }

    class Project
    {
        public string Name { get; set; }
        public string City { get; set; }
    }

    class Order
    {
        Supplier supplier;
        Detail detail;
        Project project;
        uint quantity;

        public Order(Supplier supplier, Detail detail, Project project, uint quantity)
        {
            this.supplier = supplier;
            this.detail = detail;
            this.project = project;
            this.quantity = quantity;
        }

        public Supplier Supplier { get { return supplier; } }
        public Detail Detail { get { return detail; } }
        public Project Project { get { return project; } }
        public uint Quantity { get { return quantity; } }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", supplier.City, detail.City, project.City);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {

            #region Data

            List<Detail> details = new List<Detail>();
            details.Add(new Detail() { Name = "Nut", Color = RequestEngineTest.Color.Red, City = "London", Weight = 12 });
            details.Add(new Detail() { Name = "Bolt", Color = RequestEngineTest.Color.Green, City = "Paris", Weight = 17 });
            details.Add(new Detail() { Name = "Screw", Color = RequestEngineTest.Color.Blue, City = "Rome", Weight = 17 });
            details.Add(new Detail() { Name = "Screw", Color = RequestEngineTest.Color.Red, City = "London", Weight = 14 });
            details.Add(new Detail() { Name = "Cam", Color = RequestEngineTest.Color.Blue, City = "Paris", Weight = 12 });
            details.Add(new Detail() { Name = "Cog", Color = RequestEngineTest.Color.Red, City = "London", Weight = 19 });

            List<Supplier> suppliers = new List<Supplier>();
            suppliers.Add(new Supplier() { Name = "Smith", Status = 20, City = "London" });
            suppliers.Add(new Supplier() { Name = "Jones", Status = 10, City = "Paris" });
            suppliers.Add(new Supplier() { Name = "Black", Status = 30, City = "Paris" });
            suppliers.Add(new Supplier() { Name = "Clark", Status = 15, City = "London" });
            suppliers.Add(new Supplier() { Name = "Adams", Status = 30, City = "Athens" });

            List<Project> projects = new List<Project>();
            projects.Add(new Project() { Name = "Sorter", City = "Paris" });
            projects.Add(new Project() { Name = "Display", City = "Rome" });
            projects.Add(new Project() { Name = "OCR", City = "Athens" });
            projects.Add(new Project() { Name = "Console", City = "Athens" });
            projects.Add(new Project() { Name = "RAID", City = "London" });
            projects.Add(new Project() { Name = "EDS", City = "Oslo" });
            projects.Add(new Project() { Name = "Tape", City = "London" });

            List<Order> orders = new List<Order>();
            orders.Add(new Order(suppliers[0], details[0], projects[0], 200));
            orders.Add(new Order(suppliers[0], details[0], projects[3], 700));
            orders.Add(new Order(suppliers[1], details[2], projects[0], 400));
            orders.Add(new Order(suppliers[1], details[2], projects[1], 200));
            orders.Add(new Order(suppliers[1], details[2], projects[2], 200));
            orders.Add(new Order(suppliers[1], details[2], projects[3], 500));
            orders.Add(new Order(suppliers[1], details[2], projects[4], 600));
            orders.Add(new Order(suppliers[1], details[2], projects[5], 400));
            orders.Add(new Order(suppliers[1], details[2], projects[6], 800));
            orders.Add(new Order(suppliers[1], details[4], projects[1], 100));
            orders.Add(new Order(suppliers[2], details[2], projects[0], 200));
            orders.Add(new Order(suppliers[2], details[3], projects[1], 500));
            orders.Add(new Order(suppliers[3], details[5], projects[2], 300));
            orders.Add(new Order(suppliers[3], details[5], projects[6], 300));
            orders.Add(new Order(suppliers[4], details[1], projects[1], 200));
            orders.Add(new Order(suppliers[4], details[1], projects[3], 100));
            orders.Add(new Order(suppliers[4], details[4], projects[4], 500));
            orders.Add(new Order(suppliers[4], details[4], projects[6], 100));
            orders.Add(new Order(suppliers[4], details[5], projects[4], 200));
            orders.Add(new Order(suppliers[4], details[0], projects[1], 100));
            orders.Add(new Order(suppliers[4], details[2], projects[3], 200));
            orders.Add(new Order(suppliers[4], details[3], projects[3], 800));
            orders.Add(new Order(suppliers[4], details[4], projects[3], 400));
            orders.Add(new Order(suppliers[4], details[5], projects[3], 500));

            #endregion

            //This selects all orders for which supplier is from London and its status <= 15
            Func<Order, bool> filterOrders = ExpressionBuilder.BuildFunctor<Order, bool>("not(Supplier.City<>\"London\" or Supplier.Status>5*3)");
            List<Order> londonOrders = new List<Order>(orders.Where(filterOrders));

            //This is artificial example to demonstrate usage of () and xor
            Func<Order, bool> filterComplex = ExpressionBuilder.BuildFunctor<Order, bool>("Project.City=Supplier.City xor (Project.City=Detail.City and Detail.Weight<15)");
            List<Order> filteredOrders = new List<Order>(orders.Where(filterComplex));

            //This selects Colors of details which stored in London and used for projects in London presented in String format
            Func<Order, bool> londonFilter = ExpressionBuilder.BuildFunctor<Order, bool>("Project.City=\"London\" and Detail.City=Project.City");
            Func<Order, string> ordStrConv = ExpressionBuilder.BuildFunctor<Order, string>("Detail.Color.ToString()");
            List<string> privelegeSuppliers = new List<string>(orders.Where(londonFilter).Select(ordStrConv).Distinct());

            //Performs Join of Details and Suppliers by City
            Func<Supplier, string> suppKey = ExpressionBuilder.BuildFunctor<Supplier, string>("City");
            Func<Detail, string> detailKey = ExpressionBuilder.BuildFunctor<Detail, string>("City");
            var res = suppliers.Join(details, suppKey, detailKey, 
                (supplier, detail) => new { SName=supplier.Name, Status=supplier.Status, DName=detail.Name, Color=detail.Color, Weight=detail.Weight });

            //Just sorts suppliers by their status (arg1 usage demonstrated)
            Func<Supplier, Supplier, int> suppsorter = ExpressionBuilder.BuildFunctor<Supplier, Supplier, int>("Status-arg1.Status");
            suppliers.Sort(suppsorter.Invoke);

            //Also sorts details by their weight (type conversion demonstrated)
            Func<Detail, Detail, int> detsorter = ExpressionBuilder.BuildFunctor<Detail, Detail, int>("int(Weight-arg1.Weight)");
            details.Sort(detsorter.Invoke);
        }
    }
}
