# D3D12 Backend Refactor Summary

## Ziel

`D3D12CommandList` soll kleiner, lesbarer und strukturell naeher am Vulkan Backend werden. Die D3D12 Semantik bleibt erhalten, aber Teilverantwortlichkeiten werden wie im Vulkan Backend in klar benannte State-, Binder- und Utility-Klassen ausgelagert.

## Bisher geaendert

- `D3D12CommandList` delegiert ResourceSet-Dirty-State an `D3D12BoundResourceSetState`.
- Vertex-/Index-Buffer-State, Topology-Cache und Stencil-Reference-Cache wurden in `D3D12InputAssemblerState` verschoben.
- Root-Buffer- und Root-Descriptor-Table-Caches wurden in `D3D12RootBindingCache` verschoben.
- ResourceSet-Binding-Plaene werden ueber `D3D12ResourceSetBindingPlanCache` gecacht, statt bei jedem Flush neu gescannt zu werden.
- Transition- und UAV-Barriers werden ueber `D3D12ResourceBarrierTracker` gesammelt und no-alloc gebatcht emittiert.
- Swapchain-Backbuffer-Caching und Present-Transitions liegen in `D3D12SwapchainBackBufferTracker`.
- Shader-visible Descriptor-Heaps, Descriptor-Cursor, Copy-Scratch-Arrays, DescriptorTable-Handle-Cache und `SetDescriptorHeaps` wurden in `D3D12DescriptorHeapState` verschoben.
- Indirect command signature creation, lazy availability tracking and disposal wurden in `D3D12IndirectCommandSignatures` verschoben.
- D3D12 Performance-Logging, Report-Fenster, Spike-Reporting, GC/Allocation-Erfassung und Debug-Scope-Attribution wurden in `D3D12CommandListPerfTracker` verschoben.
- Dynamic Viewport-/Scissor-State, redundante State-Checks und no-alloc `RSSet*`-Aufrufe wurden in `D3D12ViewportScissorState` verschoben.
- Command-Allocator-Rotation, Frame-Slot-Fence-Werte, Begin-Waits, Submission-Markierung und Allocator-Dispose wurden in `D3D12CommandListFrameState` verschoben.
- GPU-Mipmap-Erzeugung, HLSL-Compute-Shader, D3DCompiler-Interop, Pipeline/Layout/Sampler-Lifetime und Mipmap-State-Restore wurden in `D3D12GpuMipmapGenerator` verschoben.
- ResourceSet-Flushing, Root-Buffer-Binding, DescriptorTable-Binding, DescriptorTable-Resource-Transitions und der DescriptorHeap-State wurden in `D3D12DescriptorSetBinder` gebuendelt. `D3D12CommandList` startet nur noch den Flush zum passenden Draw-/Dispatch-Zeitpunkt.
- Copy/Resolve-Texture-Orchestrierung, temporaere State-Capture-Arrays und Restore-Transitions wurden in `D3D12TextureCopyPlanner` verschoben.
- Pipeline-Wechsel, Compute/Graphics Pipeline-State, RootSignature-Wechsel, ResourceSet-Clears bei RootSignature-Wechsel und Pipeline-nahe IA-State-Updates wurden in `D3D12PipelineStateBinder` verschoben.
- Framebuffer-/RenderTarget-Binding, Swapchain-Backbuffer-RTV-Binding, offscreen RTV/DSV-Binding und Output-Merger-Binding-Cache wurden in `D3D12RenderTargetBinder` verschoben.
- Dynamic-Buffer-Snapshot-Ring, Snapshot-Kapazitaetsberechnung, native Offset-Aufloesung, Bind-Version und Snapshot-Perf-Metriken wurden aus `D3D12DeviceBuffer` in `D3D12DynamicBufferSnapshotState` verschoben.
- D3D12-Staging-Buffer-Read/Write-Paar, persistente Map-Pointer, Dirty-State und Upload/Readback-Synchronisation wurden aus `D3D12DeviceBuffer` in `D3D12StagingBufferState` verschoben.
- `D3D12CommandList` stellt fuer interne D3D12-Helfer nur noch kleine Recording-Hooks bereit, z. B. no-alloc Pipeline-/RootSignature-/Dispatch-Aufrufe und Barrier-Flushes.
- Die neu extrahierten Klassen enthalten XML-Summaries fuer Felder, Properties, Konstruktoren und Methoden.

