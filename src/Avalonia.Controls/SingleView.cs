using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Controls
{
    public class SingleView : UserControl
    {
        public void NavigateTo(SingleView view)
        {
            if(Application.Current != null && Application.Current.ApplicationLifetime is ISingleViewApplicationLifetime)
            {
                if(VisualRoot is TopLevel topLevel)
                {
                    topLevel.PlatformImpl?.Navigate(view);
                }
            }
        }
    }
}
