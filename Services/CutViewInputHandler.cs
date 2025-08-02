using Filmauswertung_ModernUI.Core.Interfaces;
using Filmauswertung_ModernUI.MVVM.ViewModel.ViewCutRotateViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Filmauswertung_ModernUI.Core.Interfaces;

namespace Filmauswertung_ModernUI.Services
{
    public class CutViewInputHandler : ICutViewInputHandler
    {
        private readonly CutViewModel _viewModel;

        // For ROI drawing state
        private bool _isDrawingRoi = false;
        private Point _roiStartPoint;
        private Rect _currentRoiVisualRect;

        public CutViewInputHandler(CutViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void OnMouseLeftButtonDown(Point uiPoint)
        {
            if (_viewModel.IsBatchCutMode)
            {
                // Batch mode: add marker at click point
                _viewModel.AddMarker(uiPoint);
            }
            else
            {
                // Manual mode: start drawing ROI
                _isDrawingRoi = true;
                _roiStartPoint = uiPoint;
                _currentRoiVisualRect = new Rect(uiPoint, new Size(0, 0));
                _viewModel.StartRoiDrawing(_roiStartPoint);
            }
        }

        public void OnMouseMove(Point uiPoint)
        {
            if (_isDrawingRoi)
            {
                // Update ROI rectangle as user drags
                _currentRoiVisualRect = new Rect(_roiStartPoint, uiPoint);
                _viewModel.UpdateRoiDrawing(_currentRoiVisualRect);
            }
        }

        public void OnMouseLeftButtonUp(Point uiPoint)
        {
            if (_isDrawingRoi && !_viewModel.IsBatchCutMode)
            {
                _isDrawingRoi = false;
                var roiRect = new Rect(_roiStartPoint, uiPoint);
                _viewModel.CompleteRoiDrawing(roiRect);
            }
        }
    }
}
