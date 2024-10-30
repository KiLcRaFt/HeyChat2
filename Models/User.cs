namespace HeyChat2.Models
{
    public class User
    {
        public User()
        {
            created_at = DateTime.Now;
        }

        public int id { get; set; }
        public string name { get; set; }
        public DateTime created_at { get; set; }
    }
}
