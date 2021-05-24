using System.Threading.Tasks;

namespace ExportMediaToNebula
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var blidget = new ExportMediaToNebulaBlidget();
            await blidget.ExecuteAsync();
        }
    }
}
