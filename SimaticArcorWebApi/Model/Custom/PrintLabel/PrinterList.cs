using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticWebApi.Model.Custom.PrintLabel
{
    public class PrinterListModel
    {
        public ListValues[] PrinterList { get; set; }
    }

    public class ListValues
    {
        public string EquipmentNId { get; set; }
        public string Printer { get; set; }
    }
}
