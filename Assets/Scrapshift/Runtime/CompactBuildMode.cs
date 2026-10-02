using UnityEngine;
using Scrapshift.Compact;

namespace Scrapshift
{
    // The game coordinator owns the visible ghost, pause state and input-release gate.
    // This preview never spends cash; Confirm runs authoritative placement validation again.
    public sealed class CompactBuildMode
    {
        readonly ConstructionModel construction;
        public bool Active { get; private set; }
        public bool GridSnap { get; set; }
        public float GridSize { get; set; }
        public bool Valid { get; private set; }
        public string Reason { get; private set; }
        public EquipmentKind SelectedKind { get; private set; }
        public int MovingId { get; private set; }
        public Vector3 PreviewPosition { get; private set; }
        public float Yaw { get; private set; }
        public CompactBuildMode(ConstructionModel construction)
        {
            this.construction = construction; GridSnap = true; GridSize = .5f; Reason = "Select equipment in the catalogue.";
        }
        public void Begin(EquipmentKind kind)
        {
            SelectedKind = kind; MovingId = 0; Yaw = 0; Active = true; UpdatePreview(Vector3.zero);
        }
        public bool BeginMove(int id)
        {
            string reason;
            if (!construction.CanMove(id, out reason)) { Reason = reason; return false; }
            var item = construction.Find(id);
            SelectedKind = item.kind; MovingId = id; Yaw = item.yaw; Active = true;
            UpdatePreview(new Vector3(item.x, 0, item.z)); return true;
        }
        public void Cancel()
        {
            Active = false; Valid = false; MovingId = 0; Reason = "Construction cancelled; nothing was spent.";
        }
        public void Rotate()
        {
            if (!Active) return;
            Yaw = (Mathf.Round(Yaw / 90) * 90 + 90) % 360; ValidatePreview();
        }
        public void UpdatePreview(Vector3 point)
        {
            if (!Active) return;
            bool usableGrid = !float.IsNaN(GridSize) && !float.IsInfinity(GridSize) && GridSize >= .1f && GridSize <= 4;
            PreviewPosition = GridSnap && usableGrid ? new Vector3(Mathf.Round(point.x / GridSize) * GridSize, 0,
                Mathf.Round(point.z / GridSize) * GridSize) : new Vector3(point.x, 0, point.z);
            ValidatePreview();
        }
        void ValidatePreview()
        {
            string reason;
            Valid = construction.CanPlace(SelectedKind, PreviewPosition.x, PreviewPosition.z, Yaw, MovingId, out reason);
            Reason = reason;
        }
        public bool Confirm()
        {
            if (!Active) return false;
            bool changed = MovingId == 0 ? construction.Place(SelectedKind, PreviewPosition.x, PreviewPosition.z, Yaw) :
                construction.Move(MovingId, PreviewPosition.x, PreviewPosition.z, Yaw);
            if (!changed) { Valid = false; Reason = construction.LastMessage; return false; }
            Active = false; Valid = false; MovingId = 0; Reason = construction.LastMessage; return true;
        }
    }
}
