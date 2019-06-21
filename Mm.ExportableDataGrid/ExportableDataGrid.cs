/*
 *  Copyright 2015 Magnus Montin

        Licensed under the Apache License, Version 2.0 (the "License");
        you may not use this file except in compliance with the License.
        You may obtain a copy of the License at

            http://www.apache.org/licenses/LICENSE-2.0

        Unless required by applicable law or agreed to in writing, software
        distributed under the License is distributed on an "AS IS" BASIS,
        WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
        See the License for the specific language governing permissions and
        limitations under the License.

 */

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Mm.ExportableDataGrid
{
    public class ExportableDataGrid : DataGrid
    {
        private readonly ICommand _exportCommand = new RoutedCommand();
        private readonly IExporter _exporter = new CsvExporter(';');

        public ExportableDataGrid()
        {
            CommandBindings.Add(new CommandBinding(_exportCommand, ExecutedExportCommand,
                CanExecuteExportCommand));
            Loaded += ExportableDataGrid_Loaded;
        }

        private void CanExecuteExportCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Items.Count > 0;
        }

        private void ContextMenu_Loaded(object sender, RoutedEventArgs e)
        {
            var mi = new MenuItem();
            mi.Header = "Export to CSV";
            mi.Command = _exportCommand;

            if (ContextMenu.ItemsSource != null)
            {
                var cc = new CompositeCollection();

                var boundCollection = new CollectionContainer();
                boundCollection.Collection = ContextMenu.ItemsSource;
                cc.Add(boundCollection);

                var exportCollection = new CollectionContainer();
                var exportMenuItems = new List<Control>(2);
                exportMenuItems.Add(new Separator());
                exportMenuItems.Add(mi);
                exportCollection.Collection = exportMenuItems;
                cc.Add(exportCollection);

                ContextMenu.ItemsSource = cc;
            }
            else
            {
                if (ContextMenu.HasItems)
                    ContextMenu.Items.Add(new Separator());

                ContextMenu.Items.Add(mi);
            }

            ContextMenu.Loaded -= ContextMenu_Loaded;
        }

        private void ExecutedExportCommand(object sender, ExecutedRoutedEventArgs e)
        {
            this.ExportUsingRefection(_exporter, string.Empty);
        }

        private void ExportableDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (ContextMenu == null)
                ContextMenu = new ContextMenu();

            ContextMenu.Loaded += ContextMenu_Loaded;
        }
    }
}