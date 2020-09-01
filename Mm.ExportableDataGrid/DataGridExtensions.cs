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

using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace Mm.ExportableDataGrid
{
    public static class DataGridExtensions
    {
        public static void ExportUsingRefection(this DataGrid grid, IExporter exporter, string exportPath)
        {
            /* Execute the private DoExportUsingRefection method on a background thread by starting a new task */
            Task.Factory.StartNew(() => { DoExportUsingRefection(grid, exporter, exportPath); });
        }

        private static void DoExportUsingRefection(this DataGrid grid, IExporter exporter, string exportPath)
        {
            if (grid.ItemsSource == null || grid.Items.Count.Equals(0))
                throw new InvalidOperationException("You cannot export any data from an empty DataGrid.");

            var checkAccess = grid.Dispatcher.CheckAccess();
            ICollectionView collectionView = null;
            IList<DataGridColumn> columns = null;
            if (checkAccess)
            {
                columns = grid.Columns.OrderBy(c => c.DisplayIndex).ToList();
                if (grid.SelectedItems != null && grid.SelectedItems.Count > 0)
                    collectionView = CollectionViewSource.GetDefaultView(grid.SelectedItems);
                else
                    collectionView = CollectionViewSource.GetDefaultView(grid.ItemsSource);
            }
            else
            {
                grid.Dispatcher.Invoke(() => { columns = grid.Columns.OrderBy(c => c.DisplayIndex).ToList(); });

                grid.Dispatcher.Invoke(() =>
                {
                    if (grid.SelectedItems != null && grid.SelectedItems.Count > 0)
                        collectionView = CollectionViewSource.GetDefaultView(grid.SelectedItems);
                    else
                        collectionView = CollectionViewSource.GetDefaultView(grid.ItemsSource);
                });
            }

            foreach (var column in columns)
            {
                var exportString = string.Empty;
                if (checkAccess)
                    exportString = column.Header.ToString();
                else
                    grid.Dispatcher.Invoke(() => { exportString = column.Header.ToString(); });

                if (!string.IsNullOrEmpty(exportString)) exporter.AddColumn(exportString);
            }

            exporter.AddLineBreak();

            foreach (var o in collectionView)
            {
                if (o.Equals(CollectionView.NewItemPlaceholder))
                    continue;

                foreach (var column in columns)
                {
                    var exportString = string.Empty;
                    if (checkAccess)
                        exportString = ExportBehaviour.GetExportString(column);
                    else
                        grid.Dispatcher.Invoke(() => { exportString = ExportBehaviour.GetExportString(column); });

                    if (!string.IsNullOrEmpty(exportString))
                    {
                        exporter.AddColumn(exportString);
                    }
                    else if (column is DataGridBoundColumn)
                    {
                        var propertyValue = string.Empty;

                        /* Get the property name from the column's binding */
                        var bb = (column as DataGridBoundColumn).Binding;
                        if (bb != null)
                        {
                            var binding = bb as Binding;
                            if (binding != null)
                            {
                                var boundProperty = binding.Path.Path;

                                /// Trying on a sub-class member
                                ///
                                var splitProperties = boundProperty.Split('.');
                                var value = o;
                                for (var i = 0; i < splitProperties.Count(); i++)
                                {
                                    if (value == null)
                                    {
                                        value = string.Empty;
                                        break;
                                    }

                                    string[] splitArray = null;
                                    var index = 0;
                                    if (splitProperties[i].Contains('['))
                                    {
                                        splitArray = splitProperties[i].Split('[');
                                        splitProperties[i] = splitArray[0];
                                        if (splitArray.Count() > 1)
                                        {
                                            var strIndex = splitArray[1].Replace('[', ' ');
                                            strIndex = strIndex.Replace(']', ' ').Trim();
                                            int.TryParse(strIndex, out index);
                                        }
                                    }

                                    var type = value.GetType();
                                    var prop = type.GetProperty(splitProperties[i]);
                                    var pi = value.GetType().GetProperty(splitProperties[i]);
                                    if (pi != null)
                                    {
                                        value = pi.GetValue(value, null);
                                        if (splitArray != null && splitArray.Count() > 1)
                                            value = (value as BulkObservableCollection<int>)[index];
                                    }
                                }

                                if (value != null)
                                    propertyValue = value.ToString();
                                else if (column is DataGridCheckBoxColumn) propertyValue = "-";

                                /* Get the property value using reflection */
                                //PropertyInfo pi = o.GetType().GetProperty(boundProperty);

                                //if (pi != null)
                                //{
                                //   object value = pi.GetValue(o);
                                //    if (value != null)
                                //       propertyValue = value.ToString();
                                //   else if (column is DataGridCheckBoxColumn)
                                //       propertyValue = "-";
                                //}
                            }
                        }

                        exporter.AddColumn(propertyValue);
                    }
                    else if (column is DataGridComboBoxColumn)
                    {
                        var cmbColumn = column as DataGridComboBoxColumn;
                        var propertyValue = string.Empty;
                        var displayMemberPath = string.Empty;
                        if (checkAccess)
                            displayMemberPath = cmbColumn.DisplayMemberPath;
                        else
                            grid.Dispatcher.Invoke(() => { displayMemberPath = cmbColumn.DisplayMemberPath; });

                        /* Get the property name from the column's binding */
                        var bb = cmbColumn.SelectedValueBinding;
                        if (bb != null)
                        {
                            var binding = bb as Binding;
                            if (binding != null)
                            {
                                var boundProperty = binding.Path.Path; //returns "Category" (or CategoryId)

                                /* Get the selected property */
                                var pi = o.GetType().GetProperty(boundProperty);
                                if (pi != null)
                                {
                                    var boundProperyValue =
                                        pi.GetValue(o); //returns the selected Category object or CategoryId
                                    if (boundProperyValue != null)
                                    {
                                        var propertyType = boundProperyValue.GetType();
                                        if (propertyType.IsPrimitive || propertyType.Equals(typeof(string)))
                                        {
                                            if (cmbColumn.ItemsSource != null)
                                            {
                                                /* Find the Category object in the ItemsSource of the ComboBox with
                                                 * an Id (SelectedValuePath) equal to the selected CategoryId */
                                                var comboBoxSource = cmbColumn.ItemsSource.Cast<object>();
                                                var obj = (from oo in comboBoxSource
                                                           let prop = oo.GetType().GetProperty(cmbColumn.SelectedValuePath)
                                                           where prop != null && prop.GetValue(oo).Equals(boundProperyValue)
                                                           select oo).FirstOrDefault();
                                                if (obj != null)
                                                {
                                                    /* Get the Name (DisplayMemberPath) of the Category object */
                                                    if (string.IsNullOrEmpty(displayMemberPath))
                                                    {
                                                        propertyValue = obj.GetType().ToString();
                                                    }
                                                    else
                                                    {
                                                        var displayNameProperty = obj.GetType()
                                                            .GetProperty(displayMemberPath);
                                                        if (displayNameProperty != null)
                                                        {
                                                            var displayName = displayNameProperty.GetValue(obj);
                                                            if (displayName != null)
                                                                propertyValue = displayName.ToString();
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                /* Export the scalar property value of the selected object
                                                 * specified by the SelectedValuePath property of the DataGridComboBoxColumn */
                                                propertyValue = boundProperyValue.ToString();
                                            }
                                        }
                                        else if (!string.IsNullOrEmpty(displayMemberPath))
                                        {
                                            /* Get the Name (DisplayMemberPath) property of the selected Category object */
                                            var pi2 = boundProperyValue.GetType()
                                                .GetProperty(displayMemberPath);

                                            if (pi2 != null)
                                            {
                                                var displayName = pi2.GetValue(boundProperyValue);
                                                if (displayName != null)
                                                    propertyValue = displayName.ToString();
                                            }
                                        }
                                        else
                                        {
                                            propertyValue = o.GetType().ToString();
                                        }
                                    }
                                }
                            }
                        }

                        exporter.AddColumn(propertyValue);
                    }
                }

                exporter.AddLineBreak();
            }

            /* Create and open export file */
            Process.Start(exporter.Export(exportPath));
        }
    }
}