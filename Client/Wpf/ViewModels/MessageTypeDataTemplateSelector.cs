using System.Windows;
using System.Windows.Controls;
using Domain.Models;

namespace Client.Wpf.ViewModels
{
    class MessageTypeDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;

            if (element != null && item != null && item is Message message)
            {
                if (message.Type == MessageType.Plain)
                {
                    return element.FindResource("PlainMessageTemplate") as DataTemplate;
                } 
                if (message.Type == MessageType.Picture)
                {

                }
                if (message.Type == MessageType.File)
                {
                    return element.FindResource("FileMessageTemplate") as DataTemplate;
                }
                if (message.Type == MessageType.Info)
                {
                    return element.FindResource("InfoMessageTemplate") as DataTemplate;
                }
            }

            return null;
        }
    }
}
