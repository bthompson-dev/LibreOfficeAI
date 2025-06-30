
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LibreOfficeAI.Models
{
    internal class MessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate UserTemplate { get; set; }
        public DataTemplate AITemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            var message = item as ChatMessage;
            return message?.IsUser == true ? UserTemplate : AITemplate;
        }
    }
}
