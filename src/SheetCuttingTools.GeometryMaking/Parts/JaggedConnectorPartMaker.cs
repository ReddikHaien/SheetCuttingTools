using g3;
using SheetCuttingTools.Abstractions.Behaviors;

namespace SheetCuttingTools.GeometryMaking.Parts
{
    public class JaggedConnectorPartMaker(int numTags, double gapSize, double tagHeight) : IPartMaker
    {
        private const string name = $"{IPartMaker.RootName}/JaggedConnector";

        private readonly int numTags = numTags;
        private readonly double gapSize = gapSize;
        private readonly double tagHeight = tagHeight;

        public void CreatePart(IPartMakerContext context, IPartGeometryOutput geometryOutput)
        {
            var pointOffsets = context.A.Distance(context.B) / ((numTags * 2 - 1) * 2);

            var side = context.MaleSide ? 1 : -1;

            List<Vector2d> vertices = [];

            Vector2d a = context.A;
            Vector2d b = context.B;
            Vector2d u = context.U;
            Vector2d v = context.V;


            for (int i = 0; i < numTags * 2 - 1; i++)
            {
                double offset = pointOffsets + pointOffsets * i * 2;

                vertices.Add(a + u * offset + v * tagHeight * side);

                if (side > 0 && i > 0 && i < numTags * 2 - 2)
                {
                    geometryOutput.AddCircle(a + u * offset - v * 0.4, 1.125);
                }

                side *= -1;
            }

            geometryOutput.AddLine([a, .. vertices, b]);


        }

        public double GetRequiredGap(IPartMakerContext context)
            => gapSize;

        public string Name() => name;
    }
}