## Performance-relevante Aenderungen

- ResourceSet-Flushing scannt nur noch die dirty Range statt pauschal alle Slots.
- Binding-Plaene werden pro Pipeline/Layout/Slot gecacht und zusaetzlich mit einem schnellen Pipeline-Slot-Cache bedient.
- Root-Buffer- und Root-Table-Bindings werden vor D3D12-Aufrufen gegen den lokalen Cache verglichen.
- Descriptor-Table-Kopien werden gruppiert ueber wiederverwendete Scratch-Arrays ausgefuehrt.
- Descriptor-Table-GPU-Handles werden am `D3D12ResourceSet` gecacht und bei gleicher Table-Signatur wiederverwendet.
- Barriers werden gesammelt und als Batch aufgezeichnet.
- Dynamic-Buffer-Rebinds fuer ResourceSets und Input-Assembler-State werden gezielt dirty markiert.
- Der GPU-Mipmap-Pfad nutzt jetzt den no-alloc Pipeline-State-Bind statt des managed COM-Wrappers.
- GPU-Mipmap-Subresource-States werden sofort im Texture-State mitgefuehrt, damit der generische ResourceSet-Binder keine doppelten TextureView-Transitions erzeugt.
- GPU-Mipmap-Dispatches flushen wie der normale Dispatch-Pfad UAV- und Transition-Barriers vor dem Dispatch und markieren danach einen UAV-Hazard.
- Der temporaere Mipmap-Compute-ResourceSet-Slot wird nach der Generierung wiederhergestellt und dirty markiert, statt im CommandList-State zu verbleiben.
- Texture2DArray-Mipmaps nutzen jetzt eine eigene `Texture2DArray`/`RWTexture2DArray` Compute-Pipeline und verarbeiten wie Vulkan alle Array-Layer eines Mip-Levels in einem Dispatch.
- ResourceSet-Binding liegt jetzt in einem Binder, der wie Vulkan `FlushNewResourceSets` nur geaenderte Slots abarbeitet, dabei aber D3D12-spezifisch Root-Buffer, DescriptorTables und TextureView-Transitions behandelt.
- Copy/Resolve-State-Capture nutzt jetzt wiederverwendbare, bei Bedarf wachsende Arrays. Dadurch bleibt der uebliche Pfad allokationsfrei, aber Texturen mit mehr als 128 Subresources koennen nicht mehr die alten festen Capture-Arrays ueberlaufen.
- Pipeline-Binding folgt jetzt strukturell dem Vulkan-Muster: gleiche Pipeline wird frueh uebersprungen, RootSignature-/Layoutwechsel werden separat erkannt, und ResourceSets werden nur bei RootSignature-Wechsel verworfen. Die D3D12-Aufrufe bleiben no-alloc.
- RenderTarget-Binding cached die zuletzt an `OMSetRenderTargets` uebergebenen RTV/DSV-Handles und ueberspringt redundante Output-Merger-Binds.
- `D3D12CommandListPerfTracker` berichtet nun `rtBind` und `rtSkip`, damit die Wirkung des RenderTarget-Binding-Caches bei `VELDRID_D3D12_PERF=1` sichtbar ist.
- Die StructuredBuffer-DescriptorTable-Optimierung wurde wieder entfernt, weil sie in der Praxis Flackern verursachte und keine sichtbare Performance-Verbesserung gebracht hat. `StructuredBufferReadOnly` und `StructuredBufferReadWrite` werden in D3D12 wieder stabil ueber Root-SRV/UAV gebunden; Texturen und Sampler bleiben DescriptorTables.
- Der nicht mehr genutzte StructuredBuffer-SRV/UAV-Descriptor-Code wurde aus `D3D12DeviceBuffer`, `D3D12ResourceSet`, `D3D12DescriptorSetBinder` und `D3D12DescriptorHeapState` entfernt.
- `D3D12DescriptorHeapState` validiert gecachte DescriptorTable-GPU-Handles jetzt zusaetzlich mit einer Heap-Cache-ID. Ein `D3D12ResourceSet` kann dadurch nicht mehr versehentlich einen GPU-Handle aus dem shader-visible Heap einer anderen CommandList wiederverwenden.
- `D3D12DescriptorHeapState` kann Descriptor-Copy-Scratch-Arrays jetzt dynamisch vergroessern. Dadurch sind ResourceSets mit mehr als 16 DescriptorTable-Eintraegen korrekt abgedeckt.
- `D3D12CommandListFrameState` kapselt die CommandAllocator-Rotation, verwendet aber wieder die feste 3-Slot-Rotation mit Fence-Wait pro Slot wie der vorherige stabile Code. Der dynamisch wachsende Allocator-Pool wurde zurueckgenommen, weil er als verbleibender Flicker-Kandidat die Synchronisations-Topologie geaendert hatte.
- Staging-Buffer synchronisieren Upload- und Readback-Seite bei bekannten Teilupdates jetzt nur noch ueber eine vereinigte Dirty-Range statt immer ueber die komplette Buffergroesse. `Map`/`Unmap` ohne konkrete Range bleibt absichtlich konservativ und markiert den ganzen Buffer, um Verhalten stabil zu halten.
- `D3D12DeviceBuffer` ist naeher am Vulkan-Design: die Hauptklasse haelt wieder hauptsaechlich Allocation/Handle, waehrend dynamische Update-Strategie und Staging-Synchronisation in eigenen State-Klassen liegen. Die D3D12-spezifische doppelte Staging-Ressource bleibt erhalten, weil Upload- und Readback-Heaps unterschiedliche CPU/GPU-Zugriffssemantik haben.
- Batched-Immediate-Upload-Aufzeichnung in `D3D12GraphicsDevice.RecordBatchedImmediateCommand` haelt nicht mehr den globalen Queue-Lock. Der Queue-Lock bleibt beim Flush/Submit aktiv, aber reine Upload-CommandList-Aufzeichnung blockiert Present/Submit-nahe Arbeit nicht mehr unnoetig.
- `D3D12ResourceBarrierTracker` coalesced pending Resource- und Subresource-Transitions vor dem Emit. Mehrere ungeflushte Transitions fuer dieselbe Resource/Subresource werden zu einer Transition zusammengefaltet; eine Rueckkehr zum Ursprungszustand vor dem Flush entfernt die Barriers komplett.

