﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ComicBud.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ComicInfoView : ContentView
    {
        public ComicInfoView()
        {
            InitializeComponent();
        }

        public virtual bool IsLastReadVisible { get { return true; } }
        public virtual bool IsLastUpdatedVisible { get { return true; } }
    }
}