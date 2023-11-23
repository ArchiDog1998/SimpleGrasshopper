using Grasshopper.Kernel;
using System.Drawing;

namespace SimpleGrasshopper.GHTests
{
    public class SimpleGrasshopper_GHTestsInfo : GH_AssemblyInfo
    {
        public override string Name => "SimpleGrasshopperTesting";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("8bc4c536-97be-4160-8f39-3eb65ba1f5a8");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}