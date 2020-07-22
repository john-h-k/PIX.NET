# PIX.NET

PIX.NET provides APIs for PIX (the graphical and ML debugger for windows) that imitate the native bindings provided with `pix3.h`

PIX has the concepts of events and markers. Events have a beginning and an end, whereas markers are a single point.
You can have CPU only events/markers, or tie them to an `ID3D12CommandQueue` or `ID3D12CommandList*`.
PIX.NET uses `TerraFX.Interop.Windows` for the bindings for these types, although a way to completely remove these bindings from being public is TODO. 
For now, pass PIX.NET the raw D3D12 pointer your bindings provide, and either cast it to `TerraFX.Interop.ID3D12CommandQueue` or `TerraFX.Interop.ID3D12CommandList`.

To insert markers, call `PIXMethods.SetMarker`. You provide an `Arg32` color (`Argb32` has a series of static default colors you can choose, as well as `Arg32.FromIndex` to derive a determinstic color from an integer), a format string, and optionally any format variables. 
PIX uses C-style formats (`%d` or `%f` rather than `{0}` as in C#). Up to 16 format variables are supported. 
If your format string ends up being more than 512 bytes (256 characters), it is truncated - this is a limitation of PIX.

To create events, the process is the same, except you use `PIXMethods.BeginEvent` to begin the event and `PIXMethods.EndEvent` to end it. Alternative, you can use `ScopedEvent`, by calling `ScopedEvent.Create` with the same arguments you would call `PIXMethods.BeginEvent` with, and dispose it to end the event (likely using `using` blocks or locals).