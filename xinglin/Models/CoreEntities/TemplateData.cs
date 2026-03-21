using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace xinglin.Models.CoreEntities
{
    public enum EditorState
    {
        DesignMode,
        PreviewMode
    }

    public partial class TemplateData : ObservableObject
    {
        [ObservableProperty]
        private string _templateId;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _description;

        [ObservableProperty]
        private LayoutMetadata _layout;

        [ObservableProperty]
        private TemplateConfig _config = new TemplateConfig();

        [ObservableProperty]
        private DateTime _createdDate;

        [ObservableProperty]
        private DateTime _modifiedDate;

        [ObservableProperty]
        private string _parentId;

        [ObservableProperty]
        private List<string> _childTemplateIds;

        [ObservableProperty]
        private EditorState _editorState;

        [ObservableProperty]
        private string _version;

        public TemplateData()
        {
            TemplateId = Guid.NewGuid().ToString();
            CreatedDate = DateTime.Now;
            ModifiedDate = DateTime.Now;
            EditorState = EditorState.DesignMode;
            Version = "1.0";
            ChildTemplateIds = new List<string>();
            Layout = new LayoutMetadata();
        }
    }
}
