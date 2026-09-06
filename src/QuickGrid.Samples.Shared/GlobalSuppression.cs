// Keep these attributes in the assembly in every build configuration.
#define CODE_ANALYSIS
using System.Diagnostics.CodeAnalysis;

// This RCL deliberately retains RootNamespace QuickGrid.Samples for both sample hosts.
[assembly: SuppressMessage("NDepend", "ND2103", Target = "QuickGrid.Samples.Dtos",
    Justification = "Matches the project's explicit QuickGrid.Samples root namespace and Dtos directory.")]

[assembly: SuppressMessage("NDepend", "ND2103", Target = "QuickGrid.Samples.Services",
    Justification = "Matches the project's explicit QuickGrid.Samples root namespace and Services directory.")]

[assembly: SuppressMessage("NDepend", "ND2103", Target = "QuickGrid.Samples.Interfaces",
    Justification = "Matches the project's explicit QuickGrid.Samples root namespace and Interfaces directory.")]

[assembly: SuppressMessage("NDepend", "ND2103", Target = "QuickGrid.Samples.Extensions",
    Justification = "Matches the project's explicit QuickGrid.Samples root namespace and Extensions directory.")]