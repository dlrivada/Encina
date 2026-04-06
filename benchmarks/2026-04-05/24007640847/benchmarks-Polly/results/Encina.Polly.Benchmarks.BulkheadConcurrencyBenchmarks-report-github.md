```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-SWDXLI : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

UnrollFactor=1  

```
| Method                      | Job        | InvocationCount | IterationCount | LaunchCount | RunStrategy | WarmupCount | ConcurrentRequests | Mean         | Error     | StdDev    | Allocated |
|---------------------------- |----------- |---------------- |--------------- |------------ |------------ |------------ |------------------- |-------------:|----------:|----------:|----------:|
| **ConcurrentAcquireAndRelease** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **10**                 |     **36.70 μs** |  **16.36 μs** |  **0.897 μs** |    **7.3 KB** |
| ConcurrentAcquireAndRelease | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10                 | 17,964.00 μs |        NA |  0.000 μs |    7.3 KB |
| **ConcurrentAcquireAndRelease** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **50**                 |     **95.42 μs** | **626.71 μs** | **34.352 μs** |  **34.39 KB** |
| ConcurrentAcquireAndRelease | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 50                 | 17,891.19 μs |        NA |  0.000 μs |  34.39 KB |
| **ConcurrentAcquireAndRelease** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **100**                |    **154.59 μs** | **468.41 μs** | **25.675 μs** |  **68.57 KB** |
| ConcurrentAcquireAndRelease | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 100                | 18,299.91 μs |        NA |  0.000 μs |  68.38 KB |
