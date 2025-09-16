namespace Crm.Models
{
    public class MenuItem
    {
        public string Icon { get; set; }
        public string Text { get; set; }
        public string[] Submenu { get; set; } = new string[0];
        public string Url { get; set; } = "#";
    }
}
