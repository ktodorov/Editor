﻿

#pragma checksum "C:\Users\Phoenix\Editor\Imageff_project\MainPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "22BCA42F005AE96094DF74E6E77A0BA8"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RemedyPic
{
    partial class MainPage : global::RemedyPic.Common.LayoutAwarePage, global::Windows.UI.Xaml.Markup.IComponentConnector
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
 
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1:
                #line 391 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.BackFeedbackClicked;
                 #line default
                 #line hidden
                break;
            case 2:
                #line 373 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ToggleButton)(target)).Checked += this.FiltersChecked;
                 #line default
                 #line hidden
                #line 373 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ToggleButton)(target)).Unchecked += this.FiltersUnchecked;
                 #line default
                 #line hidden
                break;
            case 3:
                #line 374 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ToggleButton)(target)).Unchecked += this.ColorsUnchecked;
                 #line default
                 #line hidden
                #line 374 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ToggleButton)(target)).Checked += this.ColorsChecked;
                 #line default
                 #line hidden
                break;
            case 4:
                #line 375 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ToggleButton)(target)).Unchecked += this.RotationsUnchecked;
                 #line default
                 #line hidden
                #line 375 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ToggleButton)(target)).Checked += this.RotationsChecked;
                 #line default
                 #line hidden
                break;
            case 5:
                #line 361 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnRotateClick;
                 #line default
                 #line hidden
                break;
            case 6:
                #line 362 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnHFlipClick;
                 #line default
                 #line hidden
                break;
            case 7:
                #line 363 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnVFlipClick;
                 #line default
                 #line hidden
                break;
            case 8:
                #line 365 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnRotateApplyClick;
                 #line default
                 #line hidden
                break;
            case 9:
                #line 366 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnRotateResetClick;
                 #line default
                 #line hidden
                break;
            case 10:
                #line 355 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.BackPopupClicked;
                 #line default
                 #line hidden
                break;
            case 11:
                #line 328 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnBlackWhiteClick;
                 #line default
                 #line hidden
                break;
            case 12:
                #line 329 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnInvertClick;
                 #line default
                 #line hidden
                break;
            case 13:
                #line 330 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnEmbossClick;
                 #line default
                 #line hidden
                break;
            case 14:
                #line 331 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnSharpenClick;
                 #line default
                 #line hidden
                break;
            case 15:
                #line 332 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnBlurClick;
                 #line default
                 #line hidden
                break;
            case 16:
                #line 334 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnFilterApplyClick;
                 #line default
                 #line hidden
                break;
            case 17:
                #line 335 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnFilterResetClick;
                 #line default
                 #line hidden
                break;
            case 18:
                #line 322 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.BackPopupClicked;
                 #line default
                 #line hidden
                break;
            case 19:
                #line 277 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.RangeBase)(target)).ValueChanged += this.OnValueChanged;
                 #line default
                 #line hidden
                break;
            case 20:
                #line 289 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.RangeBase)(target)).ValueChanged += this.OnRColorChanged;
                 #line default
                 #line hidden
                break;
            case 21:
                #line 290 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.RangeBase)(target)).ValueChanged += this.OnGColorChanged;
                 #line default
                 #line hidden
                break;
            case 22:
                #line 291 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.RangeBase)(target)).ValueChanged += this.OnBColorChanged;
                 #line default
                 #line hidden
                break;
            case 23:
                #line 293 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.RangeBase)(target)).ValueChanged += this.OnRContrastChanged;
                 #line default
                 #line hidden
                break;
            case 24:
                #line 294 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.RangeBase)(target)).ValueChanged += this.OnGContrastChanged;
                 #line default
                 #line hidden
                break;
            case 25:
                #line 295 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.RangeBase)(target)).ValueChanged += this.OnBContrastChanged;
                 #line default
                 #line hidden
                break;
            case 26:
                #line 297 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.RangeBase)(target)).ValueChanged += this.OnGamaChanged;
                 #line default
                 #line hidden
                break;
            case 27:
                #line 298 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.RangeBase)(target)).ValueChanged += this.OnGamaChanged;
                 #line default
                 #line hidden
                break;
            case 28:
                #line 299 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.RangeBase)(target)).ValueChanged += this.OnGamaChanged;
                 #line default
                 #line hidden
                break;
            case 29:
                #line 301 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnColorApplyClick;
                 #line default
                 #line hidden
                break;
            case 30:
                #line 302 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnColorResetClick;
                 #line default
                 #line hidden
                break;
            case 31:
                #line 270 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.BackPopupClicked;
                 #line default
                 #line hidden
                break;
            case 32:
                #line 230 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.GetPhotoButton_Click;
                 #line default
                 #line hidden
                break;
            case 33:
                #line 231 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnSaveButtonClick;
                 #line default
                 #line hidden
                break;
            }
            this._contentLoaded = true;
        }
    }
}


