//----------------------------------------------------------------------------
//
// Copyright 2020 by Northern Digital Inc.
// All Rights Reserved
//
//----------------------------------------------------------------------------
// By using this Sample Code the licensee agrees to be bound by the
// provisions of the agreement found in the LICENSE.txt file.

// System
using System;
using System.IO;

// NDI
using NDI.CapiSample.Utility;

namespace NDI.CapiSample.Protocol.GBF
{
    /// <summary>
    /// Base class storing header data common to all GbfComponents
    /// </summary>
    public class GbfComponent
    {
        /// <summary>
        /// Type of Component
        /// </summary>
        public ComponentType type;

        /// <summary>
        /// Payload size of the component
        /// </summary>
        public int size;

        /// <summary>
        /// Each type will have its own set of options that provide all the information needed to parse the content of the component.
        /// The Item Format Option implies the Item’s size.
        /// </summary>
        public int itemFormatOption;

        /// <summary>
        /// Number of occurrences for the component type.
        /// </summary>
        public int itemCount;

        /// <summary>
        /// Reads a component off of the Stream.
        /// </summary>
        /// <param name="reader">The reader whose buffer contains binary data to parse.</param>
        /// <returns></returns>
        public static GbfComponent buildComponent(Stream reader)
        {
            GbfComponent component;

            // Read 12 byte header
            int headerSize = 12;
            ComponentType type = (ComponentType)BytePacker.UnpackInt(reader, 2);
            int size = BytePacker.UnpackInt(reader, 4);
            int option = BytePacker.UnpackInt(reader, 2);
            int count = BytePacker.UnpackInt(reader, 4);

            // Read the specific component type
            switch(type)
            {
                case ComponentType.Data3D:
                    component = new GbfData3D(reader, count);
                    break;
                case ComponentType.Data6D:
                    component = new GbfData6D(reader, count);
                    break;
                case ComponentType.Frame:
                    component = new GbfFrame(reader, count);
                    break;
                case ComponentType.SystemAlert:
                    component = new GbfSystemAlert(reader, count);
                    break;
                // TODO: Implement other component types
                default:
                    // Skip the data for an unhandled component type and return a generic component with the header information.
                    reader.Seek(size - headerSize, SeekOrigin.Current);
                    component = new GbfComponent();
                    break;
            }

            // Update the header information of the new component
            component.type = type;
            component.size = size;
            component.itemFormatOption = option;
            component.itemCount = count;

            return component;
        }

        public override string ToString()
        {
            return String.Format("componentType={0:X4} ({1})\ncomponentSize={2:X8}\nitemOption={3:X4}\nitemCount={4:X8}\n", type, Enum.GetName(typeof(Type), type), size, itemFormatOption, itemCount);
        }
    }

    /// <summary>
    /// Data components in a GBF stream must be one of these types.
    /// </summary>
    public enum ComponentType
    {
        Frame = 0x0001,
        Data6D = 0x0002,
        Data3D = 0x0003,
        Button1D = 0x0004,
        Data2D = 0x0005,
        // Reserved
        Image = 0x000A,
        // Reserved
        UV = 0x0011,
        SystemAlert = 0x0012
    }

}
