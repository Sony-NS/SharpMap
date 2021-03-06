using DelftTools.Utils;
using GeoAPI.Geometries;
using SharpMap.UI.Editors;

namespace SharpMap.UI.Tools
{
    public class DeleteTool : MapTool
    {
        public DeleteTool()
        {
            Name = "Delete";
        }

        public override void OnMouseDown(ICoordinate worldPosition, System.Windows.Forms.MouseEventArgs e)
        {
            MapControl.SelectTool.OnMouseDown(worldPosition, e);
        }

        public override void OnMouseUp(ICoordinate worldPosition, System.Windows.Forms.MouseEventArgs e)
        {
            MapControl.SelectTool.OnMouseUp(worldPosition, e);
            // todo? use worldposition for extra check?
            DeleteSelection();
            //return;
            MapControl.Refresh();
        }

        public void DeleteSelection()
        {
            if (MapControl.SelectTool.FeatureEditors.Count == 0)
            {
                return;
            }
            int featuresDeleted = 0;

            IEditableObject editableObject = MapControl.SelectTool.FeatureEditors[0].EditableObject;
            
            for (int i = 0; i < MapControl.SelectTool.FeatureEditors.Count; i++)
            {
                IFeatureEditor featureMutator = MapControl.SelectTool.FeatureEditors[i];
                if (!featureMutator.AllowDeletion())
                {
                    continue;
                }

                if (featuresDeleted == 0 && editableObject != null)
                {
                    editableObject.BeginEdit("Delete feature(s)");
                }

                featureMutator.Delete();
                featuresDeleted++;
            }
            if (featuresDeleted > 0)
            {
                // Better not to reset the selection if you haven't done anything.
                MapControl.SelectTool.Clear();

                if (editableObject != null)
                {
                    editableObject.EndEdit();
                }
            }
        }

        public override bool IsBusy
        {
            get { return false; }
        }
    }
}
