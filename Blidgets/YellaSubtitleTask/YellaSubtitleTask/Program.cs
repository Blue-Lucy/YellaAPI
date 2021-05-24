using System.Threading.Tasks;

namespace YellaSubtitleTask
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var blidget = new YellaSubtitleTaskBlidget();
            await blidget.ExecuteAsync();
        }
    }
}