## Bewusst noch nicht verschoben

- Ein echter D3D12-Filter-Blit-Pfad wie in Vulkan existiert im Backend noch nicht; der Compute-Pfad bleibt deshalb die portable GPU-Mipmap-Route.
- Mipmap-TextureView-/ResourceSet-Caching wurde noch nicht eingefuehrt, weil die Lifetime an Texture-Dispose/Resize sauber geloest werden muss. Ohne diese Loesung waere das ein moeglicher Descriptor-/View-Leak.

## Naechste sinnvolle Schritte

1. Clear-Logik gegen Vulkan RenderPass-Clear-Handling vergleichen; fuer D3D12 ggf. in einen kleinen `D3D12ClearPlanner` verschieben und Clear-Target-Lookups cachen.
2. Mit `VELDRID_D3D12_PERF=1` pruefen, ob `wait=`, `frameWaitMs` oder `immWaitMs` hoch bleiben. Hohe Werte deuten weiter auf CPU/GPU-Synchronisations-Stalls statt reinen Binding-Overhead.
3. Dynamic-Buffer-Snapshot-Strategie weiter messen: hohe `dynPrefixKB`-/`dynRot`-Werte deuten darauf hin, dass Rotationen und Prefix-Kopien mehr kosten als ResourceSet-Binds. Dann waere ein per-frame/per-command-list Upload-Ring wie bei Vulkan-Staging-Pfaden der naechste grosse Hebel.
4. Barrier-Zusammenfassung erweitern: wenn viele Subresource-Transitions fuer dasselbe Texture-Objekt entstehen, koennen volle Ressourcen-Transitions oder zusammenhaengende Subresource-Batches CPU- und Driver-Overhead sparen.
5. Optional Descriptor-/TextureView-Caching fuer den Mipmap-Pfad nur dann einfuehren, wenn Lifetime und Resize/Dispose sauber geklaert sind.
6. Push-Constants, Debug-Marker und allgemeine no-alloc Recording-Hooks pruefen; nur auslagern, wenn es die Lesbarkeit verbessert, ohne Hot-Path-Indirektion zu erhoehen.
7. D3D12-Buffer-Uploads gegen Vulkan-Staging weiter vergleichen: fuer Default-Buffer koennte ein command-list-lokaler Upload-Ring weniger kleine `RentUploadBuffer`-Objekte erzeugen, muss aber sauber an Submission-Lifetime und Fence-Recycling gebunden werden.
8. Root-Constants fuer sehr kleine dynamische UniformBuffer pruefen. Das koennte Matrix-/Material-Uniforms pro Draw guenstiger machen als Root-CBV-Rebinding, muss aber RootSignature-Limits, Shaderregister-Mapping und bestehende Push-Constants sauber beruecksichtigen.
9. Einen `vkCmdUpdateBuffer`-aehnlichen kleinen Update-Pfad fuer D3D12 pruefen. `WriteBufferImmediate` waere theoretisch passend, ist aber in der aktuell verwendeten Vortice-API nicht als stabiler Public-Pfad sichtbar; ohne saubere API-Absicherung wurde dieser Schritt bewusst nicht eingebaut.

