using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Android
{
    internal class AndroidNavigation
    {
        private readonly AvaloniaView _avaloniaView;

        public AndroidNavigation(AvaloniaView avaloniaView)
        {
            _avaloniaView = avaloniaView;
        }

        public void Navigate(object content)
        {
            var view = new AvaloniaView(content);

            _avaloniaView.ParentFragmentManager.BeginTransaction()
                .SetReorderingAllowed(true)
                .AddToBackStack(null)
                .SetCustomAnimations(Resource.Animation.slide_in, Resource.Animation.fade_out, Resource.Animation.fade_in, Resource.Animation.slide_out)
                .Add(Resource.Id.container, view, null)
                .Commit();
        }
    }
}
