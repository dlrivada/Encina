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
| **ConcurrentAcquireAndRelease** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **10**                 |     **59.96 μs** | **419.13 μs** | **22.974 μs** |   **7.37 KB** |
| ConcurrentAcquireAndRelease | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10                 | 18,350.36 μs |        NA |  0.000 μs |    7.2 KB |
| **ConcurrentAcquireAndRelease** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **50**                 |     **81.30 μs** | **161.97 μs** |  **8.878 μs** |  **34.59 KB** |
| ConcurrentAcquireAndRelease | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 50                 | 18,237.46 μs |        NA |  0.000 μs |  34.39 KB |
| **ConcurrentAcquireAndRelease** | **Job-SWDXLI** | **1**               | **3**              | **Default**     | **Default**     | **2**           | **100**                |    **127.79 μs** | **115.59 μs** |  **6.336 μs** |  **68.38 KB** |
| ConcurrentAcquireAndRelease | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 100                | 18,120.45 μs |        NA |  0.000 μs |   69.4 KB |