## Verifikation

- `dotnet build Veldrith/Veldrith.csproj --no-restore` wurde nach den Descriptor-Heap- und Indirect-Signature-Auslagerungen erfolgreich ausgefuehrt.
- `dotnet build Veldrith/Veldrith.csproj --no-restore` wurde nach der Viewport-/Scissor-Auslagerung erfolgreich ausgefuehrt.
- `dotnet build Veldrith/Veldrith.csproj --no-restore` wurde nach der FrameState-Auslagerung erfolgreich ausgefuehrt.
- `dotnet build Veldrith/Veldrith.csproj --no-restore` wurde nach der GPU-Mipmap-Auslagerung und nach den Barrier-/State-Fixes erfolgreich ausgefuehrt.
- `dotnet build Veldrith/Veldrith.csproj --no-restore` wurde nach der `D3D12DescriptorSetBinder`-Auslagerung erfolgreich ausgefuehrt.
- `dotnet build Bliss.Test/Bliss.Test.csproj --no-restore` wurde nach der `D3D12DescriptorSetBinder`-Auslagerung erfolgreich ausgefuehrt.
- `dotnet build Sparkle.Test/Sparkle.Test.csproj --no-restore` wurde nach der `D3D12DescriptorSetBinder`-Auslagerung erfolgreich ausgefuehrt.
- `dotnet build Veldrith/Veldrith.csproj --no-restore` wurde nach der `D3D12TextureCopyPlanner`-Auslagerung erfolgreich ausgefuehrt.
- `dotnet build Bliss.Test/Bliss.Test.csproj --no-restore` wurde nach der `D3D12TextureCopyPlanner`-Auslagerung erfolgreich ausgefuehrt.
- `dotnet build Sparkle.Test/Sparkle.Test.csproj --no-restore` wurde nach der `D3D12TextureCopyPlanner`-Auslagerung erfolgreich ausgefuehrt.
- `dotnet build Veldrith/Veldrith.csproj --no-restore` wurde nach der `D3D12PipelineStateBinder`-Auslagerung erfolgreich ausgefuehrt.
- `dotnet build Bliss.Test/Bliss.Test.csproj --no-restore` wurde nach der `D3D12PipelineStateBinder`-Auslagerung erfolgreich ausgefuehrt.
- `dotnet build Sparkle.Test/Sparkle.Test.csproj --no-restore` wurde nach der `D3D12PipelineStateBinder`-Auslagerung erfolgreich ausgefuehrt.
- `dotnet build Veldrith/Veldrith.csproj --no-restore` wurde nach der `D3D12RenderTargetBinder`-Auslagerung erfolgreich ausgefuehrt.
- `dotnet build Bliss.Test/Bliss.Test.csproj --no-restore` wurde nach der `D3D12RenderTargetBinder`-Auslagerung erfolgreich ausgefuehrt.
- `dotnet build Sparkle.Test/Sparkle.Test.csproj --no-restore` wurde nach der `D3D12RenderTargetBinder`-Auslagerung erfolgreich ausgefuehrt.
- `dotnet build Veldrith/Veldrith.csproj --no-restore` wurde nach der StructuredBuffer-DescriptorTable-Umstellung erfolgreich ausgefuehrt.
- `dotnet build Bliss.Test/Bliss.Test.csproj --no-restore` wurde nach der StructuredBuffer-DescriptorTable-Umstellung erfolgreich ausgefuehrt.
- `dotnet build Sparkle.Test/Sparkle.Test.csproj --no-restore` wurde nach der StructuredBuffer-DescriptorTable-Umstellung erfolgreich ausgefuehrt.
- `dotnet build Veldrith/Veldrith.csproj --no-restore` wurde nach der CommandAllocator-Pool-Umstellung erfolgreich ausgefuehrt.
- `dotnet build Bliss.Test/Bliss.Test.csproj --no-restore` wurde nach der CommandAllocator-Pool-Umstellung erfolgreich ausgefuehrt.
- `dotnet build Sparkle.Test/Sparkle.Test.csproj --no-restore` wurde nach der CommandAllocator-Pool-Umstellung erfolgreich ausgefuehrt.
- `dotnet build Veldrith/Veldrith.csproj --no-restore` wurde nach dem DescriptorTable-Flicker-Fix erfolgreich ausgefuehrt.
- `dotnet build Bliss.Test/Bliss.Test.csproj --no-restore` wurde nach dem DescriptorTable-Flicker-Fix erfolgreich ausgefuehrt.
- `dotnet build Sparkle.Test/Sparkle.Test.csproj --no-restore` wurde nach dem DescriptorTable-Flicker-Fix erfolgreich ausgefuehrt.
- `dotnet build Veldrith/Veldrith.csproj --no-restore` wurde nach dem StructuredBuffer-DescriptorTable-Rollback erfolgreich ausgefuehrt.
- `dotnet build Bliss.Test/Bliss.Test.csproj --no-restore` wurde nach dem StructuredBuffer-DescriptorTable-Rollback erfolgreich ausgefuehrt.
- `dotnet build Sparkle.Test/Sparkle.Test.csproj --no-restore` wurde nach dem StructuredBuffer-DescriptorTable-Rollback erfolgreich ausgefuehrt.
- `dotnet build Veldrith/Veldrith.csproj --no-restore` wurde nach dem CommandAllocator-Pool-Rollback erfolgreich ausgefuehrt.
- `dotnet build Sparkle.Test/Sparkle.Test.csproj --no-restore` wurde erfolgreich ausgefuehrt.
- `dotnet build Bliss.Test/Bliss.Test.csproj --no-restore` wurde erfolgreich ausgefuehrt.
- `dotnet build Veldrith/Veldrith.csproj --no-restore` wurde nach der `D3D12DeviceBuffer`-Snapshot-/Staging-Auslagerung erfolgreich ausgefuehrt.
- `dotnet build Bliss.Test/Bliss.Test.csproj --no-restore` wurde nach der `D3D12DeviceBuffer`-Snapshot-/Staging-Auslagerung erfolgreich ausgefuehrt.
- `dotnet build Sparkle.Test/Sparkle.Test.csproj --no-restore` wurde nach der `D3D12DeviceBuffer`-Snapshot-/Staging-Auslagerung erfolgreich ausgefuehrt.
- `dotnet build Veldrith/Veldrith.csproj --no-restore` wurde nach der Batched-Immediate-Upload-Lock-Reduzierung erfolgreich ausgefuehrt.
- `dotnet build Veldrith/Veldrith.csproj --no-restore` wurde nach dem Barrier-Coalescing erfolgreich ausgefuehrt.
- `dotnet build Bliss.Test/Bliss.Test.csproj --no-restore` wurde nach dem Barrier-Coalescing erfolgreich ausgefuehrt.
- `dotnet build Sparkle.Test/Sparkle.Test.csproj --no-restore` wurde nach dem Barrier-Coalescing erfolgreich ausgefuehrt.
- `git diff --check` meldet keine Whitespace-Fehler.
- Die verbleibenden Warnungen sind bestehende Vulkan-`CS9191`-Warnungen sowie bestehende nullable Warnungen in `Bliss.Test`; sie wurden nicht durch diesen D3D12-Refactor eingefuehrt.

## Aktueller Stand

- `D3D12CommandList.cs` liegt nach der RenderTarget-Auslagerung bei etwa 1681 Zeilen.
- `D3D12DeviceBuffer.cs` liegt nach der Snapshot-/Staging-Auslagerung bei etwa 504 Zeilen.
- Der groesste verbleibende zusammenhaengende Verantwortungsblock in `D3D12CommandList` ist jetzt Clear-Logik und die allgemeinen no-alloc D3D12-Recording-Hooks.
