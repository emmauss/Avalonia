using Avalonia.Layout;
using System;

namespace Avalonia.Controls
{
    public class UniformStackPanel : StackPanel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            Size stackDesiredSize = new Size();
            var children = Children;
            Size layoutSlotSize = availableSize;
            bool fHorizontal = (Orientation == Orientation.Horizontal);
            double spacing = Spacing;
            bool hasVisibleChild = false;

            //
            // Initialize child sizing and iterator data
            // Allow children as much size as they want along the stack.
            //
            if (fHorizontal)
            {
                layoutSlotSize = layoutSlotSize.WithWidth(children.Count == 0 ? double.PositiveInfinity : (availableSize.Width / children.Count) - spacing * (children.Count - 1));
            }
            else
            {
                layoutSlotSize = layoutSlotSize.WithHeight(children.Count == 0 ? double.PositiveInfinity : (availableSize.Height / children.Count) - spacing * (children.Count - 1));
            }

            //
            //  Iterate through children.
            //  While we still supported virtualization, this was hidden in a child iterator (see source history).
            //
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                // Get next child.
                var child = children[i];

                if (child == null)
                { continue; }

                bool isVisible = child.IsVisible;

                if (isVisible && !hasVisibleChild)
                {
                    hasVisibleChild = true;
                }

                // Measure the child.
                child.Measure(layoutSlotSize);
                Size childDesiredSize = child.DesiredSize;

                // Accumulate child size.
                if (fHorizontal)
                {
                    stackDesiredSize = stackDesiredSize.WithWidth(stackDesiredSize.Width + (isVisible ? spacing : 0) + childDesiredSize.Width);
                    stackDesiredSize = stackDesiredSize.WithHeight(Math.Max(stackDesiredSize.Height, childDesiredSize.Height));
                }
                else
                {
                    stackDesiredSize = stackDesiredSize.WithWidth(Math.Max(stackDesiredSize.Width, childDesiredSize.Width));
                    stackDesiredSize = stackDesiredSize.WithHeight(stackDesiredSize.Height + (isVisible ? spacing : 0) + childDesiredSize.Height);
                }
            }

            if (fHorizontal)
            {
                stackDesiredSize = stackDesiredSize.WithWidth(stackDesiredSize.Width - (hasVisibleChild ? spacing : 0));
            }
            else
            {
                stackDesiredSize = stackDesiredSize.WithHeight(stackDesiredSize.Height - (hasVisibleChild ? spacing : 0));
            }

            return stackDesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var children = Children;
            bool fHorizontal = (Orientation == Orientation.Horizontal);
            Rect rcChild = new Rect(finalSize);
            double previousChildSize = 0.0;
            var spacing = Spacing;

            double uniformSize = 0;

            if (children.Count > 0)
            {
                if (fHorizontal)
                {
                    uniformSize = (finalSize.Width / children.Count) - spacing * (children.Count - 1);
                }
                else
                {
                    uniformSize = (finalSize.Height / children.Count) - spacing * (children.Count - 1);
                }
            }

            //
            // Arrange and Position Children.
            //
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                var child = children[i];

                if (child == null || !child.IsVisible)
                { continue; }

                if (fHorizontal)
                {
                    rcChild = rcChild.WithX(rcChild.X + previousChildSize);
                    previousChildSize = uniformSize;
                    rcChild = rcChild.WithWidth(previousChildSize);
                    rcChild = rcChild.WithHeight(Math.Max(finalSize.Height, child.DesiredSize.Height));
                    previousChildSize += spacing;
                }
                else
                {
                    rcChild = rcChild.WithY(rcChild.Y + previousChildSize);
                    previousChildSize = uniformSize;
                    rcChild = rcChild.WithHeight(previousChildSize);
                    rcChild = rcChild.WithWidth(Math.Max(finalSize.Width, child.DesiredSize.Width));
                    previousChildSize += spacing;
                }

                ArrangeChild(child, rcChild, finalSize, Orientation);
            }

            return finalSize;
        }
    }
}
